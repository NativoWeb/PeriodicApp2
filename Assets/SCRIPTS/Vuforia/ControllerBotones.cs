using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ControllerBotones : MonoBehaviour
{
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonUI;
    public Button botonCompletarMision;
    public Button Regresar;
    public GameObject PanelMisionCompletada;
    public Button botonContinuar;


    public GameObject PanelContinuar;

    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;
    public Button btnContinuarPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string ruta = PlayerPrefs.GetString("CargarVuforia", ""); // Obtiene la ruta almacenada

        botonContinuar.onClick.AddListener(CompletarMision);

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


    public void CompletarMision()
    {
        PanelMisionCompletada.SetActive(true);
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                PanelContinuar.SetActive(false);
                if (GuardarMisionCompletada.instancia != null)
                {
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada
                    );

                    btnContinuarPanel.onClick.AddListener(() =>
                    {
                        SceneManager.LoadScene("Categorías");
                    });
                }
            });
        }
    }

}
