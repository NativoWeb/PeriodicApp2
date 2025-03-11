using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ControllerPieze : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private Vector3 posicionInicial;
    private Transform parentInicial;
    private CanvasGroup canvasGroup;
    private ControllerPuzzle puzzleManager;
    private ControllerPieze piezaIntercambio;

    public int indiceCorrecto;
    public int indiceActual;
    public Image imagen; // Referencia a la imagen de la pieza

    public void Configurar(ControllerPuzzle puzzle, int indice, Sprite sprite)
    {
        puzzleManager = puzzle;
        indiceCorrecto = indice;
        indiceActual = indice;  // Inicialmente la posición actual es la misma que la correcta

        posicionInicial = transform.position;
        parentInicial = transform.parent;

        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // Asegurar que la pieza tiene una imagen y no está en blanco
        imagen = GetComponent<Image>();
        if (imagen != null && sprite != null)
        {
            imagen.sprite = sprite;
        }
        else
        {
            Debug.LogError($"[ERROR] La pieza {gameObject.name} no tiene un componente Image o el sprite es nulo.");
        }

        Debug.Log($"[START] Pieza {gameObject.name} inicializada en posición {posicionInicial}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[OnBeginDrag] Iniciando arrastre de {gameObject.name}");

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (piezaIntercambio != null)
        {
            Debug.Log($"[OnEndDrag] Intercambiando {gameObject.name} con {piezaIntercambio.gameObject.name}");

            // Intercambiar posiciones en la jerarquía
            Transform tempParent = this.transform.parent;
            this.transform.SetParent(piezaIntercambio.transform.parent);
            piezaIntercambio.transform.SetParent(tempParent);

            // Intercambiar índices
            int tempIndex = indiceActual;
            indiceActual = piezaIntercambio.indiceActual;
            piezaIntercambio.indiceActual = tempIndex;

            // Validar si el rompecabezas está completo
            puzzleManager.ValidarPuzzle();
        }
        else
        {
            Debug.Log($"[OnEndDrag] {gameObject.name} regresando a su posición inicial");
            transform.position = posicionInicial;
        }

        piezaIntercambio = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ControllerPieze otraPieza = eventData.pointerDrag.GetComponent<ControllerPieze>();
            if (otraPieza != null && otraPieza != this)
            {
                piezaIntercambio = otraPieza;
                Debug.Log($"[OnDrop] {gameObject.name} detectó que {otraPieza.gameObject.name} fue soltada sobre él.");
            }
        }
    }

    public bool EnPosicionCorrecta()
    {
        return Vector3.Distance(transform.position, posicionInicial) < 0.1f;
    }
}
