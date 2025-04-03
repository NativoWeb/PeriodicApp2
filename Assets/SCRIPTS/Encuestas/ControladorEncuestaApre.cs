using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ControladorEncuestaAprendizaje;

public class ControladorEncuestaApre : MonoBehaviour
{
    public TextMeshProUGUI textoPregunta;
    public Slider barraProgreso;
    public ContenedorPreguntas contenedor;
    private int indexActual = 0;

    private List<PreguntaEstilo> preguntasOrdenadas = new List<PreguntaEstilo>();
    private Dictionary<string, int> respuestasPositivas = new Dictionary<string, int>();

    // instanciar firebase
    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    // Internet
    private bool hayInternet = false;

    [System.Serializable]
    public class Pregunta
    {
        public string textoAfirmacion;
    }

    [System.Serializable]
    public class PreguntasPorEstilo
    {
        public List<Pregunta> Gamificacion;
        public List<Pregunta> Metodologia_Tradicional;
        public List<Pregunta> Aprendizaje_Basado_en_Proyectos;
        public List<Pregunta> Aprendizaje_Basado_en_Problemas;
        public List<Pregunta> Aprendizaje_Cooperativo;
    }

    [System.Serializable]
    public class PreguntaEstilo
    {
        public string texto;
        public string categoria;
    }


    [System.Serializable]
    public class ContenedorPreguntas
    {
        public PreguntasPorEstilo preguntasEstiloBinario;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CargarPreguntaDesdeJson();
        auth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null) // Evitar errores si el usuario no está autenticado
        {
            Debug.LogError("❌ No hay un usuario autenticado.");

        }
    }

    void CargarPreguntaDesdeJson()
    {
        TextAsset archivoJson = Resources.Load<TextAsset>("preguntas_estilo_aprendizaje_2"); // sin extensión
        if (archivoJson != null)
        {
            contenedor = JsonUtility.FromJson<ContenedorPreguntas>(archivoJson.text);
            Debug.Log("Preguntas cargadas correctamente ✅");

            // Llenar el diccionario de respuestas
            respuestasPositivas["Gamificacion"] = 0;
            respuestasPositivas["Metodologia_Tradicional"] = 0;
            respuestasPositivas["Aprendizaje_Basado_en_Proyectos"] = 0;
            respuestasPositivas["Aprendizaje_Basado_en_Problemas"] = 0;
            respuestasPositivas["Aprendizaje_Cooperativo"] = 0;

            // Unir todas las preguntas en una sola lista
            foreach (var p in contenedor.preguntasEstiloBinario.Gamificacion)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Gamificacion" });

            foreach (var p in contenedor.preguntasEstiloBinario.Metodologia_Tradicional)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Metodologia_Tradicional" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Basado_en_Proyectos)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Basado_en_Proyectos" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Basado_en_Problemas)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Basado_en_Problemas" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Cooperativo)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Cooperativo" });

            MostrarPregunta();
        }
        else
        {
            Debug.LogError("❌ No se encontró el archivo JSON");
            textoPregunta.text = "Error al cargar preguntas.";
        }
    }

    void MostrarPregunta()
    {
        if (indexActual < preguntasOrdenadas.Count)
        {
            textoPregunta.text = preguntasOrdenadas[indexActual].texto;
            barraProgreso.value = (float)indexActual / preguntasOrdenadas.Count;
        }
        else
        {
            MostrarResultadoFinal();
        }
    }

    public void Responder(bool respuesta)
    {
        string categoria = preguntasOrdenadas[indexActual].categoria;

        if (respuesta)
        {
            respuestasPositivas[categoria]++;
        }

        indexActual++;
        MostrarPregunta();
    }

    public void MostrarResultadoFinal()
    {
        string estiloDominante = "";
        int mayorValor = -1;

        foreach (var estilo in respuestasPositivas)
        {
            if (estilo.Value > mayorValor)
            {
                mayorValor = estilo.Value;
                estiloDominante = estilo.Key;
            }
        }


        StartCoroutine(MostrarYEsperar(estiloDominante));
        barraProgreso.value = 1f;
        
    }
    private IEnumerator MostrarYEsperar(string estiloDominante)
    {
        // Mostrar el resultado
        textoPregunta.text = $"🧠 Tu estilo de aprendizaje dominante es:\n<b>{estiloDominante.Replace("_", " ")}</b>";
        barraProgreso.value = 1f;

        // Esperar 3 segundos
        yield return new WaitForSeconds(3f);

        // Continuar con la función FinalizarEncuesta
        FinalizarEncuesta();
    }

    public void FinalizarEncuesta()
    {
        PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", 1);
        PlayerPrefs.Save();
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        bool estadoencuestaaprendizaje = false;
        bool estadoencuestaconocimiento = false;

        if (hayInternet)
        {
            firestore = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            FirebaseUser currentUser = auth.CurrentUser;

            if (currentUser == null)
            {
                Debug.LogError("❌ No hay un usuario autenticado.");
            }

            string userId = currentUser.UserId;

            estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            ActualizarEstadoEncuestaAprendizaje(userId, estadoencuestaaprendizaje);

            DocumentReference docRef = firestore.Collection("users").Document(userId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("❌ Error al obtener los datos del usuario.");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    Debug.LogError("❌ No se encontraron datos para este usuario.");
                    return;
                }

                // Obtener valores de Firestore
                estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;
                estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;

                // Verificar si se deben cargar las categorías
                if (estadoencuestaaprendizaje && estadoencuestaconocimiento)
                {
                    SceneManager.LoadScene("Categorías");
                }
                else
                {
                    SceneManager.LoadScene("SeleccionarEncuesta");
                }
            });
        }
        else
        {
            estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;


            // Validar el estado de ambas encuestas para pasar a scena 
            if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
            {
                SceneManager.LoadScene("Categorías");
            }
            else
            {
                SceneManager.LoadScene("SeleccionarEncuesta");
            }
        }
    }

    private async void ActualizarEstadoEncuestaAprendizaje(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = firestore.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaAprendizaje", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Aprendizaje... {userId}: {estadoencuesta} desde EncuestaAprendizaje");
    }
}
