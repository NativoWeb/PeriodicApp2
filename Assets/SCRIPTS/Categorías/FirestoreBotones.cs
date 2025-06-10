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
        Debug.Log("📌 Cargando categorías desde archivo local...");
        CargarCategorias();
        botonSeleccionado.onClick.AddListener(OnClickContinuar);
    }

    void CargarCategorias()
    {
        categorias = CargarCategoriasDesdeArchivo();

        if (categorias.Count == 0)
        {
            Debug.LogWarning("⚠ No se encontraron categorías guardadas. Usando categorías predeterminadas.");
            categorias = ObtenerCategoriasPorDefecto();
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

        Debug.Log("✅ Categorías cargadas correctamente.");
    }

    List<Categoria> CargarCategoriasDesdeArchivo()
    {
        string rutaArchivo = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
        if (File.Exists(rutaArchivo))
        {
            try
            {
                string json = File.ReadAllText(rutaArchivo);
                CategoriasData data = JsonUtility.FromJson<CategoriasData>(json);
                if (data != null && data.categorias != null)
                {
                    return data.categorias;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error leyendo categorias.json: {ex.Message}");
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
        if (img != null && coloresPorCategoria.TryGetValue(categoria.Titulo, out string hex))
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            img.color = c;

            // ✅ Sombra primero (debajo del borde)
            Shadow shadow = img.GetComponent<Shadow>();
            if (shadow == null)
                shadow = img.gameObject.AddComponent<Shadow>();

            Color sombraColor = c * 0.5f;
            sombraColor.a = 0.8f;
            shadow.effectColor = sombraColor;
            shadow.effectDistance = new Vector2(0f, -8f);
            shadow.useGraphicAlpha = true;
            shadow.enabled = false;

            // ✅ Borde después (encima de la sombra)
            Outline outline = img.GetComponent<Outline>();
            if (outline == null)
                outline = img.gameObject.AddComponent<Outline>();

            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(4f, 4f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
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
            string jsonText = File.ReadAllText(rutaMisiones);
            float progreso = ProcesarProgresoDesdeJSON(jsonText, categoriaTitulo);
            callback(progreso);
        }
        else
        {
            Debug.LogWarning("⚠️ Json_Misiones.json no encontrado en persistentDataPath, cargando desde StreamingAssets...");
            StartCoroutine(CargarDesdeStreamingAssets("Json_Misiones.json", (jsonText) =>
            {
                float progreso = ProcesarProgresoDesdeJSON(jsonText, categoriaTitulo);
                callback(progreso);
            }));
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

    IEnumerator CargarDesdeStreamingAssets(string nombreArchivo, System.Action<string> callback)
    {
        string rutaArchivo = Path.Combine(Application.streamingAssetsPath, nombreArchivo);

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(rutaArchivo))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                callback(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"Error al cargar {nombreArchivo} desde StreamingAssets: {www.error}");
                callback(null);
            }
        }
    }


    List<Categoria> ObtenerCategoriasPorDefecto()
    {
        return new List<Categoria>
        {
            new Categoria("Metales Alcalinos", "¡Prepárate para la reactividad extrema! ¿Podrás dominar estos metales explosivos?"),
            new Categoria("Metales Alcalinotérreos", "¡Más estables, pero igual de sorprendentes! Descubre su papel esencial en la química."),
            new Categoria("Metales de Transición", "¡Los maestros del cambio! Explora los metales que forman los colores más vibrantes."),
            new Categoria("Metales postransicionales", "¡Menos famosos, pero igual de útiles! ¿Cuánto sabes de estos metales versátiles?"),
            new Categoria("Metaloides", "¡Ni metal ni no metal! Atrévete a jugar con los elementos más enigmáticos."),
            new Categoria("No Metales", "¡Elementos esenciales para la vida! Descubre su impacto en nuestro mundo."),
            new Categoria("Gases Nobles", "¡Silenciosos pero poderosos! ¿Podrás jugar con los elementos más estables?"),
            new Categoria("Lantánidos", "¡Los metales raros que hacen posible la tecnología moderna! ¿Aceptas el reto?"),
            new Categoria("Actinoides", "¡La energía del futuro! Juega con los elementos más radioactivos y misteriosos."),
            new Categoria("Propiedades desconocidas", "¡Aventúrate en lo desconocido! ¿Cuánto sabes de estos elementos misteriosos?")
        };
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
