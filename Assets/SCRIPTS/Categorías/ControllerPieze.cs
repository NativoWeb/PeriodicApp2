using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControllerPieze : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ControllerPuzzle puzzleManager;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 posicionInicial;
    private Transform panelPiezasDisponibles;

    public int indiceCorrecto;
    public int indiceActual;
    public Image imagen;

    public void Configurar(ControllerPuzzle puzzle, int indiceCorrecto, Sprite sprite, Transform panelPiezas)
    {
        puzzleManager = puzzle;
        this.indiceCorrecto = indiceCorrecto;
        this.indiceActual = -1; // No está en el grid aún
        panelPiezasDisponibles = panelPiezas;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        imagen = GetComponent<Image>();
        if (imagen != null && sprite != null)
        {
            imagen.sprite = sprite;
        }

        posicionInicial = rectTransform.position; // Guarda la posición original
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        posicionInicial = rectTransform.position;
        transform.SetAsLastSibling(); // Asegurar que siempre quede encima al moverla
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        Transform celdaDestino = puzzleManager.ObtenerCeldaBajoCursor(eventData);

        if (celdaDestino != null)
        {
            ControllerPieze otraPieza = celdaDestino.GetComponentInChildren<ControllerPieze>();

            if (otraPieza != null && otraPieza != this)
            {
                // 🔹 Intercambiar las piezas si hay otra en la celda
                IntercambiarCon(otraPieza);
            }
            else
            {
                // 🔹 Si la celda está vacía, mover la pieza allí
                transform.SetParent(celdaDestino, false);
                transform.SetSiblingIndex(celdaDestino.GetSiblingIndex());
                rectTransform.position = celdaDestino.position;

                // 🔹 Ajustar tamaño en el GridLayoutGroup
                AjustarPieza(this);

                // 🔹 Actualizar índice actual
                puzzleManager.ActualizarIndices();
            }

            puzzleManager.VerificarOrden();
        }
        else
        {
            // 🔹 Si no está en una celda válida, vuelve a su posición original
            rectTransform.position = posicionInicial;
        }
    }



    private void AjustarPieza(ControllerPieze pieza)
    {
        RectTransform rt = pieza.GetComponent<RectTransform>();

        // Ajustar tamaño y anclaje para que se adapte correctamente dentro del GridLayoutGroup
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.localScale = Vector3.one;
    }
    private void IntercambiarCon(ControllerPieze otraPieza)
    {
        Transform tempParent = this.transform.parent;
        Transform otraParent = otraPieza.transform.parent;

        // 🔹 Intercambiar los padres de las piezas
        this.transform.SetParent(otraParent, false);
        otraPieza.transform.SetParent(tempParent, false);

        // 🔹 Intercambiar posiciones en el GridLayoutGroup
        int tempSiblingIndex = this.transform.GetSiblingIndex();
        this.transform.SetSiblingIndex(otraPieza.transform.GetSiblingIndex());
        otraPieza.transform.SetSiblingIndex(tempSiblingIndex);

        // 🔹 Ajustar tamaño en el GridLayoutGroup
        AjustarPieza(this);
        AjustarPieza(otraPieza);

        // 🔹 Actualizar índices después del intercambio
        puzzleManager.ActualizarIndices();

        // 🔹 Verificar si el puzzle está completo
        puzzleManager.VerificarOrden();
    }




}
