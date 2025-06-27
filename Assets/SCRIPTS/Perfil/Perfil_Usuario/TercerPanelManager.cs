using UnityEngine;

public class TercerPanelManager : MonoBehaviour
{
    public static TercerPanelManager instancia;

    public RectTransform panelInferior;
    private Vector2 posicionBase;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            posicionBase = panelInferior.anchoredPosition;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetearPosicion()
    {
        if (panelInferior != null)
        {
            panelInferior.anchoredPosition = posicionBase;
        }
    }

    public Vector2 GetPosicionBase()
    {
        return posicionBase;
    }
}
