using UnityEngine;
using Vuforia;

public class ImageRecognition : MonoBehaviour
{
    private bool logroDesbloqueado = false;
    public string elementoQuimico; // Se asignará automáticamente

    void Start()
    {
        var trackable = GetComponent<ObserverBehaviour>();
        if (trackable)
        {
            trackable.OnTargetStatusChanged += OnImageDetected;
            elementoQuimico = trackable.TargetName; // Asigna el nombre de la imagen detectada
        }
    }

    private void OnImageDetected(ObserverBehaviour observer, TargetStatus status)
    {
        if (status.Status == Status.TRACKED && !logroDesbloqueado)
        {
            Debug.Log($"¡Imagen detectada! {elementoQuimico} desbloqueado.");
            logroDesbloqueado = true;
            DesbloquearLogro(elementoQuimico);
        }
    }

    void DesbloquearLogro(string elemento)
    {
        Debug.Log($"🏆 Logro desbloqueado: {elemento}");
        // Aquí puedes guardar el progreso del jugador
    }
}
