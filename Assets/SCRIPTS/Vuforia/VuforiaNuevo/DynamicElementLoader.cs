using UnityEngine;
using Vuforia;
using Firebase;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using static DynamicMoleculeLoader;
using SimpleJSON;  // Necesitas agregar "using SimpleJSON" si usas SimpleJSON para el parseo

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
    private string elementoTarget;
    private Dictionary<string, Element> elementDatabase = new Dictionary<string, Element>();
    private JSONNode jsonData;  // Estructura para manejar el JSON

    private ObserverBehaviour trackable;
    private ControllerBotones ControladorBotones;

    void Start()
    {
        ControladorBotones = FindAnyObjectByType<ControllerBotones>();

        elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "hidrogeno").ToLower();
        elementoTarget = PlayerPrefs.GetString("NumeroAtomico", "1").Trim() + "_" + PlayerPrefs.GetString("ElementoSeleccionado", "Hidrogeno").Trim();
        //string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (trackable.TargetName.Trim().ToLower() != elementoTarget.Trim().ToLower())
        {
            gameObject.SetActive(false);
        }
        //elementoSeleccionado = "hierro";
        Debug.Log($"Elemento seleccionado: {elementoSeleccionado}");
        //imageTargetBehaviour = GameObject.Find("ImageTarget").GetComponent<ObserverBehaviour>();
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED)
        {
            CargarJSON();
            Debug.Log($"¡Imagen detectada! {trackable.TargetName} desbloqueado.");
            DesbloquearLogro(trackable.TargetName);
        }
    }


    void DesbloquearLogro(string elemento)
    {
        Debug.Log($"🏆 Logro desbloqueado: {elemento}");
        ControladorBotones.PanelBotonUI.SetActive(true);
        ControladorBotones.botonCompletarMision.interactable = true;
    }

    void CargarJSON()
    {
        string jsonString = PlayerPrefs.GetString("moleculasJSON", "");

        if (string.IsNullOrEmpty(jsonString))
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Moleculas");
            if (jsonFile != null)
            {
                jsonString = jsonFile.text;
                PlayerPrefs.SetString("moleculasJSON", jsonString);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("No se encontró el archivo JSON en Resources.");
                return;
            }
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
        int electronLevels = elementoData["electronLevels"].AsInt;
        int showNeutrons = elementoData["neutrons"].AsInt;
        int showProtons = elementoData["protons"].AsInt;
        int electrons = elementoData["electrons"].AsInt;  // Se debe extraer este valor también
        string modelName = elementoData["arModel"];

        List<string> electronModels = new List<string>();
        foreach (JSONNode electron in elementoData["electrones"].AsArray)
        {
            electronModels.Add(electron.Value);
        }

        //// Llamar a la función LoadMoleculeModel con los datos extraídos
        StartCoroutine(LoadMoleculeModel(electronLevels, showProtons, showNeutrons, electrons, modelName, electronModels));
    }

    private IEnumerator LoadMoleculeModel(int electronLevels, int protons, int neutrons, int electrons, string modelName, List<string> electronModels)
    {

        LimpiarModelos();

        // 1. Configuración exacta de distribución
        int[] levelCapacity = { 2, 8, 18, 32, 32, 18, 8 }; // Máximo por nivel
        float[] orbitRadii = { .2f, .4f, .6f, .8f, 1f, 1.2f, 1.4f }; // Radios para cada capa

        // 2. Distribución exacta de electrones (corregido para último nivel)
        int[] electronDistribution = new int[electronLevels];
        int remaining = electrons;

        for (int level = 0; level < electronLevels && remaining > 0; level++)
        {
            // Asegurar que no excedamos el nivel máximo
            int maxForLevel = level < levelCapacity.Length ? levelCapacity[level] : levelCapacity[levelCapacity.Length - 1];
            electronDistribution[level] = Mathf.Min(maxForLevel, remaining);
            remaining -= electronDistribution[level];
        }

        // 3. Generación visual
        GameObject atomContainer = new GameObject("AtomContainer");
        atomContainer.transform.SetParent(imageTargetPrefab.transform);
        atomContainer.transform.localPosition = Vector3.zero;

        // Núcleo (manteniendo tu implementación)
        yield return StartCoroutine(CreateNucleus(atomContainer.transform, protons, neutrons,
            new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 0.8f)));

        // Generar TODAS las capas y electrones
        for (int level = 0; level < electronLevels; level++)
        {
            float radius = orbitRadii[level];
            GameObject orbit = CreateOrbitRing(atomContainer.transform, radius, new Color(1, 1, 1, 0.3f), level);

            int electronsInLevel = electronDistribution[level];

            // Si no hay electrones en este nivel, saltar
            if (electronsInLevel <= 0) continue;

            // Determinar cuántos pares debemos generar
            int pairsToGenerate = Mathf.CeilToInt(electronsInLevel / 2f);

            for (int pair = 0; pair < pairsToGenerate; pair++)
            {
                // Ángulo para este par (distribución uniforme)
                float angle = 2 * Mathf.PI * pair / pairsToGenerate;

                // Primer electrón del par
                Vector3 basePos = new Vector3(
                    radius * Mathf.Cos(angle),
                    0,
                    radius * Mathf.Sin(angle)
                );

                yield return StartCoroutine(CreateElectron(
                    electronModels[level % electronModels.Count],
                    orbit.transform,
                    basePos,
                    new Color(0.878f, 0.408f, 0.169f),
                    3.5f
                ));

                // Segundo electrón del par (opuesto) solo si corresponde
                if (pair * 2 + 1 < electronsInLevel)
                {
                    Vector3 oppositePos = -basePos;
                    yield return StartCoroutine(CreateElectron(
                        electronModels[level % electronModels.Count],
                        orbit.transform,
                        oppositePos,
                        new Color(0.878f, 0.408f, 0.169f),
                        3.5f
                    ));
                }

                yield return new WaitForSeconds(.03f);
            }
        }

        //// Esperar 3 segundos antes de activar animaciones
        //yield return new WaitForSeconds(3f);

        //// Activar todas las animaciones
        //OrbitAnimation[] orbitAnimations = atomContainer.GetComponentsInChildren<OrbitAnimation>();
        //foreach (var anim in orbitAnimations)
        //{
        //    anim.EnableAnimation();

        //    // Opcional: ajustar parámetros basados en nivel
        //    float levelFactor = anim.level / (float)electronLevels;
        //    anim.baseRotationSpeed = 15f + (levelFactor * 25f);
        //    anim.maxTiltAngle = 10f + (levelFactor * 20f);
        //}

        //yield break;

        // Al final de LoadMoleculeModel:
        StartCoroutine(EnableAnimationsAfterDelay(atomContainer, 3f));
        yield break;

    }

    private IEnumerator EnableAnimationsAfterDelay(GameObject atomContainer, float delay)
    {
        yield return new WaitForSeconds(delay);

        OrbitAnimation[] animations = atomContainer.GetComponentsInChildren<OrbitAnimation>();
        foreach (var anim in animations)
        {
            anim.EnableAnimation();
            // Puedes añadir efectos especiales aquí
        }
    }


    // Versión corregida de CreateNucleus
    private IEnumerator CreateNucleus(Transform parent, int protons, int neutrons, Color protonColor, Color neutronColor)
    {
        GameObject nucleus = new GameObject("Nucleus");
        nucleus.transform.SetParent(parent);
        nucleus.transform.localPosition = Vector3.zero;

        // Distribución de partículas nucleares
        for (int i = 0; i < protons; i++)
        {
            Vector3 pos = FibonacciSphere(i, protons, 0.1f);
            GameObject proton = Instantiate(Resources.Load<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_PROTON"), nucleus.transform);
            proton.transform.localPosition = pos;
            ApplyColorToParticle(proton, protonColor);
            yield return new WaitForSeconds(0.02f);
        }

        for (int i = 0; i < neutrons; i++)
        {
            Vector3 pos = FibonacciSphere(i, neutrons, 0.1f);
            GameObject neutron = Instantiate(Resources.Load<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_NEUTRON"), nucleus.transform);
            neutron.transform.localPosition = pos;
            ApplyColorToParticle(neutron, neutronColor);
            yield return new WaitForSeconds(0.02f);
        }
    }

    // Versión corregida de CreateElectron
    private IEnumerator CreateElectron(string modelName, Transform parent, Vector3 position, Color color, float size)
    {
        string path = "Moleculas/NuevoElemento/" + modelName;
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
        yield return request;

        if (request.asset != null)
        {
            GameObject electron = Instantiate(request.asset as GameObject, parent);
            electron.transform.localPosition = position;
            electron.transform.localScale = Vector3.one * size;

            // Material corregido (versión compatible con todos los shaders)
            Renderer renderer = electron.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.material.SetFloat("_Glossiness", 0.9f); // Equivalente a glossiness
                renderer.material.SetFloat("_Metallic", 0.7f); // Equivalente a metallic
            }
            electron.AddComponent<ElectronOrbit>();
        }
    }

    // Tu método exacto para crear órbitas
    private GameObject CreateOrbitRing(Transform parent, float radius, Color color, int level)
    {
        GameObject orbit = new GameObject($"Orbit_Level_{level}");
        orbit.transform.SetParent(parent);
        orbit.transform.localPosition = Vector3.zero;

        var anim = orbit.AddComponent<OrbitAnimation>();
        anim.Configure(
            level,                      // Nivel de órbita
            20f + (level * 3f),        // Velocidad base + incremento por nivel
            10f + (level * 2f)         // Inclinación base + incremento por nivel
        );

        // Configurar el LineRenderer (tu código existente)
        LineRenderer line = orbit.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.startWidth = .05f;
        line.endWidth = .05f;
        line.positionCount = 100;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        line.material = mat;

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

        return orbit; // Devuelve la órbita creada
    }



    //// Método auxiliar para aplicar color a cualquier partícula
    private void ApplyColorToParticle(GameObject particle, Color color)
    {
        Renderer renderer = particle.GetComponent<Renderer>();
        if (renderer != null)
        {
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);

            // Opcional: Añadir efectos especiales para electrones
            if (color == new Color(0.9f, 0.9f, 0.2f)) // Si es electrón
            {
                renderer.material.SetFloat("_Glossiness", 0.9f); // Equivalente a glossiness
                renderer.material.SetFloat("_Metallic", 0.7f); // Equivalente a metallic
                //propBlock.SetFloat("_Metallic", 0.7f);
                //propBlock.SetFloat("_Glossiness", 0.9f);
            }

            renderer.SetPropertyBlock(propBlock);
        }
    }


    private Vector3 FibonacciSphere(int index, int total, float radius)
    {
        // Distribución uniforme en esfera usando algoritmo de Fibonacci
        float y = 1 - (index / (float)(total - 1)) * 2;
        float radiusAtY = Mathf.Sqrt(1 - y * y);
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
    }
}

    // Clases auxiliares primero
    //public class ElectronOrbit : MonoBehaviour
    //{
    //    private float orbitSpeed;
    //    private float currentAngle;
    //    private float radius;
    //    private Vector3 axis;

    //    public void Initialize(float speed, float orbitRadius, Vector3 rotationAxis)
    //    {
    //        this.orbitSpeed = speed;
    //        this.radius = orbitRadius;
    //        this.axis = rotationAxis;
    //        this.currentAngle = Random.Range(0, 360f);
    //        UpdatePosition();
    //    }

    //    void Update()
    //    {
    //        currentAngle += orbitSpeed * Time.deltaTime;
    //        UpdatePosition();
    //    }

    //    void UpdatePosition()
    //    {
    //        transform.localPosition = Quaternion.Euler(0, currentAngle, 0) * (axis * radius);
    //    }
    //}

    //public class SelectiveOrbitRotation : MonoBehaviour
    //{
    //    [System.Serializable]
    //    public class OrbitSettings
    //    {
    //        public int level;
    //        public float rotationSpeed;
    //        public bool shouldRotate;
    //        [Range(0, 90)] public float maxTiltAngle;
    //    }

    //    public OrbitSettings[] orbitSettings;
    //    private List<Transform> orbitTransforms = new List<Transform>();

    //    void Start()
    //    {
    //        for (int i = 0; i < orbitSettings.Length; i++)
    //        {
    //            if (i < orbitTransforms.Count && orbitSettings[i].shouldRotate)
    //            {
    //                SetupOrbitRotation(orbitTransforms[i], orbitSettings[i]);
    //            }
    //        }
    //    }

    //    void SetupOrbitRotation(Transform orbit, OrbitSettings settings)
    //    {
    //        // Configurar electrones de esta órbita
    //        foreach (Transform child in orbit)
    //        {
    //            var electronOrbit = child.GetComponent<ElectronOrbit>();
    //            if (electronOrbit != null)
    //            {
    //                electronOrbit.Initialize(
    //                    settings.rotationSpeed * Random.Range(0.8f, 1.2f),
    //                    Vector3.Distance(child.localPosition, orbit.localPosition),
    //                    new Vector3(
    //                        Random.Range(-0.3f, 0.3f),
    //                        1f,
    //                        Random.Range(-0.3f, 0.3f)
    //                    ).normalized
    //                );
    //            }
    //        }
    //    }

    //    public void RegisterOrbit(Transform orbitTransform, int level)
    //    {
    //        if (level < orbitSettings.Length && orbitSettings[level].shouldRotate)
    //        {
    //            orbitTransforms.Add(orbitTransform);
    //        }
    //    }
    //}

