using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Linq;

public class GestorPreguntas : MonoBehaviour
{
    public TextMeshProUGUI txtPregunta;
    public Toggle[] opciones;
    public Text txtTiempo;
    public Text txtRacha;
    public TextMeshProUGUI txtResultado;
    public GameObject PanelContinuar;

    public Slider barraProgresoSlider;
    private List<Pregunta> preguntasFiltradas;
    private int preguntaActual = 0;
    private int rachaActual;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string elementoSeleccionado;
    private string simboloSeleccionado;
    private string elementoCompleto;
    private string categoriaSeleccionada;
    private List<string> temasFallados;

    [Header("Referencias para Animación de Misión")]
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;
    public ParticleSystem particulasMisionCompletada;



    [System.Serializable]
    public class Pregunta
    {
        public string textoPregunta;
        public List<string> opcionesRespuesta;
        public int indiceRespuestaCorrecta;
        public string tema;

    }

    void Start()
    {
        // FIREBASE
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        temasFallados = new List<string>();

        // Configurar el slider de progreso
        barraProgresoSlider.minValue = 0;
        barraProgresoSlider.value = preguntaActual;

        // Recuperar datos de PlayerPrefs
        elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "").Trim() + " ";
        simboloSeleccionado = "(" + PlayerPrefs.GetString("SimboloElemento", "").Trim() + ")";
        elementoCompleto = elementoSeleccionado + simboloSeleccionado;
        categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "").Trim();
        rachaActual = PlayerPrefs.GetInt("RachaActual");

        // Cargar progreso guardado para el elemento
        preguntaActual = PlayerPrefs.GetInt($"Progreso_{elementoCompleto}", 0);

        CargarPreguntasDesdeJSON(categoriaSeleccionada, elementoCompleto);
        if (preguntasFiltradas.Count > 0)
        {
            MostrarPregunta();
            StartCoroutine(Temporizador());
        }

        SistemaXP.CrearInstancia();
    }


    void CargarPreguntasDesdeJSON(string categoriaSeleccionada, string elementoSeleccionado)
    {

        string nombreArchivo = categoriaSeleccionada.ToLower().Replace(' ', '_');

        string rutaCompleta = "Preguntas_misiones_json/" + nombreArchivo;

        TextAsset jsonFile = Resources.Load<TextAsset>(rutaCompleta); // Cargar JSON desde Resources
        if (jsonFile == null)
        {
            Debug.LogError($"No se encontró el archivo JSON en la ruta: '{rutaCompleta}'. " +
                       $"Verifica que el nombre del archivo y la categoría seleccionada ('{categoriaSeleccionada}') coincidan.");
            return;
        }

        var json = JSON.Parse(jsonFile.text);

        if (json == null || !json.HasKey("grupo") || !json.HasKey("elementos") || !json["elementos"].IsArray)
        {
            Debug.LogError("❌ El JSON no tiene la estructura esperada.");
            return;
        }

        Debug.Log("JSON '{nombreArchivo}.json' cargado correctamente.");

        // 💡 Verifica si preguntasFiltradas ha sido inicializada
        if (preguntasFiltradas == null)
        {
            preguntasFiltradas = new List<Pregunta>();
        }
        preguntasFiltradas.Clear();

        bool categoriaEncontrada = json["grupo"].Value == categoriaSeleccionada;
        bool elementoEncontrado = false;

        foreach (JSONNode elementoJson in json["elementos"].AsArray)
        {
            if (elementoJson.HasKey("elemento") && elementoJson["elemento"].Value == elementoSeleccionado)
            {

                elementoEncontrado = true;
                if (elementoJson.HasKey("preguntas") && elementoJson["preguntas"].IsArray)
                {
                    foreach (JSONNode preguntaJson in elementoJson["preguntas"].AsArray)
                    {
                        if (!preguntaJson.HasKey("opcionesRespuesta") || !preguntaJson["opcionesRespuesta"].IsArray)
                        {
                            Debug.LogError("⚠ La pregunta no tiene opciones de respuesta.");
                            continue;
                        }

                        List<string> opciones = new List<string>();
                        foreach (JSONNode opcion in preguntaJson["opcionesRespuesta"].AsArray)
                        {
                            opciones.Add(opcion.Value);
                        }

                        Pregunta pregunta = new Pregunta
                        {
                            textoPregunta = preguntaJson["textoPregunta"].Value,
                            opcionesRespuesta = opciones,
                            indiceRespuestaCorrecta = preguntaJson["indiceRespuestaCorrecta"].AsInt,
                            tema = preguntaJson.HasKey("tema") ? preguntaJson["tema"].Value : "General"
                        };
                        preguntasFiltradas.Add(pregunta);
                    }
                }
                else
                {
                    Debug.LogError("⚠ El elemento no tiene preguntas registradas.");
                }
                break;
            }
        }


        if (!elementoEncontrado)
        {
            Debug.LogError("⚠ No se encontró el elemento seleccionado en la categoría.");
            return;
        }

        if (preguntasFiltradas.Count == 0)
        {
            Debug.LogError("⚠ No se encontraron preguntas para este elemento.");
            return;
        }
        //valor maximo del slider de progreso
        barraProgresoSlider.maxValue = preguntasFiltradas.Count;
    }

    public void MostrarPregunta()
    {
        if (preguntasFiltradas == null || preguntasFiltradas.Count == 0)
        {
            Debug.LogError("❌ Error: No hay preguntas disponibles.");
            return;
        }

        if (preguntaActual >= preguntasFiltradas.Count)
        {
            Debug.Log("✅ Todas las preguntas han sido respondidas. Mostrando resultados finales...");
            MostrarResultadosFinales();
            return;
        }

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        txtPregunta.text = pregunta.textoPregunta;

        // Actualizar el progreso en la barra
        barraProgresoSlider.value = preguntaActual + 1;

        // Asignar opciones aleatorizadas
        List<(string opcion, int indice)> opcionesIndexadas = new List<(string, int)>();
        for (int i = 0; i < pregunta.opcionesRespuesta.Count; i++)
            opcionesIndexadas.Add((pregunta.opcionesRespuesta[i], i));

        opcionesIndexadas = opcionesIndexadas.OrderBy(x => Random.value).ToList();
        int nuevoIndiceCorrecto = opcionesIndexadas.FindIndex(x => x.indice == pregunta.indiceRespuestaCorrecta);
        pregunta.indiceRespuestaCorrecta = nuevoIndiceCorrecto;

        for (int i = 0; i < opciones.Length; i++)
        {
            if (i >= opcionesIndexadas.Count) continue;
            opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = opcionesIndexadas[i].opcion;
            opciones[i].isOn = false;
            opciones[i].GetComponentInChildren<Image>().color = Color.white;

            int index = i;
            opciones[i].onValueChanged.RemoveAllListeners();
            opciones[i].onValueChanged.AddListener(delegate { ValidarRespuesta(index); });
        }

        Debug.Log($"✅ Pregunta {preguntaActual + 1} mostrada correctamente.");
        preguntaEnCurso = true;
        StopCoroutine("ActualizarTimer");
        StartCoroutine("Temporizador");
    }

    public void ValidarRespuesta(int indiceSeleccionado)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("ActualizarTimer");

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        Color verdeCorrecto = new Color(0xAA / 255f, 0xC4 / 255f, 0x3D / 255f);
        Color rojoIncorrecto = new Color(0xC4 / 255f, 0x3E / 255f, 0x3B / 255f);
        opciones[indiceSeleccionado].GetComponentInChildren<Image>().color =
            (indiceSeleccionado == pregunta.indiceRespuestaCorrecta) ? verdeCorrecto : rojoIncorrecto;

        if (indiceSeleccionado == pregunta.indiceRespuestaCorrecta)
        {
            rachaActual++;
            respuestasCorrectas++;
        }
        else
        {
            rachaActual = 0;
            if (!temasFallados.Contains(pregunta.tema))
            {
                temasFallados.Add(pregunta.tema);
            }
        }

        txtRacha.text = "" + rachaActual;
        StartCoroutine(EsperarYSiguientePregunta());
    }

    IEnumerator Temporizador()
    {
        tiempoRestante = tiempoPorPregunta;
        while (tiempoRestante > 0)
        {
            if (!preguntaEnCurso) yield break;
            tiempoRestante -= Time.deltaTime;
            txtTiempo.text = tiempoRestante.ToString("F1");
            yield return null;
        }

        preguntaEnCurso = false;
        rachaActual = 0;
        txtRacha.text = "" + rachaActual;

        if (!temasFallados.Contains(preguntasFiltradas[preguntaActual].tema))
        {
            temasFallados.Add(preguntasFiltradas[preguntaActual].tema);
        }
        StartCoroutine(EsperarYSiguientePregunta());
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        preguntaActual++;

        // Guardar progreso por elemento
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntaActual);
        PlayerPrefs.Save();

        barraProgresoSlider.value = preguntaActual + 1;
        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        // 1. Asegurarse de que el panel de resultados esté activo
        PanelContinuar.SetActive(true);

        // 2. Calcular los resultados y el mensaje
        float porcentajeAciertos = (preguntasFiltradas.Count > 0) ? (float)respuestasCorrectas / preguntasFiltradas.Count * 100f : 0f;
        bool ganoElQuiz = porcentajeAciertos >= 60f;
        int xpGanado = 0;
        string mensajeResultado = "";

        if (ganoElQuiz)
        {
            // --- LÓGICA DE VICTORIA ---
            int xpBase = respuestasCorrectas * 10;
            int bonoRacha = rachaActual * 5;
            xpGanado = xpBase + bonoRacha;
            mensajeResultado = $"<color=#AED581>¡QUIZ SUPERADO!</color>\n\n" +
                               $"Puntuación: {respuestasCorrectas}/{preguntasFiltradas.Count} ({porcentajeAciertos:F0}%)\n" +
                               $"XP Obtenido: {xpGanado} (Base: {xpBase} + Racha: {bonoRacha})";
        }
        else
        {
            // --- LÓGICA DE DERROTA Y GUÍA ---
            xpGanado = respuestasCorrectas * 5;
            mensajeResultado = $"<color=#E57373>¡CASI LO LOGRAS!</color>\n\n" +
                               $"Puntuación: {respuestasCorrectas}/{preguntasFiltradas.Count} ({porcentajeAciertos:F0}%)\n" +
                               $"XP de consolación: {xpGanado}\n\n" +
                               $"<color=#FFD54F>Para mejorar, te recomendamos reforzar:</color>\n";
            foreach (string tema in temasFallados)
            {
                mensajeResultado += $"- {tema}\n";
            }
        }

        // 3. Mostrar el mensaje en el texto de resultado
        if (txtResultado != null)
        {
            txtResultado.text = mensajeResultado;
        }

        // 4. Configurar el botón "Continuar"
        Button botonContinuar = PanelContinuar.GetComponentInChildren<Button>();
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                // Ocultamos el panel del quiz
                PanelContinuar.SetActive(false);

                // Comprobamos que el Singleton exista
                if (GuardarMisionCompletada.instancia != null)
                {
                    // --- LLAMADA MODIFICADA ---
                    // Le PASAMOS las referencias de esta escena al Singleton.
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada,
                        particulasMisionCompletada
                    );

                    // Iniciamos la espera para cambiar de escena.
                    StartCoroutine(EsperarYCambiarDeEscena(3.0f));
                }
                else
                {
                    Debug.LogError("No se encontró la instancia de GuardarMisionCompletada. Cambiando de escena directamente.");
                    SceneManager.LoadScene("Categorías");
                }
            });
        }

        // 5. Guardar todo el progreso (esto se ejecuta después de mostrar los resultados, pero antes de que el usuario haga clic)
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            GuardarResultadosEnFirebase(xpGanado, porcentajeAciertos, ganoElQuiz);
        }
        else
        {
            SumarXPTemporario(xpGanado);
        }

        if (SistemaXP.Instance != null)
        {
            SistemaXP.Instance.AgregarXP(xpGanado);
        }
        else
        {
            Debug.LogWarning("⚠ No se encontró la instancia de SistemaXP. XP no agregado.");
        }

        PlayerPrefs.DeleteKey($"Progreso_{elementoCompleto}");
        PlayerPrefs.Save();
    }

    private IEnumerator EsperarYCambiarDeEscena(float tiempoDeEspera)
    {
        // Espera el tiempo especificado
        yield return new WaitForSeconds(tiempoDeEspera);

        // Luego, carga la siguiente escena
        SceneManager.LoadScene("Categorías");
    }

    async void GuardarResultadosEnFirebase(int xp, float puntaje, bool completado)
    {
        var user = auth.CurrentUser;
        if (user == null) return;

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        string claveMision = elementoCompleto.Replace("(", "").Replace(")", "").Replace(".", "");

        var datosMision = new Dictionary<string, object>
        {
            { "mejorPuntaje", puntaje },
            { "completado", completado },
            { "ultimoIntento", Timestamp.GetCurrentTimestamp() },
        };

        var updates = new Dictionary<string, object>
        {
            //incrementa el xp total del usuario
            {"xp", FieldValue.Increment(xp) },
            //Guarda/actualiza los datos de esta mision en un mapa "misionesCompletadas"
            {$"misionesCompletadas.{claveMision}", datosMision }
        };

        try
        {
            await userRef.UpdateAsync(updates);
            Debug.Log($"Resultados guardados en Firebase. XP: +{xp}, Misión: {claveMision}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar resultados en Firebase: {e.Message}");
            SumarXPTemporario(xp);
        }
    }

    public void GuardarYSalir()
    {
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntaActual);
        PlayerPrefs.SetInt("RachaActual", rachaActual);
        PlayerPrefs.SetFloat("ProgresoBarra", barraProgresoSlider.value);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Categorías");
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }


}


public static class ListExtensions
{
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
