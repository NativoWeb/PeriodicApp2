using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public class ImageRecognition : MonoBehaviour
{
    private bool logroDesbloqueado = false;
    public string elementoQuimico; // Se asignará automáticamente
    private ImageTargetSpawner spawner; // Referencia al script ImageTargetSpawner

    void Start()
    {
        spawner = FindObjectOfType<ImageTargetSpawner>(); // Encuentra el spawner en la escena

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
        Debug.Log("Logro desbloqueado");
        spawner.botonCompletarMision.interactable = true;
    }
}
