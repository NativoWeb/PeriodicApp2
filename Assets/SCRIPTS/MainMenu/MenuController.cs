using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("RawImages")]
    public RawImage rawCamara;
    public RawImage rawJuegos;


    [Header("Paneles")]

    public GameObject PanelMainMenu;
    public GameObject PanelSeleccion;


    [Header("Escenas")]
    public string escenaCamara = "NombreDeTuEscenaAR";

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
}
