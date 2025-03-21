using System.Xml.Linq;
using UnityEngine;
using Vuforia;

public class ScanearElemento : MonoBehaviour
{
    private ObserverBehaviour trackable;

    private ControllerBotones ControladoBotones;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ControladoBotones = FindAnyObjectByType<ControllerBotones>();

        string elemento = PlayerPrefs.GetString("NumeroAtomico", "").Trim() + "_" + PlayerPrefs.GetString("ElementoSeleccionado", "").Trim();
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (ruta == "Misiones")
        {
            if (trackable.TargetName.Trim().ToLower() != elemento.Trim().ToLower())
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
        ControladoBotones.botonCompletarMision.interactable = true;
    }
}
