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
        // Marcar que se ganó la misión (similar a como se hace en GestorOraciones)
        PlayerPrefs.SetInt("UltimoQuizGanado", 1);
        PlayerPrefs.SetInt("xpGanado", 15); // XP por escanear elemento
        PlayerPrefs.SetInt("xp_mision", 15); // XP por escanear elemento
        PlayerPrefs.Save();

        PanelMisionCompletada.SetActive(true);
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                PanelMisionCompletada.SetActive(false);
                if (GuardarMisionCompletada.instancia != null)
                {
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada,
                        () => MostrarBotonContinuar()
                    );
                }
            });
        }
    }

    void MostrarBotonContinuar()
    {
        // Mostrar el panel con el botón continuar después de la animación
        if (PanelContinuar != null)
        {
            PanelContinuar.SetActive(true);
        }

        if (btnContinuarPanel != null)
        {
            btnContinuarPanel.onClick.RemoveAllListeners();
            btnContinuarPanel.onClick.AddListener(() =>
            {
                VolverAPantallaAnterior();
            });
        }
    }

    void VolverAPantallaAnterior()
    {
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        if (ruta == "Inicio")
        {
            SceneManager.LoadScene("Perfil_Usuario");
        }
        else if (ruta == "Misiones")
        {
            SceneManager.LoadScene("Categorías");
        }
        else if (ruta == "Profesor")
        {
            SceneManager.LoadScene("InicioProfesor");
        }
        else
        {
            // Por defecto, volver a Categorías
            SceneManager.LoadScene("Categorías");
        }
    }

}
