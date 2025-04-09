using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using SimpleJSON;

public class LogrosManager : MonoBehaviour
{
    public GameObject categoriaPrefab;
    public GameObject elementoPrefab;
    public Transform categoriaPanel;
    public Transform elementoPanel;

    private Dictionary<string, UI.Categoria> categorias;
    private Dictionary<string, Elemento> elementos;
    private JSONNode jsonData;

    private void Awake()
    {
        CargarJSON();
    }

    private void Start()
    {
        if (jsonData == null || !jsonData.HasKey("Misiones_Categorias") || !jsonData["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida.");
            return;
        }

        var categoriasJson = jsonData["Misiones_Categorias"]["Categorias"];
        categorias = new Dictionary<string, UI.Categoria>();
        elementos = new Dictionary<string, Elemento>();

        foreach (var categoriaKey in categoriasJson.Keys)
        {
            var categoriaData = categoriasJson[categoriaKey];

            CategoriaData categoriaInfo = new CategoriaData
            {
                nombre = categoriaKey,
                TituloMisionFinal = categoriaData["Mision Final"]["MisionFinal"]["titulo"],
                Elementos = new Dictionary<string, ElementoData>()
            };

            if (categoriaData.HasKey("Elementos"))
            {
                foreach (KeyValuePair<string, JSONNode> kvpElemento in categoriaData["Elementos"].AsObject)
                {
                    string elementoKey = kvpElemento.Key;
                    JSONNode elementoData = kvpElemento.Value;

                    ElementoData elementoInfo = new ElementoData
                    {
                        nombre = elementoData.HasKey("nombre") ? elementoData["nombre"].Value : "Sin nombre",
                        logro = elementoData.HasKey("logro") ? elementoData["logro"].Value : "Sin logro",
                        misiones = new List<MisionData>()
                    };

                    if (elementoData.HasKey("misiones"))
                    {
                        foreach (JSONNode misionNode in elementoData["misiones"].AsArray)
                        {
                            MisionData mision = new MisionData
                            {
                                id = misionNode["id"].AsInt,
                                titulo = misionNode["titulo"],
                                descripcion = misionNode["descripcion"],
                                tipo = misionNode["tipo"],
                                completada = misionNode["completada"].AsBool,
                                rutaescena = misionNode["rutaescena"]
                            };
                            elementoInfo.misiones.Add(mision);
                        }
                    }

                    categoriaInfo.Elementos[elementoKey] = elementoInfo;
                }
            }

            // ✅ Revisamos si la misión final está completadabool
            bool misionFinalDesbloqueada = categoriaData.HasKey("Mision Final") &&
            categoriaData["Mision Final"].HasKey("MisionFinal") &&
            categoriaData["Mision Final"]["MisionFinal"].HasKey("completada") &&
            categoriaData["Mision Final"]["MisionFinal"]["completada"].AsBool;



            // ✅ Instanciamos la categoría usando ese valor
            UI.Categoria categoria = new UI.Categoria(categoriaKey, categoriaInfo, misionFinalDesbloqueada);
            categorias.Add(categoriaKey, categoria);

            CreateCategoriaLogro(categoria);

            foreach (var elementoData in categoria.ElementosData.Values)
            {
                Elemento nuevoElemento = new Elemento(elementoData);
                elementos.Add(nuevoElemento.Nombre, nuevoElemento);
                CreateElementoLogro(nuevoElemento);
            }
        }
    }

    private void CargarJSON()
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON");

        if (string.IsNullOrEmpty(jsonString))
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("Misiones_Categorias");
            if (jsonFile != null)
            {
                jsonString = jsonFile.text;
                PlayerPrefs.SetString("misionesCategoriasJSON", jsonString);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("No se encontró el archivo JSON en Resources.");
                return;
            }
        }
        jsonData = JSON.Parse(jsonString);
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

        logroCategoria.ActualizarLogro(categoria.TituloMisionFinal, categoria.EstaCompletada());
    }

    private void CreateElementoLogro(Elemento elemento)
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

        logroElemento.ActualizarLogro(elemento.Nombre, elemento.Logro, elemento.EstaCompletado());
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
    public string nombre;
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

namespace UI
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
}
public class Elemento
{
    public string Nombre { get; private set; }
    public string Logro { get; private set; }
    public List<UI.Mision> Misiones { get; private set; }

    public Elemento(ElementoData data)
    {
        Nombre = data.nombre;
        Logro = data.logro;
        Misiones = data.misiones != null ? data.misiones.Select(m => new UI.Mision(m)).ToList() : new List<UI.Mision>();
    }

    public bool EstaCompletado()
    {
        return Misiones != null && Misiones.Count > 0 && Misiones.All(m => m.completada);
    }
}
