using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;

public class GestorElementos : MonoBehaviour
{
    [Header("Panel Principal del Elemento")]
    public GameObject PanelDatosElemento;
    public TextMeshProUGUI txtSimbolo;
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNumeroAtomico;

    [Header("Contenedores")]
    public GameObject scrollMisiones;
    public GameObject scrollInformacion;
    public Transform contenedorElementos;

    [Header("Botones de Cambio")]
    public Button btnMisiones;
    public Button btnInformacion;
    private Color colorSeleccionado = new Color(110f / 255f, 106f / 255f, 169f / 255f);
    private Color colorNoSeleccionado = new Color(255, 255, 255);

    [Header("UI Misiones")]
    public GameObject prefabMision;
    public Transform contenedorMisiones;

    [Header("Prefab de Elementos")]
    public GameObject prefabElemento;
    public Button botonMisionFinal; // Asigna el botón desde el Inspector
    [SerializeField] private Slider sliderProgreso;


    [Header("UI Información")]
    public TextMeshProUGUI txtMasaAtomica;
    public TextMeshProUGUI txtPuntoFusion;
    public TextMeshProUGUI txtPuntoEbullicion;
    public TextMeshProUGUI txtElectronegatividad;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtDescripcion;

    [Header("Descripción de la Categoría")]
    public TextMeshProUGUI txtDescripcionCategoria;
    public TextMeshProUGUI txtTitulo;

    [Header("Botón de Regreso")]
    public Button btnRegresar;
    public GameObject panelMisionesInfo;

    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;

    private JSONNode jsonData;

    // Mapea cada categoría a un Color32 único
    private static readonly Dictionary<string, Color32> ColoresPorCategoria = new Dictionary<string, Color32>
{
    { "Metales Alcalinos",        new Color32(0x41, 0xB9, 0xDE, 0xFF) },
    { "Metales Alcalinotérreos",  new Color32(0xF0, 0x81, 0x2F, 0xFF) },
    { "Metales de Transición",     new Color32(0xED, 0x6D, 0x9D, 0xFF) },
    { "Metales Postransicionales", new Color32(0x72, 0x65, 0xAA, 0xFF) },
    { "Metaloides",                new Color32(0xCD, 0xCB, 0xCC, 0xFF) },
    { "No Metales Reactivos",      new Color32(0x79, 0xBB, 0x51, 0xFF) },
    { "Gases Nobles",              new Color32(0x00, 0xA2, 0x93, 0xFF) },
    { "Lantánidos",                new Color32(0xC0, 0x20, 0x3C, 0xFF) },
    { "Actínoides",                new Color32(0x33, 0x37, 0x8E, 0xFF) },
    { "Propiedades Desconocidas",  new Color32(0xC2, 0x89, 0x58, 0xFF) },
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
        InicializarPanelElemento();
        string PlayerFref = PlayerPrefs.GetString("CategoriaSeleccionada");
        Debug.Log(PlayerFref);
    }

    void InicializarPanelElemento()
    {
        Debug.Log("Iniciando GestorElementos...");
        // 1) Título de categoría
        string cat = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        txtTitulo.text = cat;

        // 2) Descripción según la lista por defecto
        var lista = ObtenerCategoriasPorDefecto();
        var match = lista.Find(c => c.Titulo == cat);
        txtDescripcionCategoria.text = match != null
            ? match.Descripcion
            : "Descripción no disponible.";


        Debug.Log("Iniciando GestorElementos...");
        txtTitulo.text = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        CargarJSON();
        CargarElementosDesdeJSON();
        ActualizarProgresoCategoria();
        btnMisiones.onClick.AddListener(MostrarMisiones);
        btnInformacion.onClick.AddListener(MostrarInformacion);
        btnRegresar.onClick.AddListener(RegresarAlPanelElementos);
        botonMisionFinal.onClick.AddListener(IrAMisionFinal);
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
        MostrarMisiones();
    }
    
