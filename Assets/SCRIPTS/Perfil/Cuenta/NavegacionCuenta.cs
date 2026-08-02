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
            if (btnDatos != null) btnDatos.gameObject.SetActive(false);
            Color customColor = new Color(80f / 255f, 178f / 255f, 125f / 255f, 1f);
            if (panelSuperior != null) panelSuperior.color = customColor;
        }
        else
        {
            if (btnDatos != null) btnDatos.gameObject.SetActive(true);
            Color customColor = new Color(59f / 255f, 53f / 255f, 139f / 255f, 1f);
            if (panelSuperior != null) panelSuperior.color = customColor;
        }

        if (PanelIdiomas != null)
        {
            if (btnEspanol == null || btnIngles == null)
            {
                var botones = PanelIdiomas.GetComponentsInChildren<Button>(true);
                foreach (var btn in botones)
                {
                    string nombre = btn.gameObject.name.ToLower();
                    if (nombre.Contains("espa")) btnEspanol = btn;
                    else if (nombre.Contains("ingl")) btnIngles = btn;
                }
            }

            PanelIdiomas.SetActive(false);
        }

        if (btnIdiomas != null) btnIdiomas.onClick.AddListener(cambiarIdioma);
        if (btnEspanol != null) btnEspanol.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(0));
        if (btnIngles != null) btnIngles.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(1));
    }

    public void verMenuCuenta()
    {
        panelMenuCuenta.SetActive(true);
        panelTerminos_Condiciones.SetActive(false);
        panelPoliticas.SetActive(false);
        panelDatosPersonales.SetActive(false);
        CerrarPanelIdiomas();
    }

    public void verTerminosCondiciones()
    {
        panelTerminos_Condiciones.SetActive(true);
        panelMenuCuenta.SetActive(false);
        panelPoliticas.SetActive(false);
        panelDatosPersonales.SetActive(false);
        CerrarPanelIdiomas();
    }

    public void verPoliticas()
    {
        panelPoliticas.SetActive(true);
        panelTerminos_Condiciones.SetActive(false);
        panelMenuCuenta.SetActive(false);
        panelDatosPersonales.SetActive(false);
        CerrarPanelIdiomas();
    }

    public void verDatosPersonales()
    {
        panelDatosPersonales.SetActive(true);
        panelPoliticas.SetActive(false);
        panelTerminos_Condiciones.SetActive(false);
        panelMenuCuenta.SetActive(false);
        CerrarPanelIdiomas();
    }

    public void cambiarIdioma()
    {
        if (PanelIdiomas != null)
            PanelIdiomas.SetActive(!PanelIdiomas.activeSelf);
    }

    public void CerrarPanelIdiomas()
    {
        if (PanelIdiomas != null)
            PanelIdiomas.SetActive(false);
    }

    private void CambiarIdiomaY_CerrarPanel(int id)
    {
        if (ControladorIdioma.instancia != null)
            ControladorIdioma.instancia.ChangeLocale(id);
        CerrarPanelIdiomas();
    }

    public void ActivarPaneCerrarSesion()
    {
        if (panelCerrarSesion != null)
            panelCerrarSesion.SetActive(true);
    }

    public void DesactivarPaneCerrarSesion()
    {
        if (panelCerrarSesion != null)
            panelCerrarSesion.SetActive(false);
    }
}
