using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image imagen;
    private Vector3 posicionCorrecta;
    private Vector3 posicionInicial;
    private Transform panelPiezas;
    private Transform tablero;

    void Awake()
    {
        imagen = GetComponent<Image>();
    }

    public void ConfigurarPieza(Sprite sprite, Vector3 posicionCorrecta, Transform panelPiezas, Transform tablero)
    {
        imagen.sprite = sprite;
        this.posicionCorrecta = posicionCorrecta;
        this.panelPiezas = panelPiezas;
        this.tablero = tablero;
        this.posicionInicial = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        Debug.Log($"🔄 Arrastrando pieza: {name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float distancia = Vector2.Distance(transform.position, posicionCorrecta);
        if (distancia < 50f)
        {
            transform.position = posicionCorrecta;
            Debug.Log($"✅ Pieza '{name}' colocada correctamente.");
        }
        else
        {
            transform.position = posicionInicial;
            Debug.Log($"🔄 Pieza '{name}' devuelta a su posición inicial.");
        }
    }
}