//}



    //private IEnumerator CreateNuclearParticles(Transform parent, int protons, int neutrons, Color pColor, Color nColor, float radius)
    //{
    //    GameObject nucleusParticles = new GameObject("NuclearParticles");
    //    nucleusParticles.transform.SetParent(parent);
    //    nucleusParticles.transform.localPosition = Vector3.zero;

    //    // Protones
    //    for (int i = 0; i < protons; i++)
    //    {
    //        Vector3 pos = Random.insideUnitSphere * radius * 0.8f;
    //        GameObject p = Instantiate(Resources.Load<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_PROTON"), nucleusParticles.transform);
    //        p.transform.localPosition = pos;
    //        p.transform.localScale = Vector3.one * 0.3f;
    //        ApplyColorToParticle(p, pColor);
    //        yield return new WaitForSeconds(0.02f);
    //    }

    //    // Neutrones
    //    for (int i = 0; i < neutrons; i++)
    //    {
    //        Vector3 pos = Random.insideUnitSphere * radius * 0.8f;
    //        GameObject n = Instantiate(Resources.Load<GameObject>("Moleculas/NuevoElemento/SM_MOLECULA_NEUTRON"), nucleusParticles.transform);
    //        n.transform.localPosition = pos;
    //        n.transform.localScale = Vector3.one * 0.3f;
    //        ApplyColorToParticle(n, nColor);
    //        yield return new WaitForSeconds(0.02f);
    //    }
    //}

    // Método auxiliar para asegurar la distribución correcta
    //private int[] CalculateExactElectronsPerLevel(int totalElectrons, int levels)
    //{
    //    int[] levelCapacity = { 2, 8, 18, 32, 32, 18, 8 };
    //    int[] distribution = new int[levels];
    //    int remaining = totalElectrons;

    //    for (int i = 0; i < levels && remaining > 0; i++)
    //    {
    //        distribution[i] = Mathf.Min(levelCapacity[i], remaining);
    //        remaining -= distribution[i];

    //        Debug.Log($"Nivel {i + 1}: {distribution[i]} electrones");
    //    }

    //    if (remaining > 0)
    //    {
    //        Debug.LogWarning($"El elemento tiene {totalElectrons} electrones pero solo se distribuyeron {totalElectrons - remaining}");
    //    }

    //    return distribution;
    //}

    //private IEnumerator CreateElectron(string electronModel, Transform parent, Vector3 position, Color color)
    //{
    //    string path = "Moleculas/NuevoElemento/" + electronModel;
    //    ResourceRequest request = Resources.LoadAsync<GameObject>(path);
    //    yield return request;

    //    if (request.asset != null)
    //    {
    //        GameObject electron = Instantiate(request.asset as GameObject, parent);
    //        electron.transform.localPosition = position;
    //        electron.transform.localScale = Vector3.one * 0.2f;
    //        ApplyColorToParticle(electron, color);
    //        electron.AddComponent<CircularOrbit>().Initialize(parent, position.magnitude);
    //    }
    //}

    //private float[] CalculateLevelRadii(int levels)
    //{
    //    float[] radii = new float[levels];
    //    for (int i = 0; i < levels; i++)
    //    {
    //        radii[i] = 1.0f + (i * 1.5f); // Radios más grandes y mejor espaciados
    //    }
    //    return radii;
    //}
    //private Vector3 CalculateElectronPosition(float radius, int index, int totalInLevel)
    //{
    //    // Distribución simétrica de electrones en la órbita
    //    float angle = 2 * Mathf.PI * index / totalInLevel;
    //    return new Vector3(
    //        radius * Mathf.Cos(angle),
    //        0,
    //        radius * Mathf.Sin(angle));
    //}


    //private IEnumerator ScaleInAnimation(Transform target, float duration)
    //{
    //    Vector3 initialScale = Vector3.zero;
    //    Vector3 finalScale = target.localScale;
    //    float elapsed = 0f;

    //    target.localScale = initialScale;

    //    while (elapsed < duration)
    //    {
    //        target.localScale = Vector3.Lerp(initialScale, finalScale, elapsed / duration);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    target.localScale = finalScale;
    //}

    //private float[] CalculateLevelRadii(int levels)
    //{
    //    float[] radii = new float[levels];
    //    for (int i = 0; i < levels; i++)
    //    {
    //        radii[i] = 0.5f + (i * 0.8f); // Radios más grandes para mejor visualización
    //    }
    //    return radii;
    //}

    //private Vector3 CalculateElectronPosition(float radius, int index, int totalInLevel)
    //{
    //    // Distribución simétrica de electrones
    //    float angle = 2 * Mathf.PI * index / totalInLevel;
    //    return new Vector3(
    //        radius * Mathf.Cos(angle),
    //        0,
    //        radius * Mathf.Sin(angle));
    //}




    //private IEnumerator LoadMoleculeModel(int electronLevels, int protons, int neutrons, int electrons, string modelName, List<string> electronModels)
    //{
    //    // Definición de colores (puedes mover esto a variables de clase si lo necesitas en otros lugares)
    //    Color protonColor = new Color(0.8f, 0.2f, 0.2f);    // Rojo
    //    Color neutronColor = new Color(0.2f, 0.2f, 0.8f);   // Azul
    //    Color electronColor = new Color(0.878f, 0.408f, 0.169f); // naranja

    //    LimpiarModelos();

    //    string modelPath = "Moleculas/NuevoElemento/" + modelName;
    //    Debug.Log($"Intentando cargar modelo base: {modelPath}");

    //    ResourceRequest baseRequest = Resources.LoadAsync<GameObject>(modelPath);
    //    yield return baseRequest;

    //    if (baseRequest.asset != null)
    //    {
    //        GameObject molecule = Instantiate(baseRequest.asset as GameObject, transform);
    //        Debug.Log($"Modelo base {modelName} cargado correctamente");

    //        // Configuración de niveles de electrones
    //        float[] levelRadii = CalculateLevelRadii(electronLevels);
    //        int[] electronsPerLevel = CalculateElectronsPerLevel(electrons, electronLevels);

    //        // Crear órbitas y distribuir electrones por nivel
    //        for (int level = 0; level < electronLevels; level++)
    //        {
    //            float radius = levelRadii[level];
    //            int electronsInThisLevel = electronsPerLevel[level];

    //            // Distribuir electrones en este nivel
    //            for (int i = 0; i < electronsInThisLevel; i++)
    //            {
    //                string electronModel = electronModels[level % electronModels.Count];
    //                Vector3 position = CalculateElectronPosition(radius, i, electronsInThisLevel);
    //                StartCoroutine(LoadElectron(electronModel, molecule.transform, position, radius, electronColor));
    //            }
    //        }

    //        // Cargar protones y neutrones
    //        for (int i = 0; i < protons; i++)
    //        {
    //            StartCoroutine(LoadSubatomicParticle("SM_MOLECULA_PROTON", molecule.transform, protonColor));
    //        }

    //        for (int i = 0; i < neutrons; i++)
    //        {
    //            StartCoroutine(LoadSubatomicParticle("SM_MOLECULA_NEUTRON", molecule.transform, neutronColor));
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogError($"No se encontró el modelo base: {modelPath}");
    //    }
    //}

    //// Método modificado para electrones con color
    //private IEnumerator LoadElectron(string electronModel, Transform parent, Vector3 position, float orbitRadius, Color color)
    //{
    //    string path = "Moleculas/NuevoElemento/" + electronModel;
    //    ResourceRequest request = Resources.LoadAsync<GameObject>(path);
    //    yield return request;

    //    if (request.asset != null)
    //    {
    //        GameObject electron = Instantiate(request.asset as GameObject, parent);
    //        electron.transform.localPosition = position;

    //        // Aplicar color al electrón
    //        ApplyColorToParticle(electron, color);

    //        if (electron.TryGetComponent<Collider>(out var collider))
    //        {
    //            collider.enabled = false;
    //        }

    //        ElectronOrbit orbit = electron.AddComponent<ElectronOrbit>();
    //        orbit.Initialize(parent, orbitRadius, 0.5f);
    //    }
    //    else
    //    {
    //        Debug.LogError($"Modelo no encontrado: {electronModel}");
    //    }
    //}

    //// Método modificado para protones/neutrones con color
    //private IEnumerator LoadSubatomicParticle(string particleName, Transform parent, Color color)
    //{
    //    string particlePath = "Moleculas/NuevoElemento/" + particleName;
    //    ResourceRequest particleRequest = Resources.LoadAsync<GameObject>(particlePath);
    //    yield return particleRequest;

    //    if (particleRequest.asset != null)
    //    {
    //        GameObject particle = Instantiate(particleRequest.asset as GameObject, parent);
    //        particle.transform.localPosition = Random.insideUnitSphere * 0.1f;

    //        // Aplicar color a la partícula
    //        ApplyColorToParticle(particle, color);

    //        Debug.Log($"{particleName} cargado correctamente");
    //    }
    //    else
    //    {
    //        Debug.LogError($"No se encontró el modelo: {particleName}");
    //    }
    //}



    //// Nuevos métodos auxiliares
    //private float[] CalculateLevelRadii(int levels)
    //{
    //    float[] radii = new float[levels];
    //    for (int i = 0; i < levels; i++)
    //    {
    //        radii[i] = 0.5f + (i * 0.5f); // Ajusta estos valores según necesites
    //    }
    //    return radii;
    //}

    //private int[] CalculateElectronsPerLevel(int totalElectrons, int levels)
    //{
    //    int[] electronsPerLevel = new int[levels];
    //    int[] levelCapacity = { 2, 8, 18, 32, 32, 18, 8 }; // Capacidad por nivel
    //    int remaining = totalElectrons;
    //    for (int i = 0; i < levels && remaining > 0; i++)
    //    {
    //        // Usamos el mínimo entre la capacidad del nivel y los electrones restantes
    //        electronsPerLevel[i] = Mathf.Min(levelCapacity[i], remaining);
    //        remaining -= electronsPerLevel[i];
    //    }
    //    return electronsPerLevel;
    //}

    //private Vector3 CalculateElectronPosition(float radius, int index, int totalInLevel)
    //{
    //    // Misma fórmula que en CreateOrbitRing
    //    float angle = 2 * Mathf.PI * index / totalInLevel;
    //    angle /= 2;
    //    return new Vector3(
    //        radius * Mathf.Cos(angle),
    //        0,
    //        radius * Mathf.Sin(angle));
    //}
    //private Color GetLevelColor(int level)
    //{
    //    // Colores distintos por nivel
    //    Color[] colors = { Color.blue, Color.green, Color.yellow, Color.red };
    //    return colors[level % colors.Length];
    //}
    //private void LimpiarModelos()
    //{
    //    foreach (Transform child in imageTargetPrefab.transform)
    //    {
    //        Destroy(child.gameObject);
    //    }
    //}
//}

