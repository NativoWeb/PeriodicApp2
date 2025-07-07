using UnityEngine;

public class vistaController : MonoBehaviour
{
    [SerializeField] private GameObject InicioPanel = null;
    [SerializeField] private GameObject CrearEncuestaPanel = null;
    [SerializeField] private GameObject IniciarEncuestaPanel = null;
    [SerializeField] private ListarEncuestas listarEncuestas;

    private EncuestasManager encuestasManager; // Referencia al EncuestaManager

    void Start()
    {
        encuestasManager = FindFirstObjectByType<EncuestasManager>();
        if (listarEncuestas == null)
            listarEncuestas = FindFirstObjectByType<ListarEncuestas>();

        listarEncuestas?.CargarEncuestas(); // Ejecutar ListarEncuestas al iniciar
    }


    // Mostrar el panel de inicio
    public void Inicio()
    {
        listarEncuestas?.CargarEncuestas();
        InicioPanel.SetActive(true);
        CrearEncuestaPanel.SetActive(false);
        IniciarEncuestaPanel.SetActive(false);
    }

    // Nuevo método específico para edición
    public void CambiarAVistaEdicion()
    {
        CrearEncuestaPanel.SetActive(true);
        InicioPanel.SetActive(false);

    }
}
