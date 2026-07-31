using UnityEngine;
using UnityEngine.UI;

public class ResponsiveMissionLayout : MonoBehaviour
{
    public float phoneColumns = 1;
    public float tabletColumns = 2;
    public float cardAspectRatio = 2.4f;

    private GridLayoutGroup _gridLayout;
    private RectTransform _rectTransform;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();

        var vertical = GetComponent<VerticalLayoutGroup>();
        if (vertical != null) DestroyImmediate(vertical);

        var horizontal = GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) DestroyImmediate(horizontal);

        _gridLayout = GetComponent<GridLayoutGroup>();
        if (_gridLayout == null)
            _gridLayout = gameObject.AddComponent<GridLayoutGroup>();

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        Invoke(nameof(RecalcularLayout), 0.05f);
    }

    private void Update()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            RecalcularLayout();
        }
    }

    private void RecalcularLayout()
    {
        if (_rectTransform == null) return;

        float containerWidth = _rectTransform.rect.width;
        if (containerWidth <= 0)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var canvasRect = canvas.GetComponent<RectTransform>();
                containerWidth = canvasRect.rect.width;
            }
            else
            {
                containerWidth = Screen.width;
            }
        }

        bool esTablet = (float)Screen.width / Screen.height < 1.5f || Screen.width >= 1200;
        int columnas = esTablet ? (int)tabletColumns : (int)phoneColumns;
        float spacing = esTablet ? 30f : 20f;
        float padding = esTablet ? 40f : 20f;

        float cellWidth = (containerWidth - padding * 2 - spacing * (columnas - 1)) / columnas;
        if (cellWidth < 100f) cellWidth = containerWidth - padding * 2;
        float cellHeight = cellWidth / cardAspectRatio;

        _gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        _gridLayout.spacing = new Vector2(spacing, spacing);
        _gridLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _gridLayout.constraintCount = columnas;
        _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _gridLayout.childAlignment = TextAnchor.UpperCenter;
    }
}
