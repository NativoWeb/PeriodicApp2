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
    private string ruta;
    private Dictionary<string, Element> elementDatabase = new Dictionary<string, Element>();
    private JSONNode jsonData;  // Estructura para manejar el JSON

    private ObserverBehaviour trackable;
    private ControllerBotones ControladorBotones;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;


    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        ControladorBotones = FindAnyObjectByType<ControllerBotones>();

        elementoTarget = PlayerPrefs.GetString("NumeroAtomico", "").Trim() + "_" + PlayerPrefs.GetString("ElementoSeleccionado", "").Trim();
        ruta = PlayerPrefs.GetString("CargarVuforia", "");

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (ruta == "Misiones")
        {
            if (trackable.TargetName.Trim().ToLower() != elementoTarget.Trim().ToLower())
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        LimpiarModelos();
        if (status.Status == Status.TRACKED)
        {
            string resultado = trackable.TargetName.Split('_')[1];
            elementoSeleccionado = resultado.ToLower();
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
            }else
                DesbloquearLogro(trackable.TargetName);
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
        FindAnyObjectByType<ModeloLoader>()?.InicializarCambioVisual(
            elementoSeleccionado, imageTargetPrefab
        );
    }

    private IEnumerator LoadMoleculeModel(int electronLevels, int protons, int neutrons, int electrons, string modelName, List<string> electronModels)
    {
        // 1. Configuración exacta de distribución de electrones por nivel
        int[] levelCapacity = { 2, 8, 18, 32, 32, 18, 8 }; // Capacidad máxima por nivel
        float[] orbitRadii = { .2f, .25f, .3f, .35f, .4f, .45f, .5f }; // Radios para cada capa

        // 2. Distribución exacta de electrones
        int[] electronDistribution = new int[electronLevels];
        int remaining = electrons;

        for (int level = 0; level < electronLevels && remaining > 0; level++)
        {
            int maxForLevel = level < levelCapacity.Length ? levelCapacity[level] : levelCapacity[levelCapacity.Length - 1];
            electronDistribution[level] = Mathf.Min(maxForLevel, remaining);
            remaining -= electronDistribution[level];
        }

        // 3. Generación visual
        GameObject atomContainer = new GameObject("AtomContainer");
        atomContainer.transform.SetParent(imageTargetPrefab.transform);
        atomContainer.transform.localPosition = Vector3.zero;

        // Núcleo
        yield return StartCoroutine(CreateNucleus(atomContainer.transform, protons, neutrons,
            new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.2f, 0.8f)));

        // Generar todas las capas y electrones
        for (int level = 0; level < electronLevels; level++)
        {
            float radius = orbitRadii[level];
            int electronsInLevel = electronDistribution[level];

            if (electronsInLevel <= 0) continue;

            // Crear órbita con animación completa
            GameObject orbit = CreateOrbitRing(atomContainer.transform, radius,
                new Color(1, 1, 1, 0.8f), level, electronsInLevel);

            // Distribución simétrica de electrones
            float angleStep = 360f / electronsInLevel;
            float currentAngle = 0f;

            for (int e = 0; e < electronsInLevel; e++)
            {
                Vector3 electronPos = new Vector3(
                    radius * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                    0,
                    radius * Mathf.Sin(currentAngle * Mathf.Deg2Rad)
                );

                yield return StartCoroutine(CreateElectron(
                    electronModels[level % electronModels.Count],
                    orbit.transform,
                    electronPos,
                    new Color(0.878f, 0.408f, 0.169f),
                    3f,
                    level
                ));

                currentAngle += angleStep;
                yield return new WaitForSeconds(0.03f);
            }
        }

        StartCoroutine(EnableAnimationsAfterDelay(atomContainer, 1f));
        yield break;
    }

    private IEnumerator CreateElectron(string modelName, Transform parent, Vector3 position, Color color, float size, int level)
    {
        string path = "Moleculas/NuevoElemento/" + modelName;
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
        yield return request;

        if (request.asset != null)
        {
            GameObject electron = Instantiate(request.asset as GameObject, parent);
            electron.transform.localPosition = position;
            electron.transform.localScale = Vector3.one * size;

            // Configurar renderer
            Renderer renderer = electron.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
                renderer.material.SetFloat("_Glossiness", 0.9f);
                renderer.material.SetFloat("_Metallic", 0.7f);
            }

            // Añadir comportamiento orbital sincronizado con la órbita padre
            var orbitBehavior = electron.AddComponent<ElectronOrbit>();
            orbitBehavior.Configure(level);
        }
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
        line.startWidth = 0.008f;
        line.endWidth = 0.008f;
        line.positionCount = 100;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Mode", 3); // Transparent mode
        line.material = mat;

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
    }

    private IEnumerator CreateNucleus(Transform parent, int protons, int neutrons, Color protonColor, Color neutronColor)
    {
        GameObject nucleus = new GameObject("Nucleus");
        nucleus.transform.SetParent(parent);
        nucleus.transform.localPosition = Vector3.zero;

        // Nuclear particle distribution
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
        elementoSeleccionado = "";
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