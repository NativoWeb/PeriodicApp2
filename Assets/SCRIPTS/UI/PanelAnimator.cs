using System;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class PanelAnimator : MonoBehaviour
{
    private const float ShowDuration = 0.2f;
    private const float HideDuration = 0.15f;
    private const Ease ShowEase = Ease.OutCubic;
    private const Ease HideEase = Ease.InCubic;
    private const float ScaleFrom = 0.95f;
    private const float ScaleTo = 1.0f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    public void AnimateShow(Action onComplete = null)
    {
        EnsureComponents();
        KillTweens();

        gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = Vector3.one * ScaleFrom;

        Sequence seq = DOTween.Sequence();
        seq.Join(_canvasGroup.DOFade(1f, ShowDuration).SetEase(ShowEase));
        seq.Join(_rectTransform.DOScale(ScaleTo, ShowDuration).SetEase(ShowEase));
        seq.SetUpdate(true);
        seq.SetTarget(gameObject);
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void AnimateHide(Action onComplete = null)
    {
        EnsureComponents();

        if (!gameObject.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        KillTweens();

        Sequence seq = DOTween.Sequence();
        seq.Join(_canvasGroup.DOFade(0f, HideDuration).SetEase(HideEase));
        seq.Join(_rectTransform.DOScale(ScaleFrom, HideDuration).SetEase(HideEase));
        seq.SetUpdate(true);
        seq.SetTarget(gameObject);
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            _rectTransform.localScale = Vector3.one;
            onComplete?.Invoke();
        });
    }

    private void EnsureComponents()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
    }

    private void KillTweens()
    {
        DOTween.Kill(gameObject);
    }

    public static void Show(GameObject panel, Action onComplete = null)
    {
        if (panel == null) return;
        PrepareAnimator(panel).AnimateShow(onComplete);
    }

    public static void Show(GameObject panel)
    {
        Show(panel, null);
    }

    public static void Hide(GameObject panel, Action onComplete = null)
    {
        if (panel == null) return;

        if (!panel.activeSelf)
        {
            onComplete?.Invoke();
            return;
        }

        PrepareAnimator(panel).AnimateHide(onComplete);
    }

    public static void Hide(GameObject panel)
    {
        Hide(panel, null);
    }

    private static PanelAnimator PrepareAnimator(GameObject panel)
    {
        if (panel.GetComponent<CanvasGroup>() == null)
            panel.AddComponent<CanvasGroup>();

        PanelAnimator animator = panel.GetComponent<PanelAnimator>();
        if (animator == null)
            animator = panel.AddComponent<PanelAnimator>();

        return animator;
    }
}
