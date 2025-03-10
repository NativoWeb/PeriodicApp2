using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using static ControladorEncuesta;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class ControladorEncuestaAprendizaje : MonoBehaviour
{
    public Text TextTimer;  // Referencia al componente Text de la UI
    public float tiempoRestante = 10f;  // Tiempo inicial del temporizador en segundos (10 segundos)
    private bool preguntaFinalizada = false;  // Flag para saber si la pregunta ha sido finalizada

    private bool eventosToggleHabilitados = false;

    [Header("Referencias UI")]
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;
    public TextMeshProUGUI textoPreguntaUI;
    public TextMeshProUGUI resultadoUI; // Texto para mostrar el resultado final

    [Header("Paneles UI")]
    public GameObject panelPreguntas;
    public GameObject panelResultados;

    [System.Serializable]
    public class PreguntaEstiloAprendizaje
    {
        public string textoPregunta;
        public List<int> escalaLikert;
        public string categoria;  // Se asigna al cargar la pregunta
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

    private FirebaseFirestore db;
    private List<PreguntaEstiloAprendizaje> preguntas;
    private List<PreguntaEstiloAprendizaje> preguntasAleatorias;
    private int preguntaActualIndex;
    private PreguntaEstiloAprendizaje preguntaActual;

    // Diccionario para convertir números a textos Likert
    private Dictionary<int, string> likertTextos = new Dictionary<int, string>()
    {
        {1, "Muy en desacuerdo"},
        {2, "En desacuerdo"},
        {3, "Neutral"},
        {4, "De acuerdo"},
        {5, "Muy de acuerdo"}
    };

    // Diccionario para almacenar las respuestas por categoría
    private Dictionary<string, List<int>> respuestasPorCategoria = new Dictionary<string, List<int>>()
    {
        {"Metodologia_Tradicional", new List<int>()},
        {"Aprendizaje_Basado_en_Proyectos", new List<int>()},
        {"Aprendizaje_Basado_en_Problemas", new List<int>()},
        {"Aprendizaje_Cooperativo", new List<int>()},
        {"Gamificacion", new List<int>()}
    };

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // 1. Cargar preguntas desde el JSON
        CargarPreguntaDesdeJson();
        AleatorizarPreguntas();

        // 2. Configurar toggles
        ConfigurarToggleListeners();
        AsignarToggleGroup();
        ActivarInteractividadToggles(true);

        // 3. Mostrar la primera pregunta
        MostrarPreguntaActual();

        eventosToggleHabilitados = true;
    }

    void Update()
    {
        ActualizarTextoTiempo();

        if (!preguntaFinalizada)
        {
            if (tiempoRestante > 0)
            {
                tiempoRestante -= Time.deltaTime;
            }
            else
            {
                preguntaFinalizada = true;
                // Si se agota el tiempo sin respuesta, se puede asignar un valor por defecto (por ejemplo, 3)
                RegistrarRespuesta(3);
                siguientePregunta();
            }
        }
    }

    void ActualizarTextoTiempo()
    {
        TextTimer.text = tiempoRestante.ToString("00") + " Segundos";
    }

    void siguientePregunta()
    {
        Debug.Log($"siguientePregunta() llamado. preguntaActualIndex ANTES: {preguntaActualIndex}");
        preguntaFinalizada = true;
        preguntaActualIndex++;

        Debug.Log($"siguientePregunta() DESPUÉS: {preguntaActualIndex} de {preguntasAleatorias.Count}");

        if (preguntaActualIndex < preguntasAleatorias.Count)
        {
            MostrarPreguntaActual();
            ActivarInteractividadOpciones();
            tiempoRestante = 10f;
            preguntaFinalizada = false;
        }
        else
        {
            Debug.Log("Encuesta Finalizada");
            textoPreguntaUI.text = "¡Encuesta Finalizada!";
            grupoOpcionesUI.enabled = false;
            FinalizarEncuesta();

            //******************Descomentar a futuro con el server ON*******************************************



            //panelPreguntas.SetActive(false);
            //panelResultados.SetActive(true);

            //CanvasGroup cg = panelResultados.GetComponent<CanvasGroup>();
            //if (cg != null)
            //{
            //    Debug.Log("CanvasGroup encontrado en panelResultados");
            //    cg.alpha = 1;
            //    cg.interactable = true;
            //    cg.blocksRaycasts = true;
            //}

            //// Calcular y mostrar el resultado final de la encuesta de estilo de aprendizaje
            CalcularYMostrarResultadoFinal();


            //******************Descomentar a futuro con el server ON*******************************************

        }
    }

    void ActivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = true;
        }
    }

    void DesactivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = false;
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

                // Seleccionar 3 preguntas por cada categoría
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Metodologia_Tradicional, rnd, "Metodologia_Tradicional", 3);
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Basado_en_Proyectos, rnd, "Aprendizaje_Basado_en_Proyectos", 3);
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Basado_en_Problemas, rnd, "Aprendizaje_Basado_en_Problemas", 3);
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Aprendizaje_Cooperativo, rnd, "Aprendizaje_Cooperativo", 3);
                AgregarPreguntasAleatorias(wrapper.preguntasEstilo.Gamificacion, rnd, "Gamificacion", 3);

                Debug.Log($"Se cargaron {preguntas.Count} preguntas desde JSON.");
            }
            else
            {
                Debug.LogError("No se pudo cargar las preguntas desde JSON.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el archivo JSON 'preguntas_estilo_aprendizaje' en Resources.");
        }
    }

    void AgregarPreguntasAleatorias(List<PreguntaEstiloAprendizaje> preguntasCategoria, System.Random rnd, string nombreCategoria, int cantidad)
    {
        if (preguntasCategoria != null && preguntasCategoria.Count >= cantidad)
        {
            var preguntasSeleccionadas = preguntasCategoria.OrderBy(x => rnd.Next()).Take(cantidad).ToList();
            foreach (var pregunta in preguntasSeleccionadas)
            {
                pregunta.categoria = nombreCategoria;
            }
            preguntas.AddRange(preguntasSeleccionadas);
        }
        else if (preguntasCategoria != null)
        {
            foreach (var pregunta in preguntasCategoria)
            {
                pregunta.categoria = nombreCategoria;
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
                // Mostrar opciones Likert
                for (int i = 0; i < opcionesToggleUI.Length; i++)
                {
                    if (i < preguntaActual.escalaLikert.Count)
                    {
                        int valor = preguntaActual.escalaLikert[i];
                        string texto = likertTextos.ContainsKey(valor) ? likertTextos[valor] : "Valor inválido";
                        opcionesToggleUI[i].gameObject.SetActive(true);
                        opcionesToggleUI[i].GetComponentInChildren<TextMeshProUGUI>().text = texto;
                        opcionesToggleUI[i].isOn = false;
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
        tiempoRestante = 10f;
        preguntaFinalizada = false;
    }

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

    private void ConfigurarToggleListeners()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }
        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            int index = i;
            opcionesToggleUI[index].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    DesactivarInteractividadOpciones();
                    // Registrar la respuesta del usuario para la pregunta actual
                    int respuestaSeleccionada = preguntaActual.escalaLikert[index]; // valor Likert
                    RegistrarRespuesta(respuestaSeleccionada);
                    Debug.Log($"Respuesta registrada: {respuestaSeleccionada} para la categoría {preguntaActual.categoria}");
                    siguientePregunta();
                }
            });
        }
    }

    void ActivarInteractividadToggles(bool activar)
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = activar;
        }
    }

    // Método para registrar la respuesta seleccionada en la categoría correspondiente
    void RegistrarRespuesta(int valorRespuesta)
    {
        if (respuestasPorCategoria.ContainsKey(preguntaActual.categoria))
        {
            respuestasPorCategoria[preguntaActual.categoria].Add(valorRespuesta);
        }
        else
        {
            respuestasPorCategoria[preguntaActual.categoria] = new List<int>() { valorRespuesta };
        }
    }

    // Método que se llama al finalizar la encuesta para calcular y mostrar el resultado
    void CalcularYMostrarResultadoFinal()
    {
        // 1. Calcular promedios
        Dictionary<string, float> promedios = new Dictionary<string, float>();
        foreach (var entry in respuestasPorCategoria)
        {
            List<int> respuestas = entry.Value;
            if (respuestas.Count > 0)
            {
                float promedio = (float)respuestas.Average();
                promedios[entry.Key] = promedio;
            }
            else
            {
                promedios[entry.Key] = 0f;
            }
        }

        // 2. Encontrar la categoría con el mayor promedio
        string estiloPredominante = promedios.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

        // 3. Construir texto y mostrarlo en la UI
        string resultadoTexto = "Resultados de la encuesta:\n";
        foreach (var entry in promedios)
        {
            resultadoTexto += $"{entry.Key}: {entry.Value:F2}\n";
        }
        resultadoTexto += $"\nEstilo de aprendizaje predominante: {estiloPredominante}";
        if (resultadoUI != null) resultadoUI.text = resultadoTexto;

        // 4. Guardar en Firestore
        GuardarResultadosEnFirestore(promedios, estiloPredominante);
    }


    private void GuardarResultadosEnFirestore(Dictionary<string, float> promedios, string estiloPredominante)
    {
        // Crear un objeto con la información a guardar
        Dictionary<string, object> data = new Dictionary<string, object>();

        // Convertir promedios a Dictionary<string, object>
        Dictionary<string, object> promediosObj = new Dictionary<string, object>();
        foreach (var entry in promedios)
        {
            // Firestore no admite float directamente, pero sí double
            promediosObj[entry.Key] = (double)entry.Value;
        }

        data["promedios"] = promediosObj;
        data["estiloPredominante"] = estiloPredominante;
        data["timestamp"] = System.DateTime.UtcNow.ToString("o"); // O una marca de tiempo Firestore

        // Referencia a la colección "encuestasEstiloAprendizaje" (puedes llamarla como quieras)
        CollectionReference coleccion = db.Collection("encuestasEstiloAprendizaje");

        // Agregar el documento
        coleccion.AddAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                Debug.Log("Resultados guardados con éxito en Firestore.");
            }
            else
            {
                Debug.LogError("Error al guardar resultados: " + task.Exception);
            }
        });
    }

    public void FinalizarEncuesta()
    {

        // Recuperamos el userId almacenado en el login
        string userId = PlayerPrefs.GetString("userId", "");
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No se puede actualizar Firestore porque userId es nulo.");
            return;
        }

        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.UpdateAsync("EncuestaCompletada", true).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ EncuestaCompletada actualizado correctamente en Firestore.");
                SceneManager.LoadScene("Inicio"); // Redirigir al inicio después de completar la encuesta
            }
            else
            {
                Debug.LogError("❌ Error al actualizar EncuestaCompletada en Firestore.");
            }
        });
    }



}
