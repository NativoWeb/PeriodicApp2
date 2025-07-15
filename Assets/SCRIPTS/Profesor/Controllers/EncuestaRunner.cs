using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.IO;

// ===================================================================================
// === DATA MODELS ===================================================================
// ===================================================================================

// (Tus clases OpcionModelo, PreguntaModelo, EncuestaModelo ya existen en otro archivo)

// La clase para el reporte final no cambia.
[System.Serializable]
public class ReporteIntento
{
    public string idEncuesta;
    public string idUsuario;
    public string fechaIntento;
    public int respuestasCorrectas;
    public int totalPreguntas;
    public int minimoParaAprobar;
    public string resultadoFinal; // "Aprobado" o "Reprobado"
}

// ===================================================================================

public class EncuestaRunner : MonoBehaviour
{
    [Header("Configuración de la Encuesta (Datos)")]
    private EncuestaModelo encuestaActual;
    private List<PreguntaModelo> listaPreguntas;
    private int preguntaActualIndex = 0;
    private int respuestasCorrectas = 0;
    private Coroutine temporizadorCoroutine;
    private bool respuestaEnviada = false;

    [Header("Referencias a la UI de la Encuesta")]
    [SerializeField] private GameObject panelEncuesta;
    [SerializeField] private TextMeshProUGUI txtTituloEncuesta;
    [SerializeField] private TextMeshProUGUI txtContadorPregunta;
    [SerializeField] private TextMeshProUGUI txtTextoPregunta;
    [SerializeField] private Slider sliderTemporizador;
    [SerializeField] private Transform contenedorOpciones;
    [SerializeField] private GameObject botonOpcionPrefab;

    [Header("Referencias a la UI de Resultados")]
    [SerializeField] private GameObject panelResultados;
    [SerializeField] private TextMeshProUGUI txtResultadoTitulo;
    [SerializeField] private TextMeshProUGUI txtResultadoDetalle;
    [SerializeField] private Button btnCerrarResultados;

    /// <summary>
    /// Punto de entrada principal. Llama a esta función desde otro script pasándole el JSON.
    /// </summary>
    public void IniciarEncuesta(string jsonString)
    {
        encuestaActual = JsonUtility.FromJson<EncuestaModelo>(jsonString);
        if (encuestaActual == null)
        {
            Debug.LogError("Error: No se pudo cargar la encuesta desde el JSON.");
            return;
        }

        preguntaActualIndex = 0;
        respuestasCorrectas = 0;
        listaPreguntas = new List<PreguntaModelo>(encuestaActual.Preguntas); // Usamos la propiedad .Preguntas

        if (encuestaActual.AleatorizarPreguntas) // Usamos la propiedad .AleatorizarPreguntas
        {
            AleatorizarLista(listaPreguntas);
        }

        panelEncuesta.SetActive(true);
        panelResultados.SetActive(false);
        txtTituloEncuesta.text = encuestaActual.Titulo; // Usamos la propiedad .Titulo

        btnCerrarResultados.onClick.RemoveAllListeners();
        btnCerrarResultados.onClick.AddListener(() => {
            panelResultados.SetActive(false);
            Debug.Log("Encuesta cerrada.");
        });

        MostrarPreguntaActual();
    }

    private void MostrarPreguntaActual()
    {
        respuestaEnviada = false;

        foreach (Transform child in contenedorOpciones)
        {
            Destroy(child.gameObject);
        }

        PreguntaModelo pregunta = listaPreguntas[preguntaActualIndex];

        txtContadorPregunta.text = $"Pregunta {preguntaActualIndex + 1} / {listaPreguntas.Count}";
        txtTextoPregunta.text = pregunta.TextoPregunta; // Usamos la propiedad .TextoPregunta

        List<OpcionModelo> opciones = new List<OpcionModelo>(pregunta.Opciones); // Usamos la propiedad .Opciones
        if (encuestaActual.AleatorizarRespuestas) // Usamos la propiedad .AleatorizarRespuestas
        {
            AleatorizarLista(opciones);
        }

        foreach (OpcionModelo opcion in opciones)
        {
            GameObject botonGO = Instantiate(botonOpcionPrefab, contenedorOpciones);
            Button boton = botonGO.GetComponent<Button>();
            TextMeshProUGUI textoBoton = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            textoBoton.text = opcion.Texto; // Usamos la propiedad .Texto

            boton.onClick.AddListener(() => OnRespuestaSeleccionada(opcion, boton));
        }

        if (temporizadorCoroutine != null) StopCoroutine(temporizadorCoroutine);
        temporizadorCoroutine = StartCoroutine(TemporizadorCoroutine(pregunta.TiempoSegundos)); // Usamos la propiedad .TiempoSegundos
    }

