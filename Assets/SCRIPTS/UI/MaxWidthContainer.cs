using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MaxWidthContainer : MonoBehaviour
{
    public float maxWidth = 650f;

    private RectTransform _rect;
    private RectTransform _parentRect;
    private int _lastScreenWidth;
    private float _originalAnchorMinY;
    private float _originalAnchorMaxY;
    private float _originalOffsetMinY;
    private float _originalOffsetMaxY;
    private bool _initialized;

    private void OnEnable()
    {
        _rect = GetComponent<RectTransform>();
        if (transform.parent != null)
            _parentRect = transform.parent.GetComponent<RectTransform>();

        if (!_initialized)
        {
            _originalAnchorMinY = _rect.anchorMin.y;
            _originalAnchorMaxY = _rect.anchorMax.y;
            _originalOffsetMinY = _rect.offsetMin.y;
            _originalOffsetMaxY = _rect.offsetMax.y;
            _initialized = true;
        }

        _lastScreenWidth = Screen.width;
        Invoke(nameof(AjustarAncho), 0.05f);
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth)
        {
            _lastScreenWidth = Screen.width;
            AjustarAncho();
        }
    }

    private void AjustarAncho()
    {
        if (_rect == null) return;

        float parentWidth = 0f;
        if (_parentRect != null)
            parentWidth = _parentRect.rect.width;
        if (parentWidth <= 0f)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                parentWidth = canvas.GetComponent<RectTransform>().rect.width;
        }
        if (parentWidth <= 0f)
            parentWidth = Screen.width;

        if (parentWidth > maxWidth)
        {
            _rect.anchorMin = new Vector2(0.5f, _originalAnchorMinY);
            _rect.anchorMax = new Vector2(0.5f, _originalAnchorMaxY);
            _rect.sizeDelta = new Vector2(maxWidth, _rect.sizeDelta.y);
            _rect.anchoredPosition = new Vector2(0f, _rect.anchoredPosition.y);
            _rect.offsetMin = new Vector2(_rect.offsetMin.x, _originalOffsetMinY);
            _rect.offsetMax = new Vector2(_rect.offsetMax.x, _originalOffsetMaxY);
        }
        else
        {
            _rect.anchorMin = new Vector2(0f, _originalAnchorMinY);
            _rect.anchorMax = new Vector2(1f, _originalAnchorMaxY);
            _rect.offsetMin = new Vector2(0f, _originalOffsetMinY);
            _rect.offsetMax = new Vector2(0f, _originalOffsetMaxY);
        }
    }
}
