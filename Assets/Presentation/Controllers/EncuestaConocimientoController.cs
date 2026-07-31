using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Core.Domain.Interfaces;
using PeriodicApp.Infrastructure.Services;
using PeriodicApp.Presentation;
using System.IO;
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

    private int correctasAlcalinos = 0;
    private int correctasMetalesAlcalinoterreos = 0;
    private int correctasTransicion = 0;
    private int correctasLantanidos = 0;
    private int correctasActinoides = 0;
    private int correctasMetalesPostransicionales = 0;
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
        if (!ServiceLocator.AreServicesInitialized())
        {
            ServiceLocator.Logger.LogError("ServiceLocator no esta inicializado");
            return;
        }
        panelFeedback.SetActive(false);
        racha = 0;
        txtRacha.text = "0";
        tiempoRestante = tiempoInicial;

        firestoreFirebase = FirebaseFirestore.DefaultInstance;
        authFirebase = FirebaseAuth.DefaultInstance;

        obtenerPreguntasUseCase = new ObtenerPreguntasEncuestaUseCase(new EncuestaConocimientoFirebase());
        finalizarEncuestaUseCase = new FinalizarEncuestaConocimientoUseCase(
            new FirestoreService(FirebaseServiceLocator.Firestore),
            new FirebaseAuthService(FirebaseServiceLocator.Auth),
            ServiceLocator.PlayerPrefs,
            ServiceLocator.Network,
            ServiceLocator.Logger,
            ServiceLocator.Scene
        );

        preguntasFirebase = await obtenerPreguntasUseCase.EjecutarAsync();
        indiceActualFirebase = 0;

        if (sliderProgreso != null && preguntasFirebase.Count > 0)
        {
            sliderProgreso.minValue = 0f;
            sliderProgreso.maxValue = preguntasFirebase.Count;
            sliderProgreso.value = 0f;
        }

        localStorage = new LocalStorageService();
        var firestore = new FirestoreService(FirebaseServiceLocator.Firestore);
        subirDatosJSONUseCase = new SubirDatosJSON(
            firestore,
            localStorage,
            ServiceLocator.Persistence
        );

        InicializarCategorias();
        MostrarPreguntaFirebase();
    }

    private void InicializarCategorias()
    {
        categorias = new List<Categoria>
        {
            new Categoria("Metales Alcalinos", "Alkali Metals",
                "Explora a los mas reactivos de la tabla! Los metales alcalinos son tan activos que necesitan estar bajo aceite para no reaccionar con el aire.",
                "Explore the most reactive on the table! Alkali metals are so active they need to be stored under oil."),

            new Categoria("Metales Alcalinoterreos", "Alkaline Earth Metals",
                "Estables pero sorprendentes! Estos metales no son tan impulsivos como los alcalinos.",
                "Stable but surprising! These metals aren't as impulsive as the alkali metals."),

            new Categoria("Metales de Transicion", "Transition Metals",
                "Los verdaderos camaleones de la quimica! Dominan el arte de formar compuestos coloridos.",
                "The true chameleons of chemistry! They master the art of forming colorful compounds."),

            new Categoria("Metales postransicionales", "Post-transition Metals",
                "No subestimes a los discretos! Aunque menos conocidos, estos elementos son vitales.",
                "Don't underestimate the discreet ones! Although less known, these elements are vital."),

            new Categoria("Metaloides", "Metalloids",
                "En el limite entre dos mundos! Los metaloides tienen propiedades tanto de metales como de no metales.",
                "On the edge between two worlds! Metalloids have properties of both metals and non-metals."),

            new Categoria("No Metales", "Nonmetals",
                "Los pilares de la vida y la quimica organica! Desde el oxigeno que respiras hasta el carbono de tu ADN.",
                "The pillars of life and organic chemistry! From the oxygen you breathe to the carbon in your DNA."),

            new Categoria("Gases Nobles", "Noble Gases",
                "Silenciosos, invisibles e invaluables! Estos elementos no reaccionan facilmente.",
                "Silent, invisible, and invaluable! These elements don't react easily."),

            new Categoria("Lantanidos", "Lanthanides",
                "Los metales raros que mueven el mundo moderno! Utilizados en imanes potentes.",
                "The rare metals that move the modern world! Used in powerful magnets."),

            new Categoria("Actinoides", "Actinides",
                "La energia mas poderosa de la tabla! Radiactivos, misteriosos y con potencial.",
                "The most powerful energy on the table! Radioactive, mysterious, and with potential."),

            new Categoria("Propiedades desconocidas", "Unknown Properties",
                "Bienvenido al territorio inexplorado! Estos elementos estan en los limites de lo conocido.",
                "Welcome to unexplored territory! These elements are at the limits of what is known.")
        };
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

        var canvasGroupPregunta = textoPreguntaUI.GetComponent<CanvasGroup>();
        if (canvasGroupPregunta == null)
            canvasGroupPregunta = textoPreguntaUI.gameObject.AddComponent<CanvasGroup>();
        canvasGroupPregunta.alpha = 0f;
        StartCoroutine(FadeCanvasGroup(canvasGroupPregunta, 1f, 0.2f));

        var opcionesAleatorias = AleatorizarOpcionesFirebase(pregunta.Opciones, pregunta.IndiceCorrecto);
        var respuestaCorrecta = pregunta.Opciones[pregunta.IndiceCorrecto];
        pregunta.IndiceCorrecto = opcionesAleatorias.IndexOf(respuestaCorrecta);

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

                var toggleCg = opcionesToggleUI[i].GetComponent<CanvasGroup>();
                if (toggleCg == null)
                    toggleCg = opcionesToggleUI[i].gameObject.AddComponent<CanvasGroup>();
                toggleCg.alpha = 0f;
                StartCoroutine(FadeCanvasGroup(toggleCg, 1f, 0.2f, 0.05f * i));

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
            {
                opcionesToggleUI[i].image.color = colorCorrecto;
                if (correcta)
                    StartCoroutine(PunchScale(opcionesToggleUI[i].transform, 0.1f, 0.3f));
            }
            else if (opcionesToggleUI[i].isOn)
            {
                opcionesToggleUI[i].image.color = colorIncorrecto;
                StartCoroutine(ShakePosition(opcionesToggleUI[i].transform, 10f, 0.3f));
            }
            else
            {
                opcionesToggleUI[i].image.color = colorNormal;
            }
        }

        panelFeedback.GetComponent<Image>().color = correcta ? colorFondoCorrecto : colorFondoIncorrecto;
        textoFeedback.text = correcta ? "Correcto" : "Incorrecto";
        panelFeedback.SetActive(true);
        StartCoroutine(FadeCanvasGroupForPanel(panelFeedback, 1f, 0.2f));

        if (racha >= 3)
            StartCoroutine(PunchScale(txtRacha.transform, 0.2f, 0.3f));

        var preguntaActual = preguntasFirebase[indiceActualFirebase];
        dificultadTotalPreguntas += preguntaActual.Dificultad;
        cantidadPreguntasRespondidas++;

        if (correcta)
        {
            switch (preguntaActual.Grupo)
            {
                case "Metales Alcalinos": correctasAlcalinos++; break;
                case "Metales Alcalinotérreos": correctasMetalesAlcalinoterreos++; break;
                case "Metales de Transición": correctasTransicion++; break;
                case "Metales postransicionales": correctasMetalesPostransicionales++; break;
                case "Metaloides": correctasMetaloides++; break;
                case "No Metales": correctasNoMetales++; break;
                case "Gases Nobles": correctasGasesNobles++; break;
                case "Lantánidos": correctasLantanidos++; break;
                case "Actinoides": correctasActinoides++; break;
                case "Propiedades desconocidas": correctasPropiedadesDesconocidas++; break;
                default: ServiceLocator.Logger.LogWarning($"Grupo desconocido: {preguntaActual.Grupo}"); break;
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
        StartCoroutine(FadeCanvasGroupForPanel(panelFeedback, 0f, 0.15f, 0f, () =>
        {
            panelFeedback.SetActive(false);
            indiceActualFirebase++;
            MostrarPreguntaFirebase();
        }));
    }

    #region Animaciones con Coroutines

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float start = cg.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        cg.alpha = target;
    }

    private IEnumerator FadeCanvasGroupForPanel(GameObject panel, float target, float duration, float delay = 0f, Action onComplete = null)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        yield return FadeCanvasGroup(cg, target, duration, delay);
        onComplete?.Invoke();
    }

    private IEnumerator PunchScale(Transform t, float force, float duration)
    {
        Vector3 original = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float wave = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
            t.localScale = original + Vector3.one * wave * force;
            yield return null;
        }
        t.localScale = original;
    }

    private IEnumerator ShakePosition(Transform t, float force, float duration)
    {
        Vector3 original = t.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float decay = 1f - progress;
            float offsetX = UnityEngine.Random.Range(-force, force) * decay;
            t.localPosition = original + new Vector3(offsetX, 0f, 0f);
            yield return null;
        }
        t.localPosition = original;
    }

    #endregion

    private List<string> AleatorizarOpcionesFirebase(List<string> opciones, int indiceCorrecto)
    {
        List<string> opcionesAleatorias = new List<string>(opciones);
        if (indiceCorrecto < 0 || indiceCorrecto >= opcionesAleatorias.Count)
        {
            ServiceLocator.Logger.LogError("Indice de respuesta correcta fuera de rango: " + indiceCorrecto);
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
        ServiceLocator.Logger.Log("Encuesta de conocimiento finalizada (Firebase).");

        int totalCorrectas = correctasAlcalinos + correctasMetalesAlcalinoterreos + correctasTransicion +
                           correctasLantanidos + correctasActinoides + correctasMetalesPostransicionales +
                           correctasMetaloides + correctasNoMetales + correctasGasesNobles +
                           correctasPropiedadesDesconocidas;

        int totalRespuestas = totalCorrectas + incorrectasTotales;
        float porcentajeGlobal = (totalRespuestas > 0) ? ((float)totalCorrectas / totalRespuestas) * 100f : 0f;
        float dificultadMedia = (cantidadPreguntasRespondidas > 0) ? (dificultadTotalPreguntas / cantidadPreguntasRespondidas) : 0f;

        ServiceLocator.Logger.Log($"[Estadisticas] Porcentaje global: {porcentajeGlobal:F2}%");
        ServiceLocator.Logger.Log($"[Estadisticas] Dificultad media: {dificultadMedia:F2}");

        float[] features = new float[]
        {
            correctasAlcalinos, correctasMetalesAlcalinoterreos, correctasTransicion,
            correctasLantanidos, correctasActinoides, correctasMetalesPostransicionales,
            correctasMetaloides, correctasNoMetales, correctasGasesNobles,
            correctasPropiedadesDesconocidas, incorrectasTotales, dificultadMedia
        };

        var modeloAI = GetComponent<ModeloAI>();
        if (modeloAI != null)
        {
            float[] predictionResult = modeloAI.RunInference(features);
            ProcesarPrediccionDeConocimientoFirebase(predictionResult);
        }
        else
        {
            ServiceLocator.Logger.LogWarning("[Prediccion] No se encontro ModeloAI.");
        }

        GuardarCategoriasOrdenadasLocal();
        await finalizarEncuestaUseCase.EjecutarAsync();

        bool estadoAprendizaje = ServiceLocator.PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoConocimiento = ServiceLocator.PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (estadoAprendizaje && estadoConocimiento)
            ServiceLocator.Scene.LoadScene("Inicio");
        else
            ServiceLocator.Scene.LoadScene("SeleccionarEncuesta");
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
            ServiceLocator.Logger.LogError("[GuardarCategorias] La lista de categorias es null.");
            return;
        }

        try
        {
            categorias = categorias.OrderBy(c => c.Porcentaje).ToList();
            CategoriasData data = new CategoriasData { categorias = categorias };
            string json = ServiceLocator.Json.ToJson(data, true);

            if (ServiceLocator.Network.IsConnected())
            {
                string rutaArchivo = Path.Combine(ServiceLocator.Persistence.GetPersistentDataPath(), "categorias_encuesta_firebase.json");
                File.WriteAllText(rutaArchivo, json);
                ServiceLocator.Logger.Log("Categorias ordenadas guardadas en archivo: " + rutaArchivo);
            }
            else
            {
                ServiceLocator.PlayerPrefs.SetString("categorias_encuesta_firebase_json", json);
                ServiceLocator.PlayerPrefs.Save();
                ServiceLocator.Logger.Log("Categorias ordenadas guardadas en PlayerPrefs.");
            }

            StartCoroutine(CopiarJsonAuxiliaresSiEsNecesario());
        }
        catch (Exception e)
        {
            ServiceLocator.Logger.LogError($"[GuardarCategorias] Error: {e.Message}");
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
            string rutaLocal = Path.Combine(ServiceLocator.Persistence.GetPersistentDataPath(), nombreArchivo);

            if (File.Exists(rutaLocal))
            {
                ServiceLocator.Logger.Log($"Ya existe localmente: {nombreArchivo}");
            }
            else
            {
                string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
                string contenido = ServiceLocator.ResourceLoader.LoadTextAsset($"Plantillas_Json/{nombreSinExtension}");

                if (contenido != null)
                {
                    File.WriteAllText(rutaLocal, contenido);
                    ServiceLocator.Logger.Log($"Archivo copiado desde Resources: {nombreArchivo}");
                }
                else
                {
                    ServiceLocator.Logger.LogError($"No se encontro {nombreArchivo} en Resources/Plantillas_Json.");
                }
            }
            yield return null;
        }

        if (ServiceLocator.Network.IsConnected())
        {
            yield return subirDatosJSONUseCase.Ejecutar();
        }
    }
}