    private void OnRespuestaSeleccionada(OpcionModelo opcionSeleccionada, Button botonPulsado)
    {
        if (respuestaEnviada) return;
        respuestaEnviada = true;

        StopCoroutine(temporizadorCoroutine);

        foreach (Transform child in contenedorOpciones)
        {
            child.GetComponent<Button>().interactable = false;
        }

        if (opcionSeleccionada.EsCorrecta) // Usamos la propiedad .EsCorrecta
        {
            respuestasCorrectas++;
            botonPulsado.GetComponent<Image>().color = Color.green;
        }
        else
        {
            botonPulsado.GetComponent<Image>().color = Color.red;
        }

        StartCoroutine(EsperarYSiguientePregunta(1.5f));
    }

    private IEnumerator TemporizadorCoroutine(int segundos)
    {
        float tiempoRestante = segundos;
        sliderTemporizador.maxValue = segundos;
        sliderTemporizador.value = segundos;

        while (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            sliderTemporizador.value = tiempoRestante;
            yield return null;
        }

        Debug.Log("¡Tiempo agotado!");
        respuestaEnviada = true;
        StartCoroutine(EsperarYSiguientePregunta(1.0f));
    }

    private IEnumerator EsperarYSiguientePregunta(float delay)
    {
        yield return new WaitForSeconds(delay);

        preguntaActualIndex++;

        if (preguntaActualIndex < listaPreguntas.Count)
        {
            MostrarPreguntaActual();
        }
        else
        {
            FinalizarEncuesta();
        }
    }

    private void FinalizarEncuesta()
    {
        panelEncuesta.SetActive(false);
        panelResultados.SetActive(true);

        bool aprobado = respuestasCorrectas >= encuestaActual.MinimoPreguntasAprobar; // Usamos la propiedad .MinimoPreguntasAprobar

        if (aprobado)
        {
            txtResultadoTitulo.text = "¡Completada!";
            txtResultadoTitulo.color = Color.green;
        }
        else
        {
            txtResultadoTitulo.text = "¡Fallaste!";
            txtResultadoTitulo.color = Color.red;
        }

        txtResultadoDetalle.text = $"Respuestas correctas: {respuestasCorrectas} de {listaPreguntas.Count}\n(Mínimo para aprobar: {encuestaActual.MinimoPreguntasAprobar})"; // Usamos la propiedad .MinimoPreguntasAprobar

        CrearYGuardarReporte(aprobado ? "Aprobado" : "Reprobado");
    }

    private void CrearYGuardarReporte(string resultado)
    {
        ReporteIntento reporte = new ReporteIntento
        {
            idEncuesta = encuestaActual.Id, // Usamos la propiedad .Id
            idUsuario = PlayerPrefs.GetString("UserID", "usuario_desconocido"),
            fechaIntento = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            respuestasCorrectas = this.respuestasCorrectas,
            totalPreguntas = listaPreguntas.Count,
            minimoParaAprobar = encuestaActual.MinimoPreguntasAprobar, // Usamos la propiedad .MinimoPreguntasAprobar
            resultadoFinal = resultado
        };

        string reporteJson = JsonUtility.ToJson(reporte, true);

        string path = Path.Combine(Application.persistentDataPath, "ReportesEncuestas");
        Directory.CreateDirectory(path);
        string filePath = Path.Combine(path, $"reporte_{reporte.idEncuesta}_{System.DateTime.Now:yyyyMMddHHmmss}.json");

        File.WriteAllText(filePath, reporteJson);
        Debug.Log($"Reporte guardado en: {filePath}");
    }

    // --- UTILITIES ---
    private void AleatorizarLista<T>(List<T> lista)
    {
        System.Random rng = new System.Random();
        int n = lista.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = lista[k];
            lista[k] = lista[n];
            lista[n] = value;
        }
    }
}