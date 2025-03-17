using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SimpleJSON;  // Asegúrate de importar la librería

[System.Serializable]
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
}

[System.Serializable]
public class Elemento
{
    public string descripcion;
    public string imagen;
    public List<Mision> niveles;
}

[System.Serializable]
public class MisionesData
{
    public List<ElementoClave> misiones;
}

[System.Serializable]
public class ElementoClave
{
    public string clave;  // Representa el nombre del elemento (ej. "Hidrógeno")
    public Elemento valor;
}


public class CargarMisiones : MonoBehaviour
{

    public Image Prueba;
    public string elementoSeleccionado = "Hidrógeno"; // Cambia según el elemento actual
    public GameObject prefabMision;
    public Transform contenedorMisiones;

    void Start()
    {
        CargarMisionesDesdeJSON();
    }

void CargarMisionesDesdeJSON()
{
    TextAsset jsonFile = Resources.Load<TextAsset>("misiones");
    if (jsonFile == null)
    {
        Debug.LogError("No se encontró el archivo JSON en Resources.");
        return;
    }

    Debug.Log($"JSON Cargado: {jsonFile.text}");

    var json = JSON.Parse(jsonFile.text);
    if (json == null || !json.HasKey("misiones"))
    {
        Debug.LogError("El JSON no tiene la estructura esperada.");
        return;
    }

    var misionesDict = json["misiones"];

    if (!misionesDict.HasKey(elementoSeleccionado))
    {
        Debug.LogError($"No se encontró el elemento '{elementoSeleccionado}' en misiones.");
        return;
    }

    var elementoJson = misionesDict[elementoSeleccionado];

    // Crear objeto `Elemento`
    Elemento elemento = new Elemento
    {
        descripcion = elementoJson["descripcion"].Value,
        imagen = elementoJson["imagen"].Value,
        niveles = new List<Mision>()
    };

    foreach (JSONNode nivelJson in elementoJson["niveles"].AsArray)
    {
        Mision mision = new Mision
        {
            id = nivelJson["id"].AsInt,
            titulo = nivelJson["titulo"].Value,
            descripcion = nivelJson["descripcion"].Value,
            tipo = nivelJson["tipo"].Value,
            colorBoton = nivelJson["colorBoton"].Value,
            logoMision = nivelJson["logoMision"].Value,
            completada = nivelJson["completada"].AsBool,
            xp = nivelJson["xp"].AsInt,
            mensajeCompletada = nivelJson["mensajeCompletada"].Value
        };

        elemento.niveles.Add(mision);
    }

    Debug.Log($"Elemento '{elementoSeleccionado}' cargado correctamente.");

    foreach (Mision mision in elemento.niveles)
    {
        CrearPrefabMision(mision);
    }
}


void CrearPrefabMision(Mision mision)
    {
        GameObject nuevaMision = Instantiate(prefabMision, contenedorMisiones);
        nuevaMision.GetComponent<UI_Mision>().ConfigurarMision(mision);
    }
}
