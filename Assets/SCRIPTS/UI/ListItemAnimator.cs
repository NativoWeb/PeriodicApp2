using UnityEngine;
using DG.Tweening;

public static class ListItemAnimator
{
    private const float Duration = 0.25f;
    private const float StaggerDelay = 0.05f;
    private const float SlideOffset = 30f;

    public static void AnimateIn(GameObject item, int index)
    {
        if (item == null) return;

        DOTween.Kill(item);

        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = item.AddComponent<CanvasGroup>();

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null) return;

        Vector2 targetPos = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(targetPos.x, targetPos.y - SlideOffset);
        canvasGroup.alpha = 0f;

        float delay = StaggerDelay * index;

        canvasGroup.DOFade(1f, Duration)
            .SetEase(Ease.OutCubic)
            .SetDelay(delay)
            .SetUpdate(true)
            .SetTarget(item);

        rect.DOAnchorPosY(targetPos.y, Duration)
            .SetEase(Ease.OutCubic)
            .SetDelay(delay)
            .SetUpdate(true)
            .SetTarget(item);
    }
}
