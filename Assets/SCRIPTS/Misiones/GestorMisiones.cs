using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections;

public class GestorMisiones : MonoBehaviour
{
    [Header("Panel Principal del Elemento")]
    public GameObject PanelMisiones;
    public GameObject PanelDatosElemento;
    public TextMeshProUGUI txtSimbolo;
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNumeroAtomico;
    public Image ImgElemento;

    [Header("Botones de Cambio")]
    public Button btnMisiones;
    public Button btnInformacion;

    [Header("UI Misiones")]
    public GameObject prefabMision;
    public Transform contenedorMisiones;


    [Header("Botón de Regreso a categorias")]
    public Button BtnCategorias;
    public GameObject PanelCategorias;
    public GameObject PanelElemento;
    public GameObject PanelInformacion;

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
    private bool datosCargados = false;

    void Awake()
    {
        // Setup de listeners que no dependen de JSON
        btnInformacion.onClick.AddListener(IrAIformacion);
        BtnCategorias.onClick.AddListener(RegresaraCategorias);
    }

    void OnEnable()
    {
        // Sólo refresca si ya cargamos los datos
        if (datosCargados)
        {
            RefrescarUI();
        }
    }

    IEnumerator Start()
    {
        // Start es ejecutado después de Awake y OnEnable
        yield return StartCoroutine(CargarJSONYContinuar());

        datosCargados = true;

        // Aquí sí podemos inicializar la UI por primera vez
        RefrescarUI();
    }

    IEnumerator CargarJSONYContinuar()
    {
        yield return CargarJSON("json_informacion.json", nodo => jsonDataInformacion = nodo);
        yield return CargarJSON("json_misiones.json", nodo => jsonDataMisiones = nodo);
    }

    IEnumerator CargarJSON(string nombreArchivo, System.Action<JSONNode> callback)
    {
        string rutaPersistente = Path.Combine(Application.persistentDataPath, nombreArchivo);
        if (File.Exists(rutaPersistente))
        {
            callback(JSON.Parse(File.ReadAllText(rutaPersistente)));
            yield break;
        }

        // Si no existe, lo cargo de Resources
        string recurso = $"Plantillas_Json/{Path.GetFileNameWithoutExtension(nombreArchivo)}";
        TextAsset txt = Resources.Load<TextAsset>(recurso);

        // espero un frame para evitar race conditions
        yield return null;

        if (txt != null)
        {
            callback(JSON.Parse(txt.text));
        }
        else
        {
            Debug.LogError($"❌ No se encontró {nombreArchivo} en Resources/{recurso}");
            callback(null);
        }
    }

    private void RefrescarUI()
    {
        CargarInfoElementoSeleccionado();
        CargarDatosElementoSeleccionado();
    }


    void CargarInfoElementoSeleccionado()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
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


        // Cambiar color del boton de misiones
        if (btnMisiones != null)
        {
            var ImgMisiones = btnMisiones.GetComponent<Image>();
            if (ImgMisiones != null)
            {
                Color32 colorCat = ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var c) ? c : new Color32(255, 255, 255, 255);
                ImgMisiones.color = colorCat;
            }
        }

        // Verificar si el JSON cargado existe
        if (jsonDataInformacion == null ||
            !jsonDataInformacion.HasKey("Informacion") ||
            !jsonDataInformacion["Informacion"].HasKey("Categorias"))
        {
            Debug.LogError("El JSON de información cargado desde archivo es inválido.");
            return;
        }

        var categorias = jsonDataInformacion["Informacion"]["Categorias"];

        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey(elementoSeleccionado))
        {
            Debug.LogError("No se encontró el elemento seleccionado.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada][elementoSeleccionado];

        txtSimbolo.text = elementoJson["simbolo"];
        txtNombre.text = elementoJson["nombre"];
        txtNumeroAtomico.text = elementoJson["numero_atomico"].Value;
    }

    void CargarDatosElementoSeleccionado()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada");
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado");
        // Color del panel según categoría
        if (PanelDatosElemento != null)
        {
            var imgPanel = PanelDatosElemento.GetComponent<Image>();
            if (imgPanel != null && ColoresPorCategoria.TryGetValue(categoriaSeleccionada, out var colorCat))
            {
                imgPanel.color = colorCat;
            }
        }

        if (jsonDataMisiones == null ||
            !jsonDataMisiones.HasKey("Misiones") ||
            !jsonDataMisiones["Misiones"].HasKey("Categorias") ||
            !jsonDataMisiones["Misiones"]["Categorias"].HasKey(categoriaSeleccionada) ||
            !jsonDataMisiones["Misiones"]["Categorias"][categoriaSeleccionada].HasKey("Elementos") ||
            !jsonDataMisiones["Misiones"]["Categorias"][categoriaSeleccionada]["Elementos"].HasKey(elementoSeleccionado))
        {
            Debug.LogError("No se encontró la categoría o el elemento en json_misiones.json");
            return;
        }

        var misionesArray = jsonDataMisiones["Misiones"]["Categorias"][categoriaSeleccionada]["Elementos"][elementoSeleccionado]["misiones"].AsArray;

        LimpiarMisiones(); // Limpia el contenido previo

        foreach (JSONNode misionJson in misionesArray)
        {
            Mision mision = new Mision
            {
                id = misionJson["id"].AsInt,
                titulo = misionJson["titulo"],
                descripcion = misionJson["descripcion"],
                tipo = misionJson["tipo"],
                rutaEscena = misionJson["rutaescena"],
                completada = misionJson["completada"].AsBool
            };

            switch (mision.tipo)
            {
                case "AR":
                    mision.xp = 10;
                    mision.logoMision = "logosMision/ar";
                    break;
                case "QR":
                    mision.xp = 10;
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

            PlayerPrefs.SetInt("xp_mision", mision.xp); // Puedes quitarlo si no es necesario

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

    void LimpiarMisiones()
    {
        foreach (Transform child in contenedorMisiones)
        {
            Destroy(child.gameObject);
        }
    }

    private void RegresaraCategorias()
    {
        PanelElemento.SetActive(true);
        PanelInformacion.SetActive(false);
        PanelMisiones.SetActive(false);
    }

    public void IrAIformacion()
    {
        PanelInformacion.SetActive(true);
        PanelMisiones.SetActive(false);
        PanelElemento.SetActive(false);
        PanelCategorias.SetActive(false);
    }
}
