using UnityEngine;
using Vuforia;
using Firebase;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using static DynamicMoleculeLoader;
using SimpleJSON;  // Necesitas agregar "using SimpleJSON" si usas SimpleJSON para el parseo
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using PeriodicApp.Core.Domain.Quimica;

public class DynamicMoleculeLoader : MonoBehaviour
{
    [System.Serializable]
    public class Element
    {
        public string simbolo;
        public int atomicNumber;
        public float atomicMass;
        public int electronLevels;
        public int protons;
        public int neutrons;
        public int electrons;
        public int valence;
        public string category;
        public string phase;
        public string arModel;
        public string[] electrones;
        public string color;
    }
    // 🔹 Estructura temporal para deserializar el JSON
    [System.Serializable]
    public class ElementEntry
    {
        public string key;
        public Element value;
    }

    [System.Serializable]
    public class ElementDatabaseRaw
    {
        public List<ElementEntry> elements;
    }

    public GameObject imageTargetPrefab;
    
    private ObserverBehaviour imageTargetBehaviour;

    private string elementoSeleccionado;
    private string elementoTargetprov;
    private string elementoTarget;
    private string ruta;
    private Dictionary<string, Element> elementDatabase = new Dictionary<string, Element>();
    private JSONNode jsonData;  // Estructura para manejar el JSON

    private ObserverBehaviour trackable;
    private ControllerBotones ControladorBotones;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;

    public GameObject particlePrefab;

    [Header("Geometria del atomo")]
    [Tooltip("Radio de la primera capa.")]
    public float radioBase = 0.30f;
    [Tooltip("Radio de la capa mas externa. Las capas intermedias se reparten " +
             "entre este valor y el radio base, asi el hidrogeno y el uranio " +
             "ocupan el mismo espacio sobre el marcador.")]
    public float radioMaximo = 0.62f;
    [Tooltip("Escala de cada electron.")]
    public float escalaElectron = 0.2f;

    [Header("Estilo visual")]
    [Tooltip("Tinta los electrones y las orbitas con el color del elemento definido en el JSON.")]
    public bool usarColorDelElemento = true;
    [Tooltip("Color de los electrones cuando no se usa el color del elemento.")]
    public Color colorElectron = new Color(0.40f, 0.65f, 1f);
    [Range(0f, 2f)] public float emisionNucleon = 0.15f;
    [Range(0f, 3f)] public float fuerzaBordeNucleon = 0.9f;
    [Range(0f, 4f)] public float emisionElectron = 1.2f;
    [Range(0f, 6f)] public float fuerzaHaloElectron = 2.2f;
    [Range(0f, 2f)] public float brilloOrbita = 0.35f;
    [Range(0f, 4f)] public float fuerzaPulsoOrbita = 1.4f;

    [Header("Estelas de electrones")]
    [Tooltip("Dibuja una estela luminosa detras de los electrones de la capa externa, " +
             "que es la que explica la reactividad del elemento.")]
    public bool usarEstelas = true;
    [Tooltip("Si la capa externa tiene mas electrones que este numero, se omiten las estelas.")]
    public int maxElectronesConEstela = 8;
    [Range(10f, 180f)] public float arcoEstela = 70f;
    [Range(2, 32)] public int segmentosEstela = 12;
    [Range(0.001f, 0.05f)] public float anchoEstela = 0.012f;

    [Header("Animacion de formacion")]
    [Tooltip("Anima el ensamblado del atomo. Si se desactiva, todo aparece ya colocado.")]
    public bool animarFormacion = true;
    [Tooltip("Duracion total del ensamblado del nucleo, en segundos. Se reparte " +
             "entre todos los nucleones, asi el uranio no tarda 5 segundos.")]
    public float duracionNucleo = 1.2f;
    [Tooltip("Duracion total del trazado de los anillos, en segundos.")]
    public float duracionAnillos = 0.8f;
    [Tooltip("Duracion total de la entrada de los electrones, en segundos.")]
    public float duracionElectrones = 1.4f;
    [Tooltip("Desde que distancia vuelan los nucleones hacia el centro.")]
    public float distanciaEntradaNucleon = 0.9f;
    [Tooltip("Cuanto mas lejos del anillo entran los electrones. 1 = sobre el anillo.")]
    public float factorEntradaElectron = 2.4f;
    [Tooltip("Intensidad del latido del nucleo al completarse.")]
    [Range(0f, 1f)] public float pulsoNucleo = 0.25f;
    [Tooltip("Llena las capas en el orden real de Madelung (4s antes que 3d). " +
             "Es la secuencia que se estudia en clase y no coincide con el orden de las capas.")]
    public bool llenarEnOrdenDeMadelung = true;

