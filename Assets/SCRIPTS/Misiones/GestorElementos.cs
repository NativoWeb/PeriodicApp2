using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections;

public class GestorElementos : MonoBehaviour
{
    [Header("Prefab de Elementos")]
    public GameObject prefabElemento;
    public Transform contenedorElementos;
    [SerializeField] private Slider sliderProgreso;
    public Image PanelCat;

    [Header("Mision Final")]
    public Button botonMisionFinal; // Asigna el botón desde el Inspector
    public GameObject PanelMisionCompletada;
    public TextMeshProUGUI Descripcion;
    public GameObject PanelMisionIncompleta;
    public TextMeshProUGUI Description;

    [Header("Descripción de la Categoría")]
    public TextMeshProUGUI txtDescripcionCategoria;
    public TextMeshProUGUI txtTitulo;

    [Header("Botón de Regreso")]
    public GameObject panelMisionesInfo;

    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;

    private string JsonIdioma;
    private string appIdioma;

    string categoriaSeleccionada;

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

    List<Categoria> ObtenerCategoriasPorDefecto_en()
    {
        return new List<Categoria>
                {
                    new Categoria("Alkali Metals",
                        "Explore the most reactive on the table! Alkali metals are so active they need to be stored under oil to avoid reacting with the air. Lightweight, shiny, and explosive with water: a chemical adventure is guaranteed!"),

                    new Categoria("Alkaline Earth Metals",
                        "Stable but surprising! These metals aren't as impulsive as the alkali metals, but they also know how to grab attention. Found in our bones, fireworks, and more, get ready to discover their versatility!"),

                    new Categoria("Transition Metals",
                        "The true chameleons of chemistry! They master the art of forming colorful compounds, catalyzing reactions, and building strong structures. If you like challenges and change, this is your category."),

                    new Categoria("Post-transition Metals",
                        "Don't underestimate the discreet ones! Although less known, these elements are vital for modern technology. Softly malleable, conductive, and with everyday uses, discover their silent impact!"),

                    new Categoria("Metalloids",
                        "On the edge between two worlds! Metalloids have properties of both metals and non-metals. Unpredictable, interesting, and essential in electronics, perfect for those who love the unexpected!"),

                    new Categoria("Nonmetals",
                        "The pillars of life and organic chemistry! From the oxygen you breathe to the carbon in your DNA, nonmetals are essential for everything that lives. Investigate their crucial role in the universe!"),

                    new Categoria("Noble Gases",
                        "Silent, invisible, and invaluable! These elements don't react easily, but they are present in lights, protective atmospheres, and scientific experiments. Their stability is their superpower!"),

                    new Categoria("Lanthanides",
                        "The rare metals that move the modern world! Used in powerful magnets, lasers, and high-tech screens. Although rare, their presence is fundamental in our daily lives. Discover them!"),

                    new Categoria("Actinides",
                        "The most powerful energy on the table! Radioactive, mysterious, and with the potential to revolutionize the world, these elements are linked to nuclear energy and the scientific exploration of the future."),

                    new Categoria("Unknown Properties",
                        "Welcome to unexplored territory! These elements are at the limits of what is known. Their properties are still being investigated, and each discovery can change what we know. Do you dare to discover the unknown?")
                };
    }

    List<Categoria> ObtenerCategoriasPorDefecto()
    {
        return new List<Categoria>
        {
            new Categoria("Metales Alcalinos",
                "¡Explora a los más reactivos de la tabla! Los metales alcalinos son tan activos que necesitan estar bajo aceite para no reaccionar con el aire. Livianos, brillantes y explosivos con el agua: ¡una aventura química garantizada!"),

            new Categoria("Metales Alcalinotérreos",
                "¡Estables pero sorprendentes! Estos metales no son tan impulsivos como los alcalinos, pero también saben cómo llamar la atención. Presentes en nuestros huesos, fuegos artificiales y más, ¡prepárate para descubrir su versatilidad!"),

            new Categoria("Metales de Transición",
                "¡Los verdaderos camaleones de la química! Dominan el arte de formar compuestos coloridos, catalizar reacciones y construir estructuras resistentes. Si te gustan los desafíos y los cambios, esta es tu categoría."),

            new Categoria("Metales postransicionales",
                "¡No subestimes a los discretos! Aunque menos conocidos, estos elementos son vitales para la tecnología moderna. Suavemente maleables, conductores y con usos cotidianos, ¡descubre su impacto silencioso!"),

            new Categoria("Metaloides",
                "¡En el límite entre dos mundos! Los metaloides tienen propiedades tanto de metales como de no metales. Impredecibles, interesantes y esenciales en la electrónica, ¡perfectos para quienes aman lo inesperado!"),

            new Categoria("No Metales",
                "¡Los pilares de la vida y la química orgánica! Desde el oxígeno que respiras hasta el carbono de tu ADN, los no metales son esenciales para todo lo que vive. ¡Investiga su papel crucial en el universo!"),

            new Categoria("Gases Nobles",
                "¡Silenciosos, invisibles e invaluables! Estos elementos no reaccionan fácilmente, pero están presentes en luces, atmósferas protectoras y experimentos científicos. ¡Su estabilidad es su superpoder!"),

            new Categoria("Lantánidos",
                "¡Los metales raros que mueven el mundo moderno! Utilizados en imanes potentes, láseres y pantallas de alta tecnología. Aunque raros, su presencia es fundamental en nuestra vida diaria. ¡Descúbrelos!"),

            new Categoria("Actinoides",
                "¡La energía más poderosa de la tabla! Radiactivos, misteriosos y con potencial para revolucionar el mundo, estos elementos están ligados a la energía nuclear y la exploración científica del futuro."),

            new Categoria("Propiedades desconocidas",
                "¡Bienvenido al territorio inexplorado! Estos elementos están en los límites de lo conocido. Sus propiedades aún se investigan, y cada descubrimiento puede cambiar lo que sabemos. ¿Te atreves a descubrir lo desconocido?")
        };
        
    }

