using System.Xml.Linq;
using UnityEngine;
using Vuforia;

public class ScanearElemento : MonoBehaviour
{
    private ObserverBehaviour trackable;

    private ControllerBotones ControladorBotones;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ControladorBotones = FindAnyObjectByType<ControllerBotones>();

        string numeroAtomico = PlayerPrefs.GetString("NumeroAtomico", "").Trim();
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva.
        // Se compara solo por número atómico (prefijo "21_") para evitar
        // discrepancias entre el nombre en el JSON y el nombre del target Vuforia.
        if (ruta == "Misiones")
        {
            if (string.IsNullOrEmpty(numeroAtomico) ||
                !trackable.TargetName.Trim().ToLower().StartsWith(numeroAtomico + "_"))
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED)
        {
            Debug.Log($"¡Imagen detectada! {trackable.TargetName} desbloqueado.");
            DesbloquearLogro(trackable.TargetName);
        }
    }


    void DesbloquearLogro(string elemento)
    {
        Debug.Log($"🏆 Logro desbloqueado: {elemento}");
        ControladorBotones.PanelBotonUI.SetActive(true);
        ControladorBotones.botonCompletarMision.interactable = true;
    }
}
