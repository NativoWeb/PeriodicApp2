using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using System.Collections;

public class FirestoreBotones : MonoBehaviour
{
    public Transform contenedorBotones;
    public Slider SliderProgreso;
    public GameObject prefabBoton;
    public TextMeshProUGUI nombreTMP;
    public TextMeshProUGUI descripcionTMP;

    public GameObject PanelCategorias;
    public GameObject PanelElemento;

    public Button botonSeleccionado;

    private Categoria categoriaSeleccionada;
    string appIdioma;
    // Colores por categoría
    private static readonly Dictionary<string, string> coloresPorCategoria = new Dictionary<string, string>
    {
        { "Metales Alcalinos",       "#41B9DE" },
        { "Metales Alcalinotérreos", "#F0812F" },
        { "Metales de Transición",    "#ED6D9D" },
        { "Metales postransicionales","#7265AA" },
        { "Metaloides",               "#CDCBCB" },
        { "No Metales",     "#79BB51" },
        { "Gases Nobles",             "#00A293" },
        { "Lantánidos",               "#C0203C" },
        { "Actinoides",               "#33378E" },
        { "Propiedades desconocidas", "#C28958" }
    };

    private List<Categoria> categorias = new List<Categoria>();

    void Start()
    {
        Debug.Log("📌 Cargando categorías desde archivo local o PlayerPrefs...");
        CargarCategorias();
        botonSeleccionado.onClick.AddListener(OnClickContinuar);
    }

