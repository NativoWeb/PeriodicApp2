using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Clase base para juegos de arrastrar y emparejar.
/// Unifica la lógica común de ControllerGame y ControllerGame2.
/// </summary>
public abstract class DragAndMatchBase : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Configuración de Drag & Drop")]
    [SerializeField] protected Button continueButton;
    [Tooltip("Número total de emparejamientos necesarios para completar el nivel")]
    [SerializeField] protected int totalMatches = 3;

    [Header("Debug")]
    [SerializeField] protected bool enableDebugLogs = true;

    // Estado del juego
    protected Vector3 initialPosition;
    protected RectTransform rectTransform;
    protected CanvasGroup canvasGroup;
    protected static int correctMatches = 0;

    // Componentes
    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    protected virtual void Start()
    {
        // Guardar posición inicial
        initialPosition = rectTransform.position;

        // Configurar botón de continuar
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonClick);
        }

        // Inicialización específica del juego
        OnGameStart();
    }

    #region Drag & Drop Implementation

    public virtual void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;
        canvasGroup.blocksRaycasts = false;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        GameObject droppedOn = eventData.pointerCurrentRaycast.gameObject;

        if (droppedOn != null && droppedOn.CompareTag("ZonaEmparejamiento"))
        {
            if (CheckMatch(gameObject.name, droppedOn.name))
            {
                OnCorrectMatch(droppedOn);
                correctMatches++;

                if (correctMatches >= totalMatches)
                {
                    OnAllMatchesComplete();
                }
            }
            else
            {
                OnIncorrectMatch();
            }
        }
        else
        {
            ReturnToInitialPosition();
        }
    }

    #endregion

    #region Abstract Methods - Implementar en clases hijas

    /// <summary>
    /// Verifica si el emparejamiento es correcto.
    /// </summary>
    protected abstract bool CheckMatch(string draggedName, string droppedName);

    /// <summary>
    /// Llamado cuando el juego inicia.
    /// </summary>
    protected abstract void OnGameStart();

    /// <summary>
    /// Llamado cuando todos los emparejamientos están completos.
    /// </summary>
    protected abstract void OnAllMatchesComplete();

    /// <summary>
    /// Llamado cuando se hace clic en el botón continuar.
    /// </summary>
    protected abstract void OnContinueButtonClick();

    #endregion

    #region Virtual Methods - Pueden ser sobrescritos

    /// <summary>
    /// Llamado cuando se hace un emparejamiento correcto.
    /// </summary>
    protected virtual void OnCorrectMatch(GameObject matchedObject)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"✅ Emparejamiento correcto: {gameObject.name} con {matchedObject.name}");
        }

        // Destruir ambos objetos
        Destroy(gameObject);
        Destroy(matchedObject);
    }

    /// <summary>
    /// Llamado cuando se hace un emparejamiento incorrecto.
    /// </summary>
    protected virtual void OnIncorrectMatch()
    {
        if (enableDebugLogs)
        {
            Debug.Log("❌ Emparejamiento incorrecto. Reintentando...");
        }

        ReturnToInitialPosition();
    }

    /// <summary>
    /// Retorna el objeto a su posición inicial.
    /// </summary>
    protected virtual void ReturnToInitialPosition()
    {
        rectTransform.position = initialPosition;
    }

    /// <summary>
    /// Activa el botón de continuar.
    /// </summary>
    protected virtual void ShowContinueButton()
    {
        if (continueButton != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("🎉 Todos los emparejamientos correctos. Activando botón continuar...");
            }

            continueButton.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Resetea el contador de emparejamientos.
    /// Útil para reiniciar el nivel.
    /// </summary>
    public static void ResetMatchCount()
    {
        correctMatches = 0;
    }

    /// <summary>
    /// Obtiene el número de emparejamientos correctos actuales.
    /// </summary>
    public static int GetCorrectMatches()
    {
        return correctMatches;
    }

    #endregion

    protected virtual void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClick);
        }
    }
}
