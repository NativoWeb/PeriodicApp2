using UnityEngine;
using UnityEngine.UI;


public class ControllerBotones : MonoBehaviour
{
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonUI;
    public Button botonCompletarMision;
    public Button Regresar;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string ruta = PlayerPrefs.GetString("CargarVuforia", ""); // Obtiene la ruta almacenada

        if (ruta == "Inicio")
        {
            PanelRegresarUI.SetActive(true);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
        else if (ruta == "Misiones")
        {
            PanelRegresarUI.SetActive(true);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
    }
}
