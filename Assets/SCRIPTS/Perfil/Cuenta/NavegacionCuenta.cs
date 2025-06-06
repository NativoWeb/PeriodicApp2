using UnityEngine;

public class NavegacionCuenta : MonoBehaviour
{
    // instanciamos los 3 paneles para poder navegar 

    [Header("paneles de navegaci�n")]
    [SerializeField] public GameObject panelMenuCuenta;
    [SerializeField] public GameObject panelTerminos_Condiciones;
    [SerializeField] public GameObject panelPoliticas;
    [SerializeField] public GameObject panelDatosPersonales;
    
    

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


}