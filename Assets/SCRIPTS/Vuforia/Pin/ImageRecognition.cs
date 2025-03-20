using UnityEngine;
using Vuforia;

public class ImageRecognition : MonoBehaviour
{
    private bool logroDesbloqueado = false;
    private ObserverBehaviour trackable;
    private ImageTargetSpawner spawner;

    void Start()
    {
        spawner = FindObjectOfType<ImageTargetSpawner>();

        string elemento = "pin_" + PlayerPrefs.GetString("ElementoSeleccionado", "").ToLower();

        trackable = GetComponent<ObserverBehaviour>();

        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
        }

        // Si este ImageTarget no es el elemento de la misión, se desactiva
        if (trackable.TargetName != elemento)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED)
        {
            Debug.Log($"¡Imagen detectada! {trackable.TargetName} desbloqueado.");
            logroDesbloqueado = true;
            DesbloquearLogro(trackable.TargetName);
        }
    }

    void DesbloquearLogro(string elemento)
    {
        Debug.Log($"🏆 Logro desbloqueado: {elemento}");
        spawner.botonCompletarMision.interactable = true;
    }
}
