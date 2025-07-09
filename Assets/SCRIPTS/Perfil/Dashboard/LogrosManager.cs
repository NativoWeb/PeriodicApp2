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
    public Transform categoriaPanel; 

    [Header("Paneles y Botones")]
    public GameObject PanelLogrosCat;
    public GameObject PanelLogrosElementos;
    public Button BtnDatos;

    private Dictionary<string, Elemento> elementos;
    private JSONNode jsonData;
    private bool isLoading = false;

    private void Awake()
    {
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
    }

    private void OnEnable()
    {
        if (isLoading) return;

        // Iniciar la secuencia completa de inicialización.
        StartCoroutine(InitializeSequence());
    }

    // Esta corrutina gestiona todo el proceso en el orden correcto.
    // EN: LogrosManager.cs

    // ... (El resto de tu código, variables, Awake, OnEnable, etc. se queda igual) ...
    // ... (La estructura con Contenedor_Header y las asignaciones del Inspector son correctas) ...

    // --- LA CORRUTINA DEFINITIVA PARA EL LAYOUT ---
    private IEnumerator InitializeSequence()
    {
        isLoading = true;

        // 1. Limpiar la UI y esperar un frame para que los Destroy() se procesen.
        LimpiarLogros();
        yield return null;

        // 2. Cargar datos del JSON.
        if (jsonData == null)
        {
            yield return StartCoroutine(CargarJSON());
        }

        if (jsonData == null)
        {
            Debug.LogError("❌ No se pudo inicializar la UI porque el JSON no está disponible.");
            isLoading = false;
            yield break;
        }

        // 3. Poblar la UI con los nuevos prefabs. En este momento, el layout está roto.
        PopulateUI();


        if (elementoPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(elementoPanel.GetComponent<RectTransform>());
        }


        yield return new WaitForEndOfFrame();


        if (categoriaPanel != null && categoriaPanel.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(categoriaPanel.parent.GetComponent<RectTransform>());
        }

        Debug.Log("Layout reconstruido correctamente con el método de doble pasada.");

        isLoading = false;
    }

    private void PopulateUI()
    {
        string catSeleccionada = PlayerPrefs.GetString("CatSeleccionada", "");
        if (string.IsNullOrEmpty(catSeleccionada))
        {
            Debug.LogError("No hay categoría seleccionada.");
            return;
        }

        NombreCategoria.text = catSeleccionada;

        var categoriasJson = jsonData["Logros"]["Categorias"];
        if (!categoriasJson.HasKey(catSeleccionada))
        {
            Debug.LogError($"La categoría '{catSeleccionada}' no existe en el JSON.");
            return;
        }

        var categoriaJson = categoriasJson[catSeleccionada];

        // Crear datos de categoría
        var categoriaInfo = new CategoriaData
        {
            nombre = categoriaJson["logro_categoria"]["nombre"],
            desbloqueado = categoriaJson["logro_categoria"]["desbloqueado"].AsBool,
            Elementos = new Dictionary<string, ElementoData>()
        };

        var elementosJson = categoriaJson["logros_elementos"].AsObject;
        foreach (var kvp in elementosJson)
        {
            JSONNode nodoElem = kvp.Value;
            categoriaInfo.Elementos[kvp.Key] = new ElementoData
            {
                nombre = nodoElem["nombre"],
                simbolo = nodoElem["simbolo"],
                desbloqueado = nodoElem["desbloqueado"].AsBool
            };
        }

        // Crear la UI
        var categoriaUI = new UI.Categoria(catSeleccionada, categoriaInfo, categoriaInfo.desbloqueado);
        CreateCategoriaLogro(categoriaUI);

        elementos = new Dictionary<string, Elemento>();
        foreach (var elemPair in categoriaInfo.Elementos)
        {
            var nuevoElem = new Elemento(elemPair.Value);
            elementos.Add(elemPair.Value.simbolo, nuevoElem);
            CreateElementoLogro(categoriaUI, nuevoElem);
        }
    }

    // Ya no necesita forzar reconstrucciones, solo limpiar.
    public void LimpiarLogros()
    {
        // 'categoriaPanel' es ahora 'Contenedor_Header'
        if (categoriaPanel != null)
        {
            // Esto buscará hijos DENTRO de 'Contenedor_Header' (el prefab que instancraste)
            // y los destruirá, pero dejará 'Contenedor_Header' intacto.
            foreach (Transform child in categoriaPanel.transform)
                Destroy(child.gameObject);
        }

        // ...lo mismo para elementoPanel...
        if (elementoPanel != null)
        {
            foreach (Transform child in elementoPanel.transform)
                Destroy(child.gameObject);
        }

        elementos?.Clear();
    }

    #region Métodos sin cambios
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
            Debug.LogWarning("⚠️ Json_Logros.json no encontrado en persistentDataPath, buscando en Resources...");
            bool completado = false;
            yield return StartCoroutine(CargarDesdeResources("Json_Logros.json", (json) =>
            {
                if (json != null)
                {
                    jsonData = JSON.Parse(json);
                    Debug.Log("📄 Json_Logros.json cargado temporalmente desde Resources.");
                }
                completado = true;
            }));
            if (!completado || jsonData == null)
            {
                Debug.LogError("❌ Falló la carga del JSON desde Resources.");
            }
        }
    }

    private IEnumerator CargarDesdeResources(string nombreArchivo, System.Action<string> callback)
    {
        string ruta = $"Plantillas_Json/{Path.GetFileNameWithoutExtension(nombreArchivo)}";
        TextAsset archivo = Resources.Load<TextAsset>(ruta);
        yield return null;
        if (archivo != null)
        {
            callback(archivo.text);
        }
        else
        {
            Debug.LogError($"❌ No se encontró el archivo {nombreArchivo} en Resources/Plantillas_Json/");
            callback(null);
        }
    }

    // EN: LogrosManager.cs

    private void CreateCategoriaLogro(UI.Categoria categoriaUI)
    {
        // Esta comprobación ahora se asegura de que el CONTENEDOR exista.
        if (categoriaPrefab == null || categoriaPanel == null)
        {
            Debug.LogError("Prefab de categoría o panel CONTENEDOR de categoría no asignados.");
            return;
        }

        // --- CAMBIO CLAVE ---
        // Instancia el prefab y lo haces hijo del contenedor 'categoriaPanel'.
        GameObject go = Instantiate(categoriaPrefab, categoriaPanel.transform); // <-- El .transform es importante

        var view = go.GetComponent<LogroCategoria>();
        if (view == null)
        {
            Debug.LogError("El prefab de categoría no tiene LogroCategoria.");
            return;
        }

        // El resto de la lógica sigue igual...
        int total = categoriaUI.ElementosData.Count;
        int completados = 0;
        foreach (var e in categoriaUI.ElementosData.Values)
            if (e.desbloqueado) completados++;

        view.MostrarDesdeElemento(
            categoriaUI.Nombre,
            total,
            completados,
            categoriaUI.Desbloqueado
        );
    }

    private void CreateElementoLogro(UI.Categoria categoriaUI, Elemento elemento)
    {
        if (elementoPrefab == null || elementoPanel == null) return;
        GameObject go = Instantiate(elementoPrefab, elementoPanel);
        var view = go.GetComponent<LogroElemento>();
        if (view == null) return;
        view.ActualizarLogro(elemento.Nombre, elemento.Simbolo, categoriaUI.Nombre, elemento.Desbloqueado);
    }

    private void AbrirPanelDatos()
    {
        PanelLogrosCat.SetActive(true);
        PanelLogrosElementos.SetActive(false);
        PlayerPrefs.DeleteKey("CatSeleccionada");
    }
    #endregion
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