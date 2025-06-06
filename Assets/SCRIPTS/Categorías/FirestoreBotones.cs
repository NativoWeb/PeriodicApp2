using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Google.MiniJSON;
using SimpleJSON;

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


    // Dentro de FirestoreBotones, justo después de los demás campos:
    private static readonly Dictionary<string, string> coloresPorCategoria = new Dictionary<string, string>
    {
        { "Metales Alcalinos",       "#41B9DE" },
        { "Metales Alcalinotérreos", "#F0812F" },
        { "Metales de Transición",    "#ED6D9D" },
        { "Metales Postransicionales","#7265AA" },
        { "Metaloides",               "#CDCBCB" },
        { "No Metales Reactivos",     "#79BB51" },
        { "Gases Nobles",             "#00A293" },
        { "Lantánidos",               "#C0203C" },
        { "Actínoides",               "#33378E" },
        { "Propiedades Desconocidas", "#C28958" }
    };

    private List<Categoria> categorias = new List<Categoria>();

    void Start()
    {
        Debug.Log("📌 Cargando categorías desde PlayerPrefs...");
        CargarCategorias();
        string username = PlayerPrefs.GetString("DisplayName", "");
        botonSeleccionado.onClick.AddListener(OnClickContinuar);
    }

    void CargarCategorias()
    {
        categorias = ObtenerCategoriasDesdePlayerPrefs();

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

    GameObject CrearBoton(int numero, Categoria categoria)
    {
        GameObject nuevoBoton = Instantiate(prefabBoton, contenedorBotones);
        nuevoBoton.SetActive(true);

        TextMeshProUGUI textoBoton = nuevoBoton.GetComponentInChildren<TextMeshProUGUI>();
        Button boton = nuevoBoton.GetComponent<Button>();

        if (textoBoton != null)
            textoBoton.text = numero.ToString();

        // — Asigna color de fondo según categoría —
        Image img = boton.GetComponent<Image>();
        if (img != null && coloresPorCategoria.TryGetValue(categoria.Titulo, out string hex))
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            img.color = c;
        }

        // Listener para mostrar datos en los TMP
        boton.onClick.AddListener(() => SeleccionarNivel(boton, categoria));

        return nuevoBoton;
    }
    void SeleccionarNivel(Button boton, Categoria categoria)
    {
        nombreTMP.text = categoria.Titulo;
        descripcionTMP.text = categoria.Descripcion;

        categoriaSeleccionada = categoria;

        // Mostrar progreso
        float progreso = ObtenerProgresoCategoria(categoria.Titulo);
        if (SliderProgreso != null)
        {
            SliderProgreso.value = progreso;
        }
    }

    public void OnClickContinuar()
    {
        if (categoriaSeleccionada == null)
        {
            Debug.LogWarning("⚠ No hay categoría seleccionada.");
            return;
        }

        // Guardar en PlayerPrefs
        PlayerPrefs.SetString("CategoriaSeleccionada", categoriaSeleccionada.Titulo);
        PlayerPrefs.SetString("CargarVuforia", "Misiones");
        PlayerPrefs.Save();

        // Cambiar paneles
        PanelCategorias.SetActive(false);
        PanelElemento.SetActive(true);
    }

    List<Categoria> ObtenerCategoriasDesdePlayerPrefs()
    {
        if (PlayerPrefs.HasKey("CategoriasOrdenadas"))
        {
            string json = PlayerPrefs.GetString("CategoriasOrdenadas");
            if (!string.IsNullOrEmpty(json))
            {
                CategoriasData data = JsonUtility.FromJson<CategoriasData>(json);
                if (data != null && data.categorias != null)
                {
                    return data.categorias;
                }
            }
        }
        return new List<Categoria>();
    }

    List<Categoria> ObtenerCategoriasPorDefecto()
    {
        return new List<Categoria>
        {
            new Categoria("Metales Alcalinos", "¡Prepárate para la reactividad extrema! ¿Podrás dominar estos metales explosivos?"),
            new Categoria("Metales Alcalinotérreos", "¡Más estables, pero igual de sorprendentes! Descubre su papel esencial en la química."),
            new Categoria("Metales de Transición", "¡Los maestros del cambio! Explora los metales que forman los colores más vibrantes."),
            new Categoria("Metales Postransicionales", "¡Menos famosos, pero igual de útiles! ¿Cuánto sabes de estos metales versátiles?"),
            new Categoria("Metaloides", "¡Ni metal ni no metal! Atrévete a jugar con los elementos más enigmáticos."),
            new Categoria("No Metales Reactivos", "¡Elementos esenciales para la vida! Descubre su impacto en nuestro mundo."),
            new Categoria("Gases Nobles", "¡Silenciosos pero poderosos! ¿Podrás jugar con los elementos más estables?"),
            new Categoria("Lantánidos", "¡Los metales raros que hacen posible la tecnología moderna! ¿Aceptas el reto?"),
            new Categoria("Actínoides", "¡La energía del futuro! Juega con los elementos más radioactivos y misteriosos."),
            new Categoria("Propiedades Desconocidas", "¡Aventúrate en lo desconocido! ¿Cuánto sabes de estos elementos misteriosos?")
        };
    }

    // Método para obtener el progreso de una categoría específica
    float ObtenerProgresoCategoria(string categoriaSeleccionada)
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return 0f;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Misiones_Categorias") || !json["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta.");
            return 0f;
        }

        var categorias = json["Misiones_Categorias"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) || !categorias[categoriaSeleccionada].HasKey("Elementos"))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaSeleccionada}' en el JSON.");
            return 0f;
        }

        var elementos = categorias[categoriaSeleccionada]["Elementos"];
        int totalMisiones = 0;
        int misionesCompletadas = 0;

        foreach (var elemento in elementos.Keys)
        {
            var misiones = elementos[elemento]["misiones"].AsArray;
            int misionesElemento = misiones.Count - 1; // No contar la misión final
            totalMisiones += misionesElemento;

            int misionesCompletadasElemento = 0;
            for (int i = 0; i < misionesElemento; i++)
            {
                if (misiones[i]["completada"].AsBool)
                {
                    misionesCompletadas++;
                    misionesCompletadasElemento++;
                }
            }

            // Si todas las misiones de un elemento se completan, sumar un pequeño progreso extra
            if (misionesElemento > 0 && misionesCompletadasElemento == misionesElemento)
            {
                misionesCompletadas += 1; // Bonus por completar todas las misiones de un elemento
            }
        }

        return totalMisiones > 0 ? (float)misionesCompletadas / totalMisiones : 0f;
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