    void CargarCategorias()
    {
        categorias = CargarCategoriasDesdeArchivo();

        if (categorias.Count == 0)
        {
            Debug.LogWarning("⚠ No se encontraron categorías en archivo. Intentando cargar desde PlayerPrefs...");


            string jsonDesdePrefs_en = PlayerPrefs.GetString("categorias_encuesta_firebase_json_en", "");
            string jsonDesdePrefs = PlayerPrefs.GetString("categorias_encuesta_firebase_json", "");
            if (!string.IsNullOrEmpty(jsonDesdePrefs))
            {
                try
                {
                    CategoriasData data = JsonUtility.FromJson<CategoriasData>(jsonDesdePrefs);
                    CategoriasData data_en = JsonUtility.FromJson<CategoriasData>(jsonDesdePrefs_en);
                    if (data_en != null && data_en.categorias != null)
                    {
                        categorias = data_en.categorias;
                    }
                    else if (data != null && data.categorias != null)
                    {
                        categorias = data.categorias;
                        Debug.Log("✅ Categorías cargadas desde PlayerPrefs.");
                    }
                    else
                    {
                        Debug.LogWarning("⚠ El JSON en PlayerPrefs está vacío o mal formado.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Error leyendo PlayerPrefs: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ No se encontró información de categorías en PlayerPrefs.");
            }
        }

        bool primerBotonSeleccionado = false;

        for (int i = 0; i < categorias.Count; i++)
        {
            Categoria categoria = categorias[i];
            GameObject nuevoBoton = CrearBoton(i + 1, categoria);

            if (!primerBotonSeleccionado)
            {
                SeleccionarNivel(nuevoBoton.GetComponent<Button>(), categoria);
                primerBotonSeleccionado = true;
            }
        }

        Debug.Log("✅ Proceso de carga de categorías completado.");
    }

    List<Categoria> CargarCategoriasDesdeArchivo()
    {
        string rutaArchivo;
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        Debug.Log(appIdioma);
        if (appIdioma == "español")
        {
            rutaArchivo = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
        }
        else
        {
            rutaArchivo = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase_en.json");
        }


        if (File.Exists(rutaArchivo))
        {
            try
            {
                string json = File.ReadAllText(rutaArchivo);
                CategoriasData data = JsonUtility.FromJson<CategoriasData>(json);
                if (data != null && data.categorias != null)
                {
                    Debug.Log("✅ Categorías cargadas desde archivo.");
                    return data.categorias;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error leyendo archivo de categorías: {ex.Message}");
            }
        }

        return new List<Categoria>();
    }


    GameObject CrearBoton(int numero, Categoria categoria)
    {
        GameObject nuevoBoton = Instantiate(prefabBoton, contenedorBotones);
        nuevoBoton.SetActive(true);

        // 1) Texto y color básico
        TextMeshProUGUI textoBoton = nuevoBoton.GetComponentInChildren<TextMeshProUGUI>();
        Button boton = nuevoBoton.GetComponent<Button>();
        if (textoBoton != null) textoBoton.text = numero.ToString();

        Image img = nuevoBoton.GetComponent<Image>();
        if (img != null)
        {
            // --- INICIO DE LA MODIFICACIÓN ---

            // 1. Traduce el título a español para usarlo como clave.
            // Si ya está en español, la función debería devolverlo tal cual.
            // Si está en inglés, lo traducirá.
            string claveCategoria = devolverCatTrad(categoria.Titulo);

            // 2. Usa la clave ya traducida para buscar el color.
            if (coloresPorCategoria.TryGetValue(claveCategoria, out string hex))
            {
                ColorUtility.TryParseHtmlString(hex, out Color c);
                img.color = c;

                // ... (el resto del código para sombra y borde sigue igual) ...
                Shadow shadow = img.GetComponent<Shadow>();
                if (shadow == null)
                    shadow = img.gameObject.AddComponent<Shadow>();
                Color sombraColor = c * 0.5f;
                sombraColor.a = 0.8f;
                shadow.effectColor = sombraColor;
                shadow.effectDistance = new Vector2(0f, -8f);
                shadow.useGraphicAlpha = true;
                shadow.enabled = false;

                Outline outline = img.GetComponent<Outline>();
                if (outline == null)
                    outline = img.gameObject.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(4f, 4f);
                outline.useGraphicAlpha = false;
                outline.enabled = false;
            }
            // --- FIN DE LA MODIFICACIÓN ---
        }

        // 4) Listener de selección
        boton.onClick.AddListener(() => SeleccionarNivel(boton, categoria));

        return nuevoBoton;
    }

    void SeleccionarNivel(Button boton, Categoria categoria)
    {
        // 1) Actualiza la UI de texto
        nombreTMP.text = categoria.Titulo;
        descripcionTMP.text = categoria.Descripcion;
        categoriaSeleccionada = categoria;

        // 2) Marca visualmente el botón
        MarcarBoton(boton);

        // 3) Consulta el progreso y ajusta el slider
        ObtenerProgresoCategoria(categoria.Titulo, progreso =>
        {
            if (SliderProgreso != null)
                SliderProgreso.value = progreso;
        });
    }

    private Button anteriorSeleccionado;

    private void MarcarBoton(Button nuevo)
    {
        if (anteriorSeleccionado != null)
        {
            Image imgOld = anteriorSeleccionado.GetComponent<Image>();
            if (imgOld != null)
            {
                var outlineOld = imgOld.GetComponent<Outline>();
                var shadowOld = imgOld.GetComponent<Shadow>();

                if (outlineOld != null) outlineOld.enabled = false;
                if (shadowOld != null) shadowOld.enabled = false;
            }
        }

        Image img = nuevo.GetComponent<Image>();
        if (img != null)
        {
            var outline = img.GetComponent<Outline>();
            var shadow = img.GetComponent<Shadow>();

            if (outline != null) outline.enabled = true;
            if (shadow != null) shadow.enabled = true;
        }

        anteriorSeleccionado = nuevo;
    }

    public void OnClickContinuar()
    {
        if (categoriaSeleccionada == null)
        {
            Debug.LogWarning("⚠ No hay categoría seleccionada.");
            return;
        }

        PlayerPrefs.SetString("CategoriaSeleccionada", categoriaSeleccionada.Titulo);
        PlayerPrefs.SetString("CargarVuforia", "Misiones");
        PlayerPrefs.Save();

        PanelCategorias.SetActive(false);
        PanelElemento.SetActive(true);
    }

    public void ObtenerProgresoCategoria(string categoriaTitulo, System.Action<float> callback)
    {
        string rutaMisiones = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");

        if (File.Exists(rutaMisiones))
        {
            if(appIdioma == "ingles")
                categoriaTitulo = devolverCatTrad(categoriaTitulo);

            string jsonText = File.ReadAllText(rutaMisiones);
            float progreso = ProcesarProgresoDesdeJSON(jsonText, categoriaTitulo);
            callback(progreso);
        }
        else
        {
            Debug.LogWarning("⚠️ Json_Misiones.json no encontrado. Buscando en PlayerPrefs...");

            string jsonTextPlayerPrefs = PlayerPrefs.GetString("categorias_encuesta_firebase_json", "");

            if (!string.IsNullOrEmpty(jsonTextPlayerPrefs) && jsonTextPlayerPrefs != "{}")
            {
                if (appIdioma == "ingles")
                    categoriaTitulo = devolverCatTrad(categoriaTitulo);

                float progreso = ProcesarProgresoDesdeJSON(jsonTextPlayerPrefs, categoriaTitulo);
                callback(progreso);
            }
            else
            {
                Debug.LogWarning("⚠️ No se encontró información válida en PlayerPrefs.");
                callback(0f); // O cualquier valor predeterminado
            }
        }
    }

    private float ProcesarProgresoDesdeJSON(string jsonText, string categoriaTitulo)
    {
        var json = JSON.Parse(jsonText);

        if (!json.HasKey("Misiones") || !json["Misiones"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta.");
            return 0f;
        }

        var categoriasJSON = json["Misiones"]["Categorias"];
        if (!categoriasJSON.HasKey(categoriaTitulo) || !categoriasJSON[categoriaTitulo].HasKey("Elementos"))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaTitulo}' en el JSON.");
            return 0f;
        }

        var elementos = categoriasJSON[categoriaTitulo]["Elementos"];
        int totalMisiones = 0;
        int misionesCompletadas = 0;

        foreach (string key in elementos.Keys)
        {
            var misiones = elementos[key]["misiones"].AsArray;
            int misionesElemento = misiones.Count - 1;
            totalMisiones += misionesElemento;

            int completadasElemento = 0;
            for (int i = 0; i < misionesElemento; i++)
            {
                if (misiones[i]["completada"].AsBool)
                {
                    misionesCompletadas++;
                    completadasElemento++;
                }
            }
            if (completadasElemento == misionesElemento && misionesElemento > 0)
            {
                misionesCompletadas++; // Bonus por todas completadas
            }
        }

        return totalMisiones > 0 ? (float)misionesCompletadas / totalMisiones : 0f;
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

}

[System.Serializable]
public class CategoriasData
{
    public List<Categoria> categorias;
}

[System.Serializable]
public class Categoria
{
    public string Titulo;
    public string Descripcion;

    public Categoria(string titulo, string descripcion)
    {
        Titulo = titulo;
        Descripcion = descripcion;
    }
}
