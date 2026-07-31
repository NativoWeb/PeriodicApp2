using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Presentation;

public class ControladorEncuestaApre : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textoPregunta;
    public TextMeshProUGUI textoAfirmacion;
    public Slider barraProgreso;

    [Header("Opciones")]
    [Tooltip("Contenedor que agrupa los botones de respuesta de la encuesta.")]
    public GameObject contenedorOpciones;

    [Header("Contenedor")]
    public ContenedorPreguntas contenedor;

    private FirebaseAuth _auth;
    private IUsuarioRepositorio _usuarioRepositorio;

    private List<PreguntaEstilo> _preguntas;
    private Dictionary<string, int> _respuestas = new();
    private int _indiceActual = 0;

    private CargarPreguntasEstiloUseCase _cargarPreguntasUseCase;
    private CalcularEstiloDominanteUseCase _calcularEstiloUseCase;

    private static readonly Dictionary<string, string> MapeoEstiloEncuestaALearningStyle = new()
    {
        { "Gamificacion", "Kinest\u00e9sico" },
        { "Metodologia_Tradicional", "Verbal" },
        { "Aprendizaje_Basado_en_Proyectos", "Kinest\u00e9sico" },
        { "Aprendizaje_Basado_en_Problemas", "Visual" },
        { "Aprendizaje_Cooperativo", "Verbal" }
    };

    void Start()
    {
        _auth = FirebaseAuth.DefaultInstance;
        _usuarioRepositorio = new FirebaseUsuarioRepositorio();
        _cargarPreguntasUseCase = new CargarPreguntasEstiloUseCase(ServiceLocator.Json);
        _calcularEstiloUseCase = new CalcularEstiloDominanteUseCase();
        CargarPreguntas();
    }

    private void CargarPreguntas()
    {
        TextAsset json = Resources.Load<TextAsset>("preguntas_estilo_aprendizaje_2");
        if (json != null)
        {
            _preguntas = _cargarPreguntasUseCase.Ejecutar(json.text);
            InicializarContadores();
            MostrarPregunta();
        }
        else
        {
            textoPregunta.text = "Error al cargar preguntas.";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("No se encontro el archivo JSON de preguntas de estilo de aprendizaje.");
#endif
        }
    }

    private void InicializarContadores()
    {
        foreach (var p in _preguntas)
        {
            if (!_respuestas.ContainsKey(p.Categoria))
                _respuestas[p.Categoria] = 0;
        }
    }

    private void MostrarPregunta()
    {
        if (_indiceActual < _preguntas.Count)
        {
            textoPregunta.text = _preguntas[_indiceActual].Texto;
            barraProgreso.value = (float)_indiceActual / _preguntas.Count;
            if (contenedorOpciones != null && !contenedorOpciones.activeSelf)
                contenedorOpciones.SetActive(true);
        }
        else
        {
            if (contenedorOpciones != null)
                contenedorOpciones.SetActive(false);
            if (textoAfirmacion != null)
                textoAfirmacion.gameObject.SetActive(false);

            var ranking = _calcularEstiloUseCase.EjecutarRanking(_respuestas);
            string estiloDominante = ranking.Count > 0 ? ranking[0].estilo : "Mixto";

            PersistirResultados(estiloDominante, ranking);

            StartCoroutine(MostrarYContinuar(estiloDominante, ranking));
        }
    }

    public void Responder(bool afirmativo)
    {
        if (afirmativo)
        {
            string categoria = _preguntas[_indiceActual].Categoria;
            _respuestas[categoria]++;
        }
        _indiceActual++;
        MostrarPregunta();
    }

    private void PersistirResultados(string estiloDominante, List<(string estilo, int puntaje)> ranking)
    {
        PlayerPrefs.SetString("EstiloAprendizajeDominante", estiloDominante);

        var rankingSerializable = new List<Dictionary<string, object>>();
        foreach (var (estilo, puntaje) in ranking)
        {
            rankingSerializable.Add(new Dictionary<string, object>
            {
                { "estilo", estilo },
                { "puntaje", puntaje }
            });
        }
        string rankingJson = JsonConvert.SerializeObject(rankingSerializable);
        PlayerPrefs.SetString("RankingEstilosAprendizaje", rankingJson);

        string learningStyle = MapearALearningStyle(estiloDominante, ranking);
        PlayerPrefs.SetString("LearningStyleMapped", learningStyle);

        PlayerPrefs.Save();
    }

    private string MapearALearningStyle(string estiloDominante, List<(string estilo, int puntaje)> ranking)
    {
        if (ranking.Count >= 2)
        {
            int diferencia = ranking[0].puntaje - ranking[1].puntaje;
            if (diferencia < 2)
                return "Mixto";
        }

        if (MapeoEstiloEncuestaALearningStyle.TryGetValue(estiloDominante, out string mapped))
            return mapped;

        return "Mixto";
    }

    private IEnumerator MostrarYContinuar(string estiloDominante, List<(string estilo, int puntaje)> ranking)
    {
        string texto = $"Tu estilo dominante es:\n<b>{estiloDominante.Replace("_", " ")}</b>\n";
        int mostrar = ranking.Count < 3 ? ranking.Count : 3;
        for (int i = 0; i < mostrar; i++)
        {
            string nombre = ranking[i].estilo.Replace("_", " ");
            texto += $"\n{i + 1}. {nombre}: {ranking[i].puntaje} pts";
        }

        textoPregunta.text = texto;
        barraProgreso.value = 1f;
        yield return new WaitForSeconds(4f);
        FinalizarEncuesta(estiloDominante);
    }

    private async void FinalizarEncuesta(string estilo)
    {
        PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", 1);
        PlayerPrefs.Save();

        var user = _auth.CurrentUser;
        if (user == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("Usuario no autenticado al finalizar encuesta de aprendizaje.");
#endif
            SceneManager.LoadScene("SeleccionarEncuesta");
            return;
        }

        bool aprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool conocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            try
            {
                string rankingJson = PlayerPrefs.GetString("RankingEstilosAprendizaje", "[]");
                await Task.WhenAll(
                    _usuarioRepositorio.ActualizarEstadoEncuestaAprendizajeAsync(user.UserId, true),
                    _usuarioRepositorio.GuardarEstiloAprendizajeAsync(user.UserId, estilo, rankingJson)
                );
                var (estadoAprendizaje, estadoConocimiento) = await _usuarioRepositorio.ObtenerEstadosEncuestasAsync(user.UserId);
                CargarEscenaSegunEstados(estadoAprendizaje, estadoConocimiento);
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"Error al guardar estilo en Firebase: {ex.Message}");
#endif
                CargarEscenaSegunEstados(aprendizaje, conocimiento);
            }
        }
        else
        {
            CargarEscenaSegunEstados(aprendizaje, conocimiento);
        }
    }

    private void CargarEscenaSegunEstados(bool aprendizaje, bool conocimiento)
    {
        if (aprendizaje && conocimiento)
            SceneManager.LoadScene("Inicio");
        else
            SceneManager.LoadScene("SeleccionarEncuesta");
    }
}
