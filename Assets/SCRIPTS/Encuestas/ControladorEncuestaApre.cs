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
    public Slider barraProgreso;

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
        }
        else
        {
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

        await usuarioRepositorio.ActualizarEstadoEncuestaAprendizajeAsync(user.UserId, true);

        bool aprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool conocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
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
            SceneManager.LoadScene("Categorías");
        else
            SceneManager.LoadScene("SeleccionarEncuesta");
    }
}
