using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private const float PressScale = 0.93f;
    private const float PressDuration = 0.1f;
    private const float ReleaseDuration = 0.1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        DOTween.Kill(gameObject);
        transform.DOScale(PressScale, PressDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(gameObject);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        DOTween.Kill(gameObject);
        transform.DOScale(1f, ReleaseDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetTarget(gameObject);
    }

    private void OnDisable()
    {
        DOTween.Kill(gameObject);
        transform.localScale = Vector3.one;
    }
}
