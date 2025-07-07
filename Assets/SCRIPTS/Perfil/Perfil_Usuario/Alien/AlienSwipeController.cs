using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AlienSwipeController : MonoBehaviour,
                                    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("IU — mismo orden que los aliens")]
    public RawImage[] rawImages;           // RawImages dentro del Canvas
    [Header("Modelos")]
    public AlienRotator[] alienRotators;   // Los scripts del §1
    [Header("Cámaras (opcional)")]
    public Camera[] alienCams;             // Desactiva la que no uses para ahorrar

    public Material defaultMaterial;   // material normal con textura
    private bool[] puedeUsar;          // guardamos la máscara

    private int indiceActual = 0;
    private Vector2 startTouch;
    public float distanciaMinimaSwipe = 50f;

    void Start() => ActualizarVista();

    /* ───────── Gestión de Swipe ───────── */
    public void OnBeginDrag(PointerEventData e) => startTouch = e.position;

    public void OnDrag(PointerEventData e) { /* no lo necesitamos */ }

    public void OnEndDrag(PointerEventData e)
    {
        float dx = e.position.x - startTouch.x;
        if (Mathf.Abs(dx) < distanciaMinimaSwipe) return;      // swipe muy corto

        indiceActual += dx < 0 ? +1 : -1;                      // izq→siguiente, der→anterior
        indiceActual = Mathf.Clamp(indiceActual, 0, rawImages.Length - 1);

        ActualizarVista();
    }

    /* ───────── Muestra/oculta y enciende/apaga lo necesario ───────── */
    void ActualizarVista()
    {
        for (int i = 0; i < rawImages.Length; i++)
        {
            bool esActivo = (i == indiceActual);

            rawImages[i].gameObject.SetActive(esActivo);           // Muestra solo su imagen
            alienRotators[i].enabled = true;                       // Por si estaba disabled
            if (esActivo) alienRotators[i].ComenzarRotacion();
            else alienRotators[i].DetenerRotacion();

            // Opcional: encender solo la cámara activa
            if (alienCams != null && alienCams.Length > i && alienCams[i] != null)
                alienCams[i].enabled = esActivo;
        }
    }
    public void SetUnlockMask(bool[] mask, Material lockedMat)
    {
        puedeUsar = mask;

        for (int i = 0; i < alienRotators.Length; i++)
        {
            Renderer r = alienRotators[i].GetComponentInChildren<Renderer>();

            // Cambia material según esté desbloqueado
            if (mask != null && i < mask.Length && !mask[i])
                r.material = lockedMat;        // bloqueado → gris
            else
                r.material = defaultMaterial;  // desbloqueado → normal
        }

        ActualizarVista();   // refresca lo que se muestra
    }
}
