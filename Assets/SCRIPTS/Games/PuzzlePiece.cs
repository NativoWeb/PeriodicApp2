using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int numeroAtomico; // Número atómico de la pieza

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 posicionInicial;
    private Transform parentInicial;

    private PuzzleManager puzzleManager;


    private void Awake()
    {
        puzzleManager = Object.FindFirstObjectByType<PuzzleManager>(); // Asegura encontrar el PuzzleManager en la escena
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        posicionInicial = rectTransform.anchoredPosition; // Guarda la posición inicial por si hay que devolverla
        parentInicial = transform.parent; // Guarda el parent inicial

        canvasGroup.blocksRaycasts = false; // Permite que otras piezas reciban eventos de arrastre
        transform.SetParent(transform.root); // Evita conflictos con GridLayoutGroup si hay alguno activo
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta; // Mueve la pieza mientras se arrastra
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true; // Reactiva la detección de eventos de raycast

        GameObject objetoSobre = eventData.pointerCurrentRaycast.gameObject;

        if (objetoSobre != null && objetoSobre.CompareTag("Slot")) // Si se suelta sobre un espacio válido
        {
            transform.SetParent(objetoSobre.transform);
            transform.localPosition = Vector3.zero; // Asegura alineación con el slot
        }
        else
        {
            rectTransform.anchoredPosition = posicionInicial; // Devuelve la pieza a su posición original
            transform.SetParent(parentInicial); // Vuelve al parent original si no se colocó en un slot válido
        }

        puzzleManager.VerificarOrden(); // Llama a la verificación después de cada movimiento
    }
}
