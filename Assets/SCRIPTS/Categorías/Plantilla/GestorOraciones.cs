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

public class GestorOraciones : MonoBehaviour
{
    [System.Serializable]
    public class Pregunta
    {
        public string oracion;
        public List<string> opciones;
        public int respuesta_correcta;
    }

    [System.Serializable]
    public class PreguntasPorElemento
    {
        // Usamos un diccionario para manejar dinámicamente los 118 elementos
        public Dictionary<string, List<Pregunta>> elementos;
    }

    public TextMeshProUGUI txtOracion;
    public Transform contenedorOpciones;
    public GameObject botonPrefab;
    public Text txtTiempo;
    public Text txtRacha;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;
    public BarraProgreso barraProgreso;

    private Dictionary<string, List<OracionConPalabras>> preguntasPorElemento = new Dictionary<string, List<OracionConPalabras>>();
    private List<OracionConPalabras> preguntas = new List<OracionConPalabras>();

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
            Debug.LogError("No se encontró el archivo JSON de preguntas");
            return;
        }

        // Configuración para manejar propiedades dinámicas en el JSON
        var settings = new JsonSerializerSettings
        {
            // Permite manejar propiedades no definidas en la clase
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        // Deserializar el JSON a un diccionario dinámico
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
                pregunta.opciones.ToArray(),
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
            barraProgreso.InicializarBarra(preguntas.Count);
            MostrarPregunta();
        }
        else
        {
            Debug.LogWarning($"No hay preguntas definidas para el elemento {elemento}.");
            // Cargar un mensaje de error o redirigir a otra escena
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
        txtOracion.text = preguntaActual.oracion;

        // Limpiar opciones anteriores
        foreach (Transform child in contenedorOpciones)
            Destroy(child.gameObject);

        // Barajar las opciones de respuesta
        string[] opcionesBarajadas = BarajarOpciones(preguntaActual.opciones);
        int indiceCorrectoBarajado = System.Array.IndexOf(opcionesBarajadas,
            preguntaActual.opciones[preguntaActual.indiceCorrecto]);

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
        // Barajar las opciones de respuesta
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

        bool esCorrecto = (indiceSeleccionado == indiceCorrecto);
        string colorCorrecto = "<color=#A2C94D>";
        string colorIncorrecto = "<color=#C43E3B>";
        string colorFin = "</color>";

        string palabraSeleccionada = ((TextMeshProUGUI)boton.GetComponentInChildren<TextMeshProUGUI>()).text;
        string palabraColoreada = esCorrecto ? $"{colorCorrecto}{palabraSeleccionada}{colorFin}" : $"{colorIncorrecto}{palabraSeleccionada}{colorFin}";

        txtOracion.text = preguntaActual.oracion.Replace("___", palabraColoreada);
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
            racha = 0;
            txtRacha.text = racha.ToString();
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        indicePreguntaActual++;
        barraProgreso.InicializarBarra(preguntas.Count);
        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        panelFinal.SetActive(true);
        int experiencia = (respuestasCorrectas * 100) / preguntas.Count;
        txtResultado.text = $"Bonificación de racha: {racha * 10}";

    }

    [System.Serializable]
    public class OracionConPalabras
    {
        public string oracion;
        public string[] opciones;
        public int indiceCorrecto;

        public OracionConPalabras(string oracion, string[] opciones, int indiceCorrecto)
        {
            this.oracion = oracion;
            this.opciones = opciones;
            this.indiceCorrecto = indiceCorrecto;
        }
    }
}