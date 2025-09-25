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

    [Header("Paneles")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelSeleccion;

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

    private IEnumerator ActivarRawYIrAEscena(string escena)
    {
        yield return new WaitForSeconds(viewModel.SelectionDelaySeconds);
        SceneManager.LoadScene(escena);
    }
}
