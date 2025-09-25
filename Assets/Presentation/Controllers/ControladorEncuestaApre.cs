using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

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

    private FirebaseAuth auth;
    private IUsuarioRepositorio usuarioRepositorio;

    private List<PreguntaEstilo> preguntas;
    private Dictionary<string, int> respuestas = new();
    private int indiceActual = 0;

    private CargarPreguntasEstiloUseCase cargarPreguntasUseCase;
    private CalcularEstiloDominanteUseCase calcularEstiloUseCase;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        usuarioRepositorio = new FirebaseUsuarioRepositorio();
        cargarPreguntasUseCase = new CargarPreguntasEstiloUseCase();
        calcularEstiloUseCase = new CalcularEstiloDominanteUseCase();

        CargarPreguntas();
    }

    private void CargarPreguntas()
    {
        TextAsset json = Resources.Load<TextAsset>("preguntas_estilo_aprendizaje_2");
        if (json != null)
        {
            preguntas = cargarPreguntasUseCase.Ejecutar(json.text);
            InicializarContadores();
            MostrarPregunta();
        }
        else
        {
            textoPregunta.text = "Error al cargar preguntas.";
            Debug.LogError("❌ No se encontró el archivo JSON.");
        }
    }

    private void InicializarContadores()
    {
        foreach (var p in preguntas)
        {
            if (!respuestas.ContainsKey(p.Categoria))
                respuestas[p.Categoria] = 0;
        }
    }

    private void MostrarPregunta()
    {
        if (indiceActual < preguntas.Count)
        {
            textoPregunta.text = preguntas[indiceActual].Texto;
            barraProgreso.value = (float)indiceActual / preguntas.Count;
            if (contenedorOpciones != null && !contenedorOpciones.activeSelf)
                contenedorOpciones.SetActive(true);
        }
        else
        {
            // 1. Ocultar los botones de Sí/No
            if (contenedorOpciones != null)
                contenedorOpciones.SetActive(false);

            // 2. Ocultar el texto de la pregunta original
            if (textoAfirmacion != null)
                textoAfirmacion.gameObject.SetActive(false); // <--- AÑADE ESTA LÍNEA

            // 3. Calcular y mostrar el resultado
            string estilo = calcularEstiloUseCase.Ejecutar(respuestas);
            StartCoroutine(MostrarYContinuar(estilo));
        }
    }

    public void Responder(bool afirmativo)
    {
        if (afirmativo)
        {
            string categoria = preguntas[indiceActual].Categoria;
            respuestas[categoria]++;
        }
        indiceActual++;
        MostrarPregunta();
    }

    private IEnumerator MostrarYContinuar(string estilo)
    {
        textoPregunta.text = $"🧠 Tu estilo dominante es:\n<b>{estilo.Replace("_", " ")}</b>";
        barraProgreso.value = 1f;
        yield return new WaitForSeconds(3f);
        FinalizarEncuesta(estilo);
    }

    private async void FinalizarEncuesta(string estilo)
    {
        PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", 1);
        PlayerPrefs.Save();

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            SceneManager.LoadScene("SeleccionarEncuesta");
            return;
        }

        bool aprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool conocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await usuarioRepositorio.ActualizarEstadoEncuestaAprendizajeAsync(user.UserId, true);
            var (estadoAprendizaje, estadoConocimiento) = await usuarioRepositorio.ObtenerEstadosEncuestasAsync(user.UserId);
            CargarEscenaSegunEstados(estadoAprendizaje, estadoConocimiento);
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
