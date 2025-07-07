using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeEventForwarder : MonoBehaviour,
                                   IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public AlienSwipeController receptor;

    public void OnBeginDrag(PointerEventData e) => receptor.OnBeginDrag(e);
    public void OnDrag(PointerEventData e) => receptor.OnDrag(e);
    public void OnEndDrag(PointerEventData e) => receptor.OnEndDrag(e);
}
