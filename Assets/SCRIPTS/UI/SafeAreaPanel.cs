using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (Screen.safeArea != _lastSafeArea)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;

        Vector2 anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
        Vector2 anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}
