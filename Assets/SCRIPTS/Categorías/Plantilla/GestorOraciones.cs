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
    [System.Serializable]
    public class Pregunta
    {
        public string oracion;
        public List<string> opciones;
        public int respuesta_correcta;
        public string categoriaTema;
    }

    [System.Serializable]
    public class PreguntasPorElemento
    {
        // Usamos un diccionario para manejar dinámicamente los 118 elementos
        public Dictionary<string, List<Pregunta>> elementos;
    }

    [Header("Componentes del Juego")]
    public TextMeshProUGUI txtOracion;
    public Transform contenedorOpciones;
    public GameObject botonPrefab;
    public Text txtTiempo;
    public Text txtRacha;
    public BarraProgreso barraProgreso;

    [Header("Panel de Resultados")]
    public GameObject panelFinal;
    public TextMeshProUGUI TxtMisionResultado;
    public TextMeshProUGUI TxtXpResultado;
    public TextMeshProUGUI TxtPuntuacionResultado;
    public TextMeshProUGUI TxtMotivacionResultado;
    public TextMeshProUGUI TxtRefuerzo1;
    public TextMeshProUGUI TxtRefuerzo2;
    public Button continuarCompletado;


    [Header("Referencias para Animación")]
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;

    //public TextMeshProUGUI txtOracion;
    //public Transform contenedorOpciones;
    //public GameObject botonPrefab;
    //public Text txtTiempo;
    //public Text txtRacha;
    //public GameObject panelFinal;
    //public TextMeshProUGUI txtResultado;
    //public BarraProgreso barraProgreso;


    private List<Pregunta> preguntasActuales = new List<Pregunta>();
    private List<string> categoriasFalladas = new List<string>();
    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string elementoSeleccionado;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Obtener el elemento seleccionado de PlayerPrefs
        elementoSeleccionado = PlayerPrefs.GetString("SimboloElemento", "H"); // Default a Hidrógeno si no hay valor

        // Inicializar estado del juego
        panelFinal.SetActive(false);
        racha = 0;
        respuestasCorrectas = 0;
        categoriasFalladas.Clear();


        continuarCompletado.onClick.AddListener(() =>
        {
            // Aquí puedes cargar la siguiente escena o realizar otra acción
            SceneManager.LoadScene("Categorías");
        });
        // Cargar preguntas desde JSON
        //CargarPreguntasDesdeJSON();

        // Cargar preguntas para el elemento seleccionado
        CargarPreguntasParaElemento(elementoSeleccionado);
    }

    void CargarPreguntasParaElemento(string elemento)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Oraciones_con_categoriaTema");
        if (jsonFile == null)
        {
            Debug.LogError("No se encontró el archivo JSON 'Oraciones.json'");
            return;
        }

        var preguntasJSON = JsonConvert.DeserializeObject<Dictionary<string, List<Pregunta>>>(jsonFile.text);

        if (preguntasJSON.ContainsKey(elemento))
        {
            preguntasActuales = preguntasJSON[elemento];
            BarajarPreguntas();
            if (barraProgreso != null) barraProgreso.InicializarBarra(preguntasActuales.Count);
            indicePreguntaActual = 0;
            MostrarPregunta();
        }
        else
        {
            Debug.LogWarning($"No hay preguntas definidas para el elemento {elemento}.");
            // Aquí puedes mostrar un panel de error y regresar.
        }
    }



    //List<OracionConPalabras> ConvertirPreguntas(List<Pregunta> preguntasJSON)
    //{
    //    List<OracionConPalabras> resultado = new List<OracionConPalabras>();

    //    foreach (Pregunta pregunta in preguntasJSON)
    //    {
    //        resultado.Add(new OracionConPalabras(
    //            pregunta.oracion,
    //            pregunta.opciones.ToArray(),
    //            pregunta.respuesta_correcta
    //        ));
    //    }

    //    return resultado;
    //}



    void BarajarPreguntas()
    {
        // Algoritmo de Fisher-Yates para barajar las preguntas
        System.Random rng = new System.Random();
        int n = preguntasActuales.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Pregunta value = preguntasActuales[k];
            preguntasActuales[k] = preguntasActuales[n];
            preguntasActuales[n] = value;
        }
    }

    void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntasActuales.Count)
        {
            if (barraProgreso != null)
            {
                barraProgreso.ActualizarProgreso(preguntasActuales.Count, preguntasActuales.Count);
            }
            MostrarResultadosFinales();
            return;
        }

        if (barraProgreso != null)
        {
            barraProgreso.ActualizarProgreso(indicePreguntaActual + 1, preguntasActuales.Count);
        }



        Pregunta preguntaActual = preguntasActuales[indicePreguntaActual];
        txtOracion.text = preguntaActual.oracion;

        // Limpiar opciones anteriores
        foreach (Transform child in contenedorOpciones)
            Destroy(child.gameObject);

        // Barajar las opciones de respuesta
        //string[] opcionesBarajadas = BarajarOpciones(preguntaActual.opciones);
        //int indiceCorrectoBarajado = System.Array.IndexOf(opcionesBarajadas,
        //    preguntaActual.opciones[preguntaActual.indiceCorrecto]);

        // Crear botones para cada opción
        for (int i = 0; i < preguntaActual.opciones.Count; i++)
        {
            GameObject btnObj = Instantiate(botonPrefab, contenedorOpciones);
            //TextMeshProUGUI txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = preguntaActual.opciones[i];
            //txtBtn.text = opcionesBarajadas[i];
            int opcionIndex = i; // Capturar el índice para el listener
            btnObj.GetComponent<Button>().onClick.AddListener(() => SeleccionarPalabra(opcionIndex, btnObj));
        }

        preguntaEnCurso = true;
        StopCoroutine("Temporizador");
        StartCoroutine("Temporizador");
    }

    void SeleccionarPalabra(int indiceSeleccionado, GameObject boton)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("Temporizador");

        Pregunta preguntaActual = preguntasActuales[indicePreguntaActual];
        bool esCorrecto = (indiceSeleccionado == preguntaActual.respuesta_correcta);

        string palabraSeleccionada = preguntaActual.opciones[indiceSeleccionado];
        string colorHex = esCorrecto ? "#AED581" : "#E57373"; // Verde o Rojo
        string palabraColoreada = $"<color={colorHex}>{palabraSeleccionada}</color>";

        txtOracion.text = preguntaActual.oracion.Replace("___", palabraColoreada);

        // Desactivar todos los botones para evitar más clics
        foreach (Transform child in contenedorOpciones)
        {
            child.GetComponent<Button>().interactable = false;
        }

        if (esCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
            // Guardar la categoría de la pregunta fallada
            if (!categoriasFalladas.Contains(preguntaActual.categoriaTema))
            {
                categoriasFalladas.Add(preguntaActual.categoriaTema);
            }
        }
        txtRacha.text = racha.ToString();
        StartCoroutine(EsperarYSiguientePregunta());
    }

    //void SeleccionarPalabra(int indiceSeleccionado, GameObject boton, int indiceCorrecto)
    //{
    //    if (!preguntaEnCurso) return;
    //    preguntaEnCurso = false;
    //    StopCoroutine("Temporizador");

    //    OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];

    //    bool esCorrecto = (indiceSeleccionado == indiceCorrecto);
    //    string colorCorrecto = "<color=#A2C94D>";
    //    string colorIncorrecto = "<color=#C43E3B>";
    //    string colorFin = "</color>";

    //    string palabraSeleccionada = ((TextMeshProUGUI)boton.GetComponentInChildren<TextMeshProUGUI>()).text;
    //    string palabraColoreada = esCorrecto ? $"{colorCorrecto}{palabraSeleccionada}{colorFin}" : $"{colorIncorrecto}{palabraSeleccionada}{colorFin}";

    //    txtOracion.text = preguntaActual.oracion.Replace("___", palabraColoreada);
    //    boton.GetComponent<Image>().color = esCorrecto ? new Color(0.64f, 0.79f, 0.30f) : new Color(0.77f, 0.24f, 0.23f);

    //    if (esCorrecto)
    //    {
    //        racha++;
    //        respuestasCorrectas++;
    //    }
    //    else
    //    {
    //        racha = 0;
    //    }

    //    txtRacha.text = racha.ToString();
    //    StartCoroutine(EsperarYSiguientePregunta());
    //}

    //bool EsMisionAprobada()
    //{
    //    float porcentaje = (float)respuestasCorrectas / preguntas.Count;
    //    return porcentaje >= 0.7f; 
    //}


    IEnumerator Temporizador()
    {
        tiempoRestante = tiempoPorPregunta;
        while (tiempoRestante > 0 && preguntaEnCurso)
        {
            tiempoRestante -= Time.deltaTime;
            txtTiempo.text = tiempoRestante.ToString("F1");
            yield return null;
        }
        if (preguntaEnCurso)
        {
            preguntaEnCurso = false;
            racha = 0;
            txtRacha.text = racha.ToString();
            yield return new WaitForSeconds(1.5f);
            // Guardar la categoría de la pregunta no respondida
            if (!categoriasFalladas.Contains(preguntasActuales[indicePreguntaActual].categoriaTema))
            {
                categoriasFalladas.Add(preguntasActuales[indicePreguntaActual].categoriaTema);
            }
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(2.0f);
        indicePreguntaActual++;
        //barraProgreso.InicializarBarra(preguntasActuales.Count);
        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        panelFinal.SetActive(true);

        float umbralVictoria = 69.0f;
        float porcentajeAciertos = (preguntasActuales.Count > 0) ? (float)respuestasCorrectas / preguntasActuales.Count * 100f : 0f;
        bool ganoLaMision = porcentajeAciertos > umbralVictoria;
        int xpGanado = 15;

        if (ganoLaMision)
        {
            TxtMisionResultado.text = "¡MISIÓN COMPLETADA!";
            TxtXpResultado.text = $"+{xpGanado} XP";
            TxtPuntuacionResultado.text = $"{respuestasCorrectas}/{preguntasActuales.Count}";
            TxtMotivacionResultado.text = "¡Tu conocimiento de la química está tomando forma!";

            if (TxtRefuerzo1 != null) TxtRefuerzo1.transform.parent.gameObject.SetActive(false);
            if (TxtRefuerzo2 != null) TxtRefuerzo2.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            xpGanado = 0; // No se gana XP si no se supera el umbral
            TxtMisionResultado.text = "¡CASI LO LOGRAS!";
            TxtXpResultado.text = "+0 XP";
            TxtPuntuacionResultado.text = $"{respuestasCorrectas}/{preguntasActuales.Count}";
            TxtMotivacionResultado.text = "¡Un esfuerzo más! Te recomendamos reforzar:";

            if (TxtRefuerzo1 != null) TxtRefuerzo1.transform.parent.gameObject.SetActive(true);
            if (TxtRefuerzo2 != null) TxtRefuerzo2.transform.parent.gameObject.SetActive(true);

            TxtRefuerzo1.text = (categoriasFalladas.Count > 0) ? "- " + categoriasFalladas[0] : "";
            TxtRefuerzo2.text = (categoriasFalladas.Count > 1) ? "- " + categoriasFalladas[1] : "";
        }

        PlayerPrefs.SetInt("UltimoQuizGanado", ganoLaMision ? 1 : 0);
        PlayerPrefs.SetInt("xpGanado", xpGanado);
        PlayerPrefs.Save();

        Button botonContinuar = panelFinal.GetComponentInChildren<Button>();
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                if (ganoLaMision)
                {
                    panelFinal.SetActive(false);
                    if (GuardarMisionCompletada.instancia != null)
                    {
                        GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                         panelAnimacionMision,
                         imagenAnimacionMision,
                         audioMisionCompletada
                     );
                    }
                }
                else
                {
                    panelFinal.SetActive(false);
                    if (GuardarMisionCompletada.instancia != null)
                    {
                        GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                         panelAnimacionMision,
                         imagenAnimacionMision,
                         audioMisionCompletada
                     );
                    }
                }
            });
        }

    }

    //public void DarRecomendacion(string categoria, string elemento, int idMision)
    //{
    //    string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
    //    var json = JSON.Parse(jsonString);
    //    var categorias = json["Misiones_Categorias"]["Categorias"].AsObject;
    //    var elementoJson = categorias[categoria]["Elementos"][elemento];
    //    var misiones = elementoJson["misiones"].AsArray;

    //    JSONNode misionFallida = null;
    //    foreach (var m in misiones)
    //    {
    //        if (m.Value["id"].AsInt == idMision)
    //        {
    //            misionFallida = m.Value;
    //            break;
    //        }
    //    }


    //    if (misionFallida == null)
    //    {
    //        txtResultado.text = "😕 No encontré información suficiente para ayudarte.";
    //        return;
    //    }

    //    string tipo = misionFallida["tipo"];
    //    string descripcionElemento = elementoJson["descripcion"];
    //    string mensaje = "";

    //    switch (tipo)
    //    {
    //        case "QR":
    //            mensaje = $"📲 ¡Intenta escanear el código QR del elemento {elemento} nuevamente! Asegúrate de tener buena luz y enfocar correctamente. ¿Sabías esto?: {descripcionElemento}";
    //            break;
    //        case "AR":
    //            mensaje = $"🔍 ¿Ya exploraste el modelo 3D de {elemento}? Acércate y rota el objeto en realidad aumentada para ver detalles clave. Esto te ayudará a entender mejor la misión. 🧪\nDato: {descripcionElemento}";
    //            break;
    //        case "Juego":
    //            mensaje = $"🎮 ¡Reintenta el mini juego del elemento {elemento}! Concéntrate en las pistas y recuerda que puedes repetirlo las veces que necesites. ¿Sabías que: {descripcionElemento}";
    //            break;
    //        case "Quiz":
    //            mensaje = $"🧠 Si fallaste el quiz sobre {elemento}, revisa sus propiedades como número atómico, masa y electronegatividad. Aquí un dato útil: {descripcionElemento}";
    //            break;
    //        case "Evaluacion":
    //            mensaje = $"📋 La evaluación final requiere que recuerdes todo sobre {elemento}. Repasa las otras misiones y lee bien las preguntas. Aquí va un dato importante: {descripcionElemento}";
    //            break;
    //        default:
    //            mensaje = $"💡 ¿Sabías esto sobre {elemento}?: {descripcionElemento}";
    //            break;
    //    }

    //    txtResultado.text = mensaje;
    //}


}