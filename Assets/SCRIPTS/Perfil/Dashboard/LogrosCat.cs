using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.UI;
using System.IO;
using System.Collections;

public class LogrosManagarCat: MonoBehaviour
{

    [Header("Prefabs y contenedores")]
    public GameObject categoriaPrefab;
    public Transform categoriaPanel;

    [Header("Paneles y Botones")]
    public GameObject PanelLogrosCat;
    public GameObject PanelDatos;
    public GameObject PanelLogrosElementos;
    public Button BtnDatos;
    public Button BtnLogros;
    public Button BtnVolver;
    private Dictionary<string, UI.Categoria> categorias;
    private JSONNode jsonData;
    private bool isInitialized;
    private void Start()
    {
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
    }

    private void OnEnable() 
    {
        // Si ya está inicializado, no hagas nada.
        // Esto evita que se reconstruya todo si solo ocultas y muestras el panel.
        if (isInitialized) return;

        // Arrancamos la secuencia de carga + inicialización
        StartCoroutine(LoadJsonThenInit());
    }

    private IEnumerator LoadJsonThenInit()
    {
        // 1) Carga el JSON (solo si no se ha cargado antes)
        if (jsonData == null)
        {
            yield return StartCoroutine(CargarJSON());
        }

        // 2) Comprueba que vino bien
        if (jsonData == null || !jsonData.HasKey("Logros") || !jsonData["Logros"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida después de cargar.");
            yield break;
        }

        // 3) Limpiamos cualquier UI previa
        LimpiarCategoriasUI();

        // 4) Inicializar
        InicializarLogros();

        // --- LA MAGIA ESTÁ AQUÍ ---
        // Ahora que el panel está ACTIVO, forzamos la reconstrucción del layout
        yield return new WaitForEndOfFrame();

        // ✅ Reconstruir el layout del panel de categorías
        if (categoriaPanel != null)
        {
            RectTransform panelRect = categoriaPanel.GetComponent<RectTransform>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            // ✅ También reconstruir el ScrollView padre si existe
            Transform parent = categoriaPanel.parent;
            if (parent != null)
            {
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                if (parentRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }
            }

            // ✅ Forzar actualización de Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log("✅ Layout de categorías reconstruido completamente.");
        }

        isInitialized = true; // <-- Marcar como inicializado
    }

    private IEnumerator CargarJSON()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "Json_Logros.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            jsonData = JSON.Parse(jsonString);
            Debug.Log("✅ Json_Logros.json cargado desde persistentDataPath.");
        }
        else
        {
            Debug.LogWarning("⚠️ Json_Logros.json no encontrado en persistentDataPath, buscando en Resource...");

            bool completado = false;

            yield return StartCoroutine(CargarDesdeResources("Json_Logros.json", (json) =>
            {
                jsonData = JSON.Parse(json);
                Debug.Log("📄 Json_Logros.json cargado temporalmente desde Resource.");
                completado = true;
            }));

            if (!completado)
            {
                Debug.LogError("❌ Falló la carga del JSON desde Resource.");
            }
        }
    }
    private IEnumerator CargarDesdeResources(string nombreArchivo, System.Action<string> callback)
    {
        string ruta = $"Plantillas_Json/{Path.GetFileNameWithoutExtension(nombreArchivo)}";

        TextAsset archivo = Resources.Load<TextAsset>(ruta);

        yield return null; // Necesario para que funcione como Coroutine

        if (archivo != null)
        {
            if (string.IsNullOrEmpty(archivo.text))
            {
                Debug.LogWarning($"⚠️ El archivo {nombreArchivo} está vacío en Resources.");
            }
            callback(archivo.text);
        }
        else
        {
            Debug.LogError($"❌ No se encontró el archivo {nombreArchivo} en Resources/Plantillas_Json/");
            callback(null);
        }
    }
    public void InicializarLogros()
    {
        // LimpiarCategoriasUI() se llama ahora en LoadJsonThenInit

        var categoriasJson = jsonData["Logros"]["Categorias"];
        categorias = new Dictionary<string, UI.Categoria>();

        foreach (var categoriaKey in categoriasJson.Keys)
        {
            // ... (El resto de tu lógica para crear botones de categoría) ...
            // ... es exactamente la misma que ya tenías ...
            var categoriaData = categoriasJson[categoriaKey];
            bool desbloqueado = categoriaData["logro_categoria"]["desbloqueado"];
            int totalElementos = 0;
            int elementosCompletados = 0;
            if (categoriaData.HasKey("logros_elementos"))
            {
                var elementosJson = categoriaData["logros_elementos"];
                foreach (var elementoKey in elementosJson.Keys)
                {
                    totalElementos++;
                    if (elementosJson[elementoKey]["desbloqueado"].AsBool)
                        elementosCompletados++;
                }
            }
            UI.Categoria categoria = new UI.Categoria(categoriaKey, new CategoriaData
            {
                nombre = categoriaKey,
                desbloqueado = desbloqueado
            }, desbloqueado);
            categorias.Add(categoriaKey, categoria);
            CreateCategoriaLogro(categoria, totalElementos, elementosCompletados);
        }
    }

    private void CreateCategoriaLogro(UI.Categoria categoria, int totalElementos, int elementosCompletados)
    {
        if (categoriaPrefab == null || categoriaPanel == null)
        {
            Debug.LogError("❌ Error: Los prefabs o paneles de categoría no están asignados.");
            return;
        }

        GameObject categoriaObj = Instantiate(categoriaPrefab, categoriaPanel);
        LogroCategoria logroCategoria = categoriaObj.GetComponent<LogroCategoria>();

        if (logroCategoria == null)
        {
            Debug.LogError("❌ Error: No se encontró el script LogroCategoria en el prefab.");
            return;
        }

        logroCategoria.MostrarDesdeCategoria(categoria.Nombre, categoria.Desbloqueado, totalElementos, elementosCompletados);

        Button boton = categoriaObj.GetComponent<Button>();
        if (boton == null)
        {
            Debug.LogError("El prefab no tiene un componente Button.");
            return;
        }

        Debug.Log("Categoria creada:" + categoria.Nombre);

        boton.onClick.AddListener(() =>
        {
            SeleccionarElemento(categoria.Nombre);
        });
    }

    public void SeleccionarElemento(string nombreCategoria)
    {
        Debug.Log($"➡ CatSeleccionada: {nombreCategoria}");
        PlayerPrefs.SetString("CatSeleccionada", nombreCategoria);
        PlayerPrefs.Save();

        PanelDatos.SetActive(false);
        PanelLogrosCat.SetActive(false);
        PanelLogrosElementos.SetActive(true);
    }

    private void AbrirPanelDatos()
    {
        PanelDatos.SetActive(true);
        PanelLogrosCat.SetActive(false);
    }

    private void LimpiarCategoriasUI()
    {
        // Destruir todos los objetos hijos del panel
        foreach (Transform child in categoriaPanel)
        {
            Destroy(child.gameObject);
        }

        // Limpiar el diccionario de categorías
        if (categorias != null)
            categorias.Clear();

        // ✅ Resetear la posición del RectTransform del panel de categorías
        RectTransform panelRect = categoriaPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchoredPosition = Vector2.zero;

            // ✅ También resetear el ScrollView padre si existe
            Transform parent = categoriaPanel.parent;
            if (parent != null)
            {
                ScrollRect scrollRect = parent.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 1f; // Scroll al inicio (arriba)
                }
            }
        }

        Debug.Log("🧹 Panel de categorías limpiado y reseteado.");
    }

}

[System.Serializable]
public class CategoriaData
{
    public string nombre;
    public bool desbloqueado;
    public Dictionary<string, ElementoData> Elementos;
}