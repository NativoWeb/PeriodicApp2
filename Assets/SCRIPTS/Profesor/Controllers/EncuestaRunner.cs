using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Firestore;
using System;


[System.Serializable]
public class ReporteIntento
{
    public string idReporte;
    public string idEncuesta;
    public string idUsuario;
    public string fechaIntento;
    public int respuestasCorrectas;
    public int totalPreguntas;
    public int minimoParaAprobar;
    public string resultadoFinal;
    public string idComunidad; // <--- �A�ADIR ESTA L�NEA!
}

public class EncuestaRunner : MonoBehaviour
{
    // ... (Todas tus variables y referencias a la UI existentes) ...
    [Header("Configuraci�n de la Encuesta (Datos)")]
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


    private Dictionary<Button, OpcionModelo> botonesOpcionActual = new Dictionary<Button, OpcionModelo>();
    private string reportesPath;
    private DatabaseReference dbReference;
    private FirebaseFirestore db;
    public static string ReportesDirectoryPath => Path.Combine(Application.persistentDataPath, "ReportesEncuestas");


    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase inicializado en EncuestaRunner.");
            }
            else
            {
                Debug.LogError($"No se pudieron resolver las dependencias de Firebase: {task.Result}");
            }
        });

        // Definir la ruta de los reportes locales
        reportesPath = Path.Combine(Application.persistentDataPath, "ReportesEncuestas");
        Directory.CreateDirectory(reportesPath);

        // 1. Recuperar la informaci�n guardada desde la escena anterior.
        // Se usa un valor por defecto "" por si las claves no existen.
        string encuestaId = PlayerPrefs.GetString("IDEncuestaParaEjecutar", "");
        string rutaCarpeta = PlayerPrefs.GetString("RutaCarpetaEncuesta", "");

        // 2. Validar que la informaci�n se recuper� correctamente.
        if (string.IsNullOrEmpty(encuestaId) || string.IsNullOrEmpty(rutaCarpeta))
        {
            Debug.LogError("No se encontr� un ID de encuesta o una ruta en PlayerPrefs. No se puede ejecutar la encuesta. " +
                           "Aseg�rate de llegar a esta escena desde la lista de encuestas.");
            // Opcional: Aqu� podr�as activar un panel de error y un bot�n para volver al men�.
            return; // Detiene la ejecuci�n del m�todo si no hay datos.
        }

        // 3. Construir la ruta completa al archivo JSON.
        string filePath = Path.Combine(Application.persistentDataPath, rutaCarpeta, $"{encuestaId}.json");

        // 4. Verificar si el archivo realmente existe en la ruta construida.
        if (File.Exists(filePath))
        {
            // 5. Leer el contenido del archivo y llamar a la funci�n que inicia la encuesta.
            Debug.Log($"Cargando encuesta desde: {filePath}");
            string jsonString = File.ReadAllText(filePath);
            IniciarEncuesta(jsonString);
        }
        else
        {
            Debug.LogError($"�ERROR CR�TICO! El archivo de la encuesta no se encontr� en la ruta esperada: {filePath}");
            // Opcional: Mostrar un panel de error.
        }
    }

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
        listaPreguntas = new List<PreguntaModelo>(encuestaActual.Preguntas);

        if (encuestaActual.AleatorizarPreguntas)
        {
            AleatorizarLista(listaPreguntas);
        }

        panelEncuesta.SetActive(true);
        panelResultados.SetActive(false);
        txtTituloEncuesta.text = encuestaActual.Titulo;

        btnCerrarResultados.onClick.RemoveAllListeners();
        btnCerrarResultados.onClick.AddListener(() => {
            panelResultados.SetActive(false);
            Debug.Log("Encuesta cerrada.");
            SceneManager.LoadScene("Comunidad");
            // Aqu� podr�as redirigir a otra escena, por ejemplo, la del men� principal.
            // SceneManager.LoadScene("MenuPrincipal");
        });

        MostrarPreguntaActual();
    }

    // ... (El resto de tus funciones: MostrarPreguntaActual, OnRespuestaSeleccionada, etc., no cambian)
    private void MostrarPreguntaActual()
    {
        respuestaEnviada = false;

        // �IMPORTANTE! Limpiamos el diccionario antes de llenarlo con los nuevos botones
        botonesOpcionActual.Clear();

        foreach (Transform child in contenedorOpciones)
        {
            Destroy(child.gameObject);
        }

        PreguntaModelo pregunta = listaPreguntas[preguntaActualIndex];

        txtContadorPregunta.text = $"Pregunta {preguntaActualIndex + 1} / {listaPreguntas.Count}";
        txtTextoPregunta.text = pregunta.TextoPregunta;

        List<OpcionModelo> opciones = new List<OpcionModelo>(pregunta.Opciones);
        if (encuestaActual.AleatorizarRespuestas)
        {
            AleatorizarLista(opciones);
        }

        foreach (OpcionModelo opcion in opciones)
        {
            GameObject botonGO = Instantiate(botonOpcionPrefab, contenedorOpciones);
            Button boton = botonGO.GetComponent<Button>();
            TextMeshProUGUI textoBoton = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            textoBoton.text = opcion.Texto;

            // �A�ADIDO! Guardamos la referencia entre el bot�n y su dato
            botonesOpcionActual.Add(boton, opcion);

            // El listener no cambia, sigue funcionando igual
            boton.onClick.AddListener(() => OnRespuestaSeleccionada(opcion, boton));
        }

        if (temporizadorCoroutine != null) StopCoroutine(temporizadorCoroutine);
        temporizadorCoroutine = StartCoroutine(TemporizadorCoroutine(pregunta.TiempoSegundos));
    }

    private void OnRespuestaSeleccionada(OpcionModelo opcionSeleccionada, Button botonPulsado)
    {
        if (respuestaEnviada) return;
        respuestaEnviada = true;

        StopCoroutine(temporizadorCoroutine);

        // Desactivar todos los botones para que no se pueda cambiar la respuesta
        foreach (Button btn in botonesOpcionActual.Keys)
        {
            btn.interactable = false;
        }

        // Comprobar si la respuesta es correcta
        if (opcionSeleccionada.EsCorrecta)
        {
            // El usuario acert�, se pone verde su bot�n
            respuestasCorrectas++;
            botonPulsado.GetComponent<Image>().color = Color.green;
        }
        else
        {
            // El usuario fall�.
            // 1. Se pone en rojo el bot�n que puls�.
            botonPulsado.GetComponent<Image>().color = Color.red;

            // 2. Buscamos y ponemos en verde el bot�n que S� era el correcto.
            foreach (var par in botonesOpcionActual)
            {
                Button botonEnLista = par.Key;
                OpcionModelo opcionEnLista = par.Value;

                if (opcionEnLista.EsCorrecta)
                {
                    // Encontramos la respuesta correcta, la pintamos de verde para que el usuario aprenda.
                    botonEnLista.GetComponent<Image>().color = Color.green;
                    break; // Optimizamos: ya encontramos la correcta, no hace falta seguir buscando.
                }
            }
        }

        // Esperar un poco y pasar a la siguiente pregunta (sin cambios)
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

        Debug.Log("�Tiempo agotado!");
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
        panelResultados.SetActive(true);

        bool aprobado = respuestasCorrectas >= encuestaActual.MinimoPreguntasAprobar;

        if (aprobado)
        {
            txtResultadoTitulo.text = "�Completada!";
            txtResultadoTitulo.color = Color.green;
        }
        else
        {
            txtResultadoTitulo.text = "�Fallaste!";
            txtResultadoTitulo.color = Color.red;
        }

        txtResultadoDetalle.text = $"Respuestas correctas: {respuestasCorrectas} de {listaPreguntas.Count}\n(M�nimo para aprobar: {encuestaActual.MinimoPreguntasAprobar})";

        // El m�todo antiguo se reemplaza por este nuevo flujo
        ProcesarReporte(aprobado ? "Aprobado" : "Reprobado");
    }

    private void ProcesarReporte(string resultado)
    {
        // --- VALIDACI�N EN TIEMPO REAL ---
        string userId = PlayerPrefs.GetString("UserID", null);

        // Obtenemos la ruta completa de la comunidad desde PlayerPrefs
        string rutaComunidadCompleta = PlayerPrefs.GetString("RutaCarpetaEncuesta", null);

        // --- NUEVA L�GICA DE LIMPIEZA ---
        string idComunidad = null;
        if (!string.IsNullOrEmpty(rutaComunidadCompleta))
        {
            // Parte de la ruta a eliminar. Usamos Path.DirectorySeparatorChar para que funcione en Windows ('\') y otros sistemas ('/').
            string parteAEliminar = "EncuestasAsignadas" + Path.DirectorySeparatorChar;

            // Reemplazamos la parte inicial de la ruta por una cadena vac�a.
            idComunidad = rutaComunidadCompleta.Replace(parteAEliminar, "");

            Debug.Log($"Ruta original: '{rutaComunidadCompleta}' -> ID de comunidad limpio: '{idComunidad}'");
        }
        // --- FIN DE LA NUEVA L�GICA ---

        // Doble validaci�n crucial
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idComunidad))
        {
            Debug.LogError("�CR�TICO! Se intent� crear un reporte pero falta informaci�n esencial." +
                           $"\nUserID: '{userId ?? "NULO"}'" +
                           $"\nIDComunidad (limpio): '{idComunidad ?? "NULO"}'" +
                           "\nEl reporte NO se guardar�. Revisa c�mo se guardan estos datos en PlayerPrefs.");
            return; // No se crea el reporte si falta informaci�n.
        }
        // --- FIN DE LA VALIDACI�N ---

        // Ahora creamos el reporte con toda la informaci�n validada.
        ReporteIntentos reporte = new ReporteIntentos
        {
            // ... (el resto del objeto reporte no cambia)
            idReporte = $"{userId}_{System.DateTime.Now:yyyyMMddHHmmssfff}",
            idEncuesta = encuestaActual.Id,
            idUsuario = userId,
            fechaIntento = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            respuestasCorrectas = this.respuestasCorrectas,
            totalPreguntas = listaPreguntas.Count,
            minimoParaAprobar = encuestaActual.MinimoPreguntasAprobar,
            resultadoFinal = resultado
        };

        // La l�gica de decisi�n no cambia
        if (Application.internetReachability != NetworkReachability.NotReachable && db != null)
        {
            Debug.Log("Conexi�n detectada. Subiendo reporte a Firestore...");
            _ = SubirReporteAFirebase(reporte, idComunidad);
        }
        else
        {
            Debug.Log("Sin conexi�n o Firebase no listo. Guardando reporte localmente...");
            GuardarReporteLocalmente(reporte, idComunidad);
        }
    }

    private async Task SubirReporteAFirebase(ReporteIntentos reporte, string idComunidad)
    {
        if (db == null)
        {
            Debug.LogError("Error: Firestore no est� inicializado. Guardando localmente.");
            GuardarReporteLocalmente(reporte, idComunidad);
            return;
        }

        try
        {
            // 1. Asignamos el idComunidad directamente al objeto reporte.
            //    (Aseg�rate de que la propiedad 'idComunidad' exista en tu clase ReporteIntento)
            reporte.idComunidad = idComunidad;

            // 2. Apuntamos al documento por su ID �nico.
            DocumentReference docRef = db.Collection("reportes").Document(reporte.idReporte);

            // 3. Subimos el objeto completo directamente.
            //    Firestore lo convertir� autom�ticamente gracias a los atributos [FirestoreData] y [FirestoreProperty].
            await docRef.SetAsync(reporte);

            // 4. (Opcional pero recomendado) Actualizamos el timestamp por separado.
            //    Esto es m�s limpio que mezclar el objeto con un diccionario.
            await docRef.UpdateAsync("timestamp", FieldValue.ServerTimestamp);

            Debug.Log($"[Firestore] Reporte {reporte.idReporte} para la comunidad '{idComunidad}' subido exitosamente.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firestore] Error al subir el reporte a Firestore: {e.Message}. Guardando localmente como respaldo.");
            // Pasamos el reporte, que ahora incluye el idComunidad, a la funci�n local.
            GuardarReporteLocalmente(reporte, idComunidad); // <-- Ojo: si guardas en JSON, el idComunidad ahora est� dentro del objeto.
        }
    }

    private void GuardarReporteLocalmente(ReporteIntentos reporte, string idComunidad)
    {
        // Asignamos el id de la comunidad al objeto antes de guardarlo.
        reporte.idComunidad = idComunidad;

        string reporteJson = JsonUtility.ToJson(reporte, true);
        string filePath = Path.Combine(ReportesDirectoryPath, $"reporte_{reporte.idReporte}.json");

        try
        {
            File.WriteAllText(filePath, reporteJson);
            Debug.Log($"Reporte (incluyendo comunidad '{idComunidad}') guardado localmente en: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al guardar el reporte local: {e.Message}");
        }
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

[FirestoreData] // <-- Le dice a Firestore que esta clase es un modelo de datos.
public class ReporteIntentos
{
    // [FirestoreProperty] le dice a Firestore que esta propiedad corresponde a un campo
    // en la base de datos con el mismo nombre.
    // { get; set; } permite que el c�digo lea y escriba el valor de la propiedad.

    [FirestoreProperty]
    public string idReporte { get; set; }

    [FirestoreProperty]
    public string idEncuesta { get; set; }

    [FirestoreProperty]
    public string idUsuario { get; set; }

    [FirestoreProperty]
    public string idComunidad { get; set; }

    [FirestoreProperty]
    public string fechaIntento { get; set; }

    [FirestoreProperty]
    public int respuestasCorrectas { get; set; }

    [FirestoreProperty]
    public int totalPreguntas { get; set; }

    [FirestoreProperty]
    public int minimoParaAprobar { get; set; }

    [FirestoreProperty]
    public string resultadoFinal { get; set; }

    // Esta propiedad es especial. El c�digo la usar� para enviar
    // un valor al servidor, pero al leer datos, Firestore la llenar�
    // con la fecha y hora en que se escribi� el documento.
    [FirestoreProperty]
    [ServerTimestamp] // <-- Este atributo maneja el timestamp del servidor autom�ticamente.
    public Timestamp timestamp { get; set; }
}