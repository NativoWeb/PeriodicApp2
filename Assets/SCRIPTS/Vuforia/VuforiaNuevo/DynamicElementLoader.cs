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
        elementoTarget = PlayerPrefs.GetString("NumeroAtomico", "1").Trim() + "_" + PlayerPrefs.GetString("ElementoSeleccionado", "hidrogeno").Trim();
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
        CargarJSON();
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED)
        {
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

        // Llamar a la función LoadMoleculeModel con los datos extraídos
        StartCoroutine(LoadMoleculeModel(electronLevels , showProtons, showNeutrons, electrons, modelName, electronModels));
    }

    private IEnumerator LoadMoleculeModel(int electronLevels, int protons, int neutrons, int electrons, string modelName, List<string> electronModels)
    {
        // Definición de colores (puedes mover esto a variables de clase si lo necesitas en otros lugares)
        Color protonColor = new Color(0.8f, 0.2f, 0.2f);    // Rojo
        Color neutronColor = new Color(0.2f, 0.2f, 0.8f);   // Azul
        Color electronColor = new Color(0.878f, 0.408f, 0.169f); // naranja

        LimpiarModelos();

        string modelPath = "Moleculas/NuevoElemento/" + modelName;
        Debug.Log($"Intentando cargar modelo base: {modelPath}");

        ResourceRequest baseRequest = Resources.LoadAsync<GameObject>(modelPath);
        yield return baseRequest;

        if (baseRequest.asset != null)
        {
            GameObject molecule = Instantiate(baseRequest.asset as GameObject, transform);
            Debug.Log($"Modelo base {modelName} cargado correctamente");

            // Configuración de niveles de electrones
            float[] levelRadii = CalculateLevelRadii(electronLevels);
            int[] electronsPerLevel = CalculateElectronsPerLevel(electrons, electronLevels);

            // Crear órbitas y distribuir electrones por nivel
            for (int level = 0; level < electronLevels; level++)
            {
                float radius = levelRadii[level];
                int electronsInThisLevel = electronsPerLevel[level];

                // Crear órbita visual para este nivel
                CreateOrbitRing(molecule.transform, radius, GetLevelColor(level));

                // Distribuir electrones en este nivel
                for (int i = 0; i < electronsInThisLevel; i++)
                {
                    string electronModel = electronModels[level % electronModels.Count];
                    Vector3 position = CalculateElectronPosition(radius, i, electronsInThisLevel);
                    StartCoroutine(LoadElectron(electronModel, molecule.transform, position, radius, electronColor));
                }
            }

            // Cargar protones y neutrones
            for (int i = 0; i < protons; i++)
            {
                StartCoroutine(LoadSubatomicParticle("SM_MOLECULA_PROTON", molecule.transform, protonColor));
            }

            for (int i = 0; i < neutrons; i++)
            {
                StartCoroutine(LoadSubatomicParticle("SM_MOLECULA_NEUTRON", molecule.transform, neutronColor));
            }
        }
        else
        {
            Debug.LogError($"No se encontró el modelo base: {modelPath}");
        }
    }

    // Método modificado para electrones con color
    private IEnumerator LoadElectron(string electronModel, Transform parent, Vector3 position, float orbitRadius, Color color)
    {
        string path = "Moleculas/NuevoElemento/" + electronModel;
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);
        yield return request;

        if (request.asset != null)
        {
            GameObject electron = Instantiate(request.asset as GameObject, parent);
            electron.transform.localPosition = position;

            // Aplicar color al electrón
            ApplyColorToParticle(electron, color);

            if (electron.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }

            ElectronOrbit orbit = electron.AddComponent<ElectronOrbit>();
            orbit.Initialize(parent, orbitRadius, 0.5f);
        }
        else
        {
            Debug.LogError($"Modelo no encontrado: {electronModel}");
        }
    }

    // Método modificado para protones/neutrones con color
    private IEnumerator LoadSubatomicParticle(string particleName, Transform parent, Color color)
    {
        string particlePath = "Moleculas/NuevoElemento/" + particleName;
        ResourceRequest particleRequest = Resources.LoadAsync<GameObject>(particlePath);
        yield return particleRequest;

        if (particleRequest.asset != null)
        {
            GameObject particle = Instantiate(particleRequest.asset as GameObject, parent);
            particle.transform.localPosition = Random.insideUnitSphere * 0.1f;

            // Aplicar color a la partícula
            ApplyColorToParticle(particle, color);

            Debug.Log($"{particleName} cargado correctamente");
        }
        else
        {
            Debug.LogError($"No se encontró el modelo: {particleName}");
        }
    }

    // Método auxiliar para aplicar color a cualquier partícula
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
                propBlock.SetFloat("_Metallic", 0.7f);
                propBlock.SetFloat("_Glossiness", 0.9f);
            }

            renderer.SetPropertyBlock(propBlock);
        }
    }

    // Nuevos métodos auxiliares
    private float[] CalculateLevelRadii(int levels)
    {
        float[] radii = new float[levels];
        for (int i = 0; i < levels; i++)
        {
            radii[i] = 0.5f + (i * 0.5f); // Ajusta estos valores según necesites
        }
        return radii;
    }
    private int[] CalculateElectronsPerLevel(int totalElectrons, int levels)
    {
        int[] electronsPerLevel = new int[levels];
        int[] levelCapacity = { 2, 8, 18, 32, 32, 18, 8 }; // Capacidad por nivel
        int remaining = totalElectrons;
        for (int i = 0; i < levels && remaining > 0; i++)
        {
            // Usamos el mínimo entre la capacidad del nivel y los electrones restantes
            electronsPerLevel[i] = Mathf.Min(levelCapacity[i], remaining);
            remaining -= electronsPerLevel[i];
        }
        return electronsPerLevel;
    }
    private Vector3 CalculateElectronPosition(float radius, int index, int totalInLevel)
    {
        // Misma fórmula que en CreateOrbitRing
        float angle = 2 * Mathf.PI * index / totalInLevel;
        return new Vector3(
            radius * Mathf.Cos(angle),
            0,
            radius * Mathf.Sin(angle));
    }
    private void CreateOrbitRing(Transform parent, float radius, Color color) 
    {
        //GameObject orbit = new GameObject($"Orbit_{radius}");
        //orbit.transform.SetParent(parent);
        //orbit.transform.localPosition = Vector3.zero;
        //orbit.transform.localRotation = Quaternion.identity;
        //orbit.transform.localScale = Vector3.one;
        //LineRenderer line = orbit.AddComponent<LineRenderer>();
        //line.useWorldSpace = false;
        //line.loop = true;
        //line.startWidth = 0.5f;
        //line.endWidth = 0.5f;
        //line.positionCount = 100; // Número de puntos igual al cálculo de electrones
        //Material mat = new Material(Shader.Find("Sprites/Default"));
        //mat.color = color;
        //line.material = mat;
        //Vector3[] points = new Vector3[100];
        //for (int i = 0; i < 100; i++) {
        //    float angle = 2 * Mathf.PI * i / 100;
        //    points[i] = new Vector3(
        //        radius * Mathf.Cos(angle),
        //        0,
        //        radius * Mathf.Sin(angle)
        //    );
        //}
        //line.SetPositions(points);
    }
    private Color GetLevelColor(int level)
    {
        // Colores distintos por nivel
        Color[] colors = { Color.blue, Color.green, Color.yellow, Color.red };
        return colors[level % colors.Length];
    }
    private void LimpiarModelos()
    {
        foreach (Transform child in imageTargetPrefab.transform)
        {
            Destroy(child.gameObject);
        }
    }
}

