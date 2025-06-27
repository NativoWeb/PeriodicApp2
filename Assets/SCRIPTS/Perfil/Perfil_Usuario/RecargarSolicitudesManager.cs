using UnityEngine;

public class RecargarSolicitudesManager : MonoBehaviour
{
    [SerializeField] private SolicitudesAmistadManager Solicitudesmanager;

    private void OnEnable()
    {
        if (Solicitudesmanager != null)
        {
            Solicitudesmanager = FindFirstObjectByType<SolicitudesAmistadManager>();
        }

        if (Solicitudesmanager != null)
        {
            Debug.Log("instancia correctaaaa SolicitudesAmistadManager");

            Solicitudesmanager.LoadPendingRequests();
        }
        else
        {
            Debug.LogWarning("No se encontró SolicitudesAmistadManager al activar el panel.");
        }
    }
}
