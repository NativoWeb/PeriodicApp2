using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Linq;
public class ResultadosPanel : MonoBehaviour
{

    [Header("Referencias para Animaci�n de Misi�n")]
    public TMP_Text txtMision;
    public TMP_Text txtXp;
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;
    public Button btnContinuarPanel;

    public GameObject PanelBoton;

    [Header("Referencias de UI - Botones")]
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonCompletarUI;


    private string appIdioma; // Variable para el idioma

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        appIdioma = PlayerPrefs.GetString("appIdioma", "español"); // Default a español
        Debug.Log($"🌐 [ResultadosPanel] Idioma configurado: {appIdioma}");

        // Asegurar que los paneles estén correctamente configurados al inicio
        if (panelAnimacionMision != null) panelAnimacionMision.SetActive(false);
        if (PanelBoton != null) PanelBoton.SetActive(false);
    }

    public void mostrarResultados()
    {
        panelAnimacionMision.SetActive(true);
        MostrarResultadosFinales();
    }

    void MostrarResultadosFinales()
    {
        Debug.Log("🎯 [ResultadosPanel] Mostrando resultados finales de misión QR");

        PlayerPrefs.SetInt("UltimoQuizGanado", 1);

        int xpGanado = 20;

        txtMision.text = (appIdioma == "ingles") ? "MISSION COMPLETED!" : "¡MISIÓN COMPLETADA!";
        txtXp.text = $"+{xpGanado} XP";

        PlayerPrefs.SetInt("xp_mision", xpGanado);
        PlayerPrefs.Save();

        Button botonContinuar = PanelBoton.GetComponentInChildren<Button>();
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                Debug.Log("🔘 [ResultadosPanel] Click en botón continuar");
                PanelBoton.SetActive(false);

                if (GuardarMisionCompletada.instancia != null)
                {
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada,
                        () => MostrarPanelRegreso() // Callback después de la animación
                    );
                }
                else
                {
                    Debug.LogError("❌ [ResultadosPanel] GuardarMisionCompletada.instancia es null");
                    MostrarPanelRegreso();
                }
            });
        }

        if (SistemaXP.Instance != null)
        {
            SistemaXP.Instance.AgregarXP(xpGanado);
        }
    }

    // Método para mostrar el panel de regreso después de completar la misión
    private void MostrarPanelRegreso()
    {
        Debug.Log("🔙 [ResultadosPanel] Mostrando panel de regreso");

        // Ocultar todos los paneles de resultados/animación
        if (panelAnimacionMision != null) panelAnimacionMision.SetActive(false);
        if (PanelBoton != null) PanelBoton.SetActive(false);

        // Mostrar el panel de regresar (botón atrás)
        if (PanelRegresarUI != null)
        {
            PanelRegresarUI.SetActive(true);
            Debug.Log("✅ [ResultadosPanel] Panel de regresar activado");
        }
        else
        {
            Debug.LogWarning("⚠️ [ResultadosPanel] PanelRegresarUI no está asignado. Asígnalo en el Inspector.");
        }

        // Ocultar el botón de completar misión
        if (PanelBotonCompletarUI != null)
        {
            PanelBotonCompletarUI.SetActive(false);
        }
    }
}