    [Header("Rendimiento")]
    [Tooltip("Maximo de sistemas de particulas para electrones. Por encima de este " +
             "numero los electrones se dibujan sin particulas para no hundir los FPS.")]
    public int maxParticulasElectron = 12;

    [Header("Materiales")]
    [Tooltip("Material del anillo de orbita. Si se deja vacio se genera uno por codigo, " +
             "pero conviene asignarlo aqui para asegurar que el shader entre en el build de Android.")]
    public Material materialOrbita;

    // Color del elemento que se esta mostrando, tomado del JSON.
    private Color colorActual = Color.white;

    private static readonly Color ColorProton = new Color(0.8f, 0.2f, 0.2f);
    private static readonly Color ColorNeutron = new Color(0.2f, 0.2f, 0.8f);

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        ControladorBotones = FindAnyObjectByType<ControllerBotones>();

        elementoTargetprov = PlayerPrefs.GetString("NumeroAtomico", "").Trim() + "_" + PlayerPrefs.GetString("ElementoSeleccionado", "").Trim();
        elementoTarget = FormatearNombreArchivo(elementoTargetprov);

        ruta = PlayerPrefs.GetString("CargarVuforia", "");

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            Debug.Log($"🔍 [DynamicElementLoader] ImageTarget detectado: '{trackable.TargetName}'");
            Debug.Log($"🎯 [DynamicElementLoader] Elemento esperado: '{elementoTarget}'");
            Debug.Log($"🛤️ [DynamicElementLoader] Ruta: '{ruta}'");

