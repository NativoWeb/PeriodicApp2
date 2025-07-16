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
    // --- Referencias de la Interfaz de Usuario ---
    public TextMeshProUGUI txtPregunta;
    public Toggle[] opciones;
    public Text txtTiempo;
    public Text txtRacha;
    public GameObject PanelContinuar;
    public Slider barraProgresoSlider;

    [Header("Referencias para Animación de Misión")]
    public TMP_Text txtMision;
    public TMP_Text txtXp;
    public TMP_Text txtPuntuacion;
    public TMP_Text txtMotivacion;
    public TMP_Text txtRefuerzo1;
    public TMP_Text txtRefuerzo2;
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;

    // --- Clases de Datos ---
    [System.Serializable]
    public class Pregunta
    {
        public string textoPregunta;
        public string textoPregunta_en; // Campo para inglés
        public List<string> opcionesRespuesta;
        public List<string> opcionesRespuesta_en; // Campo para inglés
        public int indiceRespuestaCorrecta;
        public string tema;
        public string categoriaTema;
    }

    // --- Variables de Estado del Juego ---
    private List<Pregunta> preguntasFiltradas;
    private int preguntaActual = 0;
    private int rachaActual;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private List<string> categoriasFalladas;

    // --- Variables de Configuración y Firebase ---
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string elementoSeleccionado;
    private string simboloSeleccionado;
    private string elementoCompleto;
    private string categoriaSeleccionada;
    private string appIdioma; // Variable para el idioma

    void Start()
    {
        // FIREBASE
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // --- Inicialización ---
        appIdioma = PlayerPrefs.GetString("appIdioma", "español"); // Default a español
        categoriasFalladas = new List<string>();
        barraProgresoSlider.minValue = 0;

        // --- Recuperar datos de PlayerPrefs ---
        elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "").Trim() + " ";
        simboloSeleccionado = "(" + PlayerPrefs.GetString("SimboloElemento", "").Trim() + ")";
        elementoCompleto = elementoSeleccionado + simboloSeleccionado;
        categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "").Trim();

        // Si el idioma es español, traducimos el nombre de la categoría para buscar el archivo correcto.
        if (appIdioma == "español")
        {
            categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);
        }

        rachaActual = PlayerPrefs.GetInt("RachaActual");
        preguntaActual = PlayerPrefs.GetInt($"Progreso_{elementoCompleto}", 0);
        barraProgresoSlider.value = preguntaActual;

        // --- Cargar y Mostrar Preguntas ---
        CargarPreguntasDesdeJSON(categoriaSeleccionada, elementoCompleto);
        if (preguntasFiltradas != null && preguntasFiltradas.Count > 0)
        {
            barraProgresoSlider.maxValue = preguntasFiltradas.Count;
            MostrarPregunta();
            StartCoroutine(Temporizador());
        }
    }

    public string devolverCatTrad(string categoriaEnIngles)
    {
        switch (categoriaEnIngles)
        {
            case "Alkali Metals": return "Metales Alcalinos";
            case "Alkaline Earth Metals": return "Metales Alcalinotérreos";
            case "Transition Metals": return "Metales de Transición";
            case "Post-transition Metals": return "Metales Postransicionales";
            case "Metalloids": return "Metaloides";
            case "Reactive Nonmetals": return "No Metales Reactivos";
            case "Noble Gases": return "Gases Nobles";
            case "Lanthanides": return "Lantánidos";
            case "Actinides": return "Actinoides";
            case "Unknown Properties": return "Propiedades Desconocidas";
            default: return categoriaEnIngles; // Retorna el mismo si no hay traducción
        }
    }

    void CargarPreguntasDesdeJSON(string categoria, string elemento)
    {
        string nombreArchivo = categoria.ToLower().Replace(' ', '_');
        string rutaCompleta = "Preguntas_misiones_json/" + nombreArchivo;

        TextAsset jsonFile = Resources.Load<TextAsset>(rutaCompleta);
        if (jsonFile == null)
        {
            Debug.LogError($"No se encontró el archivo JSON en la ruta: '{rutaCompleta}'. Categoria: '{categoria}'");
            return;
        }

        var json = JSON.Parse(jsonFile.text);
        if (json == null || !json.HasKey("elementos"))
        {
            Debug.LogError("El JSON no tiene la estructura esperada (falta 'elementos').");
            return;
        }

        preguntasFiltradas = new List<Pregunta>();

        foreach (JSONNode elementoJson in json["elementos"].AsArray)
        {
            if (elementoJson["elemento"].Value == elemento)
            {
                foreach (JSONNode preguntaJson in elementoJson["preguntas"].AsArray)
                {
                    List<string> opciones_es = new List<string>();
                    foreach (JSONNode opcion in preguntaJson["opcionesRespuesta"].AsArray)
                    {
                        opciones_es.Add(opcion.Value);
                    }

                    List<string> opciones_en = new List<string>();
                    // Validar si existe la clave en inglés antes de intentar acceder a ella
                    if (preguntaJson.HasKey("opcionesRespuesta_en"))
                    {
                        foreach (JSONNode opcion_en in preguntaJson["opcionesRespuesta_en"].AsArray)
                        {
                            opciones_en.Add(opcion_en.Value);
                        }
                    }

                    Pregunta p = new Pregunta
                    {
                        textoPregunta = preguntaJson["textoPregunta"].Value,
                        textoPregunta_en = preguntaJson.HasKey("textoPregunta_en") ? preguntaJson["textoPregunta_en"].Value : preguntaJson["textoPregunta"].Value, // Fallback a español si no existe
                        opcionesRespuesta = opciones_es,
                        opcionesRespuesta_en = opciones_en,
                        indiceRespuestaCorrecta = preguntaJson["indiceRespuestaCorrecta"].AsInt,
                        tema = preguntaJson.HasKey("tema") ? preguntaJson["tema"].Value : "General",
                        categoriaTema = preguntaJson.HasKey("categoriaTema") ? preguntaJson["categoriaTema"].Value : "Conceptos Generales"
                    };
                    preguntasFiltradas.Add(p);
                }
                break;
            }
        }
    }

    public void MostrarPregunta()
    {
        if (preguntasFiltradas == null || preguntaActual >= preguntasFiltradas.Count)
        {
            MostrarResultadosFinales();
            return;
        }

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        // Seleccionar texto de pregunta según idioma
        txtPregunta.text = (appIdioma == "ingles") ? pregunta.textoPregunta_en : pregunta.textoPregunta;

        barraProgresoSlider.value = preguntaActual + 1;

        // Seleccionar opciones según idioma y barajar
        List<string> opcionesOriginales = (appIdioma == "ingles" && pregunta.opcionesRespuesta_en.Count > 0)
            ? pregunta.opcionesRespuesta_en
            : pregunta.opcionesRespuesta;

        List<(string opcion, int indice)> opcionesIndexadas = opcionesOriginales
            .Select((opcion, i) => (opcion, i)).ToList();

        opcionesIndexadas.Shuffle();

        int nuevoIndiceCorrecto = opcionesIndexadas.FindIndex(x => x.indice == pregunta.indiceRespuestaCorrecta);
        pregunta.indiceRespuestaCorrecta = nuevoIndiceCorrecto;

        for (int i = 0; i < opciones.Length; i++)
        {
            if (i < opcionesIndexadas.Count)
            {
                opciones[i].gameObject.SetActive(true);
                opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = opcionesIndexadas[i].opcion;
                opciones[i].isOn = false;
                opciones[i].GetComponentInChildren<Image>().color = Color.white;

                int index = i;
                opciones[i].onValueChanged.RemoveAllListeners();
                opciones[i].onValueChanged.AddListener(delegate { if (opciones[index].isOn) ValidarRespuesta(index); });
            }
            else
            {
                opciones[i].gameObject.SetActive(false);
            }
        }

        preguntaEnCurso = true;
        StopAllCoroutines(); // Detiene todas las corutinas, incluido el temporizador anterior
        StartCoroutine(Temporizador());
    }

    public void ValidarRespuesta(int indiceSeleccionado)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        Color verdeCorrecto = new Color(0xAA / 255f, 0xC4 / 255f, 0x3D / 255f);
        Color rojoIncorrecto = new Color(0xC4 / 255f, 0x3E / 255f, 0x3B / 255f);

        // Desactivar todos los toggles para evitar más input
        foreach (var opt in opciones)
        {
            opt.interactable = false;
        }

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
            // Marcar la opción correcta en verde si el usuario se equivocó
            opciones[pregunta.indiceRespuestaCorrecta].GetComponentInChildren<Image>().color = verdeCorrecto;

            if (!categoriasFalladas.Contains(pregunta.categoriaTema))
            {
                categoriasFalladas.Add(pregunta.categoriaTema);
            }
        }

        txtRacha.text = rachaActual.ToString();
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

        if (preguntaEnCurso)
        {
            preguntaEnCurso = false;
            rachaActual = 0;
            txtRacha.text = rachaActual.ToString();

            if (!categoriasFalladas.Contains(preguntasFiltradas[preguntaActual].categoriaTema))
            {
                categoriasFalladas.Add(preguntasFiltradas[preguntaActual].categoriaTema);
            }
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        preguntaActual++;
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntaActual);
        PlayerPrefs.Save();

        // Reactivar los toggles
        foreach (var opt in opciones)
        {
            opt.interactable = true;
        }

        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        PanelContinuar.SetActive(true);
        float umbralVictoria = 70.0f;
        float porcentajeAciertos = (preguntasFiltradas.Count > 0) ? (float)respuestasCorrectas / preguntasFiltradas.Count * 100f : 0f;
        bool ganoElQuiz = porcentajeAciertos >= umbralVictoria;
        int xpGanado = 0;

        if (ganoElQuiz)
        {
            int xpBase = respuestasCorrectas * 10;
            int bonoRacha = rachaActual * 5;
            xpGanado = xpBase + bonoRacha;

            txtMision.text = (appIdioma == "ingles") ? "QUIZ PASSED!" : "¡QUIZ SUPERADO!";
            txtXp.text = $"+{xpGanado} XP";
            txtPuntuacion.text = $"{respuestasCorrectas}/{preguntasFiltradas.Count}";
            txtMotivacion.text = (appIdioma == "ingles") ? "Excellent work! You've shown great mastery of the subject." : "¡Excelente trabajo! Has demostrado un gran dominio del tema.";

            if (txtRefuerzo1 != null) txtRefuerzo1.transform.parent.gameObject.SetActive(false);
            if (txtRefuerzo2 != null) txtRefuerzo2.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            xpGanado = respuestasCorrectas * 5;

            txtMision.text = (appIdioma == "ingles") ? "ALMOST THERE!" : "¡CASI LO LOGRAS!";
            txtXp.text = $"+{xpGanado} XP";
            txtPuntuacion.text = $"{respuestasCorrectas}/{preguntasFiltradas.Count}";
            txtMotivacion.text = (appIdioma == "ingles") ? "Keep studying. To improve, we recommend reinforcing:" : "Sigue estudiando. Para mejorar, te recomendamos reforzar:";

            if (txtRefuerzo1 != null) txtRefuerzo1.transform.parent.gameObject.SetActive(categoriasFalladas.Count > 0);
            if (txtRefuerzo2 != null) txtRefuerzo2.transform.parent.gameObject.SetActive(categoriasFalladas.Count > 1);

            if (categoriasFalladas.Count > 0) txtRefuerzo1.text = "- " + categoriasFalladas[0];
            if (categoriasFalladas.Count > 1) txtRefuerzo2.text = "- " + categoriasFalladas[1];
        }

        PlayerPrefs.SetInt("UltimoQuizGanado", ganoElQuiz ? 1 : 0);
        PlayerPrefs.SetInt("xp_mision", xpGanado);
        PlayerPrefs.Save();

        Button botonContinuar = PanelContinuar.GetComponentInChildren<Button>();
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                PanelContinuar.SetActive(false);
                if (GuardarMisionCompletada.instancia != null)
                {
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada
                    );
                }
                else
                {
                    SceneManager.LoadScene("Categorías");
                }
            });
        }

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

        PlayerPrefs.DeleteKey($"Progreso_{elementoCompleto}");
        PlayerPrefs.Save();
    }

    // El resto de los métodos (GuardarYSalir, GuardarResultadosEnFirebase, SumarXPTemporario) permanecen igual.
    // ...
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
            {"xp", FieldValue.Increment(xp) },
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
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}