using UnityEngine;
using DG.Tweening;

public class GlowPulseAnimation : MonoBehaviour
{
    public float minSize = 0.25f;
    public float maxSize = 0.45f;
    public float pulseSpeed = 0.5f;

    private Renderer _renderer;
    private Material _material;
    private float _emissionIntensity;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _material = _renderer.material;

        float range = maxSize - minSize;
        float pulseDuration = range / pulseSpeed;

        transform.localScale = Vector3.one * minSize;
        transform.DOScale(maxSize, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetTarget(transform);

        if (_material != null)
        {
            _emissionIntensity = 1.0f;
            DOTween.To(
                    () => _emissionIntensity,
                    v =>
                    {
                        _emissionIntensity = v;
                        Color baseColor = _material.GetColor("_Color");
                        _material.SetColor("_EmissionColor", baseColor * v);
                    },
                    1.5f,
                    pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetTarget(this);
        }
    }

    private void OnDestroy()
    {
        DOTween.Kill(transform);
        DOTween.Kill(this);
    }
}