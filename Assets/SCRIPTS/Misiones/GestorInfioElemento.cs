using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using System.IO;
using System.Collections;


public class GestorInfoElemento : MonoBehaviour
{
    [Header("Panel Principal del Elemento")]
    public GameObject PanelDatosElemento;
    public TextMeshProUGUI txtSimbolo;
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNumeroAtomico;
    public Image ImgElemento;

    [Header("Botones de Cambio")]
    public Button btnMisiones;
    public Button btnInformacion;

    [Header("UI Información")]
    public GameObject prefabBotonPropiedad;
    public Transform contenedorBotonesPropiedades;
    public TextMeshProUGUI txtDescripcion;

    [Header("Panel Dato")]
    public GameObject panelPropiedad;
    public TextMeshProUGUI txtDescripcionPropiedad;
    public Button BtnCerrar;

    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;
    public GameObject PanelMisones;
    public GameObject PanelInformacion;

    JSONNode jsonDataInformacion;

    private string JsonIdioma;

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

    void OnEnable()
    {
        InicializarPanelElemento();
        string PlayerFref = PlayerPrefs.GetString("CategoriaSeleccionada");
        Debug.Log(PlayerFref);
    }

    void InicializarPanelElemento()
    {
        string appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        if (appIdioma == "español")
        {
            JsonIdioma = "Json_Informacion.json";
        }
        else
        {
            JsonIdioma = "Json_Informacion_en.json";

        }
        CargarJSON();
        
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
        BtnCerrar.onClick.AddListener(CerrarPanelPropiedad);
        btnMisiones.onClick.AddListener(IrAMisiones);
    }
    void CargarJSON()
    {
        string pathPersistent = Path.Combine(Application.persistentDataPath, JsonIdioma);

        if (File.Exists(pathPersistent))
        {
            string jsonString = File.ReadAllText(pathPersistent);
            jsonDataInformacion = JSON.Parse(jsonString);
            Debug.Log("✅ json_informacion.json cargado desde persistentDataPath.");

            CargarInfoElementoSeleccionado(); // Aquí ya se tiene el JSON, así que es seguro
        }
        else
        {
            StartCoroutine(CargarDesdeResources(JsonIdioma, (json) =>
            {
                if (!string.IsNullOrEmpty(json))
                {
                    jsonDataInformacion = JSON.Parse(json);
                    Debug.Log("📄 Json_informacion.json cargado desde Resources (temporal).");

                    CargarInfoElementoSeleccionado(); // Mover aquí para asegurar que el JSON esté listo
                }
                else
                {
                    Debug.LogError("❌ No se pudo cargar json_informacion.json desde Resources.");
                }
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

    void CargarInfoElementoSeleccionado()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado");

        // Cambiar color del panel según categoría
        if (PanelDatosElemento != null)
        {
            var imgPanel = PanelDatosElemento.GetComponent<Image>();
            if (imgPanel != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                imgPanel.color = colorCat;
            }

            var imgPanelInfo = PanelInformacion.GetComponent<Image>();
            if (imgPanelInfo != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                imgPanelInfo.color = colorCat;
            }
        }

        // Asignar color según el elemento

        if (elementoSeleccionado == "Astato" || elementoSeleccionado == "Téneso")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoVerde");
        }
        else if (elementoSeleccionado == "Proactinio" || elementoSeleccionado == "Neptunio" || elementoSeleccionado == "Berkelio" ||
                 elementoSeleccionado == "Einstenio" || elementoSeleccionado == "Fermio" || elementoSeleccionado == "Mendelevio" ||
                 elementoSeleccionado == "Nobelio" || elementoSeleccionado == "Lawrencio" || elementoSeleccionado == "Prometio")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoAzul");
        }
        else if (elementoSeleccionado == "Rutherfordio" || elementoSeleccionado == "Dubnio" || elementoSeleccionado == "Seaborgio" ||
                 elementoSeleccionado == "Bohrio" || elementoSeleccionado == "Hassio" || elementoSeleccionado == "Meitnerio" ||
                 elementoSeleccionado == "Darmstatio" || elementoSeleccionado == "Roentgenio" || elementoSeleccionado == "Copernicio" ||
                 elementoSeleccionado == "Nihonio" || elementoSeleccionado == "Flerovio" || elementoSeleccionado == "Moscovio" || elementoSeleccionado == "Livermorio")
        {
            ImgElemento.sprite = Resources.Load<Sprite>("ImagenesElementos/desconocidoRosado");
        }
        else
        {
            // Intentar cargar la imagen del elemento
            Sprite spriteElemento = Resources.Load<Sprite>("ImagenesElementos/" + elementoSeleccionado);
            ImgElemento.sprite = spriteElemento;
        }

        if (jsonDataInformacion == null ||
            !jsonDataInformacion.HasKey("Informacion") ||
            !jsonDataInformacion["Informacion"].HasKey("Categorias"))
        {
            Debug.LogError("El JSON de información cargado es inválido o no contiene las claves esperadas.");
            return;
        }

        var categorias = jsonDataInformacion["Informacion"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey(elementoSeleccionado))
        {
            Debug.LogError("No se encontró el elemento seleccionado en el JSON.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada][elementoSeleccionado];

        txtSimbolo.text = elementoJson["simbolo"];
        txtNombre.text = elementoJson["nombre"];
        txtNumeroAtomico.text = elementoJson["numero_atomico"];
        txtDescripcion.text = elementoJson["descripcion"];

        PlayerPrefs.SetString("NumeroAtomico", elementoJson["numero_atomico"]);

        // Crear botones dinámicos para cada propiedad
        LimpiarBotonesPropiedades();

        var propiedades = elementoJson["propiedades"];
        foreach (KeyValuePair<string, JSONNode> propiedad in propiedades)
        {
            string clave = propiedad.Key;
            string valor = propiedad.Value["valor"];
            string info = propiedad.Value["info"];

            CrearBotonPropiedad(clave, valor, info);
        }
    }

    public string devolverCatTrad(string categoriaSeleccionada)
    {
        switch (categoriaSeleccionada)
        {
            case "Alkali Metals":
                return "Metales Alcalinos";

            case "Alkaline Earth Metals":
                return "Metales Alcalinotérreos";

            case "Transition Metals":
                return "Metales de Transición";

            case "Post-transition Metals":
                return "Metales postransicionales";

            case "Metalloids":
                return "Metaloides";

            case "Nonmetals":
                return "No Metales";

            case "Noble Gases":
                return "Gases Nobles";

            case "Lanthanides":
                return "Lantánidos";

            case "Actinides":
                return "Actinoides";

            case "Unknown Properties":
                return "Propiedades desconocidas";

            default:
                return categoriaSeleccionada;
        }
    }
    void CrearBotonPropiedad(string clave, string valor, string info)
    {
        // Instanciar el botón
        GameObject nuevoBoton = Instantiate(prefabBotonPropiedad, contenedorBotonesPropiedades.transform);

        // Obtener referencias a los textos
        TextMeshProUGUI[] textos = nuevoBoton.GetComponentsInChildren<TextMeshProUGUI>();
        if (textos.Length >= 2)
        {
            textos[0].text = clave;
            textos[1].text = valor;
        }
        else
        {
            Debug.LogWarning("No se encontraron suficientes componentes TextMeshProUGUI en el prefab.");
        }

        // Cargar la imagen desde Resources
        string nombreImagen = ObtenerNombreImagenPropiedad(clave);
        Sprite spritePropiedad = Resources.Load<Sprite>("logosMision/" + nombreImagen);

        if (spritePropiedad != null)
        {
            // Buscar la imagen específica por nombre
            Transform iconoTransform = nuevoBoton.transform.Find("IconoPropiedad");
            if (iconoTransform != null)
            {
                Image imagen = iconoTransform.GetComponent<Image>();
                if (imagen != null)
                {
                    imagen.sprite = spritePropiedad;
                }
                else
                {
                    Debug.LogWarning("No se encontró el componente Image en 'IconoPropiedad'.");
                }
            }
            else
            {
                Debug.LogWarning("No se encontró un hijo con el nombre 'IconoPropiedad' en el prefab del botón.");
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró la imagen '{nombreImagen}' en Resources/logosMision.");
        }

        // Añadir acción al botón
        Button boton = nuevoBoton.GetComponent<Button>();
        boton.onClick.AddListener(() => MostrarPanelPropiedad(clave, valor, info));
    }

    string ObtenerNombreImagenPropiedad(string clave)
    {
        // Usamos ToLower() para hacer la comparación insensible a mayúsculas/minúsculas.
        switch (clave.ToLower())
        {
            // Caso para Masa Atómica
            case "masa_atomica":
            case "atomic_mass":
                return "m_atomica";

            // Caso para Punto de Fusión
            case "punto_fusion":
            case "melting_point":
                return "p_fusion";

            // Caso para Punto de Ebullición
            case "punto_ebullicion":
            case "boiling_point":
                return "p_ebullicion";

            // Caso para Estado
            case "estado":
            case "state":
                return "estado";

            // Caso para Electronegatividad
            case "electronegatividad":
            case "electronegativity":
                return "electronegatividad";

            // Caso por defecto si no coincide ninguna clave
            default:
                return "default"; // Devuelve una imagen por defecto
        }
    }

    void MostrarPanelPropiedad(string propiedad, string valor, string info)
    {
        panelPropiedad.SetActive(true);
        txtDescripcionPropiedad.text = info;
    }

    void CerrarPanelPropiedad()
    {
        panelPropiedad.SetActive(false);
        txtDescripcionPropiedad.text = "";
    }

    void LimpiarBotonesPropiedades()
    {
        foreach (Transform hijo in contenedorBotonesPropiedades.transform)
        {
            Destroy(hijo.gameObject);
        }
    }
   
    private void RegresaraCategorias()
    {
        PanelElemento.SetActive(true);
        PanelMisones.SetActive(false);
        PanelInformacion.SetActive(false);
    }

    public void IrAMisiones()
    {
        PanelMisones.SetActive(true);
        PanelInformacion.SetActive(false);
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(false);
    }
}