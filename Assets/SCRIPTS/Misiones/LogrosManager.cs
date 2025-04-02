using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleJSON;
using static LogrosManager;
using UI;

public class LogrosManager : MonoBehaviour
{
    public GameObject categoriaPrefab;
    public GameObject elementoPrefab;
    public Transform categoriaPanel;
    public Transform elementoPanel;

    private Dictionary<string, UI.Categoria> categorias;
    private Dictionary<string, Elemento> elementos;

    private void Start()
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON");

        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return;
        }

        var jsonData = JSON.Parse(jsonString);
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
                Elementos = new Dictionary<string, ElementoData>()
            };

            if (categoriaData.HasKey("MisionFinal") && categoriaData["MisionFinal"].HasKey("MisionFinal") &&
    categoriaData["MisionFinal"]["MisionFinal"].HasKey("titulo"))
            {
                categoriaInfo.TituloMisionFinal = categoriaData["MisionFinal"]["MisionFinal"]["titulo"].Value;
            }



            if (categoriaData.HasKey("Elementos"))
            {
                foreach (var elementoKey in categoriaData["Elementos"].Keys)
                {
                    var elementoData = categoriaData["Elementos"][elementoKey];
                    ElementoData elementoInfo = new ElementoData
                    {
                        nombre = elementoData.HasKey("nombre") ? elementoData["nombre"].Value : "Sin nombre",
                        logro = elementoData.HasKey("logro") ? elementoData["logro"].Value : "Sin logro",
                        misiones = new List<MisionData>()
                    };
                    categoriaInfo.Elementos[elementoKey] = elementoInfo;
                }
            }

            UI.Categoria categoria = new UI.Categoria(categoriaKey, categoriaInfo);
            categorias.Add(categoria.Nombre, categoria);
            CreateCategoriaLogro(categoria);


            foreach (var elemento in categoria.ElementosData.Values)
            {
                Elemento nuevoElemento = new Elemento(elemento);
                elementos.Add(nuevoElemento.Nombre, nuevoElemento);
                CreateElementoLogro(nuevoElemento);
            }
        }
    }

    private void CreateCategoriaLogro(UI.Categoria categoria)
    {
        if (categoria == null)
        {
            Debug.LogError("❌ Error: La categoría es NULL");
            return;
        }
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
        if (elemento == null)
        {
            Debug.LogError("❌ Error: El elemento es NULL");
            return;
        }
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
    public string logro;
    public string TituloMisionFinal;  // ✅ Asegúrate de que esta propiedad está declarada
    public Dictionary<string, ElementoData> Elementos;
}


public class ElementoData
{
    public string nombre;
    public string logro;
    public List<MisionData> misiones;
}

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
        public string colorBoton;
        public string logoMision;
        public bool completada;
        public int xp;
        public string mensajeCompletada;
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
        if (Misiones == null || Misiones.Count == 0)
            return false;

        return Misiones.All(m => m.completada);
    }
}


