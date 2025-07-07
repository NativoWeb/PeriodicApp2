using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de tener este using para los Dropdowns de TextMeshPro
using System.Collections.Generic;
using SimpleJSON;

public class ControladorSeleccionMision : MonoBehaviour
{
    // 1. REFERENCIAS A LOS ELEMENTOS DE LA UI
    // Arrástralos desde la jerarquía al Inspector de este script
    [SerializeField] private TMP_Dropdown categoriaDropdown;
    [SerializeField] private TMP_Dropdown elementoDropdown;
    [SerializeField] private Button btnContinuarMision;
    [SerializeField] private Button btnCerrar;
    [SerializeField] private GameObject PanelSeleccionarMision;

    public static JSONNode DatosLogros;

    private void Awake()
    {
        CargarDatosDesdeResource();
    }

    public void CargarDatosDesdeResource()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("Plantillas_Json/Json_Logros");
        DatosLogros = JSON.Parse(jsonData.text);
    }

    private Dictionary<string, List<string>> datosElementos = new Dictionary<string, List<string>>();

    void Start()
    {
        IniciarPanel();
        btnCerrar.onClick.AddListener(CerrarPanel);
    }

    public void IniciarPanel()
    {
        btnContinuarMision.interactable = false;
        elementoDropdown.interactable = false;

        CargarDatosDesdeJSON();
        PoblarCategorias();

        categoriaDropdown.onValueChanged.RemoveAllListeners();
        elementoDropdown.onValueChanged.RemoveAllListeners();

        categoriaDropdown.onValueChanged.AddListener(OnCategoriaSeleccionada);
        elementoDropdown.onValueChanged.AddListener(OnElementoSeleccionado);
    }

    void CargarDatosDesdeJSON()
    {
        datosElementos.Clear();

        var categorias = DatosLogros["Logros"]["Categorias"];

        foreach (var cat in categorias.Keys)
        {
            var elementosJSON = categorias[cat]["logros_elementos"];
            List<string> listaElementos = new List<string>();

            foreach (var elem in elementosJSON.Keys)
            {
                string simbolo = elementosJSON[elem]["simbolo"];
                listaElementos.Add($"{elem} ({simbolo})");
            }

            datosElementos[cat] = listaElementos;
        }
    }

    void PoblarCategorias()
    {
        categoriaDropdown.ClearOptions();
        categoriaDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione una categoría..."));
        categoriaDropdown.AddOptions(new List<string>(datosElementos.Keys));
        categoriaDropdown.value = 0;
        categoriaDropdown.RefreshShownValue();
    }

    private void OnCategoriaSeleccionada(int index)
    {
        if (index == 0)
        {
            elementoDropdown.interactable = false;
            elementoDropdown.ClearOptions();
            elementoDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione un elemento..."));
            elementoDropdown.value = 0;
            elementoDropdown.RefreshShownValue();
        }
        else
        {
            string categoriaSeleccionada = categoriaDropdown.options[index].text;
            PoblarElementos(datosElementos[categoriaSeleccionada]);
            elementoDropdown.interactable = true;
        }

        ValidarSeleccion();
    }

    private void PoblarElementos(List<string> elementos)
    {
        elementoDropdown.ClearOptions();
        elementoDropdown.options.Add(new TMP_Dropdown.OptionData("Seleccione un elemento..."));
        elementoDropdown.AddOptions(elementos);
        elementoDropdown.value = 0;
        elementoDropdown.RefreshShownValue();
    }

    private void OnElementoSeleccionado(int index)
    {
        ValidarSeleccion();
    }

    private void ValidarSeleccion()
    {
        bool categoriaValida = categoriaDropdown.value > 0;
        bool elementoValido = elementoDropdown.interactable && elementoDropdown.value > 0;

        btnContinuarMision.interactable = categoriaValida && elementoValido;
    }

    public (string categoria, string elemento) ObtenerSeleccion()
    {
        if (btnContinuarMision.interactable)
        {
            string cat = categoriaDropdown.options[categoriaDropdown.value].text;
            string elem = elementoDropdown.options[elementoDropdown.value].text;
            return (cat, elem);
        }
        return (null, null);
    }

    void CerrarPanel()
    {
        PanelSeleccionarMision.SetActive(false);
    }
}