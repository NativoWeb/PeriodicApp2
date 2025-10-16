using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PeriodicApp.Presentation.ViewModels.Menus;

public class MenuController : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Button volverButton;
    [SerializeField] private Button seleccionarButton;
    [SerializeField] private Button explorarButton;
    [SerializeField] private Button jugarButton;

    [Header("Paneles")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelSeleccion;
    [SerializeField] private GameObject panelModo;

    [Header("Modal Explora RA")]
    [SerializeField] private Button btnCerrarModal;

    [Header("Escenas")]
    [SerializeField] private string escenaCamara = "VuforiaNuevo";

    [Header("Configuración")]
    [SerializeField, Min(0f)] private float selectionDelaySeconds = 0.5f;

    private MenuViewModel viewModel;

    private void Awake()
    {
        viewModel = new MenuViewModel(selectionDelaySeconds);
        viewModel.StateChanged += OnStateChanged;
    }

    private void Start()
    {
        seleccionarButton.onClick.RemoveAllListeners();
        seleccionarButton.onClick.AddListener(OnSeleccionarClicked);

        if (volverButton != null)
        {
            volverButton.onClick.RemoveAllListeners();
            volverButton.onClick.AddListener(OnVolverClicked);
        }

        if (explorarButton != null)
        {
            explorarButton.onClick.RemoveAllListeners();
            explorarButton.onClick.AddListener(AbrirPanelModo);
        }

        if (jugarButton != null)
        {
            jugarButton.onClick.RemoveAllListeners();
            jugarButton.onClick.AddListener(IrAQuimicados);
        }

       

        viewModel.Initialize();
    }

    private void OnDestroy()
    {
        viewModel.StateChanged -= OnStateChanged;
    }

    private void OnSeleccionarClicked()
    {
        viewModel.RequestSelectionPanel();
        StartCoroutine(ShowSelectionAfterDelay());
    }

    private IEnumerator ShowSelectionAfterDelay()
    {
        yield return new WaitForSeconds(viewModel.SelectionDelaySeconds);
        viewModel.ShowSelection();
    }

    private void OnVolverClicked()
    {
        viewModel.ShowMainMenu();
    }

    private void OnStateChanged(MenuViewState state)
    {
        if (panelMainMenu != null)
        {
            panelMainMenu.SetActive(state.ShowMainMenu);
        }

        if (panelSeleccion != null)
        {
            panelSeleccion.SetActive(state.ShowSelection);
        }
    }

    // Función para abrir el PanelModo (botón Explora)
    public void AbrirPanelModo()
    {
        if (panelModo != null)
        {
            // Desactivar otros paneles
            if (panelSeleccion != null) panelSeleccion.SetActive(false);

            // Activar PanelModo
            panelModo.SetActive(true);
        }
    }

    // Función para cerrar PanelModo y volver al menú principal
    public void CerrarPanelModo()
    {
        if (panelModo != null)
        {
            panelModo.SetActive(false);
        }

        if (panelMainMenu != null)
        {
            panelMainMenu.SetActive(true);
        }
    }

    // Función para ir a la escena Quimicados (botón Jugar)
    public void IrAQuimicados()
    {
        StartCoroutine(CargarEscena("Quimicados"));
    }

    private IEnumerator CargarEscena(string nombreEscena)
    {
        yield return new WaitForSeconds(selectionDelaySeconds);
        SceneManager.LoadScene(nombreEscena);
    }

    public void IrAEscenaCamara()
    {
        StartCoroutine(ActivarRawYIrAEscena(escenaCamara));
    }

    private IEnumerator ActivarRawYIrAEscena(string escena)
    {
        yield return new WaitForSeconds(viewModel.SelectionDelaySeconds);
        SceneManager.LoadScene(escena);
    }
}
