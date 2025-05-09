using UnityEngine;

public class SolicitudItemController : MonoBehaviour
{
    private ComunidadDetalleManager manager;
    private GameObject itemInstance;
    private string comunidadId;
    private string solicitudId;

    public void Initialize(ComunidadDetalleManager manager, GameObject itemInstance, string comunidadId, string solicitudId)
    {
        this.manager = manager;
        this.itemInstance = itemInstance;
        this.comunidadId = comunidadId;
        this.solicitudId = solicitudId;
    }

    public void EliminarItem()
    {
        if (itemInstance != null)
        {
            Destroy(itemInstance);
        }
    }
}
