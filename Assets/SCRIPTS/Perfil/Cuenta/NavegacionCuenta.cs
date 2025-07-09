using UnityEngine;
using UnityEngine.UI;

public class NavegacionCuenta : MonoBehaviour
{
    // instanciamos los 3 paneles para poder navegar 

    [Header("paneles de navegación")]
    [SerializeField] public GameObject panelMenuCuenta;
    [SerializeField] public GameObject panelTerminos_Condiciones;
    [SerializeField] public GameObject panelPoliticas;
    [SerializeField] public GameObject panelDatosPersonales;
    [SerializeField] public GameObject panelCerrarSesion;
    [SerializeField] public GameObject PanelIdiomas;
    public Button btnIdiomas;
    public Button btnEspañol;
    public Button btnIngles;

    private void Start()
    {
        btnIdiomas.onClick.AddListener(cambiarIdioma);
        btnEspañol.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(0));
        btnIngles.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(1));
    }

    public void verMenuCuenta()
    {
        panelMenuCuenta.SetActive(true);
        panelTerminos_Condiciones.SetActive(false);
        panelPoliticas.SetActive(false);
        panelDatosPersonales.SetActive(false);
    }
    public void verTerminosCondiciones()
    {
        panelTerminos_Condiciones.SetActive(true);
        panelMenuCuenta.SetActive(false);
        panelPoliticas.SetActive(false);
        panelDatosPersonales.SetActive(false);
    }
   public void verPoliticas()
    {
        panelPoliticas.SetActive(true);
        panelTerminos_Condiciones.SetActive(false);
        panelMenuCuenta.SetActive(false);
        panelDatosPersonales.SetActive(false);
    }
    public void verDatosPersonales()
    {
        panelDatosPersonales.SetActive(true);
        panelPoliticas.SetActive(false);
        panelTerminos_Condiciones.SetActive(false);
        panelMenuCuenta.SetActive(false);
    }
    public void cambiarIdioma()
    {
        PanelIdiomas.SetActive(true);
        panelDatosPersonales.SetActive(false);
        panelPoliticas.SetActive(false);
        panelTerminos_Condiciones.SetActive(false);
    }
    private void CambiarIdiomaY_CerrarPanel(int id)
    {
        // Llama a la instancia del controlador de idioma
        if (ControladorIdioma.instancia != null)
        {
            ControladorIdioma.instancia.ChangeLocale(id);
        }
        // Cierra el panel
        PanelIdiomas.SetActive(false);
    }
    public void ActivarPaneCerrarSesion()
    {
        panelCerrarSesion.SetActive(true);
    }
    public void DesactivarPaneCerrarSesion()
    {
        if (panelCerrarSesion != null )
        panelCerrarSesion.SetActive(false);

    }


}