    void CargarJSON()
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
        Debug.Log(jsonData);
    }

    void CargarElementosDesdeJSON()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        Debug.Log("Categoría seleccionada: " + categoriaSeleccionada);


        if (string.IsNullOrEmpty(categoriaSeleccionada))
        {
            Debug.LogError("Error: No se ha seleccionado ninguna categoría.");
            return;
        }

        if (jsonData == null)
        {
            Debug.LogError("Error: jsonData es nulo.");
            return;
        }

        if (!jsonData.HasKey("Misiones_Categorias") ||
            !jsonData["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("Error: El JSON no tiene la clave 'Misiones_Categorias' o 'Categorias'.");
            return;
        }

        if (!jsonData["Misiones_Categorias"]["Categorias"].HasKey(categoriaSeleccionada))
        {
            Debug.LogError($"Error: La categoría '{categoriaSeleccionada}' no se encuentra en el JSON.");
            return;
        }

        if (!jsonData["Misiones_Categorias"]["Categorias"][categoriaSeleccionada].HasKey("Elementos"))
        {
            Debug.LogError($"Error: La categoría '{categoriaSeleccionada}' no tiene elementos.");
            return;
        }

        LimpiarElementos();
        var elementos = jsonData["Misiones_Categorias"]["Categorias"][categoriaSeleccionada]["Elementos"];

        // Obtén de una vez el color de la categoría (o blanco si no existe)
        Color32 colorCategoria = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c)
            ? c
            : new Color32(255, 255, 255, 255);

        foreach (var elemento in elementos)
        {
            // Extraes datos…
            CrearBotonElemento(elemento.Key, elemento.Value, colorCategoria );
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

        boton.onClick.AddListener(() => SeleccionarElemento(nombreElemento));

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

        CargarDatosElementoSeleccionado();
    }

    void CargarDatosElementoSeleccionado()
    {

        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");

        // Color del panel de datos según categoría
        if (PanelDatosElemento != null)
        {
            var imgPanel = PanelDatosElemento.GetComponent<Image>();
            if (imgPanel != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                imgPanel.color = colorCat;
            }
        }

        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON");
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado");

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

        var json = JSON.Parse(jsonString);

        if (json == null ||
            !json.HasKey("Misiones_Categorias") ||
            !json["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("El JSON es inválido o no se pudo parsear.");
            return;
        }

        var categorias = json["Misiones_Categorias"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey("Elementos") ||
            !categorias[categoriaSeleccionada]["Elementos"].HasKey(elementoSeleccionado))
        {
            Debug.LogError("No se encontró el elemento seleccionado en la categoría especificada.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada]["Elementos"][elementoSeleccionado];

        txtSimbolo.text = elementoJson["simbolo"].Value;
        txtNombre.text = elementoJson["nombre"].Value;
        txtNumeroAtomico.text = elementoJson["numero_atomico"].Value;
        txtMasaAtomica.text = elementoJson["masa_atomica"].Value;
        txtPuntoFusion.text = elementoJson["punto_fusion"].Value + "°C";
        txtPuntoEbullicion.text = elementoJson["punto_ebullicion"].Value + "°C";
        txtElectronegatividad.text = elementoJson["electronegatividad"].Value;
        txtEstado.text = elementoJson["estado"].Value;
        txtDescripcion.text = elementoJson["descripcion"].Value;

        PlayerPrefs.SetString("NumeroAtomico", elementoJson["numero_atomico"].Value);

        LimpiarMisiones();

        foreach (JSONNode misionJson in elementoJson["misiones"].AsArray)
        {
            Mision mision = new Mision
            {
                id = misionJson["id"].AsInt,
                titulo = misionJson["titulo"].Value,
                descripcion = misionJson["descripcion"].Value,
                tipo = misionJson["tipo"].Value,
                rutaEscena = misionJson["rutaescena"].Value,
                completada = misionJson["completada"].AsBool
            };

            // Asignar valores según el tipo de misión
            switch (mision.tipo)
            {
                case "AR":
                    mision.xp = 10;
                    mision.logoMision = "logosMision/ar";
                    break;
                case "QR":
                    mision.xp =10;
                    mision.logoMision = "logosMision/qr";
                    break;
                case "Juego":
                    mision.xp = 12;
                    mision.logoMision = "logosMision/juego";
                    break;
                case "Quiz":
                    mision.xp = 12;
                    mision.logoMision = "logosMision/quiz";
                    break;
                case "Evaluacion":
                    mision.xp = 12;
                    mision.logoMision = "logosMision/evaluacion";
                    break;
                default:
                    mision.xp = 0;
                    mision.logoMision = "logosMision/default";
                    break;
            }

            PlayerPrefs.SetInt("xp_mision", mision.xp); // Guardar XP en PlayerPrefs

            CrearPrefabMision(mision);
        }
    }

    void CrearPrefabMision(Mision mision)
    {
        string elementoseleccionado = PlayerPrefs.GetString("ElementoSeleccionado");
        GameObject nuevaMision = Instantiate(prefabMision, contenedorMisiones);
        UI_Mision uiMision = nuevaMision.GetComponent<UI_Mision>();
        uiMision.ConfigurarMision(mision);

        Button botonMision = nuevaMision.GetComponentInChildren<Button>();

        // Asignar evento para cambiar de escena
        botonMision.onClick.AddListener(() => CargarEscenaMision(mision.rutaEscena, elementoseleccionado, mision.id));
    }

    void CargarEscenaMision(string nombreEscena, string elemento, int idMision)
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("No se encontró una escena válida para esta misión.");
            return;
        }

        // Guardar el estado de la misión antes de cambiar de escena
        PlayerPrefs.SetString("ElementoSeleccionado", elemento);
        PlayerPrefs.SetString("SimboloElemento", txtSimbolo.text);
        PlayerPrefs.SetInt("MisionActual", idMision);
        if (idMision == 1)
        {
            PlayerPrefs.SetString("CargarVuforia", "Misiones");
        }
        PlayerPrefs.Save();

        // Cargar la escena de la misión
        SceneManager.LoadScene(nombreEscena);
    }
    public void MostrarInformacion()
    {
        scrollMisiones.SetActive(false);
        scrollInformacion.SetActive(true);

        // Fondo
        btnInformacion.GetComponent<Image>().color = colorSeleccionado;
        btnMisiones.GetComponent<Image>().color = colorNoSeleccionado;

        // Texto
        var txtInfo = btnInformacion.GetComponentInChildren<TextMeshProUGUI>();
        var txtMis = btnMisiones.GetComponentInChildren<TextMeshProUGUI>();
        if (txtInfo != null) txtInfo.color = Color.white;
        if (txtMis != null) txtMis.color = Color.black;
    }

    public void MostrarMisiones()
    {
        scrollMisiones.SetActive(true);
        scrollInformacion.SetActive(false);

        // Fondo
        btnMisiones.GetComponent<Image>().color = colorSeleccionado;
        btnInformacion.GetComponent<Image>().color = colorNoSeleccionado;

        // Texto
        var txtMis = btnMisiones.GetComponentInChildren<TextMeshProUGUI>();
        var txtInfo = btnInformacion.GetComponentInChildren<TextMeshProUGUI>();
        if (txtMis != null) txtMis.color = Color.white;
        if (txtInfo != null) txtInfo.color = Color.black;
    }

    public void RegresarAlPanelElementos()
    {
        PanelElemento.SetActive(true);
        panelMisionesInfo.SetActive(false);
        PanelCategorias.SetActive(false);
        LimpiarElementos();
        CargarElementosDesdeJSON();
    }

    void LimpiarMisiones()
    {
        foreach (Transform child in contenedorMisiones)
        {
            Destroy(child.gameObject);
        }
    }

    public void ActualizarProgresoCategoria()
    {
        if (sliderProgreso == null)
        {
            Debug.LogError("⚠️ No se ha asignado el Slider en el Inspector.");
            return;
        }

        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        if (string.IsNullOrEmpty(categoriaSeleccionada))
        {
            Debug.LogError("❌ No hay categoría seleccionada.");
            return;
        }

        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);

        if (!json.HasKey("Misiones_Categorias") || !json["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta.");
            return;
        }

        var categorias = json["Misiones_Categorias"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) || !categorias[categoriaSeleccionada].HasKey("Elementos"))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaSeleccionada}' en el JSON.");
            return;
        }

        var elementos = categorias[categoriaSeleccionada]["Elementos"];
        int totalMisiones = 0;
        int misionesCompletadas = 0;

        foreach (KeyValuePair<string, JSONNode> par in elementos)
        {
            string nombreElemento = par.Key;
            var nodoElemento = par.Value;

            if (!nodoElemento.HasKey("misiones"))
            {
                Debug.LogWarning($"⚠️ El elemento '{nombreElemento}' no tiene misiones.");
                continue;
            }

            var misiones = nodoElemento["misiones"].AsArray;

            if (misiones == null || misiones.Count == 0)
            {
                Debug.LogWarning($"⚠️ El elemento '{nombreElemento}' no tiene misiones válidas.");
                continue;
            }

            totalMisiones += misiones.Count;

            for (int i = 0; i < misiones.Count; i++) // ahora incluye la misión final también
            {
                var mision = misiones[i];
                string completadaStr = mision["completada"].Value.Trim().ToLower();
                bool completada = completadaStr == "true";

                if (completada)
                {
                    misionesCompletadas++;
                }
            }
        }

        sliderProgreso.maxValue = totalMisiones;

        if (totalMisiones == 0)
        {
            Debug.LogWarning("⚠️ No hay misiones que evaluar.");
            sliderProgreso.value = 0f;
            return;
        }

        int progreso = (int)misionesCompletadas / totalMisiones;
        sliderProgreso.value = misionesCompletadas;

        Debug.Log($"📊 Progreso de '{categoriaSeleccionada}': {misionesCompletadas}/{totalMisiones} ({progreso * 100:F2}%)");

        // Verificar si se debe activar la misión final
        if (misionesCompletadas == totalMisiones)
        {
            ActualizarEstadoMisionFinal();
        }
        else
        {
            botonMisionFinal.interactable = false;
            Debug.Log("🔒 Misión final aún bloqueada.");
        }
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
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogWarning("No se encontró información en PlayerPrefs.");
            return null;
        }

        try
        {
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

            if (jsonData.ContainsKey("Misiones_Categorias"))
            {
                var misionesCategorias = jsonData["Misiones_Categorias"] as JObject;
                if (misionesCategorias != null && misionesCategorias.ContainsKey("Categorias"))
                {
                    var categorias = misionesCategorias["Categorias"] as JObject;
                    if (categorias != null && categorias.ContainsKey(categoria))
                    {
                        var categoriaSeleccionada = categorias[categoria] as JObject;
                        if (categoriaSeleccionada != null && categoriaSeleccionada.ContainsKey("Mision Final"))
                        {
                            var misionFinal = categoriaSeleccionada["Mision Final"]["MisionFinal"] as JObject;
                            if (misionFinal != null && misionFinal.ContainsKey("rutaescena"))
                            {
                                return misionFinal["rutaescena"].ToString();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al procesar el JSON: " + ex.Message);
        }

        Debug.LogWarning("No se encontró la ruta de la Misión Final.");
        return null;
    }

    private void RegresaraCategorias()
    {
        PlayerPrefs.DeleteKey("CategoriaSeleccionada");
        PlayerPrefs.Save();
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(true);
    }
}
