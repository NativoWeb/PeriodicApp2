using UnityEngine;

public class RecargarSolicitudesManager : MonoBehaviour
{
    // instanciamos el script que recarga las solicitudes
    private SolicitudesAmistadManager Solicitudesmanager;
    void Start()
    {
        Solicitudesmanager = FindFirstObjectByType<SolicitudesAmistadManager>();
    }

    private void OnEnable()
    {

        Solicitudesmanager.LoadPendingRequests();
        
    }

}
