using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;
using Firebase.Firestore;
using Firebase.Auth;
using System;

public class EncuestaConocimientoController : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoPreguntaUI;
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;
    public GameObject panelFeedback;
    public TextMeshProUGUI textoFeedback;
    public Color colorFondoCorrecto = new Color(0.66f, 0.81f, 0.30f);
    public Color colorFondoIncorrecto = new Color(0.89f, 0.31f, 0.31f);
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    public Color colorNormal = Color.white;
    public Text txtTimer;
    public Text txtRacha;
    public float tiempoInicial = 10f;
    public Slider sliderProgreso;

    private FirebaseAuth authFirebase;
    private FirebaseFirestore firestoreFirebase;
    private FinalizarEncuestaConocimientoUseCase finalizarEncuestaUseCase;
    private ObtenerPreguntasEncuestaUseCase obtenerPreguntasUseCase;
    private List<PreguntaEntity> preguntasFirebase;
    private int indiceActualFirebase = 0;

    private float tiempoRestante;
    private bool preguntaRespondidaFirebase = false;
    private int racha = 0;

    // Estadísticas
    private int correctasAlcalinos = 0;
    private int correctasMetalesAlcalinotérreos = 0;
    private int correctasTransicion = 0;
    private int correctasLantanidos = 0;
    private int correctasActinoides = 0;
    private int correctasMetalesPostransiciales = 0;
    private int correctasMetaloides = 0;
    private int correctasNoMetales = 0;
    private int correctasGasesNobles = 0;
    private int correctasPropiedadesDesconocidas = 0;
    private int incorrectasTotales = 0;
    private float dificultadTotalPreguntas = 0f;
    private int cantidadPreguntasRespondidas = 0;
    private List<Categoria> categorias;

    [System.Serializable]
    public class Categoria
    {
        public string Titulo;
        public string Descripcion;
        public float Porcentaje;

        public Categoria(string nombre, string descripcion)
        {
            Titulo = nombre;
            Descripcion = descripcion;
            Porcentaje = 0f;
        }
    }

    [System.Serializable]
    public class CategoriasData
    {
        public List<Categoria> categorias;
    }

    private async void Start()
    {
        panelFeedback.SetActive(false);
        racha = 0;
        txtRacha.text = "0";
        tiempoRestante = tiempoInicial;

        firestoreFirebase = FirebaseFirestore.DefaultInstance;
        authFirebase = FirebaseAuth.DefaultInstance;

        obtenerPreguntasUseCase = new ObtenerPreguntasEncuestaUseCase(new EncuestaConocimientoFirebase());
        finalizarEncuestaUseCase = new FinalizarEncuestaConocimientoUseCase(
            new FirestoreService(FirebaseServiceLocator.Firestore),
            new FirebaseAuthService(FirebaseServiceLocator.Auth)
        );

        preguntasFirebase = await obtenerPreguntasUseCase.EjecutarAsync();
        indiceActualFirebase = 0;

        // ─── Inicializar Slider de progreso en 0 y definir maxValue ───
        if (sliderProgreso != null && preguntasFirebase.Count > 0)
        {
            sliderProgreso.minValue = 0f;
            sliderProgreso.maxValue = preguntasFirebase.Count;
            sliderProgreso.value = 0f;
        }

        categorias = new List<Categoria>
{
    new Categoria("Metales Alcalinos",
        "¡Explora a los más reactivos de la tabla! Los metales alcalinos son tan activos que necesitan estar bajo aceite para no reaccionar con el aire. Livianos, brillantes y explosivos con el agua: ¡una aventura química garantizada!"),

    new Categoria("Metales Alcalinotérreos",
        "¡Estables pero sorprendentes! Estos metales no son tan impulsivos como los alcalinos, pero también saben cómo llamar la atención. Presentes en nuestros huesos, fuegos artificiales y más, ¡prepárate para descubrir su versatilidad!"),

    new Categoria("Metales de Transición",
        "¡Los verdaderos camaleones de la química! Dominan el arte de formar compuestos coloridos, catalizar reacciones y construir estructuras resistentes. Si te gustan los desafíos y los cambios, esta es tu categoría."),

    new Categoria("Metales postransicionales",
        "¡No subestimes a los discretos! Aunque menos conocidos, estos elementos son vitales para la tecnología moderna. Suavemente maleables, conductores y con usos cotidianos, ¡descubre su impacto silencioso!"),

    new Categoria("Metaloides",
        "¡En el límite entre dos mundos! Los metaloides tienen propiedades tanto de metales como de no metales. Impredecibles, interesantes y esenciales en la electrónica, ¡perfectos para quienes aman lo inesperado!"),

    new Categoria("No Metales",
        "¡Los pilares de la vida y la química orgánica! Desde el oxígeno que respiras hasta el carbono de tu ADN, los no metales son esenciales para todo lo que vive. ¡Investiga su papel crucial en el universo!"),

    new Categoria("Gases Nobles",
        "¡Silenciosos, invisibles e invaluables! Estos elementos no reaccionan fácilmente, pero están presentes en luces, atmósferas protectoras y experimentos científicos. ¡Su estabilidad es su superpoder!"),

    new Categoria("Lantánidos",
        "¡Los metales raros que mueven el mundo moderno! Utilizados en imanes potentes, láseres y pantallas de alta tecnología. Aunque raros, su presencia es fundamental en nuestra vida diaria. ¡Descúbrelos!"),

    new Categoria("Actinoides",
        "¡La energía más poderosa de la tabla! Radiactivos, misteriosos y con potencial para revolucionar el mundo, estos elementos están ligados a la energía nuclear y la exploración científica del futuro."),

    new Categoria("Propiedades desconocidas",
        "¡Bienvenido al territorio inexplorado! Estos elementos están en los límites de lo conocido. Sus propiedades aún se investigan, y cada descubrimiento puede cambiar lo que sabemos. ¿Te atreves a descubrir lo desconocido?")
};


        MostrarPreguntaFirebase();
    }

    private void Update()
    {
        if (preguntaRespondidaFirebase) return;

        tiempoRestante -= Time.deltaTime;
        txtTimer.text = $"{(int)tiempoRestante} Segundos";

        if (tiempoRestante <= 0f)
        {
            preguntaRespondidaFirebase = true;
            MostrarResultadoFirebase(false);
        }
    }

    private void MostrarPreguntaFirebase()
    {
        // ─── Actualizar Slider antes de mostrar la pregunta ───
        if (sliderProgreso != null && preguntasFirebase.Count > 0)
        {
            sliderProgreso.value = Mathf.Clamp(indiceActualFirebase, 0, preguntasFirebase.Count);
        }

        if (indiceActualFirebase >= preguntasFirebase.Count)
        {
            FinalizarEncuestaFirebase();
            return;
        }

        var pregunta = preguntasFirebase[indiceActualFirebase];
        textoPreguntaUI.text = pregunta.Texto;

        var opcionesAleatorias = AleatorizarOpcionesFirebase(pregunta.Opciones, pregunta.IndiceCorrecto);
        var respuestaCorrecta = pregunta.Opciones[pregunta.IndiceCorrecto];
        pregunta.IndiceCorrecto = opcionesAleatorias.IndexOf(respuestaCorrecta);

        // Configurar toggles y listeners
        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            opcionesToggleUI[i].onValueChanged.RemoveAllListeners();

            if (i < opcionesAleatorias.Count)
            {
                opcionesToggleUI[i].gameObject.SetActive(true);
                opcionesToggleUI[i].GetComponentInChildren<TextMeshProUGUI>().text = opcionesAleatorias[i];
                opcionesToggleUI[i].isOn = false;
                opcionesToggleUI[i].image.color = colorNormal;
                opcionesToggleUI[i].interactable = true;

                int index = i;
                opcionesToggleUI[i].onValueChanged.AddListener((bool isOn) =>
                {
                    if (isOn && !preguntaRespondidaFirebase)
                    {
                        OnRespuestaSeleccionadaFirebase(index);
                    }
                });
            }
            else
            {
                opcionesToggleUI[i].gameObject.SetActive(false);
            }
        }

        preguntaRespondidaFirebase = false;
        tiempoRestante = tiempoInicial;
    }

    public void OnRespuestaSeleccionadaFirebase(int indice)
    {
        if (preguntaRespondidaFirebase) return;

        bool esCorrecta = (indice == preguntasFirebase[indiceActualFirebase].IndiceCorrecto);
        preguntaRespondidaFirebase = true;
        DesactivarInteractividadOpcionesFirebase();
        MostrarResultadoFirebase(esCorrecta);
    }

    private void DesactivarInteractividadOpcionesFirebase()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = false;
        }
    }

    private void MostrarResultadoFirebase(bool correcta)
    {
        if (correcta)
        {
            racha++;
            txtRacha.text = racha.ToString();
        }
        else
        {
            racha = 0;
            txtRacha.text = "0";
        }

        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            if (!opcionesToggleUI[i].gameObject.activeSelf) continue;

            if (i == preguntasFirebase[indiceActualFirebase].IndiceCorrecto)
                opcionesToggleUI[i].image.color = colorCorrecto;
            else if (opcionesToggleUI[i].isOn)
                opcionesToggleUI[i].image.color = colorIncorrecto;
            else
                opcionesToggleUI[i].image.color = colorNormal;
        }

        panelFeedback.SetActive(true);
        textoFeedback.text = correcta ? "Correcto" : "Incorrecto";
        panelFeedback.GetComponent<Image>().color = correcta ? colorFondoCorrecto : colorFondoIncorrecto;

        var preguntaActual = preguntasFirebase[indiceActualFirebase];
        dificultadTotalPreguntas += preguntaActual.Dificultad;
        cantidadPreguntasRespondidas++;

        if (correcta)
        {
            switch (preguntaActual.Grupo)
            {
                case "Metales Alcalinos": correctasAlcalinos++; break;
                case "Metales Alcalinotérreos": correctasMetalesAlcalinotérreos++; break;
                case "Metales de Transición": correctasTransicion++; break;
                case "Metales postransicionales": correctasMetalesPostransiciales++; break;
                case "Metaloides": correctasMetaloides++; break;
                case "No Metales": correctasNoMetales++; break;
                case "Gases Nobles": correctasGasesNobles++; break;
                case "Lantánidos": correctasLantanidos++; break;
                case "Actinoides": correctasActinoides++; break;
                case "Propiedades desconocidas": correctasPropiedadesDesconocidas++; break;
                default: Debug.LogWarning($"Grupo desconocido: {preguntaActual.Grupo}"); break;
            }
        }
        else
        {
            incorrectasTotales++;
        }

        Invoke(nameof(OcultarFeedbackYContinuarFirebase), 1.5f);
    }

    private void OcultarFeedbackYContinuarFirebase()
    {
        panelFeedback.SetActive(false);
        indiceActualFirebase++;
        MostrarPreguntaFirebase();
    }

    private List<string> AleatorizarOpcionesFirebase(List<string> opciones, int indiceCorrecto)
    {
        List<string> opcionesAleatorias = new List<string>(opciones);
        if (indiceCorrecto < 0 || indiceCorrecto >= opcionesAleatorias.Count)
        {
            Debug.LogError("Índice de respuesta correcta fuera de rango: " + indiceCorrecto + ". Se asignará índice 0 por defecto.");
            indiceCorrecto = 0;
        }
        string respuestaCorrecta = opcionesAleatorias[indiceCorrecto];

        for (int i = 0; i < opcionesAleatorias.Count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, opcionesAleatorias.Count);
            string temp = opcionesAleatorias[randomIndex];
            opcionesAleatorias[randomIndex] = opcionesAleatorias[i];
            opcionesAleatorias[i] = temp;
        }

        if (!opcionesAleatorias.Contains(respuestaCorrecta))
        {
            opcionesAleatorias[0] = respuestaCorrecta;
        }
        return opcionesAleatorias;
    }

    private async void FinalizarEncuestaFirebase()
    {
        Debug.Log("Encuesta de conocimiento finalizada (Firebase).");

        int totalCorrectas = correctasAlcalinos
                           + correctasMetalesAlcalinotérreos
                           + correctasTransicion
                           + correctasLantanidos
                           + correctasActinoides
                           + correctasMetalesPostransiciales
                           + correctasMetaloides
                           + correctasNoMetales
                           + correctasGasesNobles
                           + correctasPropiedadesDesconocidas;
        int totalRespuestas = totalCorrectas + incorrectasTotales;
        float porcentajeGlobal = (totalRespuestas > 0)
            ? ((float)totalCorrectas / totalRespuestas) * 100f
            : 0f;

        float dificultadMedia = (cantidadPreguntasRespondidas > 0)
            ? (dificultadTotalPreguntas / cantidadPreguntasRespondidas)
            : 0f;

        Debug.Log($"[Estadísticas] Porcentaje global de aciertos: {porcentajeGlobal:F2}%");
        Debug.Log($"[Estadísticas] Dificultad media: {dificultadMedia:F2}");

        float[] features = new float[]
        {
            correctasAlcalinos,
            correctasMetalesAlcalinotérreos,
            correctasTransicion,
            correctasLantanidos,
            correctasActinoides,
            correctasMetalesPostransiciales,
            correctasMetaloides,
            correctasNoMetales,
            correctasGasesNobles,
            correctasPropiedadesDesconocidas,
            incorrectasTotales,
            dificultadMedia
        };

        var modeloAI = GetComponent<ModeloAI>();
        if (modeloAI != null)
        {
            float[] predictionResult = modeloAI.RunInference(features);
            ProcesarPrediccionDeConocimientoFirebase(predictionResult);
        }
        else
        {
            Debug.LogWarning("[Predicción] No se encontró ModeloAI; se omite predicción.");
        }

        GuardarCategoriasOrdenadasLocal();

        await finalizarEncuestaUseCase.Ejecutar();

        bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoConocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (estadoAprendizaje && estadoConocimiento)
            SceneManager.LoadScene("Categorías");
        else
            SceneManager.LoadScene("SeleccionarEncuesta");
    }

    private void ProcesarPrediccionDeConocimientoFirebase(float[] predictions)
    {
        float umbral = 0.5f;
        for (int i = 0; i < predictions.Length && i < categorias.Count; i++)
        {
            float porcentaje = predictions[i] * 100f;
            categorias[i].Porcentaje = porcentaje;
            if (predictions[i] > umbral)
                Debug.Log($"[Predicción] CONOCE {categorias[i].Titulo}: {porcentaje:F2}%");
            else
                Debug.Log($"[Predicción] NO CONOCE {categorias[i].Titulo}: {porcentaje:F2}%");
        }
    }

    private void GuardarCategoriasOrdenadasLocal()
    {
        if (categorias == null)
        {
            Debug.LogError("[GuardarCategorias] La lista de categorías es null.");
            return; // Salir si la lista es null
        }
        try
        {
            // Ordenar las categorías por porcentaje
            categorias = categorias.OrderBy(c => c.Porcentaje).ToList();
            // Crear el objeto de datos para la serialización
            CategoriasData data = new CategoriasData { categorias = categorias };
            // Serializar a JSON
            string json = JsonUtility.ToJson(data, true);
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                // Hay conexión a internet: guardar en archivo
                string rutaArchivo = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
                File.WriteAllText(rutaArchivo, json);
                Debug.Log("✅ Categorías ordenadas guardadas en archivo: " + rutaArchivo);
            }
            else
            {
                // No hay conexión a internet: guardar en PlayerPrefs
                PlayerPrefs.SetString("categorias_encuesta_firebase_json", json);
                PlayerPrefs.Save();
                Debug.Log("✅ Categorías ordenadas guardadas en PlayerPrefs.");
            }
            // Iniciar la corrutina (asegúrate de que también maneje errores)
            StartCoroutine(CopiarJsonAuxiliaresSiEsNecesario());
        }
        catch (Exception e)
        {
            Debug.LogError($"[GuardarCategorias] Error al guardar las categorías: {e.Message}");
        }
    }

    private IEnumerator CopiarJsonAuxiliaresSiEsNecesario()
    {
        List<string> nombresArchivos = new List<string>
        {
            "Json_Misiones.json",
            "Json_Logros.json",
            "Json_Informacion.json"
        };

        foreach (string nombreArchivo in nombresArchivos)
        {
            string rutaStreaming = Path.Combine(Application.streamingAssetsPath, nombreArchivo);
            string rutaLocal = Path.Combine(Application.persistentDataPath, nombreArchivo);

            if (!File.Exists(rutaLocal))
            {
                using (UnityWebRequest request = UnityWebRequest.Get(rutaStreaming))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        File.WriteAllText(rutaLocal, request.downloadHandler.text);
                        Debug.Log($"✅ (Auxiliar) Archivo copiado localmente: {nombreArchivo}");
                    }
                    else
                    {
                        Debug.LogError($"❌ (Auxiliar) Error al copiar {nombreArchivo}: {request.error}");
                    }
                }
            }
            else
            {
                Debug.Log($"📁 (Auxiliar) Ya existe localmente: {nombreArchivo}");
            }
        }
    }
}
