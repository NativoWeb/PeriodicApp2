using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System.IO;
using Newtonsoft.Json;
using SimpleJSON;

public class GestorOraciones : MonoBehaviour
{
    // Clase para deserializar desde el JSON
    [System.Serializable]
    public class Pregunta
    {
        public string oracion;
        public string oracion_en; // Campo para la oración en inglés
        public List<string> opciones;
        public List<string> opciones_en; // Campo para las opciones en inglés
        public int respuesta_correcta;
    }

    // Clase para manejar las preguntas en tiempo de ejecución
    [System.Serializable]
    public class OracionConPalabras
    {
        public string oracion;
        public string oracion_en;
        public string[] opciones;
        public string[] opciones_en;
        public int indiceCorrecto;

        public OracionConPalabras(string oracion, string oracion_en, string[] opciones, string[] opciones_en, int indiceCorrecto)
        {
            this.oracion = oracion;
            this.oracion_en = oracion_en;
            this.opciones = opciones;
            this.opciones_en = opciones_en;
            this.indiceCorrecto = indiceCorrecto;
        }
    }

    // Diccionario principal que contiene las preguntas ya procesadas
    private Dictionary<string, List<OracionConPalabras>> preguntasPorElemento = new Dictionary<string, List<OracionConPalabras>>();
    private List<OracionConPalabras> preguntas = new List<OracionConPalabras>();

    public TextMeshProUGUI txtOracion;
    public Transform contenedorOpciones;
    public GameObject botonPrefab;
    public Text txtTiempo;
    public Text txtRacha;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;
    public BarraProgreso barraProgreso;

    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    private string elementoSeleccionado;
    private string appIdioma; // Variable para almacenar el idioma

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Obtener el idioma de la aplicación (p. ej., "es" o "en")
        appIdioma = PlayerPrefs.GetString("appIdioma", "español"); // Default a español si no hay valor

        // Obtener el elemento seleccionado de PlayerPrefs
        elementoSeleccionado = PlayerPrefs.GetString("SimboloElemento", "H"); // Default a Hidrógeno si no hay valor

        // Cargar preguntas desde JSON
        CargarPreguntasDesdeJSON();

