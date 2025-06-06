using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using SimpleJSON;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class LogrosManager : MonoBehaviour
{
    [Header("Prefabs y contenedores")]
    public GameObject categoriaPrefab;
    public GameObject elementoPrefab;
    public Transform categoriaPanel;
    public Transform elementoPanel;

    [Header("Paneles y Botones")]
    public GameObject PanelLogros;
    public GameObject PanelDatos;
    public Button BtnDatos;

    private Dictionary<string, UI.Categoria> categorias;
    private Dictionary<string, Elemento> elementos;
    private JSONNode jsonData;

    private void Awake()
    {
        CargarJSON();
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
    }

    private IEnumerator Start()
    {
        if (jsonData == null)
        {
            yield return CargarJSON(); // esperamos hasta que el JSON esté cargado
        }

        if (jsonData == null || !jsonData.HasKey("Logros") || !jsonData["Logros"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida.");
            yield break;
        }

        InicializarLogros(); // extraemos esta lógica a un nuevo método
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

    private void InicializarLogros()
    {
        var categoriasJson = jsonData["Logros"]["Categorias"];
        categorias = new Dictionary<string, UI.Categoria>();
        elementos = new Dictionary<string, Elemento>();

        foreach (var categoriaKey in categoriasJson.Keys)
        {
            var categoriaData = categoriasJson[categoriaKey];

            CategoriaData categoriaInfo = new CategoriaData
            {
                nombre = categoriaKey,
                TituloMisionFinal = categoriaData["logro_categoria"]["nombre"],
                Elementos = new Dictionary<string, ElementoData>()
            };

            bool misionFinalDesbloqueada = categoriaData["logro_categoria"]["desbloqueado"].AsBool;
            var elementosJson = categoriaData["logros_elementos"];

            foreach (KeyValuePair<string, JSONNode> kvpElemento in elementosJson.AsObject)
            {
                string elementoKey = kvpElemento.Key;
                JSONNode elementoData = kvpElemento.Value;

                ElementoData elementoInfo = new ElementoData
                {
                    simbolo = elementoData["simbolo"],
                    logro = elementoData["nombre"],
                    misiones = new List<MisionData>()
                };

                categoriaInfo.Elementos[elementoKey] = elementoInfo;
            }

            UI.Categoria categoria = new UI.Categoria(categoriaKey, categoriaInfo, misionFinalDesbloqueada);
            categorias.Add(categoriaKey, categoria);

            CreateCategoriaLogro(categoria);

            foreach (var elementoData in categoria.ElementosData.Values)
            {
                Elemento nuevoElemento = new Elemento(elementoData);
                elementos.Add(nuevoElemento.Simbolo, nuevoElemento);
                CreateElementoLogro(categoria, nuevoElemento);
            }
        }
    }

    private void CreateCategoriaLogro(UI.Categoria categoria)
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

        logroCategoria.ActualizarDesdeCategoria(categoria.TituloMisionFinal, categoria.EstaCompletada());
    }

    private void CreateElementoLogro(UI.Categoria categoria, Elemento elemento)
    {
        if (elementoPrefab == null || elementoPanel == null)
        {
            Debug.LogError("❌ Error: Los prefabs o paneles de elemento no están asignados.");
            return;
        }

        GameObject elementoObj = Instantiate(elementoPrefab, elementoPanel);
        LogroElemento logroElemento = elementoObj.GetComponent<LogroElemento>();

        if (logroElemento == null)
        {
            Debug.LogError("❌ Error: No se encontró el script LogroElemento en el prefab.");
            return;
        }

        logroElemento.ActualizarLogro(
            elemento.Simbolo,
            elemento.Logro,
            elemento.EstaCompletado(), // Será false ya que no hay misiones en este JSON
            categoria.Nombre
        );
    }

    private void AbrirPanelDatos()
    {
        PanelDatos.SetActive(true);
        PanelLogros.SetActive(false);
    }
}

[System.Serializable]
public class CategoriaData
{
    public string nombre;
    public string TituloMisionFinal;
    public Dictionary<string, ElementoData> Elementos;
}

[System.Serializable]
public class ElementoData
{
    public string simbolo;
    public string logro;
    public List<MisionData> misiones;
}

[System.Serializable]
public class MisionData
{
    public int id;
    public string titulo;
    public string descripcion;
    public string tipo;
    public bool completada;
    public string rutaescena;
}

namespace UIm
{
    public class Mision
    {
        public int id;
        public string titulo;
        public string descripcion;
        public string tipo;
        public bool completada;
        public string rutaEscena;

        public Mision(MisionData data)
        {
            id = data.id;
            titulo = data.titulo;
            descripcion = data.descripcion;
            tipo = data.tipo;
            completada = data.completada;
            rutaEscena = data.rutaescena;
        }
    }

    public class Categoria
    {
        public string Nombre { get; private set; }
        public string TituloMisionFinal { get; private set; }
        public Dictionary<string, ElementoData> ElementosData { get; private set; }
        private bool completada;

        public Categoria(string nombre, CategoriaData data, bool completada)
        {
            Nombre = nombre;
            TituloMisionFinal = data.TituloMisionFinal;
            ElementosData = data.Elementos;
            this.completada = completada;
        }

        public bool EstaCompletada()
        {
            return completada;
        }
    }
}

public class Elemento
{
    public string Simbolo { get; private set; }
    public string Logro { get; private set; }
    public List<UIm.Mision> Misiones { get; private set; }

    public Elemento(ElementoData data)
    {
        Simbolo = data.simbolo;
        Logro = data.logro;
        Misiones = data.misiones != null ? data.misiones.Select(m => new UIm.Mision(m)).ToList() : new List<UIm.Mision>();
    }

    public bool EstaCompletado()
    {
        return Misiones != null && Misiones.Count > 0 && Misiones.All(m => m.completada);
    }
}
