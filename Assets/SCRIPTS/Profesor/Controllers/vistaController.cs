using UnityEngine;

public class vistaController : MonoBehaviour
{
    [SerializeField] private GameObject InicioPanel= null;
    [SerializeField] private GameObject CrearEncuestaPanel = null;


    // funcion ver login 
    public void Inicio()
    {
        InicioPanel.SetActive(true);
        CrearEncuestaPanel.SetActive(false);
    }
    //funcion ver registro
    public void CrearEncuesta()
    {
        CrearEncuestaPanel.SetActive(true);
        InicioPanel.SetActive(false);

    }
}
