using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("ReferenciasUI")]
    public Button Volver;

    [Header("RawImages")]
    public RawImage rawCamara;
    public RawImage rawJuegos;

    [Header("Paneles")]
    public GameObject PanelMainMenu;
    public GameObject PanelSeleccion;

    [Header("Escenas")]
    public string escenaCamara = "VuforiaNuevo";

    [Header("Panel de Error")]
    public GameObject PanelSinInternet;

    void Start()
    {
        StartCoroutine(VerificarConexionPeriodicamente());
    }
    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return VerificarConexionReal();
            yield return new WaitForSeconds(5f);
        }
    }
    private IEnumerator VerificarConexionReal()
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.timeout = 3;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            PanelSinInternet.SetActive(true);
        }
        else
        {
            PanelSinInternet.SetActive(false);
        }
    }
    public void SeleccionarCamaraAR()
    {
        StartCoroutine(ActivarRawYIrAEscena(rawCamara, escenaCamara));
    }
    public void SeleccionarJuegos()
    {
        rawJuegos.gameObject.SetActive(true);
        StartCoroutine(esperar());
    }

    private IEnumerator esperar()
    {
        yield return new WaitForSeconds(.5f);
        PanelMainMenu.SetActive(false);
        Volver.onClick.AddListener(cerrarPanel);
        PanelSeleccion.SetActive(true);
    }

    private IEnumerator ActivarRawYIrAEscena(RawImage raw, string escena)
    {
        if (raw != null)
        {
            raw.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(escena);
    }

    void cerrarPanel()
    {
        PanelMainMenu.SetActive(true);
        PanelSeleccion.SetActive(false);
    }
}