        // Cargar preguntas para el elemento seleccionado
        CargarPreguntasElemento(elementoSeleccionado);
    }

    void CargarPreguntasDesdeJSON()
    {
        // Cargar el archivo JSON desde Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Oraciones");
        if (jsonFile == null)
        {
            Debug.LogError("No se encontró el archivo JSON de preguntas 'Oraciones.json'");
            return;
        }

        // Deserializar el JSON a un diccionario que usa nuestra clase Pregunta actualizada
        var preguntasJSON = JsonConvert.DeserializeObject<Dictionary<string, List<Pregunta>>>(jsonFile.text);

        // Procesar todas las preguntas para cada elemento
        foreach (var elemento in preguntasJSON)
        {
            preguntasPorElemento[elemento.Key] = ConvertirPreguntas(elemento.Value);
        }
    }

    List<OracionConPalabras> ConvertirPreguntas(List<Pregunta> preguntasJSON)
    {
        List<OracionConPalabras> resultado = new List<OracionConPalabras>();

        foreach (Pregunta pregunta in preguntasJSON)
        {
            resultado.Add(new OracionConPalabras(
                pregunta.oracion,
                pregunta.oracion_en,
                pregunta.opciones.ToArray(),
                pregunta.opciones_en.ToArray(),
                pregunta.respuesta_correcta
            ));
        }

        return resultado;
    }

    void CargarPreguntasElemento(string elemento)
    {
        if (preguntasPorElemento.ContainsKey(elemento))
        {
            preguntas = preguntasPorElemento[elemento];

            // Barajar las preguntas para orden aleatorio
            BarajarPreguntas();

            indicePreguntaActual = 0;
            respuestasCorrectas = 0;
            racha = 0;
            barraProgreso.InicializarBarra(preguntas.Count);
            MostrarPregunta();
        }
        else
        {
            Debug.LogWarning($"No hay preguntas definidas para el elemento {elemento}.");
            txtOracion.text = "No questions available for this element.";
            // Considerar mostrar un panel de error o volver a una escena anterior
        }
    }

    void BarajarPreguntas()
    {
        // Algoritmo de Fisher-Yates para barajar las preguntas
        System.Random rng = new System.Random();
        int n = preguntas.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            OracionConPalabras value = preguntas[k];
            preguntas[k] = preguntas[n];
            preguntas[n] = value;
        }
    }

    void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntas.Count)
        {
            MostrarResultadosFinales();
            return;
        }

        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];

        // Seleccionar el texto de la oración según el idioma
        txtOracion.text = (appIdioma == "ingles") ? preguntaActual.oracion_en : preguntaActual.oracion;

        // Limpiar opciones anteriores
        foreach (Transform child in contenedorOpciones)
        {
            Destroy(child.gameObject);
        }

        // Seleccionar las opciones según el idioma
        string[] opcionesParaMostrar = (appIdioma == "ingles") ? preguntaActual.opciones_en : preguntaActual.opciones;
        string[] opcionesBarajadas = BarajarOpciones(opcionesParaMostrar);

        // Encontrar la palabra correcta en el idioma actual para obtener su índice barajado
        string palabraCorrecta = (appIdioma == "ingles")
            ? preguntaActual.opciones_en[preguntaActual.indiceCorrecto]
            : preguntaActual.opciones[preguntaActual.indiceCorrecto];
        int indiceCorrectoBarajado = System.Array.IndexOf(opcionesBarajadas, palabraCorrecta);

        // Crear botones para cada opción
        for (int i = 0; i < opcionesBarajadas.Length; i++)
        {
            GameObject btn = Instantiate(botonPrefab, contenedorOpciones);
            TextMeshProUGUI txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
            txtBtn.text = opcionesBarajadas[i];
            int index = i;
            btn.GetComponent<Button>().onClick.AddListener(() => SeleccionarPalabra(index, btn, indiceCorrectoBarajado));
        }

        preguntaEnCurso = true;
        StopCoroutine("Temporizador");
        StartCoroutine("Temporizador");
    }

    string[] BarajarOpciones(string[] opciones)
    {
        System.Random rng = new System.Random();
        string[] opcionesBarajadas = (string[])opciones.Clone();
        int n = opcionesBarajadas.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = opcionesBarajadas[k];
            opcionesBarajadas[k] = opcionesBarajadas[n];
            opcionesBarajadas[n] = value;
        }
        return opcionesBarajadas;
    }

    void SeleccionarPalabra(int indiceSeleccionado, GameObject boton, int indiceCorrecto)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("Temporizador");

        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];
        string oracionBase = (appIdioma == "ingles") ? preguntaActual.oracion_en : preguntaActual.oracion;

        bool esCorrecto = (indiceSeleccionado == indiceCorrecto);
        string colorCorrecto = "<color=#A2C94D>";
        string colorIncorrecto = "<color=#C43E3B>";
        string colorFin = "</color>";

        string palabraSeleccionada = boton.GetComponentInChildren<TextMeshProUGUI>().text;
        string palabraColoreada = esCorrecto ? $"{colorCorrecto}{palabraSeleccionada}{colorFin}" : $"{colorIncorrecto}{palabraSeleccionada}{colorFin}";

        txtOracion.text = oracionBase.Replace("___", palabraColoreada);
        boton.GetComponent<Image>().color = esCorrecto ? new Color(0.64f, 0.79f, 0.30f) : new Color(0.77f, 0.24f, 0.23f);

        if (esCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
        }

        txtRacha.text = racha.ToString();
        barraProgreso.AvanzarPregunta();
        StartCoroutine(EsperarYSiguientePregunta());
    }

    bool EsMisionAprobada()
    {
        if (preguntas.Count == 0) return false;
        float porcentaje = (float)respuestasCorrectas / preguntas.Count;
        return porcentaje >= 0.7f;
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
            racha = 0;
            txtRacha.text = racha.ToString();
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        indicePreguntaActual++;
        if (indicePreguntaActual < preguntas.Count)
        {
            MostrarPregunta();
        }
        else
        {
            MostrarResultadosFinales();
        }
    }

    void MostrarResultadosFinales()
    {
        panelFinal.SetActive(true);
        int experiencia = (preguntas.Count > 0) ? (respuestasCorrectas * 100) / preguntas.Count : 0;

        // Mensajes localizados para el panel final
        if (appIdioma == "ingles")
        {
            txtResultado.text = $"You got {respuestasCorrectas} out of {preguntas.Count} answers correct. Streak bonus: {racha * 10}";
        }
        else
        {
            txtResultado.text = $"Tuviste {respuestasCorrectas} de {preguntas.Count} respuestas correctas. Bonificación de racha: {racha * 10}";
        }


        if (EsMisionAprobada())
        {
            Debug.Log("✅ Misión completada con éxito.");
            if (GameObject.Find("IAController") != null)
            {
                GameObject.Find("IAController").GetComponent<AiTutor>().gestorMisiones.MarcarMisionComoCompletada();
            }
        }
        else
        {
            Debug.Log("❌ Misión fallida, activando retroalimentación.");
            string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");
            categoria = devolverCatTrad(categoria);
            string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
            int idMision = PlayerPrefs.GetInt("MisionActual", -1);

            DarRecomendacion(categoria, elemento, idMision);
        }
    }

    public string devolverCatTrad(string categoriaSeleccionada)
    {
        // Esta función podría necesitar ser ajustada si la categoría también necesita traducción
        switch (categoriaSeleccionada)
        {
            case "Alkali Metals": return "Metales Alcalinos";
            case "Alkaline Earth Metals": return "Metales Alcalinotérreos";
            case "Transition Metals": return "Metales de Transición";
            case "Post-transition Metals": return "Metales postransicionales";
            case "Metalloids": return "Metaloides";
            case "Nonmetals": return "No Metales";
            case "Noble Gases": return "Gases Nobles";
            case "Lanthanides": return "Lantánidos";
            case "Actinides": return "Actinoides";
            case "Unknown Properties": return "Propiedades desconocidas";
            default: return categoriaSeleccionada;
        }
    }

    public void DarRecomendacion(string categoria, string elemento, int idMision)
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            txtResultado.text = (appIdioma == "ingles") ? "😕 Could not find mission data to help you." : "😕 No encontré información de la misión para ayudarte.";
            return;
        }

        var json = JSON.Parse(jsonString);
        var categorias = json["Misiones_Categorias"]["Categorias"].AsObject;

        if (!categorias.HasKey(categoria) || !categorias[categoria]["Elementos"].HasKey(elemento))
        {
            txtResultado.text = (appIdioma == "ingles") ? "😕 I could not find enough information to help you." : "😕 No encontré información suficiente para ayudarte.";
            return;
        }

        var elementoJson = categorias[categoria]["Elementos"][elemento];
        var misiones = elementoJson["misiones"].AsArray;

        JSONNode misionFallida = null;
        foreach (var m in misiones)
        {
            if (m.Value["id"].AsInt == idMision)
            {
                misionFallida = m.Value;
                break;
            }
        }

        if (misionFallida == null)
        {
            txtResultado.text = (appIdioma == "ingles") ? "😕 I could not find enough information to help you." : "😕 No encontré información suficiente para ayudarte.";
            return;
        }

        string tipo = misionFallida["tipo"];
        // Obtener descripción en el idioma correcto
        string descripcionElemento = (appIdioma == "ingles") ? elementoJson["descripcion_en"] : elementoJson["descripcion"];
        string mensaje = "";

        // Mensajes de retroalimentación localizados
        if (appIdioma == "ingles")
        {
            switch (tipo)
            {
                case "QR":
                    mensaje = $"📲 Try scanning the QR code for {elemento} again! Make sure you have good lighting and focus correctly. Did you know this?: {descripcionElemento}";
                    break;
                case "AR":
                    mensaje = $"🔍 Have you explored the 3D model of {elemento}? Zoom in and rotate the object in augmented reality to see key details. This will help you better understand the mission. 🧪\nFact: {descripcionElemento}";
                    break;
                case "Juego":
                    mensaje = $"🎮 Retry the mini-game for {elemento}! Focus on the clues and remember you can repeat it as many times as you need. Did you know: {descripcionElemento}";
                    break;
                case "Quiz":
                    mensaje = $"🧠 If you failed the quiz on {elemento}, review its properties like atomic number, mass, and electronegativity. Here's a useful fact: {descripcionElemento}";
                    break;
                case "Evaluacion":
                    mensaje = $"📋 The final evaluation requires you to remember everything about {elemento}. Review the other missions and read the questions carefully. Here's an important fact: {descripcionElemento}";
                    break;
                default:
                    mensaje = $"💡 Did you know this about {elemento}?: {descripcionElemento}";
                    break;
            }
        }
        else // Español
        {
            switch (tipo)
            {
                case "QR":
                    mensaje = $"📲 ¡Intenta escanear el código QR del elemento {elemento} nuevamente! Asegúrate de tener buena luz y enfocar correctamente. ¿Sabías esto?: {descripcionElemento}";
                    break;
                case "AR":
                    mensaje = $"🔍 ¿Ya exploraste el modelo 3D de {elemento}? Acércate y rota el objeto en realidad aumentada para ver detalles clave. Esto te ayudará a entender mejor la misión. 🧪\nDato: {descripcionElemento}";
                    break;
                case "Juego":
                    mensaje = $"🎮 ¡Reintenta el mini juego del elemento {elemento}! Concéntrate en las pistas y recuerda que puedes repetirlo las veces que necesites. ¿Sabías que: {descripcionElemento}";
                    break;
                case "Quiz":
                    mensaje = $"🧠 Si fallaste el quiz sobre {elemento}, revisa sus propiedades como número atómico, masa y electronegatividad. Aquí un dato útil: {descripcionElemento}";
                    break;
                case "Evaluacion":
                    mensaje = $"📋 La evaluación final requiere que recuerdes todo sobre {elemento}. Repasa las otras misiones y lee bien las preguntas. Aquí va un dato importante: {descripcionElemento}";
                    break;
                default:
                    mensaje = $"💡 ¿Sabías esto sobre {elemento}?: {descripcionElemento}";
                    break;
            }
        }
        // Asignar el mensaje al texto de resultado, que ahora está fuera del bloque if/else principal
        // y se actualiza con el mensaje de retroalimentación en lugar del de éxito.
        txtResultado.text = mensaje;
    }
}