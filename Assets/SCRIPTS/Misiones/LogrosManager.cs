using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LogrosManager : MonoBehaviour
{
    public GameObject categoriaPrefab;
    public GameObject elementoPrefab;
    public Transform categoriaPanel;
    public Transform elementoPanel;

    private Dictionary<string, Categoria> categorias;
    private Dictionary<string, Elemento> elementos;

    private void Start()
    {
        // Cargar los datos del JSON de PlayerPrefs
        string misionesCategoriasJSON = PlayerPrefs.GetString("misionesCategoriasJSON");
        MisionesCategorias misionesCategorias = JsonUtility.FromJson<MisionesCategorias>(misionesCategoriasJSON);

        categorias = new Dictionary<string, Categoria>();
        elementos = new Dictionary<string, Elemento>();

        // Crear logros por categoría
        foreach (var categoriaData in misionesCategorias.Misiones_Categorias.Categorias)
        {
            Categoria categoria = new Categoria(categoriaData.Key, categoriaData.Value);
            categorias.Add(categoria.Nombre, categoria);
            CreateCategoriaLogro(categoria);
        }

        // Crear logros por elemento
        foreach (var categoria in categorias.Values)
        {
            foreach (var elementoData in categoria.ElementosData)
            {
                Elemento elemento = new Elemento(elementoData.Key, elementoData.Value);
                elementos.Add(elemento.Nombre, elemento);
                CreateElementoLogro(elemento);
            }
        }
    }

    private void CreateCategoriaLogro(Categoria categoria)
    {
        GameObject categoriaObj = Instantiate(categoriaPrefab, categoriaPanel);
        TextMeshProUGUI titulo = categoriaObj.transform.Find("Titulo").GetComponent<TextMeshProUGUI>();
        Image imagenCompletada = categoriaObj.transform.Find("ImagenCompletada").GetComponent<Image>();

        titulo.text = categoria.Nombre;
        imagenCompletada.gameObject.SetActive(categoria.EstaCompletada());

        // Aquí puedes asignar la imagen del logro si es necesario
    }

    private void CreateElementoLogro(Elemento elemento)
    {
        GameObject elementoObj = Instantiate(elementoPrefab, elementoPanel);
        TextMeshProUGUI nombre = elementoObj.transform.Find("NombreElemento").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI logro = elementoObj.transform.Find("LogroElemento").GetComponent<TextMeshProUGUI>();
        Image imagenCompletada = elementoObj.transform.Find("ImagenCompletada").GetComponent<Image>();

        nombre.text = elemento.Nombre;
        logro.text = elemento.Logro;
        imagenCompletada.gameObject.SetActive(elemento.EstaCompletada());
    }

    // Clases para manejar los datos de las categorías y elementos
    [System.Serializable]
    public class MisionesCategorias
    {
        public Misiones_Categorias Misiones_Categorias;
    }

    [System.Serializable]
    public class Misiones_Categorias
    {
        public Dictionary<string, CategoriaData> Categorias;
    }

    [System.Serializable]
    public class CategoriaData
    {
        public string nombre;
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

 

    [System.Serializable]
    public class Categorias
    {
        public Elementos MetalesAlcalinos;
    }

    [System.Serializable]
    public class Elementos
    {
        public Elemento Litio;
        public Elemento Sodio;
        public Elemento Potasio;
        public Elemento Rubidio;
        public Elemento Cesio;
        public Elemento Francio;
    }

    [System.Serializable]
    public class Mision
    {
        public int id;
        public string titulo;
        public string descripcion;
        public string tipo;
        public bool completada;
        public string rutaescena;
    }

    public class Categoria
    {
        public string Nombre { get; private set; }
        public Dictionary<string, ElementoData> ElementosData { get; private set; }

        public Categoria(string nombre, CategoriaData categoriaData)
        {
            Nombre = nombre;
            ElementosData = categoriaData.Elementos;
        }

        public bool EstaCompletada()
        {
            foreach (var elemento in ElementosData.Values)
            {
                foreach (var mision in elemento.misiones)
                {
                    if (!mision.completada)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public class Elemento
    {
        public string Nombre { get; private set; }
        public string Logro { get; private set; }
        public List<MisionData> Misiones { get; private set; }

        public Elemento(string nombre, ElementoData elementoData)
        {
            Nombre = nombre;
            Logro = elementoData.logro;
            Misiones = elementoData.misiones;
        }

        public bool EstaCompletada()
        {
            foreach (var mision in Misiones)
            {
                if (!mision.completada)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
