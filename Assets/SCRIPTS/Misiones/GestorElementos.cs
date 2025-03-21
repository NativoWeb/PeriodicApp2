using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.SceneManagement;
using UnityEngine;


public class GestorElementos : MonoBehaviour
{
    [Header("Panel Principal del Elemento")]
    public TextMeshProUGUI txtSimbolo;
    public TextMeshProUGUI txtNombre;
    public TextMeshProUGUI txtNumeroAtomico;

    [Header("Contenedores")]
    public GameObject scrollMisiones;
    public GameObject scrollInformacion;

    [Header("Botones de Cambio")]
    public Button btnMisiones;
    public Button btnInformacion;
    private Color colorSeleccionado = new Color(0.47f, 0.85f, 0.54f); // #77D98B
    private Color colorNoSeleccionado = new Color(0.83f, 0.82f, 0.82f); // #D4D1D1

    [Header("UI Misiones")]
    public GameObject prefabMision;
    public Transform contenedorMisiones;

    [Header("UI Información")]
    public TextMeshProUGUI txtMasaAtomica;
    public TextMeshProUGUI txtPuntoFusion;
    public TextMeshProUGUI txtPuntoEbullicion;
    public TextMeshProUGUI txtElectronegatividad;
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtDescripcion;

    [Header("Botones de Elementos")]
    public List<Button> botonesElementos; // Lista de botones de elementos
    public List<TextMeshProUGUI> txtBotonesElementos; // Nombres de elementos en los botones

    [Header("Elemento Actual")]
    public string elementoSeleccionado = "";

    [Header("Botón de Regreso")]
    public Button btnRegresar;

    public GameObject panelBotones;
    public GameObject panelMisionesInfo;

    void Start()
    {
        // Guardar el JSON en PlayerPrefs al iniciar
        CargarElementosDesdeJSON();

        // Asignar eventos dinámicamente a cada botón de elemento
        for (int i = 0; i < botonesElementos.Count; i++)
        {
            int index = i;
            botonesElementos[i].onClick.AddListener(() => SeleccionarElemento(txtBotonesElementos[index].text));
        }

        // Asignar funciones a los botones de navegación
        btnMisiones.onClick.AddListener(MostrarMisiones);
        btnInformacion.onClick.AddListener(MostrarInformacion);
        btnRegresar.onClick.AddListener(RegresarAlPanelElementos);

        // Mostrar Misiones por defecto
        MostrarMisiones();
    }

    public void SeleccionarElemento(string nombreElemento)
    {
        elementoSeleccionado = nombreElemento;

        // Ocultar el panel de botones y mostrar el panel de información
        panelBotones.SetActive(false);
        panelMisionesInfo.SetActive(true);

        // Recargar información del nuevo elemento
        CargarElementosDesdeJSON();
    }

    void CargarElementosDesdeJSON()
    {
        string jsonString = PlayerPrefs.GetString("misionesJSON", "");

        if (string.IsNullOrEmpty(jsonString))
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("misiones");
            if (jsonFile != null)
            {
                jsonString = jsonFile.text;
                PlayerPrefs.SetString("misionesJSON", jsonString); // Guardar en PlayerPrefs para futuras cargas
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("No se encontró el archivo JSON en Resources.");
                return;
            }
        }

        var json = JSON.Parse(jsonString);

        if (json == null)
        {
            Debug.LogError("El JSON es inválido o no se pudo parsear.");
            return;
        }

        if (!json.HasKey("misiones") || !json["misiones"].HasKey(elementoSeleccionado))
        {
            Debug.LogError($"No se encontraron misiones para el elemento {elementoSeleccionado}.");
            return;
        }

        var elementoJson = json["misiones"][elementoSeleccionado];

        txtSimbolo.text = elementoJson["simbolo"].Value;
        txtNombre.text = elementoJson["nombre"].Value;
        txtNumeroAtomico.text = "Número Atómico: " + elementoJson["numero_atomico"].AsInt;
        txtMasaAtomica.text = elementoJson["masa_atomica"].Value;
        txtPuntoFusion.text = elementoJson["punto_fusion"].Value + "°C";
        txtPuntoEbullicion.text = elementoJson["punto_ebullicion"].Value + "°C";
        txtElectronegatividad.text = elementoJson["electronegatividad"].Value;
        txtEstado.text = elementoJson["estado"].Value;
        txtDescripcion.text = elementoJson["descripcion"].Value;

        LimpiarMisiones();

        foreach (JSONNode nivelJson in elementoJson["niveles"].AsArray)
        {
            Mision mision = new Mision
            {
                id = nivelJson["id"].AsInt,
                titulo = nivelJson["titulo"].Value,
                descripcion = nivelJson["descripcion"].Value,
                tipo = nivelJson["tipo"].Value,
                colorBoton = nivelJson["colorBoton"].Value,
                logoMision = nivelJson["logoMision"].Value,
                completada = nivelJson["completada"].AsBool,
                xp = nivelJson["xp"].AsInt,
                mensajeCompletada = nivelJson["mensajeCompletada"].Value,
                rutaEscena = nivelJson["rutaescena"].Value
            };

            CrearPrefabMision(mision);
        }
    }


    void LimpiarMisiones()
    {
        foreach (Transform child in contenedorMisiones)
        {
            Destroy(child.gameObject);
        }
    }

    void CrearPrefabMision(Mision mision)
    {
        GameObject nuevaMision = Instantiate(prefabMision, contenedorMisiones);
        UI_Mision uiMision = nuevaMision.GetComponent<UI_Mision>();
        uiMision.ConfigurarMision(mision);

        Button botonMision = nuevaMision.GetComponentInChildren<Button>();

        // Clave única para la misión
        string claveMision = $"Mision_{elementoSeleccionado}_{mision.id}";

        // Si la misión ya está completada, desactivar el botón
        if (PlayerPrefs.GetInt(claveMision, 0) == 1)
        {
            botonMision.interactable = false;
            botonMision.GetComponentInChildren<TextMeshProUGUI>().text = "¡Completada!";
            botonMision.GetComponent<Image>().color = Color.gray;
        }

        // Asignar evento para cambiar de escena
        botonMision.onClick.AddListener(() => CargarEscenaMision(mision.rutaEscena, elementoSeleccionado, mision.id));
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
        elementoSeleccionado = ""; // Limpiar el elemento seleccionado
        panelBotones.SetActive(true); // Mostrar el panel inicial
        panelMisionesInfo.SetActive(false); // Ocultar el panel de información/misiones

        // Opcional: Limpiar los textos si deseas que se vacíen al regresar
        txtSimbolo.text = "";
        txtNombre.text = "";
        txtNumeroAtomico.text = "";
        txtMasaAtomica.text = "";
        txtPuntoFusion.text = "";
        txtPuntoEbullicion.text = "";
        txtElectronegatividad.text = "";
        txtEstado.text = "";
        txtDescripcion.text = "";

        // Limpiar las misiones generadas en la UI
        LimpiarMisiones();
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
        PlayerPrefs.SetInt("MisionActual", idMision);
        if (idMision == 1)
        {
            PlayerPrefs.SetString("CargarVuforia", "Misiones");
        }
        PlayerPrefs.Save();

        // Cargar la escena de la misión
        SceneManager.LoadScene(nombreEscena);
    }
}