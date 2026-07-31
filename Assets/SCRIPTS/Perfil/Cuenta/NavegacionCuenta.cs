using UnityEngine;
using UnityEngine.UI;

public class NavegacionCuenta : MonoBehaviour
{
    [Header("paneles de navegacion")]
    [SerializeField] public GameObject panelMenuCuenta;
    [SerializeField] public GameObject panelTerminos_Condiciones;
    [SerializeField] public GameObject panelPoliticas;
    [SerializeField] public GameObject panelDatosPersonales;
    [SerializeField] public GameObject panelCerrarSesion;
    [SerializeField] public GameObject PanelIdiomas;
    [SerializeField] public Image panelSuperior;
    public Button btnIdiomas;
    public Button btnEspanol;
    public Button btnIngles;
    public Button btnDatos;

    private void Start()
    {
        string navegacionCuenta = PlayerPrefs.GetString("navegacionCuenta", "estudiante");

        if (navegacionCuenta == "profesor")
        {
            btnDatos.gameObject.SetActive(false);
            Color customColor = new Color(80f / 255f, 178f / 255f, 125f / 255f, 1f);
            panelSuperior.color = customColor;
        }
        else
        {
            btnDatos.gameObject.SetActive(true);
            Color customColor = new Color(59f / 255f, 53f / 255f, 139f / 255f, 1f);
            panelSuperior.color = customColor;
        }

        btnIdiomas.onClick.AddListener(cambiarIdioma);
        btnEspanol.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(0));
        btnIngles.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(1));
    }

    public void verMenuCuenta()
    {
        PanelAnimator.Show(panelMenuCuenta);
        PanelAnimator.Hide(panelTerminos_Condiciones);
        PanelAnimator.Hide(panelPoliticas);
        PanelAnimator.Hide(panelDatosPersonales);
    }

    public void verTerminosCondiciones()
    {
        PanelAnimator.Show(panelTerminos_Condiciones);
        PanelAnimator.Hide(panelMenuCuenta);
        PanelAnimator.Hide(panelPoliticas);
        PanelAnimator.Hide(panelDatosPersonales);
    }

    public void verPoliticas()
    {
        PanelAnimator.Show(panelPoliticas);
        PanelAnimator.Hide(panelTerminos_Condiciones);
        PanelAnimator.Hide(panelMenuCuenta);
        PanelAnimator.Hide(panelDatosPersonales);
    }

    public void verDatosPersonales()
    {
        PanelAnimator.Show(panelDatosPersonales);
        PanelAnimator.Hide(panelPoliticas);
        PanelAnimator.Hide(panelTerminos_Condiciones);
        PanelAnimator.Hide(panelMenuCuenta);
    }

    public void cambiarIdioma()
    {
        PanelAnimator.Show(PanelIdiomas);
        PanelAnimator.Hide(panelDatosPersonales);
        PanelAnimator.Hide(panelPoliticas);
        PanelAnimator.Hide(panelTerminos_Condiciones);
    }

    private void CambiarIdiomaY_CerrarPanel(int id)
    {
        if (ControladorIdioma.instancia != null)
        {
            ControladorIdioma.instancia.ChangeLocale(id);
        }
        PanelAnimator.Hide(PanelIdiomas);
    }

    public void ActivarPaneCerrarSesion()
    {
        PanelAnimator.Show(panelCerrarSesion);
    }

    public void DesactivarPaneCerrarSesion()
    {
        PanelAnimator.Hide(panelCerrarSesion);
    }
}
