using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using TMPro;

public class LogrosManager : MonoBehaviour
{
    [Header("Prefabs y contenedores")]
    public TMP_Text NombreCategoria;
    public GameObject categoriaPrefab;
    public GameObject elementoPrefab;
    public Transform elementoPanel;
    public GameObject categoriaPanel;

    [Header("Paneles y Botones")]
    public GameObject PanelLogrosCat;
    public GameObject PanelLogrosElementos;
    public Button BtnDatos;

    private Dictionary<string, Elemento> elementos;

    private JSONNode jsonData;

    private void Awake()
    {
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
        // Arrancamos la secuencia de carga + inicialización
        StartCoroutine(LoadJsonThenInit());
    }

    private IEnumerator LoadJsonThenInit()
    {
        // 1) Carga el JSON
        yield return StartCoroutine(CargarJSON());

        // 2) Comprueba que vino bien
        if (jsonData == null ||
            !jsonData.HasKey("Logros") ||
            !jsonData["Logros"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida después de cargar.");
            yield break;
        }
    }
    private void OnEnable()
    {
        InicializarLogros();
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
            Debug.LogWarning("⚠️ Json_Logros.json no encontrado en persistentDataPath, buscando en StreamingAssets...");

            bool completado = false;

            yield return StartCoroutine(CargarDesdeResources("Json_Logros.json", (json) =>
            {
                jsonData = JSON.Parse(json);
                Debug.Log("📄 Json_Logros.json cargado temporalmente desde StreamingAssets.");
                completado = true;
            }));

            if (!completado)
            {
                Debug.LogError("❌ Falló la carga del JSON desde StreamingAssets.");
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
        LimpiarLogros();
        // 1. Obtener nombre de categoría seleccionada
        string catSeleccionada = PlayerPrefs.GetString("CatSeleccionada", "");

        NombreCategoria.text = catSeleccionada;

        if (string.IsNullOrEmpty(catSeleccionada))
        {
            Debug.LogError("No hay categoría seleccionada en PlayerPrefs (CatSeleccionada).");
            return;
        }

        var categoriasJson = jsonData["Logros"]["Categorias"];
        if (!categoriasJson.HasKey(catSeleccionada))
        {
            Debug.LogError($"La categoría '{catSeleccionada}' no existe en el JSON.");
            return;
        }

        // 2. Tomar sólo el nodo de la categoría seleccionada
        var categoriaJson = categoriasJson[catSeleccionada];

        // 3. Construir CategoriaData
        CategoriaData categoriaInfo = new CategoriaData
        {
            nombre = categoriaJson["logro_categoria"]["nombre"],
            desbloqueado = categoriaJson["logro_categoria"]["desbloqueado"].AsBool,
            Elementos = new Dictionary<string, ElementoData>()
        };

        // 4. Recorrer cada elemento del nodo "logros_elementos"
        var elementosJson = categoriaJson["logros_elementos"].AsObject;
        foreach (var kvp in elementosJson)
        {
            string claveElemento = kvp.Key;      // ej. "Litio"
            JSONNode nodoElem = kvp.Value;

            ElementoData elemInfo = new ElementoData
            {
                nombre = nodoElem["nombre"],
                simbolo = nodoElem["simbolo"],
                desbloqueado = nodoElem["desbloqueado"].AsBool
            };

            categoriaInfo.Elementos[claveElemento] = elemInfo;
        }

        // 5. Crear instancia de UI.Categoria y mostrarla
        UI.Categoria categoriaUI = new UI.Categoria(catSeleccionada, categoriaInfo, categoriaInfo.desbloqueado);

        CreateCategoriaLogro(categoriaUI);

        // 6. Para cada elemento en esa categoría, crear el logro
        elementos = new Dictionary<string, Elemento>();
        foreach (var elemPair in categoriaInfo.Elementos)
        {
            ElementoData ed = elemPair.Value;
            Elemento nuevoElem = new Elemento(ed);
            elementos.Add(ed.simbolo, nuevoElem);
            CreateElementoLogro(categoriaUI, nuevoElem);
        }
    }

    private void CreateCategoriaLogro(UI.Categoria categoriaUI)
    {
        if (categoriaPrefab == null || categoriaPanel == null)
        {
            Debug.LogError("Prefab de categoría o panel de categoría no asignados.");
            return;
        }

        // 1) Instancia el prefab correcto bajo el panel de categorías
        GameObject go = Instantiate(categoriaPrefab, categoriaPanel.transform);
        var view = go.GetComponent<LogroCategoria>();
        if (view == null)
        {
            Debug.LogError("El prefab de categoría no tiene LogroCategoria.");
            return;
        }

        // 2) Calcula totales
        int total = categoriaUI.ElementosData.Count;
        int completados = 0;
        foreach (var e in categoriaUI.ElementosData.Values)
            if (e.desbloqueado) completados++;

        // 3) Llama al método correcto
        view.MostrarDesdeElemento(
            categoriaUI.Nombre,
            total,
            completados,
            categoriaUI.Desbloqueado
        );
    }

    private void CreateElementoLogro(UI.Categoria categoriaUI, Elemento elemento)
    {
        if (elementoPrefab == null || elementoPanel == null)
        {
            Debug.LogError("Prefab o panel de elemento no asignados.");
            return;
        }

        // 1) Instancia el prefab de elemento bajo el panel de elementos
        GameObject go = Instantiate(elementoPrefab, elementoPanel);

        // 2) Obtén el componente que pinta el elemento (tu script LogroElemento)
        var view = go.GetComponent<LogroElemento>();
        if (view == null)
        {
            Debug.LogError("El prefab de elemento no tiene LogroElemento.");
            return;
        }

        // 3) Pásale los datos adecuados
        view.ActualizarLogro(
            elemento.Nombre,         // nombre a mostrar
            elemento.Simbolo,        // para texto secundario o ruta
            categoriaUI.Nombre,      // carpeta de Resources
            elemento.Desbloqueado    // estado
        );
    }

    private void AbrirPanelDatos()
    {
        PanelLogrosCat.SetActive(true);
        PanelLogrosElementos.SetActive(false);
        PlayerPrefs.DeleteKey("CatSeleccionada");
    }

    public void LimpiarLogros()
    {
        // 1) Destruye todos los hijos de categorías
        if (categoriaPanel != null)
        {
            foreach (Transform child in categoriaPanel.transform)
                Destroy(child.gameObject);
        }

        // 2) Destruye todos los hijos de elementos
        if (elementoPanel != null)
        {
            foreach (Transform child in elementoPanel)
                Destroy(child.gameObject);
        }

        // 3) Limpia tu diccionario de datos
        elementos?.Clear();

        // 4) Forzar que Unity recalcule inmediatamente el UI vacío
        Canvas.ForceUpdateCanvases();

        // 5) Resetear el RectTransform del panel de elementos
        if (elementoPanel != null)
        {
            var rt = elementoPanel.GetComponent<RectTransform>();
            // Pon altura a 0 para “vaciar” visualmente
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 0);
            rt.anchoredPosition = Vector2.zero;
        }

        // 6) Rebuild del layoutGroup para que ajuste a 0 hijos
        var layout = elementoPanel.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)elementoPanel);

        // 7) (Opcional) otra pasada de ForceUpdateCanvases
        Canvas.ForceUpdateCanvases();
    }
}

[System.Serializable]
public class ElementoData
{
    public string nombre;
    public string simbolo;
    public bool desbloqueado;
}

public class Elemento
{
    public string Nombre { get; private set; }
    public string Simbolo { get; private set; }

    public bool Desbloqueado;

    public Elemento(ElementoData data)
    {
        Nombre = data.nombre;
        Simbolo = data.simbolo;
        Desbloqueado = data.desbloqueado;
    }
}
