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
    private Color colorSeleccionado = new Color(0.39f, 0.75f, 0.72f);
    private Color colorNoSeleccionado = new Color(0.83f, 0.82f, 0.82f);

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
    public TextMeshProUGUI txtTitulo;

    [Header("Botón de Regreso")]
    public Button btnRegresar;
    public GameObject panelBotones;
    public GameObject panelMisionesInfo;

    private JSONNode jsonData;

    void Start()
    {
        Debug.Log("Iniciando GestorElementos...");
        txtTitulo.text = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        CargarJSON();
        CargarElementosDesdeJSON();
        botonMisionFinal.interactable = false;
        ActualizarProgresoCategoria();
        btnMisiones.onClick.AddListener(MostrarMisiones);
        btnInformacion.onClick.AddListener(MostrarInformacion);
        btnRegresar.onClick.AddListener(RegresarAlPanelElementos);
        botonMisionFinal.onClick.AddListener(IrAMisionFinal);
        MostrarMisiones();
    }

    void CargarJSON()
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");

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
    }

    void CargarElementosDesdeJSON()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (jsonData == null ||
            !jsonData.HasKey("Misiones_Categorias") ||
            !jsonData["Misiones_Categorias"].HasKey("Categorias") ||
            !jsonData["Misiones_Categorias"]["Categorias"].HasKey(categoriaSeleccionada) ||
            !jsonData["Misiones_Categorias"]["Categorias"][categoriaSeleccionada].HasKey("Elementos"))
        {
            Debug.LogError("La categoría '" + categoriaSeleccionada + "' no se encuentra en el JSON o no tiene elementos.");
            return;
        }

        LimpiarElementos();
        var elementos = jsonData["Misiones_Categorias"]["Categorias"][categoriaSeleccionada]["Elementos"];

        // Obtener la paleta de colores correspondiente a la categoría
        Color32[] paleta = TablaPeriodicaColores.Paletas.ContainsKey(categoriaSeleccionada)
            ? TablaPeriodicaColores.Paletas[categoriaSeleccionada]
            : new Color32[] { new Color32(255, 255, 255, 255) }; // Blanco por defecto

        int indiceColor = 0;

        foreach (KeyValuePair<string, JSONNode> elemento in elementos)
        {
            CrearBotonElemento(elemento.Key, elemento.Value, categoriaSeleccionada, paleta[indiceColor % paleta.Length]);
            indiceColor++; // Para usar colores diferentes dentro de la misma categoría
        }
    }

    void CrearBotonElemento(string nombreElemento, JSONNode datosElemento, string categoria, Color32 colorBoton)
    {
        GameObject nuevoBoton = Instantiate(prefabElemento, contenedorElementos);

        TextMeshProUGUI[] textos = nuevoBoton.GetComponentsInChildren<TextMeshProUGUI>();
        if (textos.Length >= 3)
        {
            textos[0].text = datosElemento["nombre"];
            textos[1].text = "#" + datosElemento["numero_atomico"].AsInt;
            textos[2].text = datosElemento["simbolo"];
        }

        Button boton = nuevoBoton.GetComponent<Button>();
        boton.onClick.AddListener(() => SeleccionarElemento(nombreElemento));

        // Asignar el color correspondiente de la paleta
        nuevoBoton.GetComponent<Image>().color = colorBoton;
    }

    public static class TablaPeriodicaColores
    {
        public static Dictionary<string, Color32[]> Paletas = new Dictionary<string, Color32[]>
    {
        { "Metales Alcalinos", new Color32[] { new Color32(255, 204, 204, 255), new Color32(255, 178, 178, 255), new Color32(255, 153, 153, 255) } }, // Rojo pastel
        { "Metales Alcalinotérreos", new Color32[] { new Color32(255, 229, 204, 255), new Color32(255, 204, 153, 255), new Color32(255, 179, 128, 255) } }, // Naranja pastel
        { "Metales de Transición", new Color32[] { new Color32(204, 229, 255, 255), new Color32(179, 217, 255, 255), new Color32(153, 204, 255, 255) } }, // Azul pastel
        { "Metales Postransicionales", new Color32[] { new Color32(221, 160, 221, 255), new Color32(200, 140, 200, 255), new Color32(180, 120, 180, 255) } }, // Lavanda
        { "Metaloides", new Color32[] { new Color32(255, 239, 184, 255), new Color32(250, 222, 140, 255), new Color32(240, 200, 100, 255) } }, // Amarillo dorado pastel
        { "No Metales Reactivos", new Color32[] { new Color32(204, 255, 204, 255), new Color32(178, 255, 178, 255), new Color32(153, 255, 153, 255) } }, // Verde claro pastel
        { "Gases Nobles", new Color32[] { new Color32(238, 204, 255, 255), new Color32(221, 178, 255, 255), new Color32(200, 153, 255, 255) } }, // Violeta pastel
        { "Lantánidos", new Color32[] { new Color32(216, 191, 216, 255), new Color32(200, 170, 200, 255), new Color32(180, 150, 180, 255) } }, // Malva pastel
        { "Actínoides", new Color32[] { new Color32(255, 218, 233, 255), new Color32(255, 191, 219, 255), new Color32(255, 165, 204, 255) } }, // Rosa claro pastel
        { "Propiedades Desconocidas", new Color32[] { new Color32(211, 211, 211, 255), new Color32(190, 190, 190, 255), new Color32(170, 170, 170, 255) } } // Gris claro neutro
    };
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

        panelBotones.SetActive(false);
        panelMisionesInfo.SetActive(true);

        CargarDatosElementoSeleccionado();
    }

    void CargarDatosElementoSeleccionado()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        string elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "");

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
        txtNumeroAtomico.text = "Número Atómico: " + elementoJson["numero_atomico"].AsInt;
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
                    mision.xp = 100;
                    mision.colorBoton = "#FFD700"; // Dorado
                    mision.logoMision = "logosMision/ar";
                    break;
                case "QR":
                    mision.xp = 80;
                    mision.colorBoton = "#00CED1"; // Turquesa
                    mision.logoMision = "logosMision/qr";
                    break;
                case "Juego":
                    mision.xp = 120;
                    mision.colorBoton = "#32CD32"; // Verde lima
                    mision.logoMision = "logosMision/juego";
                    break;
                case "Quiz":
                    mision.xp = 90;
                    mision.colorBoton = "#4169E1"; // Azul real
                    mision.logoMision = "logosMision/quiz";
                    break;
                case "Evaluacion":
                    mision.xp = 150;
                    mision.colorBoton = "#8B0000"; // Rojo oscuro
                    mision.logoMision = "logosMision/evaluacion";
                    break;
                default:
                    mision.xp = 50;
                    mision.colorBoton = "#808080"; // Gris
                    mision.logoMision = "logosMision/default";
                    break;
            }

            PlayerPrefs.SetInt("xp_mision", mision.xp); // Guardar XP en PlayerPrefs

            CrearPrefabMision(mision);
        }
    }

    void CrearPrefabMision(Mision mision)
    {
        string elementoseleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "");
        GameObject nuevaMision = Instantiate(prefabMision, contenedorMisiones);
        UI_Mision uiMision = nuevaMision.GetComponent<UI_Mision>();
        uiMision.ConfigurarMision(mision);

        Button botonMision = nuevaMision.GetComponentInChildren<Button>();

        // Clave única para la misión
        string claveMision = $"Mision_{elementoseleccionado}_{mision.id}";

        // Si la misión ya está completada, desactivar el botón
        if (PlayerPrefs.GetInt(claveMision, 0) == 1)
        {
            botonMision.interactable = false;
            botonMision.GetComponentInChildren<TextMeshProUGUI>().text = "¡Completada!";
            botonMision.GetComponent<Image>().color = Color.gray;
        }

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
        btnInformacion.GetComponent<Image>().color = colorSeleccionado;
        btnMisiones.GetComponent<Image>().color = colorNoSeleccionado;
    }

    public void MostrarMisiones()
    {
        scrollMisiones.SetActive(true);
        scrollInformacion.SetActive(false);
        btnMisiones.GetComponent<Image>().color = colorSeleccionado;
        btnInformacion.GetComponent<Image>().color = colorNoSeleccionado;
    }

    public void RegresarAlPanelElementos()
    {
        panelBotones.SetActive(true);
        panelMisionesInfo.SetActive(false);
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

        foreach (var elemento in elementos.Keys)
        {
            var misiones = elementos[elemento]["misiones"].AsArray;
            totalMisiones += misiones.Count - 1; // No contar la misión final

            for (int i = 0; i < misiones.Count - 1; i++) // Ignorar la última misión
            {
                if (misiones[i]["completada"].AsBool)
                {
                    misionesCompletadas++;
                }
            }
        }

        float progreso = (float)misionesCompletadas / totalMisiones;
        sliderProgreso.value = progreso;

        Debug.Log($"📊 Progreso de '{categoriaSeleccionada}': {misionesCompletadas}/{totalMisiones} ({progreso * 100}%)");

        // Verificar si se debe activar la misión final
        if (misionesCompletadas == totalMisiones)
        {
            ActualizarEstadoMisionFinal();
        }
    }

    public void IrAMisionFinal()
    {
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");
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
}
