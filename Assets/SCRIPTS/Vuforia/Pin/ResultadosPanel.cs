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

    [Header("Referencias para Animación de Misión")]
    public TMP_Text txtMision;
    public TMP_Text txtXp;
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;
    public Button btnContinuarPanel;

    public GameObject PanelBoton;


    private string appIdioma; // Variable para el idioma

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        appIdioma = PlayerPrefs.GetString("appIdioma", "español"); // Default a español
    }

    public void mostrarResultados()
    {
        panelAnimacionMision.SetActive(true);
        MostrarResultadosFinales();
    }

    void MostrarResultadosFinales()
    {

        PlayerPrefs.SetInt("UltimoQuizGanado", 1);

        int xpGanado = 20;

        txtMision.text = (appIdioma == "ingles") ? "QUIZ PASSED!" : "¡QUIZ SUPERADO!";
        txtXp.text = $"+{xpGanado} XP";

        PlayerPrefs.SetInt("xp_mision", xpGanado);
        PlayerPrefs.Save();

        Button botonContinuar = PanelBoton.GetComponentInChildren<Button>();
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveAllListeners();
            botonContinuar.onClick.AddListener(() =>
            {
                PanelBoton.SetActive(false);
                if (GuardarMisionCompletada.instancia != null)
                {
                    GuardarMisionCompletada.instancia.IniciarProcesoMisionCompletada(
                        panelAnimacionMision,
                        imagenAnimacionMision,
                        audioMisionCompletada
                    );

                    btnContinuarPanel.onClick.AddListener(() => {
                        SceneManager.LoadScene("Categorías");
                    });
                }
            });
        }

        if (SistemaXP.Instance != null)
        {
            SistemaXP.Instance.AgregarXP(xpGanado);
        }
    }
}
