using UnityEngine;

public class SafeAreaManager : MonoBehaviour
{
    private RectTransform panelSafeArea;

    void Start()
    {
        panelSafeArea = GetComponent<RectTransform>();
        AjustarSafeArea();
    }

    void AjustarSafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2 minAnchor = safeArea.position;
        Vector2 maxAnchor = safeArea.position + safeArea.size;

        minAnchor.x /= Screen.width;
        minAnchor.y /= Screen.height;
        maxAnchor.x /= Screen.width;
        maxAnchor.y /= Screen.height;

        panelSafeArea.anchorMin = minAnchor;
        panelSafeArea.anchorMax = maxAnchor;
    }
}
