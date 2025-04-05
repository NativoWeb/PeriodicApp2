using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using static GeneradorElementosUI;

public class GeneradorElementosUI : MonoBehaviour
{
    public GameObject prefabElemento; // Prefab con Image y TMP
    public Transform contenedor; // Donde colocar los elementos
    public Color colorCompletado = Color.green;
    public Color colorIncompleto = Color.gray;


    public TMP_Text TotalMisionesCompletadas;
    public TMP_Text TotalLogrosDesbloqueados;
    public TMP_Text TotalXP;

    private JSONNode jsonData;

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

    [System.Serializable]
    public class Elemento
    {
        public string nombre;
        public string simbolo;
        public List<Mision> misiones = new List<Mision>();

        public bool EstaCompletado()
        {
            return misiones != null && misiones.Count > 0 && misiones.All(m => m.completada);
        }
    }

    private void Awake()
    {
        CargarJSON();
    }

    void Start()
    {
        if (jsonData == null || !jsonData.HasKey("Misiones_Categorias") || !jsonData["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Error: Estructura del JSON no válida.");
            return;
        }

        var categoriasJson = jsonData["Misiones_Categorias"]["Categorias"];

        foreach (KeyValuePair<string, JSONNode> categoria in categoriasJson)
        {
            var categoriaNombre = categoria.Key;
            var categoriaData = categoria.Value;

            if (!categoriaData.HasKey("Elementos")) continue;

            var elementosJson = categoriaData["Elementos"];

            foreach (KeyValuePair<string, JSONNode> elemento in elementosJson)
            {
                string simboloElemento = elemento.Key;
                JSONNode datosElemento = elemento.Value;

                Elemento nuevoElemento = new Elemento
                {
                    simbolo = datosElemento["simbolo"],
                    nombre = datosElemento["nombre"],
                    misiones = new List<Mision>()
                };

                if (datosElemento.HasKey("misiones"))
                {
                    foreach (JSONNode m in datosElemento["misiones"].AsArray)
                    {
                        Mision nuevaMision = new Mision
                        {
                            id = m["id"].AsInt,
                            titulo = m["titulo"],
                            descripcion = m["descripcion"],
                            tipo = m["tipo"],
                            completada = m["completada"].AsBool,
                            rutaescena = m["rutaescena"]
                        };
                        nuevoElemento.misiones.Add(nuevaMision);
                    }
                }
                GenerarElementoUI(nuevoElemento);
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
                Debug.LogError("❌ No se encontró el archivo JSON en Resources.");
                return;
            }
        }

        jsonData = JSON.Parse(jsonString);
    }

    void GenerarElementoUI(Elemento elemento)
    {
        GameObject nuevo = Instantiate(prefabElemento, contenedor);

        // Color
        Image imagen = nuevo.GetComponent<Image>();
        if (imagen != null)
        {
            imagen.color = elemento.EstaCompletado() ? colorCompletado : colorIncompleto;
        }
        else
        {
            Debug.LogWarning("⚠ Prefab no tiene componente Image.");
        }

        // Texto TMP
        TextMeshProUGUI tmp = nuevo.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = elemento.simbolo;
        }
        else
        {
            Debug.LogWarning("⚠ Prefab no tiene un TMP como hijo.");
        }
    }
}
