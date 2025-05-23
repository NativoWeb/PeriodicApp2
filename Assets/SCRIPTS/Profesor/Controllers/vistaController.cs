using UnityEngine;

public class vistaController : MonoBehaviour
{
    [SerializeField] private GameObject InicioPanel = null;
    [SerializeField] private GameObject CrearEncuestaPanel = null;
    [SerializeField] private GameObject IniciarEncuestaPanel = null;
    [SerializeField] private GameObject RankingPanel = null;
    [SerializeField] private GameObject Perfil = null;


    private EncuestaManager encuestaManager; // Referencia al EncuestaManager

    void Start()
    {
        encuestaManager = FindFirstObjectByType<EncuestaManager>(); // Buscar automáticamente el script en la escena
    }

    // Mostrar el panel de inicio
    public void Inicio()
    {
        InicioPanel.SetActive(true);
        CrearEncuestaPanel.SetActive(false);
        IniciarEncuestaPanel.SetActive(false);
        RankingPanel.SetActive(false);
        encuestaManager.cerrarmodoedicion();
    }

    // Mostrar el panel de creación de encuestas y limpiar los campos
    public void CrearEncuesta()
    {
        CrearEncuestaPanel.SetActive(true);
        InicioPanel.SetActive(false);

        if (encuestaManager != null)
        {
            encuestaManager.LimpiarCampos(); // Llamar la función cuando se abra la vista
        }
        else
        {
            Debug.LogError("❌ No se encontró el EncuestaManager en la escena.");
        }
    }

    // Nuevo método específico para edición
    public void CambiarAVistaEdicion()
    {
        CrearEncuestaPanel.SetActive(true);
        InicioPanel.SetActive(false);
        
    }

    public void IniciarEncuesta()
    {
        IniciarEncuestaPanel.SetActive(true);
    }

    public void Puntuaciones()
    {
        RankingPanel.SetActive(true);

        InicioPanel.SetActive(false);
        CrearEncuestaPanel.SetActive(false);
        IniciarEncuestaPanel.SetActive(false);
    }

    public void MostrarPerfil()
    {
        Perfil.SetActive(true);
        
        RankingPanel.SetActive(false);
        InicioPanel.SetActive(false);
        CrearEncuestaPanel.SetActive(false);
        IniciarEncuestaPanel.SetActive(false);
    }
}