            trackable.OnTargetStatusChanged += OnImageDetected;
        }
        else
        {
            Debug.LogError($"❌ [DynamicElementLoader] No se encontró ObserverBehaviour en {gameObject.name}");
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (ruta == "Misiones")
        {
            if (trackable != null && trackable.TargetName.Trim().ToLower() != elementoTarget.Trim().ToLower())
            {
                Debug.Log($"⏸️ [DynamicElementLoader] Desactivando ImageTarget '{trackable.TargetName}' (no coincide con '{elementoTarget}')");
                gameObject.SetActive(false);
            }
            else if (trackable != null)
            {
                Debug.Log($"✅ [DynamicElementLoader] ImageTarget '{trackable.TargetName}' activado (coincide con misión)");
            }
        }
    }
    string FormatearNombreArchivo(string original)
    {
        string sinTildes = original
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ñ", "n");

        string sinEspacios = sinTildes.Replace(" ", ""); // Quitar espacios internos

        return sinEspacios.Trim(); // Por seguridad
    }
    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        // ⚠️ Verificar que seguimos en la escena VuforiaNuevo antes de procesar
        string escenaActual = SceneManager.GetActiveScene().name;
        if (escenaActual != "VuforiaNuevo")
        {
            Debug.Log($"🔒 [DynamicElementLoader] Ignorando evento de tracking - Ya no estamos en VuforiaNuevo (estamos en: {escenaActual})");
            return;
        }

        if (status.Status == Status.TRACKED)
        {
            Debug.Log($"📸 [DynamicElementLoader] Imagen DETECTADA: '{trackable.TargetName}' - Status: TRACKED");

            LimpiarModelos();

            // Validar que el TargetName tiene el formato correcto (NumeroAtomico_NombreElemento)
            string[] partes = trackable.TargetName.Split('_');
            if (partes.Length < 2)
            {
                Debug.LogError($"❌ [DynamicElementLoader] El TargetName '{trackable.TargetName}' no tiene el formato correcto. Debe ser: NumeroAtomico_NombreElemento (ej: 1_hidrogeno)");
                return;
            }

            string resultado = partes[1];
            elementoSeleccionado = resultado.ToLower();
            Debug.Log($"🧪 [DynamicElementLoader] Elemento seleccionado: '{elementoSeleccionado}'");

            CargarJSON();

            if (ruta == "Inicio")
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    SumarXPFirebase(5);
                }
                else
                {
                    SumarXPTemporario(5);
                }
            }
            else
            {
                DesbloquearLogro(trackable.TargetName);
            }
        }
        // Si pierde el tracking, limpiar modelos y detener audio
        else if (status.Status == Status.NO_POSE || status.Status == Status.LIMITED)
        {
            Debug.Log($"⚠️ [DynamicElementLoader] Se perdió el tracking de '{trackable.TargetName}' - Status: {status.Status}");

            // Ya verificamos al inicio del método que estamos en VuforiaNuevo
            // Detener audio, limpiar modelos y ocultar botón
            ModeloLoader modeloLoader = FindAnyObjectByType<ModeloLoader>();
            if (modeloLoader != null)
            {
                modeloLoader.DetenerAudio();
                modeloLoader.LimpiarModelos();
                modeloLoader.OcultarBotonCambiarModelo();
            }

            // Limpiar los modelos del contenedor atómico
            LimpiarModelos();

            Debug.Log($"🧹 [DynamicElementLoader] Modelos, audio y botón limpiados por pérdida de tracking");
        }
        else
        {
            Debug.Log($"ℹ️ [DynamicElementLoader] Estado de tracking: {status.Status} para '{trackable.TargetName}'");
        }
    }

    // Desregistrar el evento cuando se destruye el objeto
    void OnDestroy()
    {
        if (trackable != null)
        {
            trackable.OnTargetStatusChanged -= OnImageDetected;
            Debug.Log($"🧹 [DynamicElementLoader] Evento OnTargetStatusChanged desregistrado para '{trackable.TargetName}'");
        }
    }


    void DesbloquearLogro(string elemento)
    {
        if (ruta == "Misiones")
        {
            ControladorBotones.PanelBotonUI.SetActive(true);
            ControladorBotones.botonCompletarMision.interactable = true;
        }
    }

    void CargarJSON()
    {
        string jsonString;

        TextAsset jsonFile = Resources.Load<TextAsset>("Moleculas");
        if (jsonFile != null)
        {
            jsonString = jsonFile.text;
        }
        else
        {
            Debug.LogError("No se encontró el archivo JSON en Resources.");
            return;
        }

        jsonData = JSON.Parse(jsonString);

        if (jsonData == null || jsonData["elements"] == null)
        {
            Debug.LogError("Error al deserializar el JSON.");
            return;
        }

        JSONNode elementoData = jsonData["elements"][elementoSeleccionado];

        if (elementoData == null)
        {
            Debug.LogError($"El elemento '{elementoSeleccionado}' no existe en el JSON.");
            return;
        }

        // Obtener los valores necesarios
        int atomicNumber = elementoData["atomicNumber"].AsInt;
        int showNeutrons = elementoData["neutrons"].AsInt;
        int showProtons = elementoData["protons"].AsInt;
        string modelName = elementoData["arModel"];

        // Cada elemento trae su color en el JSON; se usa para tintar electrones
        // y órbitas, de modo que cada átomo tenga identidad visual propia.
        colorActual = usarColorDelElemento
            ? AtomoMateriales.DesdeHex(elementoData["color"], colorElectron)
            : colorElectron;

        List<string> electronModels = new List<string>();
        foreach (JSONNode electron in elementoData["electrones"].AsArray)
        {
            electronModels.Add(electron.Value);
        }

        //// Llamar a la función LoadMoleculeModel con los datos extraídos
        StartCoroutine(LoadMoleculeModel(atomicNumber, showProtons, showNeutrons, modelName, electronModels));

        Debug.Log($"🎨 [DynamicElementLoader] Inicializando cambio visual para elemento: '{elementoSeleccionado}'");
        FindAnyObjectByType<ModeloLoader>()?.InicializarCambioVisual(
            elementoSeleccionado, imageTargetPrefab
        );
    }

    private IEnumerator LoadMoleculeModel(int atomicNumber, int protons, int neutrons, string modelName, List<string> electronModels)
    {
        // 1. Distribución electrónica REAL del elemento.
        //    Antes se llenaba cada capa hasta su capacidad teórica (2, 8, 18, 32...),
        //    lo que daba resultados incorrectos en 86 de los 118 elementos: el potasio
        //    salía como 2-8-9 en vez de 2-8-8-1, escondiendo justo el electrón de
        //    valencia que explica su reactividad.
        if (!ConfiguracionElectronica.EsValido(atomicNumber))
        {
            Debug.LogError($"❌ [DynamicElementLoader] Número atómico inválido: {atomicNumber}");
            yield break;
        }

        if (electronModels == null || electronModels.Count == 0)
        {
            Debug.LogError($"❌ [DynamicElementLoader] El elemento '{elementoSeleccionado}' no tiene modelos de electrón en el JSON.");
            yield break;
        }

        int[] electronDistribution = ConfiguracionElectronica.ObtenerCapas(atomicNumber);
        int electronLevels = electronDistribution.Length;

        Debug.Log($"⚛️ [DynamicElementLoader] Z={atomicNumber} → capas [{string.Join(", ", electronDistribution)}] " +
                  $"({ConfiguracionElectronica.NotacionSpdf(atomicNumber)})");

        // 2. Generación visual
        GameObject atomContainer = new GameObject("AtomContainer");
        atomContainer.transform.SetParent(imageTargetPrefab.transform);
        atomContainer.transform.localPosition = Vector3.zero;

        AtomoEnsamblador ensamblador = atomContainer.AddComponent<AtomoEnsamblador>();

        // ---------------------------------------------------------- ACTO 1: el núcleo
        GameObject nucleo = null;
        yield return StartCoroutine(CreateNucleus(atomContainer.transform, protons, neutrons,
            ensamblador, resultado => nucleo = resultado));

        if (animarFormacion)
        {
            // Esperar a que todos los nucleones hayan llegado
            while (ensamblador != null && !ensamblador.Terminado)
            {
                yield return null;
            }

            // Latido: la masa ya está completa
            if (nucleo != null && pulsoNucleo > 0f)
            {
                yield return StartCoroutine(ensamblador.Pulso(nucleo.transform, pulsoNucleo, 0.35f));
            }
        }

        int totalElectrones = Mathf.Max(1, atomicNumber);

        // Solo los átomos pequeños llevan partículas por electrón; en los grandes
        // hundirían los FPS del móvil.
        bool usarParticulas = particlePrefab != null && totalElectrones <= maxParticulasElectron;

        // La capa externa lleva estelas: es la que define el comportamiento
        // químico del elemento, así que conviene que destaque.
        int capaValencia = electronLevels - 1;
        bool estelasEnValencia = usarEstelas
            && electronDistribution[capaValencia] <= maxElectronesConEstela;

        Material materialElectron = AtomoMateriales.Electron(
            colorActual, emisionElectron, fuerzaHaloElectron);
        Material materialEstela = AtomoMateriales.Estela(colorActual);

        // Los modelos de electrón se cargan una sola vez (son 7 como mucho).
        // Antes se pedía uno por electrón: 118 cargas asíncronas en el oganesón,
        // cada una costando al menos un frame.
        GameObject[] prefabsElectron = new GameObject[electronLevels];
        for (int level = 0; level < electronLevels; level++)
        {
            string nombre = electronModels[level % electronModels.Count];
            ResourceRequest peticion = Resources.LoadAsync<GameObject>(
                "Moleculas/NuevoElemento/" + nombre);
            yield return peticion;

            prefabsElectron[level] = peticion.asset as GameObject;

            if (prefabsElectron[level] == null)
            {
                Debug.LogWarning($"⚠️ [DynamicElementLoader] No se encontró el modelo de electrón '{nombre}'.");
            }
        }

        // ------------------------------------------------- ACTO 2: se trazan las órbitas
        GameObject[] orbitas = new GameObject[electronLevels];
        float[] radios = new float[electronLevels];

        for (int level = 0; level < electronLevels; level++)
        {
            radios[level] = RadioDeCapa(level, electronLevels);
            orbitas[level] = CreateOrbitRing(atomContainer.transform, radios[level],
                colorActual, level, electronDistribution[level]);

            if (animarFormacion)
            {
                // En cascada de dentro hacia fuera, sin bloquear: las capas
                // exteriores empiezan a dibujarse antes de que acaben las internas.
                LineRenderer linea = orbitas[level].GetComponent<LineRenderer>();
                float duracionCapa = duracionAnillos / electronLevels;
                StartCoroutine(ensamblador.TrazarAnillo(linea, radios[level], 100, duracionAnillos * 0.6f));
                yield return new WaitForSeconds(duracionCapa);
            }
        }

        // ------------------------------------------- ACTO 3: entran los electrones
        //
        // El orden importa: los electrones no llenan las capas de dentro a fuera,
        // sino por subcapas siguiendo Madelung. En el hierro, los dos electrones
        // de la capa 4 (4s) entran ANTES que los seis últimos de la capa 3 (3d).
        // Verlo en movimiento explica de golpe algo que en el cuaderno cuesta.
        Subcapa[] subcapas = ConfiguracionElectronica.ObtenerSubcapas(atomicNumber);
        int[] colocadosPorCapa = new int[electronLevels];
        float esperaPorElectron = duracionElectrones / totalElectrones;

        if (!llenarEnOrdenDeMadelung)
        {
            subcapas = OrdenarPorNivel(subcapas);
        }

        for (int s = 0; s < subcapas.Length; s++)
        {
            Subcapa subcapa = subcapas[s];
            int level = subcapa.Nivel - 1;

            if (level < 0 || level >= electronLevels || orbitas[level] == null)
            {
                continue;
            }

            int enLaCapa = electronDistribution[level];
            float angleStep = enLaCapa > 0 ? 360f / enLaCapa : 0f;
            bool estelasAqui = estelasEnValencia && level == capaValencia;

            for (int e = 0; e < subcapa.Electrones; e++)
            {
                int indice = colocadosPorCapa[level]++;
                float angulo = angleStep * indice;
                float radio = radios[level];

                Vector3 electronPos = new Vector3(
                    radio * Mathf.Cos(angulo * Mathf.Deg2Rad),
                    0,
                    radio * Mathf.Sin(angulo * Mathf.Deg2Rad)
                );

                CreateElectron(
                    prefabsElectron[level],
                    orbitas[level].transform,
                    electronPos,
                    escalaElectron,
                    level,
                    angulo,
                    usarParticulas,
                    materialElectron,
                    estelasAqui ? materialEstela : null,
                    ensamblador
                );

                if (esperaPorElectron > 0f)
                {
                    yield return new WaitForSeconds(esperaPorElectron);
                }
            }
        }

        // -------------------------------------------- ACTO 4: el átomo cobra vida
        if (animarFormacion)
        {
            while (ensamblador != null && !ensamblador.Terminado)
            {
                yield return null;
            }
        }

        StartCoroutine(EnableAnimationsAfterDelay(atomContainer, animarFormacion ? 0.1f : 0.5f));
        yield break;
    }

    /// <summary>
    /// Reordena las subcapas por nivel principal, para quien prefiera ver el
    /// llenado capa a capa en vez del orden real de Madelung.
    /// </summary>
    private static Subcapa[] OrdenarPorNivel(Subcapa[] subcapas)
    {
        Subcapa[] copia = (Subcapa[])subcapas.Clone();
        System.Array.Sort(copia, (a, b) => a.Nivel.CompareTo(b.Nivel));
        return copia;
    }

    private void CreateElectron(GameObject prefab, Transform parent, Vector3 position,
        float size, int level, float anguloInicial, bool usarParticulas,
        Material material, Material materialEstela, AtomoEnsamblador ensamblador)
    {
        if (prefab != null)
        {
            GameObject electron = Instantiate(prefab, parent);
            electron.transform.localPosition = position;
            electron.transform.localScale = Vector3.one * size;

            if (material != null)
            {
                AplicarMaterialCompartido(electron, material);
            }

            // Entrada animada: el electrón llega desde fuera y encaja en su
            // órbita con un pequeño rebote, como atraído por el núcleo.
            if (animarFormacion && ensamblador != null)
            {
                ensamblador.Registrar(
                    electron.transform,
                    position * factorEntradaElectron,
                    position,
                    size,
                    0f,
                    Mathf.Max(0.25f, duracionElectrones * 0.4f),
                    AtomoEnsamblador.Curva.Rebote);
            }

            // El sistema de partículas por electrón es carísimo: en el uranio serían
            // 92 ParticleSystem simultáneos. Solo se usa en átomos pequeños.
            if (usarParticulas)
            {
                var electronParticles = Instantiate(particlePrefab, electron.transform);
                electronParticles.transform.localPosition = Vector3.zero;

                var ps = electronParticles.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSize = 1f;
                }
            }

            // Añadir comportamiento orbital sincronizado con la órbita padre.
            // El radio y el ángulo se pasan explícitos porque durante la entrada
            // animada el electrón todavía no está en su posición final.
            var orbitBehavior = electron.AddComponent<ElectronOrbit>();
            orbitBehavior.Configure(level, position.magnitude, anguloInicial);

            if (materialEstela != null)
            {
                CrearEstela(parent, orbitBehavior, materialEstela);
            }
        }
    }

    /// <summary>
    /// Crea la estela de un electrón. Cuelga de la órbita, no del electrón, para
    /// compartir su espacio local y quedar pegada al arco.
    /// </summary>
    private void CrearEstela(Transform orbita, ElectronOrbit electron, Material material)
    {
        GameObject objetoEstela = new GameObject("Estela");
        objetoEstela.transform.SetParent(orbita, false);
        objetoEstela.transform.localPosition = Vector3.zero;
        objetoEstela.transform.localRotation = Quaternion.identity;

        ElectronTrail estela = objetoEstela.AddComponent<ElectronTrail>();
        estela.Configurar(electron, material, arcoEstela, segmentosEstela, anchoEstela);
    }

    /// <summary>
    /// Radio de una capa. Las capas se reparten entre <see cref="radioBase"/> y
    /// <see cref="radioMaximo"/>, así que un átomo de 7 capas ocupa lo mismo que
    /// uno de 2 y siempre encaja sobre el marcador.
    /// </summary>
    private float RadioDeCapa(int nivel, int totalNiveles)
    {
        if (totalNiveles <= 1)
        {
            return radioBase;
        }

        return Mathf.Lerp(radioBase, radioMaximo, nivel / (float)(totalNiveles - 1));
    }

    private GameObject CreateOrbitRing(Transform parent, float radius, Color color, int level, int electronCount)
    {
        GameObject orbit = new GameObject($"Orbit_Level_{level + 1}");
        orbit.transform.SetParent(parent);
        orbit.transform.localPosition = Vector3.zero;


        // Configurar animación completa en X e Y
        var anim = orbit.AddComponent<OrbitAnimation>();
        anim.Configure(
            level,
            15f + (level * 2f),  // Velocidad de rotación Y
            20f + (level * 3f),   // Velocidad de rotación X
            electronCount          // Para sincronización de electrones
        );

        // Crear anillo visual
        LineRenderer line = orbit.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.startWidth = 0.002f;
        line.endWidth = 0.002f;
        line.positionCount = 100;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;

        // Material compartido entre todas las órbitas de todos los átomos.
        // Antes se creaba uno nuevo por anillo (hasta 7 por átomo) y además
        // Shader.Find en runtime es lento y puede fallar en build si el shader
        // no quedó incluido.
        line.sharedMaterial = materialOrbita != null
            ? materialOrbita
            : AtomoMateriales.Orbita(color, brilloOrbita, fuerzaPulsoOrbita);

        // Con animación de formación el anillo nace vacío: lo dibuja
        // AtomoEnsamblador.TrazarAnillo. Sin ella se pinta entero de una vez.
        if (animarFormacion)
        {
            line.positionCount = 0;
            line.loop = false;
            return orbit;
        }

        // Crear puntos del anillo
        Vector3[] points = new Vector3[100];
        for (int i = 0; i < 100; i++)
        {
            float angle = 2 * Mathf.PI * i / 100;
            points[i] = new Vector3(
                radius * Mathf.Cos(angle),
                0,
                radius * Mathf.Sin(angle)
            );
        }
        line.SetPositions(points);

        return orbit;
    }

    private IEnumerator EnableAnimationsAfterDelay(GameObject atomContainer, float delay)
    {
        yield return new WaitForSeconds(delay);

        OrbitAnimation[] animations = atomContainer.GetComponentsInChildren<OrbitAnimation>();
        foreach (var anim in animations)
        {
            anim.EnableAnimation();
        }

        ElectronOrbit[] electrons = atomContainer.GetComponentsInChildren<ElectronOrbit>();
        foreach (var electron in electrons)
        {
            electron.EnableOrbit();
        }

        // Las estelas se activan al final: si se encendieran antes de que los
        // electrones empiecen a moverse, se verían como rayas fijas.
        ElectronTrail[] estelas = atomContainer.GetComponentsInChildren<ElectronTrail>();
        foreach (var estela in estelas)
        {
            estela.Activar();
        }
    }
    private IEnumerator CreateNucleus(Transform parent, int protons, int neutrons,
        AtomoEnsamblador ensamblador, System.Action<GameObject> devolverNucleo)
    {
        int totalNucleones = Mathf.Max(1, protons + neutrons);
        float escala = Mathf.Clamp(100f / Mathf.Pow(totalNucleones, 1f / 3f), 10f, 100f);

        GameObject nucleus = new GameObject("Nucleus");
        nucleus.transform.SetParent(parent);
        nucleus.transform.localPosition = Vector3.zero;

        if (devolverNucleo != null)
        {
            devolverNucleo(nucleus);
        }

        // Un solo sistema de partículas para todo el núcleo
        if (particlePrefab != null)
        {
            var protonParticles = Instantiate(particlePrefab, nucleus.transform);
            protonParticles.transform.localPosition = Vector3.zero;
        }

        // Los prefabs se cargan UNA vez, no dentro del bucle: en el uranio eso
        // eran 238 llamadas síncronas a Resources.Load.
        ResourceRequest reqProton = Resources.LoadAsync<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_PROTON");
        yield return reqProton;
        ResourceRequest reqNeutron = Resources.LoadAsync<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_NEUTRON");
        yield return reqNeutron;

        GameObject prefabProton = reqProton.asset as GameObject;
        GameObject prefabNeutron = reqNeutron.asset as GameObject;

        if (prefabProton == null || prefabNeutron == null)
        {
            Debug.LogError("❌ [DynamicElementLoader] Faltan los prefabs de protón o neutrón en Resources/Moleculas/NuevoElemento/");
            yield break;
        }

        // El retardo se reparte entre todos los nucleones: así el hidrógeno y el
        // uranio tardan lo mismo en formarse.
        float esperaPorNucleon = duracionNucleo / totalNucleones;

        Material matProton = AtomoMateriales.Nucleon(ColorProton, emisionNucleon, fuerzaBordeNucleon);
        Material matNeutron = AtomoMateriales.Nucleon(ColorNeutron, emisionNucleon, fuerzaBordeNucleon);

        // Los nucleones se instancian de golpe y es el ensamblador quien escalona
        // su llegada. Así 238 partículas no cuestan 238 esperas encadenadas.
        int indiceGlobal = 0;

        for (int i = 0; i < protons; i++)
        {
            Vector3 pos = FibonacciSphere(i, protons, 0.12f);
            GameObject proton = Instantiate(prefabProton, nucleus.transform);
            AplicarMaterialCompartido(proton, matProton);

            ColocarNucleon(proton.transform, pos, escala, ensamblador,
                esperaPorNucleon * indiceGlobal++);

            if (!animarFormacion && esperaPorNucleon > 0f)
            {
                yield return new WaitForSeconds(esperaPorNucleon);
            }
        }

        for (int i = 0; i < neutrons; i++)
        {
            Vector3 pos = FibonacciSphere(i, neutrons, 0.1f);
            GameObject neutron = Instantiate(prefabNeutron, nucleus.transform);
            AplicarMaterialCompartido(neutron, matNeutron);

            ColocarNucleon(neutron.transform, pos, escala, ensamblador,
                esperaPorNucleon * indiceGlobal++);

            if (!animarFormacion && esperaPorNucleon > 0f)
            {
                yield return new WaitForSeconds(esperaPorNucleon);
            }
        }
    }

    /// <summary>
    /// Coloca un nucleón: o directamente en su sitio, o volando hacia él desde
    /// una dirección aleatoria si la animación de formación está activa.
    /// </summary>
    private void ColocarNucleon(Transform nucleon, Vector3 destino, float escala,
        AtomoEnsamblador ensamblador, float retardo)
    {
        if (!animarFormacion || ensamblador == null)
        {
            nucleon.localPosition = destino;
            nucleon.localScale = Vector3.one * escala;
            return;
        }

        // Dirección de entrada aleatoria, pero siempre desde fuera del núcleo.
        // Se cualifica el espacio de nombres porque este archivo importa System
        // y UnityEngine, y ambos definen un tipo Random.
        Vector3 direccion = UnityEngine.Random.onUnitSphere;
        Vector3 origen = destino + direccion * distanciaEntradaNucleon;

        ensamblador.Registrar(
            nucleon,
            origen,
            destino,
            escala,
            retardo,
            0.45f,
            AtomoEnsamblador.Curva.Suave);
    }

    /// <summary>
    /// Asigna el material compartido a todos los renderers del objeto. Usar
    /// sharedMaterial evita que Unity clone un material por instancia, que era
    /// la causa principal de las draw calls en átomos grandes.
    /// </summary>
    private static void AplicarMaterialCompartido(GameObject objeto, Material material)
    {
        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterial = material;
        }
    }

    private Vector3 FibonacciSphere(int index, int total, float radius)
    {
        // Con un solo nucleón la fórmula original hacía 0/0 = NaN y la partícula
        // desaparecía. Le pasaba justo al hidrógeno, que tiene un único protón.
        if (total <= 1)
        {
            return Vector3.zero;
        }

        // Distribución uniforme en esfera usando algoritmo de Fibonacci
        float y = 1 - (index / (float)(total - 1)) * 2;
        float radiusAtY = Mathf.Sqrt(Mathf.Max(0f, 1 - y * y));
        float theta = Mathf.PI * (3 - Mathf.Sqrt(5)) * index;

        float x = Mathf.Cos(theta) * radiusAtY * radius;
        float z = Mathf.Sin(theta) * radiusAtY * radius;

        return new Vector3(x, y * radius, z);
    }

    private void LimpiarModelos()
    {
        foreach (Transform child in imageTargetPrefab.transform)
        {
            Destroy(child.gameObject);
        }

        // Antes había un Caching.ClearCache() aquí: borra la caché de AssetBundles,
        // que no tiene nada que ver con estos modelos, y provoca recargas costosas.
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);
        xpTemp += xp;
        PlayerPrefs.SetInt("TempXP", xpTemp);
        PlayerPrefs.Save();
        Debug.Log($"🔄 XP {xp} sumado temporalmente. Total TempXP: {xpTemp}");
    }

    async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = snapshot.Exists && snapshot.TryGetValue("xp", out int valor) ? valor : 0;
            int nuevoXP = xpActual + xp;
            await userRef.UpdateAsync("xp", nuevoXP);
            Debug.Log($"✅ XP actualizado: {nuevoXP}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al subir XP: {e.Message}");
        }
    }

}
