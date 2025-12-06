using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using QuantumAI.Config;
using QuantumAI.Core;

namespace QuantumAI.UI
{
    /// <summary>
    /// Icono flotante de Quantum que permanece visible en todas las escenas
    /// Permite acceso rápido al chat y muestra notificaciones
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class FloatingAssistant : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region UI Components

        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject notificationBadge;
        [SerializeField] private TextMeshProUGUI notificationCount;
        [SerializeField] private RectTransform containerRect;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite thinkingSprite;
        [SerializeField] private Sprite speakingSprite;
        [SerializeField] private Sprite celebratingSprite;

        private AIConfig config;
        private Button button;
        private QuantumUIController uiController;

        #endregion

        #region State

        private AssistantState currentState = AssistantState.Idle;
        private int unreadMessages = 0;
        private bool isHovering = false;

        #endregion

        #region Initialization

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnIconClicked);

            config = QuantumAICore.Instance?.Config;

            // Buscar el controller padre
            uiController = GetComponentInParent<QuantumUIController>();

            SetupPosition();
        }

        private void Start()
        {
            // Configurar tamaño
            if (config != null && containerRect != null)
            {
                float size = config.iconSize;
                containerRect.sizeDelta = new Vector2(size, size);
            }

            // Ocultar badge inicialmente
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(false);
            }

            // Estado inicial
            SetState(AssistantState.Idle);

            // Animación de entrada
            PlayEntryAnimation();
        }

        private void SetupPosition()
        {
            if (containerRect == null)
            {
                containerRect = GetComponent<RectTransform>();
            }

            if (config == null) return;

            // Configurar ancla según posición configurada
            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.zero;
            Vector2 pivot = Vector2.zero;
            Vector2 anchoredPosition = Vector2.zero;

            float margin = 20f;

            switch (config.iconPosition)
            {
                case FloatingIconPosition.TopLeft:
                    anchorMin = anchorMax = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    anchoredPosition = new Vector2(margin, -margin);
                    break;

                case FloatingIconPosition.TopRight:
                    anchorMin = anchorMax = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    anchoredPosition = new Vector2(-margin, -margin);
                    break;

                case FloatingIconPosition.BottomLeft:
                    anchorMin = anchorMax = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    anchoredPosition = new Vector2(margin, margin);
                    break;

                case FloatingIconPosition.BottomRight:
                default:
                    anchorMin = anchorMax = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    anchoredPosition = new Vector2(-margin, margin);
                    break;
            }

            containerRect.anchorMin = anchorMin;
            containerRect.anchorMax = anchorMax;
            containerRect.pivot = pivot;
            containerRect.anchoredPosition = anchoredPosition;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Muestra el icono
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Oculta el icono
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Muestra el badge de notificación
        /// </summary>
        public void ShowNotificationBadge()
        {
            unreadMessages++;
            UpdateNotificationBadge();

            if (notificationBadge != null && !notificationBadge.activeSelf)
            {
                notificationBadge.SetActive(true);
                // Animación de aparición
                notificationBadge.transform.localScale = Vector3.zero;
                notificationBadge.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// Oculta el badge de notificación
        /// </summary>
        public void HideNotificationBadge()
        {
            unreadMessages = 0;
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(false);
            }
        }

        #endregion

        #region Animations

        /// <summary>
        /// Animación de entrada
        /// </summary>
        private void PlayEntryAnimation()
        {
            if (!config.enableIconAnimations) return;

            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Animación de latido idle
        /// </summary>
        private void PlayIdleAnimation()
        {
            if (!config.enableIconAnimations) return;

            // Pulso sutil cada 3 segundos
            transform.DOScale(1.1f, 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetDelay(3f)
                .OnComplete(() =>
                {
                    if (currentState == AssistantState.Idle)
                    {
                        PlayIdleAnimation();
                    }
                });
        }

        /// <summary>
        /// Animación al iniciar misión
        /// </summary>
        public void PlayStartMissionAnimation()
        {
            if (!config.enableIconAnimations) return;

            SetState(AssistantState.Thinking);

            transform.DOShakeRotation(0.5f, 10f, 10);

            // Volver a idle después de 2 segundos
            DOVirtual.DelayedCall(2f, () => SetState(AssistantState.Idle));
        }

        /// <summary>
        /// Animación de éxito
        /// </summary>
        public void PlaySuccessAnimation()
        {
            if (!config.enableIconAnimations) return;

            SetState(AssistantState.Celebrating);

            // Salto de alegría
            transform.DOPunchPosition(Vector3.up * 20f, 0.5f, 5, 1f);

            DOVirtual.DelayedCall(1f, () => SetState(AssistantState.Idle));
        }

        /// <summary>
        /// Animación de celebración
        /// </summary>
        public void PlayCelebrationAnimation()
        {
            if (!config.enableIconAnimations) return;

            SetState(AssistantState.Celebrating);

            // Rotación completa + escalado
            transform.DORotate(new Vector3(0, 0, 360f), 1f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad);
            transform.DOScale(1.2f, 0.5f).SetLoops(2, LoopType.Yoyo);

            DOVirtual.DelayedCall(1.5f, () => SetState(AssistantState.Idle));
        }

        /// <summary>
        /// Animación de pensamiento
        /// </summary>
        public void PlayThinkingAnimation()
        {
            if (!config.enableIconAnimations) return;

            SetState(AssistantState.Thinking);

            // Rotación continua mientras piensa
            transform.DORotate(new Vector3(0, 0, 10f), 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId("thinking");
        }

        /// <summary>
        /// Detiene animación de pensamiento
        /// </summary>
        public void StopThinkingAnimation()
        {
            DOTween.Kill("thinking");
            transform.rotation = Quaternion.identity;
            SetState(AssistantState.Idle);
        }

        #endregion

        #region State Management

        private void SetState(AssistantState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            UpdateVisuals();

            if (newState == AssistantState.Idle)
            {
                PlayIdleAnimation();
            }
        }

        private void UpdateVisuals()
        {
            if (iconImage == null) return;

            switch (currentState)
            {
                case AssistantState.Idle:
                    if (idleSprite != null) iconImage.sprite = idleSprite;
                    break;

                case AssistantState.Thinking:
                    if (thinkingSprite != null) iconImage.sprite = thinkingSprite;
                    break;

                case AssistantState.Speaking:
                    if (speakingSprite != null) iconImage.sprite = speakingSprite;
                    break;

                case AssistantState.Celebrating:
                    if (celebratingSprite != null) iconImage.sprite = celebratingSprite;
                    break;
            }
        }

        private void UpdateNotificationBadge()
        {
            if (notificationCount != null)
            {
                notificationCount.text = unreadMessages > 9 ? "9+" : unreadMessages.ToString();
            }
        }

        #endregion

        #region Event Handlers

        private void OnIconClicked()
        {
            // Animación de click
            transform.DOScale(0.9f, 0.1f).SetLoops(2, LoopType.Yoyo);

            // Abrir panel de chat
            uiController?.ToggleChatPanel();

            // Limpiar notificaciones
            HideNotificationBadge();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;

            if (config.enableIconAnimations)
            {
                transform.DOScale(1.15f, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;

            if (config.enableIconAnimations)
            {
                transform.DOScale(1f, 0.2f).SetEase(Ease.OutCubic);
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Limpiar todas las animaciones
            DOTween.Kill(transform);
        }

        #endregion
    }

    /// <summary>
    /// Estados del asistente
    /// </summary>
    public enum AssistantState
    {
        Idle,           // En reposo
        Thinking,       // Procesando
        Speaking,       // Hablando/respondiendo
        Celebrating     // Celebrando logros
    }
}
