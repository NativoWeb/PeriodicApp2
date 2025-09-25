using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Core.Domain.Entities;
using PeriodicApp.Core.Domain.Interfaces;
using PeriodicApp.Infrastructure.Services;
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

    // EstadÃ­sticas
    private int correctasAlcalinos = 0;
    private int correctasMetalesAlcalinoterreos = 0;
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
    private IServicioLocalStorage localStorage;


    private SubirDatosJSON subirDatosJSONUseCase;

    [System.Serializable]
    public class Categoria
    {
        public string Titulo;
        public string Titulo_en;
        public string Descripcion;
        public string Descripcion_en;
        public float Porcentaje;

        public Categoria(string titulo, string titulo_en, string descripcion, string descripcion_en)
        {
            Titulo = titulo;
            Titulo_en = titulo_en;
            Descripcion = descripcion;
            Descripcion_en = descripcion_en;
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

        // âââ Inicializar Slider de progreso en 0 y definir maxValue âââ
        if (sliderProgreso != null && preguntasFirebase.Count > 0)
        {
            sliderProgreso.minValue = 0f;
            sliderProgreso.maxValue = preguntasFirebase.Count;
            sliderProgreso.value = 0f;
        }

        localStorage = new LocalStorageService();
        var firestore = new FirestoreService(FirebaseServiceLocator.Firestore);
        subirDatosJSONUseCase = new SubirDatosJSON(firestore, localStorage);

        categorias = new List<Categoria>
{
    new Categoria(
        // EspaÃ±ol
        "Metales Alcalinos",
        // InglÃ©s
        "Alkali Metals",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Explora a los mÃ¡s reactivos de la tabla! Los metales alcalinos son tan activos que necesitan estar bajo aceite para no reaccionar con el aire. Livianos, brillantes y explosivos con el agua: Â¡una aventura quÃ­mica garantizada!",
        // DescripciÃ³n InglÃ©s
        "Explore the most reactive on the table! Alkali metals are so active they need to be stored under oil to avoid reacting with the air. Lightweight, shiny, and explosive with water: a chemical adventure is guaranteed!"
    ),

    new Categoria(
        // EspaÃ±ol
        "Metales AlcalinotÃ©rreos",
        // InglÃ©s
        "Alkaline Earth Metals",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Estables pero sorprendentes! Estos metales no son tan impulsivos como los alcalinos, pero tambiÃ©n saben cÃ³mo llamar la atenciÃ³n. Presentes en nuestros huesos, fuegos artificiales y mÃ¡s, Â¡prepÃ¡rate para descubrir su versatilidad!",
        // DescripciÃ³n InglÃ©s
        "Stable but surprising! These metals aren't as impulsive as the alkali metals, but they also know how to grab attention. Found in our bones, fireworks, and more, get ready to discover their versatility!"
    ),

    new Categoria(
        // EspaÃ±ol
        "Metales de TransiciÃ³n",
        // InglÃ©s
        "Transition Metals",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Los verdaderos camaleones de la quÃ­mica! Dominan el arte de formar compuestos coloridos, catalizar reacciones y construir estructuras resistentes. Si te gustan los desafÃ­os y los cambios, esta es tu categorÃ­a.",
        // DescripciÃ³n InglÃ©s
        "The true chameleons of chemistry! They master the art of forming colorful compounds, catalyzing reactions, and building strong structures. If you like challenges and change, this is your category."
    ),

    new Categoria(
        // EspaÃ±ol
        "Metales postransicionales",
        // InglÃ©s
        "Post-transition Metals",
        // DescripciÃ³n EspaÃ±ol
        "Â¡No subestimes a los discretos! Aunque menos conocidos, estos elementos son vitales para la tecnologÃ­a moderna. Suavemente maleables, conductores y con usos cotidianos, Â¡descubre su impacto silencioso!",
        // DescripciÃ³n InglÃ©s
        "Don't underestimate the discreet ones! Although less known, these elements are vital for modern technology. Softly malleable, conductive, and with everyday uses, discover their silent impact!"
    ),

    new Categoria(
        // EspaÃ±ol
        "Metaloides",
        // InglÃ©s
        "Metalloids",
        // DescripciÃ³n EspaÃ±ol
        "Â¡En el lÃ­mite entre dos mundos! Los metaloides tienen propiedades tanto de metales como de no metales. Impredecibles, interesantes y esenciales en la electrÃ³nica, Â¡perfectos para quienes aman lo inesperado!",
        // DescripciÃ³n InglÃ©s
        "On the edge between two worlds! Metalloids have properties of both metals and non-metals. Unpredictable, interesting, and essential in electronics, perfect for those who love the unexpected!"
    ),

    new Categoria(
        // EspaÃ±ol
        "No Metales",
        // InglÃ©s
        "Nonmetals",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Los pilares de la vida y la quÃ­mica orgÃ¡nica! Desde el oxÃ­geno que respiras hasta el carbono de tu ADN, los no metales son esenciales para todo lo que vive. Â¡Investiga su papel crucial en el universo!",
        // DescripciÃ³n InglÃ©s
        "The pillars of life and organic chemistry! From the oxygen you breathe to the carbon in your DNA, nonmetals are essential for everything that lives. Investigate their crucial role in the universe!"
    ),

    new Categoria(
        // EspaÃ±ol
        "Gases Nobles",
        // InglÃ©s
        "Noble Gases",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Silenciosos, invisibles e invaluables! Estos elementos no reaccionan fÃ¡cilmente, pero estÃ¡n presentes en luces, atmÃ³sferas protectoras y experimentos cientÃ­ficos. Â¡Su estabilidad es su superpoder!",
        // DescripciÃ³n InglÃ©s
        "Silent, invisible, and invaluable! These elements don't react easily, but they are present in lights, protective atmospheres, and scientific experiments. Their stability is their superpower!"
    ),

    new Categoria(
        // EspaÃ±ol
        "LantÃ¡nidos",
        // InglÃ©s
        "Lanthanides",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Los metales raros que mueven el mundo moderno! Utilizados en imanes potentes, lÃ¡seres y pantallas de alta tecnologÃ­a. Aunque raros, su presencia es fundamental en nuestra vida diaria. Â¡DescÃºbrelos!",
        // DescripciÃ³n InglÃ©s
        "The rare metals that move the modern world! Used in powerful magnets, lasers, and high-tech screens. Although rare, their presence is fundamental in our daily lives. Discover them!"
    ),

    new Categoria(
        // EspaÃ±ol
        "Actinoides",
        // InglÃ©s
        "Actinides",
        // DescripciÃ³n EspaÃ±ol
        "Â¡La energÃ­a mÃ¡s poderosa de la tabla! Radiactivos, misteriosos y con potencial para revolucionar el mundo, estos elementos estÃ¡n ligados a la energÃ­a nuclear y la exploraciÃ³n cientÃ­fica del futuro.",
        // DescripciÃ³n InglÃ©s
        "The most powerful energy on the table! Radioactive, mysterious, and with the potential to revolutionize the world, these elements are linked to nuclear energy and the scientific exploration of the future."
    ),

    new Categoria(
        // EspaÃ±ol
        "Propiedades desconocidas",
        // InglÃ©s
        "Unknown Properties",
        // DescripciÃ³n EspaÃ±ol
        "Â¡Bienvenido al territorio inexplorado! Estos elementos estÃ¡n en los lÃ­mites de lo conocido. Sus propiedades aÃºn se investigan, y cada descubrimiento puede cambiar lo que sabemos. Â¿Te atreves a descubrir lo desconocido?",
        // DescripciÃ³n InglÃ©s
        "Welcome to unexplored territory! These elements are at the limits of what is known. Their properties are still being investigated, and each discovery can change what we know. Do you dare to discover the unknown?"
    )
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
        // âââ Actualizar Slider antes de mostrar la pregunta âââ
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
                case "Metales AlcalinotÃ©rreos": correctasMetalesAlcalinoterreos++; break;
                case "Metales de TransiciÃ³n": correctasTransicion++; break;
                case "Metales postransicionales": correctasMetalesPostransiciales++; break;
                case "Metaloides": correctasMetaloides++; break;
                case "No Metales": correctasNoMetales++; break;
                case "Gases Nobles": correctasGasesNobles++; break;
                case "LantÃ¡nidos": correctasLantanidos++; break;
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
            Debug.LogError("Ãndice de respuesta correcta fuera de rango: " + indiceCorrecto + ". Se asignarÃ¡ Ã­ndice 0 por defecto.");
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
                           + correctasMetalesAlcalinoterreos
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

        Debug.Log($"[EstadÃ­sticas] Porcentaje global de aciertos: {porcentajeGlobal:F2}%");
        Debug.Log($"[EstadÃ­sticas] Dificultad media: {dificultadMedia:F2}");

        float[] features = new float[]
        {
            correctasAlcalinos,
            correctasMetalesAlcalinoterreos,
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
            Debug.LogWarning("[PredicciÃ³n] No se encontrÃ³ ModeloAI; se omite predicciÃ³n.");
        }

        GuardarCategoriasOrdenadasLocal();

        await finalizarEncuestaUseCase.EjecutarAsync();

        bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoConocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (estadoAprendizaje && estadoConocimiento)
            SceneManager.LoadScene("Inicio");
        else
            SceneManager.LoadScene("SeleccionarEncuesta");
    }

    private void ProcesarPrediccionDeConocimientoFirebase(float[] predictions)
    {
        for (int i = 0; i < predictions.Length && i < categorias.Count; i++)
        {
            float porcentaje = predictions[i] * 100f;
            categorias[i].Porcentaje = porcentaje;
        }
    }

    private void GuardarCategoriasOrdenadasLocal()
    {
        if (categorias == null)
        {
            Debug.LogError("[GuardarCategorias] La lista de categorÃ­as es null.");
            return; // Salir si la lista es null
        }
        try
        {
            // Ordenar las categorÃ­as por porcentaje
            categorias = categorias.OrderBy(c => c.Porcentaje).ToList();

            // Crear el objeto de datos para la serializaciÃ³n
            CategoriasData data = new CategoriasData { categorias = categorias };
            // Serializar a JSON
            string json = JsonUtility.ToJson(data, true);

            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                // Hay conexiÃ³n a internet: guardar en archivo
                string rutaArchivo = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
                File.WriteAllText(rutaArchivo, json);

                Debug.Log("La CategorÃ­as ordenadas guardadas en archivo: " + rutaArchivo);
            }
            else
            {
                // No hay conexiÃ³n a internet: guardar en PlayerPrefs
                PlayerPrefs.SetString("categorias_encuesta_firebase_json", json);
                PlayerPrefs.Save();
                Debug.Log("Las CategorÃ­as ordenadas guardadas en PlayerPrefs.");
            }
            // Iniciar la corrutina (asegÃºrate de que tambiÃ©n maneje errores)
            StartCoroutine(CopiarJsonAuxiliaresSiEsNecesario());
        }
        catch (Exception e)
        {
            Debug.LogError($"[GuardarCategorias] Error al guardar las categorÃ­as: {e.Message}");
        }
    }

    private IEnumerator CopiarJsonAuxiliaresSiEsNecesario()
    {
        List<string> nombresArchivos = new List<string>
    {
        "Json_Misiones.json",
        "Json_Logros.json",
        "Json_Informacion.json",
        "Json_Informacion_en.json"
    };

        foreach (string nombreArchivo in nombresArchivos)
        {
            string rutaLocal = Path.Combine(Application.persistentDataPath, nombreArchivo);

            // Verificar si el archivo ya existe en la ruta persistente
            if (File.Exists(rutaLocal))
            {
                Debug.Log($"ð (Auxiliar) Ya existe localmente: {nombreArchivo}");
            }
            else
            {
                // Cargar el archivo desde Resources si no existe localmente
                string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
                TextAsset archivoJson = Resources.Load<TextAsset>($"Plantillas_Json/{nombreSinExtension}");

                if (archivoJson != null)
                {
                    File.WriteAllText(rutaLocal, archivoJson.text);
                    Debug.Log($" (Auxiliar) Archivo copiado desde Resources: {nombreArchivo}");
                }
                else
                {
                    Debug.LogError($"â (Auxiliar) No se encontrÃ³ {nombreArchivo} en Resources/Plantillas_Json.");
                }
            }
            // Pausar un frame entre cada archivo por seguridad
            yield return null;
        }

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            yield return subirDatosJSONUseCase.Ejecutar();
        }
    }
}
