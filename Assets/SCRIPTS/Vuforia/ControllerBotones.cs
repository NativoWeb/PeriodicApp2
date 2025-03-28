using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


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
        else if (ruta == "Profesor")
        {
            PanelRegresarUI.SetActive(true);
            Regresar.onClick.AddListener(CargarVuforiaProfesor);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
    }


    void CargarVuforiaProfesor()
    {
        SceneManager.LoadScene("InicioProfesor");
    }


}