    void OnEnable()
    {
        categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);

        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        if (appIdioma == "español")
        {
            JsonIdioma = "Json_Informacion.json";
        }
        else
        {
            JsonIdioma = "Json_Informacion_en.json";
        }

        StartCoroutine(InicializarPanelElementoAsync());
    }

    IEnumerator InicializarPanelElementoAsync()
    {
        Debug.Log("Iniciando GestorElementos...");
        
        // 1) Título de categoría
        string cat = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        txtTitulo.text = cat;

        // 2) Descripción

        if (appIdioma == "español")
        {
            cat = devolverCatTrad(cat);
            var lista = ObtenerCategoriasPorDefecto();
            var match = lista.Find(c => c.Titulo == cat);
            txtDescripcionCategoria.text = match != null
                ? match.Descripcion
                : "Descripción no disponible.";
        }
        else
        {
            var lista = ObtenerCategoriasPorDefecto_en();
            var match = lista.Find(c => c.Titulo == cat);
            txtDescripcionCategoria.text = match != null
                ? match.Descripcion
                : "Description not available.";
        }

        yield return StartCoroutine(CargarJSON());

        // Esperar a que termine la carga antes de continuar
        CargarElementosDesdeJSON();
        ActualizarProgresoCategoria();
        ActualizarEstadoMisionFinal();

        botonMisionFinal.onClick.AddListener(IrAMisionFinal);
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
    }

    IEnumerator CargarJSON()
    {
        // INFORMACIÓN
        string pathInformacion = Path.Combine(Application.persistentDataPath, JsonIdioma);

        if (File.Exists(pathInformacion))
        {
            string jsonStringInformacion = File.ReadAllText(pathInformacion);
            jsonDataInformacion = JSON.Parse(jsonStringInformacion);
            Debug.Log("json_informacion.json cargado desde persistentDataPath.");
        }
        else
        {
            yield return StartCoroutine(CargarDesdeResources(JsonIdioma, (json) =>
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
        Debug.Log(jsonDataInformacion);

        if (string.IsNullOrEmpty(categoriaSeleccionada))
        {
            Debug.LogError("Error: No se ha seleccionado ninguna categoría.");
            return;
        }

        if (PanelCat == null)
        {
            Debug.LogError("PanelCatBackground no asignado en el Inspector.");
            return;
        }

        // Construye la ruta relativa dentro de Resources
        string ruta = "FondoCategorias/" + categoriaSeleccionada;
        Sprite fondo = Resources.Load<Sprite>(ruta);

        if (fondo != null)
        {
            PanelCat.sprite = fondo;
            PanelCat.enabled = true;
            Debug.Log($"✅ Fondo de categoría '{categoriaSeleccionada}' cargado: Resources/{ruta}.png");
        }
        else
        {
            PanelCat.enabled = false;
            Debug.LogWarning($"⚠️ No se encontró sprite en Resources/{ruta}.png");
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

        panelMisionesInfo.SetActive(true);
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(false);
    }

    public void ActualizarProgresoCategoria()
    {
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

            // 🔍 Extraer título de la misión final si existe
            if (elementoNode.HasKey("Mision Final") &&
                elementoNode["Mision Final"].HasKey("MisionFinal") &&
                elementoNode["Mision Final"]["MisionFinal"].HasKey("titulo"))
            {
                string tituloMisionFinal = elementoNode["Mision Final"]["MisionFinal"]["titulo"];
                Descripcion.text = tituloMisionFinal;
                Description.text = tituloMisionFinal;
                Debug.Log($"🧪 Misión Final del elemento '{elemento}': {tituloMisionFinal}");
            }

            // Verificar si misión final está completada
            if (elementoNode.HasKey("Mision Final") &&
                elementoNode["Mision Final"].HasKey("MisionFinal"))
            {
                var misionFinal = elementoNode["Mision Final"]["MisionFinal"];
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

    string devolverCatTrad(string categoriaSeleccionada)
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
    public void IrAMisionFinal()
    {
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
            PanelMisionCompletada.SetActive(true);
            PanelMisionIncompleta.SetActive(false);
        }
        else
        {
            PanelMisionIncompleta.SetActive(true);
            PanelMisionCompletada.SetActive(false);
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