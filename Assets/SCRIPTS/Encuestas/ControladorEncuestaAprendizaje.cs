using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static ControladorEncuesta;
using Firebase.Firestore;
using Firebase;
using Firebase.Extensions; // 👈 Necesario para ContinueWithOnMainThread
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class ControladorEncuestaAprendizaje : MonoBehaviour
{

    public Text TextTimer;  // Referencia al componente Text de la UI
    public float tiempoRestante = 10f;  // Tiempo inicial del temporizador en segundos (10 segundos)
    private bool preguntaFinalizada = false;  // Flag para saber si la pregunta ha sido finalizada (cuando se pasa a la siguiente pregunta
    public string respuestaUsuario;
    private bool eventosToggleHabilitados = false;
    private List<string> opcionesAleatorias;

    public BarraProgreso barraProgreso;

    private FirebaseFirestore firestore;


    private FirebaseAuth auth;


    [System.Serializable]
    public class PreguntaEstiloAprendizaje
    {
        public string textoPregunta;
        public List<int> escalaLikert;
        public string categoria;
        public string respuestaUsuario;
    }

    [System.Serializable]
    public class CategoriaPreguntas
    {
        public List<PreguntaEstiloAprendizaje> Metodologia_Tradicional;
        public List<PreguntaEstiloAprendizaje> Aprendizaje_Basado_en_Proyectos;
        public List<PreguntaEstiloAprendizaje> Aprendizaje_Basado_en_Problemas;
        public List<PreguntaEstiloAprendizaje> Aprendizaje_Cooperativo;
        public List<PreguntaEstiloAprendizaje> Gamificacion;
    }

    [System.Serializable]
    public class PreguntasEstiloWrapper
    {
        public CategoriaPreguntas preguntasEstilo;
    }

    private List<PreguntaEstiloAprendizaje> preguntas;
    private List<PreguntaEstiloAprendizaje> preguntasAleatorias;
    private int preguntaActualIndex;
    private PreguntaEstiloAprendizaje preguntaActual;

    public TextMeshProUGUI textoPreguntaUI;

    // Diccionario para convertir números a textos Likert
    private Dictionary<int, string> likertTextos = new Dictionary<int, string>()
    {
        {1, "Muy en desacuerdo"},
        {2, "En desacuerdo"},
        {3, "Neutral"},
        {4, "De acuerdo"},
        {5, "Muy de acuerdo"}
    };


    // Diccionario para almacenar puntuaciones por categoría
    private Dictionary<string, int> puntuacionCategorias = new Dictionary<string, int>()
    {
        {"Metodologia_Tradicional", 0},
        {"Aprendizaje_Basado_en_Proyectos", 0},
        {"Aprendizaje_Basado_en_Problemas", 0},
        {"Aprendizaje_Cooperativo", 0},
        {"Gamificacion", 0}
    };

    // Variable para guardar la categoría de la pregunta actual
    private string categoriaActual;

    void Start()
    {
        // 1. Cargar preguntas
        CargarPreguntaDesdeJson();
        AleatorizarPreguntas();

        // 2. Configurar toggles
        ConfigurarToggleListeners(); // Asigna y resetea los listeners
        AsignarToggleGroup();        // Asegúrate de que cada toggle use 'grupoOpciones'
        ActivarInteractividadToggles(true); // Asegúrate de que estén interactuables

        // 3. Mostrar la primera pregunta
        MostrarPreguntaActual();


        eventosToggleHabilitados = true;

        auth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
        // Recuperamos el userId almacenado en el login
        string userId = PlayerPrefs.GetString("userId", "");

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("⚠️ No se encontró userId en PlayerPrefs.");
        }
        else
        {
            Debug.Log($"📌 UserId encontrado: {userId}");
        }

    }

    void Update()
    {
        ActualizarTextoTiempo();

        // Solo actualizar el temporizador si la pregunta no ha sido finalizada
        if (!preguntaFinalizada)
        {

            if (tiempoRestante > 0)
            {
                tiempoRestante -= Time.deltaTime; // Reduce el tiempo
            }
            else  // Verifica que la pregunta a n no se ha respondido
            {
                preguntaFinalizada = true; // Evita que el c digo se ejecute varias veces en un solo frame
            }
        }

    }

    void ActualizarTextoTiempo()
    {
        TextTimer.text = tiempoRestante.ToString("00") + " Segundos";
    }

    void siguientePregunta()
    {
        Debug.Log($"siguientePregunta() llamado. preguntaActualIndex ANTES de incrementar: {preguntaActualIndex}");
        preguntaFinalizada = true;
        preguntaActualIndex++; // Incrementamos el  ndice para la siguiente pregunta

        Debug.Log($"siguientePregunta() preguntaActualIndex DESPUÉS de incrementar: {preguntaActualIndex}, preguntasAleatorias.Count: {preguntasAleatorias.Count}"); // DEBUG LOG


        if (preguntaActualIndex < preguntasAleatorias.Count)
        {
            //preguntaActualIndex++;
            MostrarPreguntaActual();  // Mostrar la siguiente pregunta
            ActivarInteractividadOpciones();
            tiempoRestante = 10f;  // Reiniciar el temporizador para la nueva pregunta
            Debug.Log($"siguientePregunta(): Temporizador reiniciado a {tiempoRestante} segundos.");
            preguntaFinalizada = false;  // Permitir que el temporizador funcione de nuevo
        }
        else
        {
            Debug.Log("siguientePregunta(): ¡Encuesta Finalizada! (No hay más preguntas).");
            Debug.Log("Encuesta Finalizada");
            textoPreguntaUI.text = " Encuesta Finalizada!";
            grupoOpcionesUI.enabled = false;
            FinalizarEncuesta();
        }
        Debug.Log("siguientePregunta() finalizado.");
    }

    void ActivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = true; // Reactiva la interactividad de cada Toggle de opci n
        }
    }

    void DesactivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = false; // Desactiva la interactividad de cada Toggle de opci n
        }
    }

    void CargarPreguntaDesdeJson()
    {
        TextAsset archivoJSON = Resources.Load<TextAsset>("preguntas_estilo_aprendizaje");

        if (archivoJSON != null)
        {
            string jsonString = archivoJSON.text;
            PreguntasEstiloWrapper wrapper = JsonUtility.FromJson<PreguntasEstiloWrapper>(jsonString);
            if (wrapper != null && wrapper.preguntasEstilo != null)
            {
                preguntas = new List<PreguntaEstiloAprendizaje>();
                System.Random rnd = new System.Random();

                // Cargar 2 preguntas de cada categoria
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Metodologia_Tradicional, rnd, "Metodologia_Tradicional");
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Basado_en_Proyectos, rnd, "Aprendizaje_Basado_en_Proyectos");
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Basado_en_Problemas, rnd, "Aprendizaje_Basado_en_Problemas");
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Cooperativo, rnd, "Aprendizaje_Cooperativo");
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Gamificacion, rnd, "Gamificacion");

                Debug.Log($"Se cargaron {preguntas.Count} preguntas desde JSON.");
            }
            else
            {
                Debug.LogError("No se pudo cargar las preguntas desde JSON.");
            }
        }
        else
        {
            Debug.LogError("No se pudo encontrar el archivo JSON 'preguntas_estilo_aprendizaje' en Resources.");
        }
    }

    void AgregarPreguntasAleatorias(List<PreguntaEstiloAprendizaje> preguntasCategoria, System.Random rnd, string nombreCategoria)
    {
        if (preguntasCategoria != null && preguntasCategoria.Count >= 2)
        {
            var preguntasSeleccionadas = preguntasCategoria
            .OrderBy(x => rnd.Next())
            .Take(2)
            .ToList();

            // Asignar categoría a cada pregunta seleccionada
            foreach (var pregunta in preguntasSeleccionadas)
            {
                pregunta.categoria = nombreCategoria; // Requiere agregar este campo en tu clase
            }

            preguntas.AddRange(preguntasSeleccionadas);
        }
        else if (preguntasCategoria != null)
        {
            foreach (var pregunta in preguntasCategoria)
            {
                pregunta.categoria = nombreCategoria; // Asignar categoría incluso si hay menos de 2
            }
            preguntas.AddRange(preguntasCategoria);
        }
    }

    void AleatorizarPreguntas()
    {
        if (preguntas != null && preguntas.Count > 0)
        {
            preguntasAleatorias = new List<PreguntaEstiloAprendizaje>(preguntas);
            for (int i = 0; i < preguntasAleatorias.Count - 1; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, preguntasAleatorias.Count);
                PreguntaEstiloAprendizaje temp = preguntasAleatorias[randomIndex];
                preguntasAleatorias[randomIndex] = preguntasAleatorias[i];
                preguntasAleatorias[i] = temp;
            }
            preguntaActualIndex = 0;
        }
        else
        {
            Debug.LogError("No hay preguntas para aleatorizar.");
        }
    }

    void MostrarPreguntaActual()
    {
        grupoOpcionesUI.SetAllTogglesOff();
        if (preguntaActualIndex < preguntasAleatorias.Count)
        {
            preguntaActual = preguntasAleatorias[preguntaActualIndex];

            if (preguntaActual != null && preguntaActual.escalaLikert != null)
            {
                textoPreguntaUI.text = preguntaActual.textoPregunta;

                // Mostrar opciones
                for (int i = 0; i < opcionesToggleUI.Length; i++)
                {
                    if (i < preguntaActual.escalaLikert.Count)
                    {
                        // Convertir número a texto usando el diccionario
                        int valor = preguntaActual.escalaLikert[i];
                        string texto = likertTextos.ContainsKey(valor) ? likertTextos[valor] : "Valor inválido";

                        opcionesToggleUI[i].gameObject.SetActive(true);
                        opcionesToggleUI[i].GetComponentInChildren<TextMeshProUGUI>().text = texto;
                        preguntaActual.escalaLikert[i].ToString();
                        opcionesToggleUI[i].isOn = false;  // Resetea estado
                    }
                    else
                    {
                        opcionesToggleUI[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError("preguntaActual o escalaLikert es null");
            }
        }
        else
        {
            Debug.Log("No hay más preguntas. Encuesta terminada.");
        }


        // 7. Reiniciar temporizador
        tiempoRestante = 10f;
        preguntaFinalizada = false;
    }

    // === NUEVO MÉTODO: Asignar ToggleGroup a cada toggle ===
    void AsignarToggleGroup()
    {
        if (grupoOpcionesUI == null)
        {
            Debug.LogWarning("No se asignó un ToggleGroup en el Inspector.");
            return;
        }

        foreach (Toggle t in opcionesToggleUI)
        {
            t.group = grupoOpcionesUI;
        }
    }

    // === NUEVO MÉTODO: Configurar listeners de cada toggle ===

    private void ConfigurarToggleListeners()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.onValueChanged.RemoveAllListeners(); // Limpiar listeners previos
        }

        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            int index = i;
            opcionesToggleUI[index].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    DesactivarInteractividadOpciones();
                    barraProgreso.InicializarBarra(preguntas.Count);
                    siguientePregunta();
                }
            });
        }
    }

    // === NUEVO MÉTODO: Activar/Desactivar la interactividad de los toggles ===
    void ActivarInteractividadToggles(bool activar)
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = activar;
        }
    }

    // Método que se llama cuando un toggle cambia su estado
    public void ToggleValueChanged(Toggle toggle)
    {
        // Si el toggle se enciende (isOn = true), detectamos cuál fue y registramos
        if (toggle.isOn)
        {
            int indiceSeleccionado = -1; // Inicializamos con -1 (no encontrado)
            for (int i = 0; i < opcionesToggleUI.Length; i++)
            {
                if (opcionesToggleUI[i] == toggle)
                {
                    indiceSeleccionado = i;
                    break; // Salimos del bucle una vez encontrado
                }
            }

            // Solo para debug, mostrar el valor en la consola
            Debug.Log($"Respuesta seleccionada: {preguntaActual.escalaLikert[indiceSeleccionado]} (índice {indiceSeleccionado})");

            // Aquí podrías avanzar a la siguiente pregunta o lo que requieras
            // siguientePregunta() ...
        }
    }

    public void FinalizarEncuesta()
    {
        if (TieneConexion())
        {
            // Si hay conexión, actualiza en Firestore
            string userId = PlayerPrefs.GetString("userId", "");
            Debug.LogError("UserId: " + userId);

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("❌ No se puede actualizar Firestore porque userId es nulo.");
                return;
            }

            DocumentReference docRef = firestore.Collection("users").Document(userId);

            docRef.UpdateAsync("EncuestaCompletada", true).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("✅ EncuestaCompletada actualizado correctamente en Firestore.");
                }
                else
                {
                    Debug.LogError("❌ Error al actualizar EncuestaCompletada en Firestore.");
                }

                SceneManager.LoadScene("Categorías");
            });
        }
        else
        {
            // Si no hay conexión, guarda en PlayerPrefs y cambia de escena
            Debug.LogWarning("⚠ No hay conexión WiFi, guardando en PlayerPrefs.");
            PlayerPrefs.SetInt("EncuestaCompletada", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Categorías");
        }
    }

    private bool TieneConexion()
    {
        try
        {
            using (var client = new System.Net.WebClient())
            using (client.OpenRead("http://www.google.com"))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }



    [Header("Referencias UI")]
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;
}