using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("ReferenciasUI")]
    public Button Volver;
    public Button btnCamara;
    public Button btnSeleccion;

    [Header("Paneles")]
    public GameObject PanelMainMenu;
    public GameObject PanelSeleccion;

    [Header("Escenas")]
    public string escenaCamara = "VuforiaNuevo";

    [Header("Panel de Error")]
    public GameObject PanelSinInternet;

    private void Start()
    {
        PanelMainMenu.SetActive(true);
        PanelSeleccion.SetActive(false);
        btnCamara.onClick.RemoveAllListeners();
        btnSeleccion.onClick.RemoveAllListeners();
        btnCamara.onClick.AddListener(SeleccionarCamaraAR);
        btnSeleccion.onClick.AddListener(SeleccionarJuegos);
    }

    private void SeleccionarCamaraAR()
    {
        StartCoroutine(ActivarRawYIrAEscena(escenaCamara));
    }
    private void SeleccionarJuegos()
    {
        StartCoroutine(esperar());
    }

    private IEnumerator esperar()
    {
        yield return new WaitForSeconds(.5f);
        PanelMainMenu.SetActive(false);
        Volver.onClick.AddListener(cerrarPanel);
        PanelSeleccion.SetActive(true);
    }
    private IEnumerator ActivarRawYIrAEscena( string escena)
    { 

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(escena);
    }

    void cerrarPanel()
    {
        PanelMainMenu.SetActive(true);
        PanelSeleccion.SetActive(false);
    }
}
