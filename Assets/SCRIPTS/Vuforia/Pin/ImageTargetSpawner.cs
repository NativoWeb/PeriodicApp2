using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class ImageTargetSpawner : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el botón desde el Inspector
    public ControllerBotones controladorBotones; // Referencia al controlador de botones

    [Header("Referencias de UI - ScannerPin")]
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonCompletarUI;

    void Start()
    {
        Debug.Log("🚀 [ImageTargetSpawner] Inicializando ScannerPin");

        // Configurar el botón de completar misión como no interactuable al inicio
        if (botonCompletarMision != null)
        {
            botonCompletarMision.interactable = false;
        }

        // Buscar el ControllerBotones si no está asignado
        if (controladorBotones == null)
        {
            controladorBotones = FindObjectOfType<ControllerBotones>();
        }

        // Configurar UI inicial - Mostrar botón de regresar, ocultar botón de completar misión
        ConfigurarUIInicial();
    }

    void ConfigurarUIInicial()
    {
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");
        Debug.Log($"📍 [ImageTargetSpawner] Ruta de carga: '{ruta}'");

        // Mostrar el panel de regresar
        if (PanelRegresarUI != null)
        {
            PanelRegresarUI.SetActive(true);
            Debug.Log("✅ [ImageTargetSpawner] Panel de regresar activado");
        }
        else
        {
            Debug.LogWarning("⚠️ [ImageTargetSpawner] PanelRegresarUI no está asignado. Asígnalo en el Inspector.");
        }

        // Ocultar el panel de botón de completar misión (se mostrará cuando se escanee el QR)
        if (PanelBotonCompletarUI != null)
        {
            PanelBotonCompletarUI.SetActive(false);
            Debug.Log("✅ [ImageTargetSpawner] Panel de completar misión oculto");
        }
    }
}
