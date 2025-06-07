﻿using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class GestorElementos : MonoBehaviour
{
    [Header("Prefab de Elementos")]
    public GameObject prefabElemento;
    public Transform contenedorElementos;
    public Button botonMisionFinal; // Asigna el botón desde el Inspector
    [SerializeField] private Slider sliderProgreso;


    [Header("Descripción de la Categoría")]
    public TextMeshProUGUI txtDescripcionCategoria;
    public TextMeshProUGUI txtTitulo;

    [Header("Botón de Regreso")]
    public GameObject panelMisionesInfo;

    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;

    JSONNode jsonDataInformacion;
    JSONNode jsonDataMisiones;

    // Mapea cada categoría a un Color32 único
    private static readonly Dictionary<string, Color32> ColoresPorCategoria = new Dictionary<string, Color32>
{
    { "Metales Alcalinos",        new Color32(0x41, 0xB9, 0xDE, 0xFF) },
    { "Metales Alcalinotérreos",  new Color32(0xF0, 0x81, 0x2F, 0xFF) },
    { "Metales de Transición",     new Color32(0xED, 0x6D, 0x9D, 0xFF) },
    { "Metales postransicionales", new Color32(0x72, 0x65, 0xAA, 0xFF) },
    { "Metaloides",                new Color32(0xCD, 0xCB, 0xCC, 0xFF) },
    { "No Metales",      new Color32(0x79, 0xBB, 0x51, 0xFF) },
    { "Gases Nobles",              new Color32(0x00, 0xA2, 0x93, 0xFF) },
    { "Lantánidos",                new Color32(0xC0, 0x20, 0x3C, 0xFF) },
    { "Actinoides",                new Color32(0x33, 0x37, 0x8E, 0xFF) },
    { "Propiedades desconocidas",  new Color32(0xC2, 0x89, 0x58, 0xFF) },
};

    List<Categoria> ObtenerCategoriasPorDefecto()
    {
        return new List<Categoria>
    {
        new Categoria("Metales Alcalinos",       "Extremadamente reactivos al agua y al aire: desde el sodio en jabones hasta el potasio en fertilizantes."),
        new Categoria("Metales Alcalinotérreos", "Menos reactivos, clave en aleaciones aeronáuticas y huesos: magnesio y calcio."),
        new Categoria("Metales de Transición",   "Múltiples estados de oxidación y colores vivos: hierro en construcción, cobre en circuitos."),
        new Categoria("Metales Postransicionales","Suaves y maleables: aluminio en aviones, estaño en soldaduras, plomo en baterías."),
        new Categoria("Metaloides",               "Intermedios metal/no metal: silicio en semiconductores, boro en vidrios resistentes."),
        new Categoria("No Metales Reactivos",     "Oxígeno en respiración, nitrógeno en fertilizantes, fósforo en detergentes."),
        new Categoria("Gases Nobles",             "Inertes pero útiles: helio en RMN, argón en soldadura, neón en iluminación."),
        new Categoria("Lantánidos",               "Tierras raras en tecnología: neodimio en imanes, cerio en catalizadores."),
        new Categoria("Actínoides",               "Radioactivos potentes: uranio en centrales, americio en detectores de humo."),
        new Categoria("Propiedades Desconocidas", "Elementos inestables o poco estudiados: la frontera de la química.")
    };
    }

    void OnEnable()
    {
        StartCoroutine(InicializarPanelElementoAsync());
    }

    IEnumerator InicializarPanelElementoAsync()
    {
        Debug.Log("Iniciando GestorElementos...");

        // 1) Título de categoría
        string cat = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        txtTitulo.text = cat;

        // 2) Descripción
        var lista = ObtenerCategoriasPorDefecto();
        var match = lista.Find(c => c.Titulo == cat);
        txtDescripcionCategoria.text = match != null
            ? match.Descripcion
            : "Descripción no disponible.";

        yield return StartCoroutine(CargarJSON());

        // Esperar a que termine la carga antes de continuar
        CargarElementosDesdeJSON();
        ActualizarProgresoCategoria();

        botonMisionFinal.onClick.AddListener(IrAMisionFinal);
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
    }

    IEnumerator CargarJSON()
    {
        // INFORMACIÓN
        string pathInformacion = Path.Combine(Application.persistentDataPath, "json_informacion.json");

        if (File.Exists(pathInformacion))
        {
            string jsonStringInformacion = File.ReadAllText(pathInformacion);
            jsonDataInformacion = JSON.Parse(jsonStringInformacion);
            Debug.Log("json_informacion.json cargado desde persistentDataPath.");
        }
        else
        {
            yield return StartCoroutine(CargarDesdeResources("json_informacion.json", (json) =>
            {
                jsonDataInformacion = JSON.Parse(json);
                Debug.Log("json_informacion.json cargado temporalmente desde StreamingAssets.");
            }));
        }

        // MISIONES
        string pathMisiones = Path.Combine(Application.persistentDataPath, "json_misiones.json");
        Debug.Log(pathMisiones);

        if (File.Exists(pathMisiones))
        {
            string jsonStringMisiones = File.ReadAllText(pathMisiones);
            jsonDataMisiones = JSON.Parse(jsonStringMisiones);
            Debug.Log("json_misiones.json cargado desde persistentDataPath.");
        }
        else
        {
            yield return StartCoroutine(CargarDesdeResources("json_misiones.json", (json) =>
            {
                jsonDataMisiones = JSON.Parse(json);
                Debug.Log("json_misiones.json cargado desde Resources.");
            }));
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



    void CargarElementosDesdeJSON()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        Debug.Log(jsonDataInformacion);

        if (string.IsNullOrEmpty(categoriaSeleccionada))
        {
            Debug.LogError("Error: No se ha seleccionado ninguna categoría.");
            return;
        }

        if (jsonDataInformacion == null)
        {
            Debug.LogError("Error: jsonData es nulo.");
            return;
        }

        if (!jsonDataInformacion.HasKey("Informacion") ||
            !jsonDataInformacion["Informacion"].HasKey("Categorias"))
        {
            Debug.LogError("Error: El JSON no tiene la clave 'Informacion' o 'Categorias'.");
            return;
        }

        if (!jsonDataInformacion["Informacion"]["Categorias"].HasKey(categoriaSeleccionada))
        {
            Debug.LogError($"Error: La categoría '{categoriaSeleccionada}' no se encuentra en el JSON.");
            return;
        }

        LimpiarElementos();

        var elementos = jsonDataInformacion["Informacion"]["Categorias"][categoriaSeleccionada];

        Color32 colorCategoria = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c)
            ? c
            : new Color32(255, 255, 255, 255);

        foreach (var elemento in elementos)
        {
            CrearBotonElemento(elemento.Key, elemento.Value, colorCategoria);
        }
    }

    void CrearBotonElemento(string nombreElemento, JSONNode datosElemento, Color32 colorBoton)
    {
        if (prefabElemento == null)
        {
            Debug.LogError("prefabElemento no está asignado.");
            return;
        }

        if (contenedorElementos == null)
        {
            Debug.LogError("contenedorElementos no está asignado.");
            return;
        }

        if (datosElemento == null)
        {
            Debug.LogError($"datosElemento es null para {nombreElemento}");
            return;
        }

        GameObject nuevoBoton = Instantiate(prefabElemento, contenedorElementos, false);

        // Obtener textos
        TextMeshProUGUI[] textos = nuevoBoton.GetComponentsInChildren<TextMeshProUGUI>();
        if (textos.Length == 0)
        {
            Debug.LogError("No se encontraron componentes TextMeshProUGUI en el prefab.");
            return;
        }

        textos[0].text = datosElemento["simbolo"];

        // Listener
        Button boton = nuevoBoton.GetComponent<Button>();
        if (boton == null)
        {
            Debug.LogError("El prefab no tiene un componente Button.");
            return;
        }

        boton.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("NumeroAtomico", datosElemento["numero_atomico"]);
            SeleccionarElemento(nombreElemento);
        }
        );

        // Color
        Image img = nuevoBoton.GetComponent<Image>();
        if (img != null)
            img.color = colorBoton;
    }

    void LimpiarElementos()
    {
        foreach (Transform child in contenedorElementos)
        {
            Destroy(child.gameObject);
        }
    }

    public void SeleccionarElemento(string nombreElemento)
    {
        Debug.Log($"➡ Elemento seleccionado: {nombreElemento}");
        PlayerPrefs.SetString("ElementoSeleccionado", nombreElemento);
        PlayerPrefs.Save();

        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(false);
        panelMisionesInfo.SetActive(true);
    }

    public void ActualizarProgresoCategoria()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");

        if (sliderProgreso == null)
        {
            Debug.LogError("⚠️ No se ha asignado el Slider en el Inspector.");
            return;
        }

        if (jsonDataMisiones == null)
        {
            Debug.LogError("❌ jsonDataMisiones no ha sido cargado correctamente.");
            return;
        }

        var misionesNode = jsonDataMisiones["Misiones"];

        if (!misionesNode.HasKey("Categorias") ||
            !misionesNode["Categorias"].HasKey(categoriaSeleccionada))
        {
            Debug.LogError($"❌ La categoría '{categoriaSeleccionada}' no se encuentra en el JSON.");
            return;
        }

        var categoriaNode = misionesNode["Categorias"][categoriaSeleccionada];
        var elementosNode = categoriaNode["Elementos"];

        int totalMisiones = 0;
        int misionesCompletadas = 0;

        foreach (var elemento in elementosNode.Keys)
        {
            var elementoNode = elementosNode[elemento];

            if (elementoNode.HasKey("Misiones"))
            {
                var misionesElemento = elementoNode["Misiones"];

                foreach (var misionKey in misionesElemento.Keys)
                {
                    var mision = misionesElemento[misionKey];
                    totalMisiones++;

                    if (mision.HasKey("completada") && mision["completada"].AsBool)
                    {
                        misionesCompletadas++;
                    }
                }
            }

            // Verificar si hay misión final
            if (elementoNode.HasKey("MisionFinal"))
            {
                var misionFinal = elementoNode["MisionFinal"];
                totalMisiones++;

                if (misionFinal.HasKey("completada") && misionFinal["completada"].AsBool)
                {
                    misionesCompletadas++;
                }
            }
        }

        float progreso = (totalMisiones > 0) ? (float)misionesCompletadas / totalMisiones : 0f;
        sliderProgreso.value = progreso;

        Debug.Log($"✅ Progreso actualizado: {misionesCompletadas}/{totalMisiones} misiones completadas ({progreso * 100:F2}%)");
    }

    public void IrAMisionFinal()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        string rutaMisionFinal = ObtenerRutaMisionFinal(categoriaSeleccionada);

        if (!string.IsNullOrEmpty(rutaMisionFinal))
        {
            Debug.Log($"🔄 Cargando misión final: {rutaMisionFinal}");
            SceneManager.LoadScene(rutaMisionFinal);
        }
        else
        {
            Debug.LogError("❌ No se encontró la ruta de la misión final.");
        }
    }

    private void ActualizarEstadoMisionFinal()
    {
        if (sliderProgreso == null || botonMisionFinal == null)
        {
            Debug.LogError("⚠️ Slider o botón no asignados en el Inspector.");
            return;
        }

        // Comprobar si el slider está lleno (es decir, si el valor es 1)
        if (sliderProgreso.value >= 1f)
        {
            botonMisionFinal.interactable = true;
            Debug.Log("✔️ Misión final desbloqueada, botón activado.");
        }
        else
        {
            botonMisionFinal.interactable = false;
            Debug.Log("❌ Misión final bloqueada, botón desactivado.");
        }
    }
    public string ObtenerRutaMisionFinal(string categoria)
    {
        if (jsonDataMisiones == null)
        {
            Debug.LogWarning("❌ jsonDataMisiones no está cargado.");
            return null;
        }

        var misionesNode = jsonDataMisiones["Misiones"];
        if (misionesNode == null || !misionesNode.HasKey("Categorias"))
        {
            Debug.LogWarning("❌ Estructura JSON incorrecta o no contiene 'Categorias'.");
            return null;
        }

        var categorias = misionesNode["Categorias"];
        if (!categorias.HasKey(categoria))
        {
            Debug.LogWarning($"❌ La categoría '{categoria}' no se encontró en JSON.");
            return null;
        }

        var categoriaSeleccionada = categorias[categoria];
        if (!categoriaSeleccionada.HasKey("Mision Final"))
        {
            Debug.LogWarning($"❌ La categoría '{categoria}' no contiene 'Mision Final'.");
            return null;
        }

        var misionFinal = categoriaSeleccionada["Mision Final"];
        if (misionFinal == null || !misionFinal.HasKey("rutaescena"))
        {
            Debug.LogWarning("❌ No se encontró 'rutaescena' en Mision Final.");
            return null;
        }

        return misionFinal["rutaescena"];
    }

    private void RegresaraCategorias()
    {
        PlayerPrefs.DeleteKey("CategoriaSeleccionada");
        PlayerPrefs.Save();
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(true);
    }
}