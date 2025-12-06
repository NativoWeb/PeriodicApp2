using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using QuantumAI.Config;
using QuantumAI.Core;

namespace QuantumAI.UI
{
    /// <summary>
    /// Panel de chat expandible para interactuar con Quantum
    /// Muestra burbujas de mensajes del usuario y la IA
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {
        #region UI References

        [Header("Main Panel")]
        [SerializeField] private RectTransform panelContainer;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;

        [Header("Chat Area")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform messagesContainer;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject aiMessagePrefab;
        [SerializeField] private GameObject systemMessagePrefab;

        [Header("Input Area")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button voiceButton;

        [Header("Quick Actions")]
        [SerializeField] private Transform quickActionsContainer;
        [SerializeField] private GameObject quickActionButtonPrefab;

        #endregion

        #region Configuration

        private AIConfig config;
        private QuantumUIController uiController;

        #endregion

        #region State

        private bool isVisible = false;
        private bool isTyping = false;
        private List<GameObject> messageObjects = new List<GameObject>();

        #endregion

        #region Initialization

        private void Awake()
        {
            config = QuantumAICore.Instance?.Config;
            uiController = GetComponentInParent<QuantumUIController>();

            // Configurar botones
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (voiceButton != null)
            {
                voiceButton.onClick.AddListener(OnVoiceButtonClicked);
                voiceButton.interactable = config != null && config.enableVoiceNarration;
            }

            // Configurar input field
            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);
            }
        }

        private void Start()
        {
            // Ocultar panel inicialmente
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Generar acciones rápidas
            GenerateQuickActions();
        }

        #endregion

        #region Visibility

        /// <summary>
        /// Muestra el panel de chat con animación
        /// </summary>
        public void Show()
        {
            if (isVisible) return;

            isVisible = true;

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(1f, 0.3f);
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (panelContainer != null)
            {
                panelContainer.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutCubic);
            }

            // Focus en el input field
            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        /// <summary>
        /// Oculta el panel de chat con animación
        /// </summary>
        public void Hide()
        {
            if (!isVisible) return;

            isVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.3f);
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panelContainer != null)
            {
                float height = panelContainer.rect.height;
                panelContainer.DOAnchorPosY(-height, 0.3f).SetEase(Ease.InCubic);
            }
        }

        /// <summary>
        /// Verifica si el panel está visible
        /// </summary>
        public bool IsVisible()
        {
            return isVisible;
        }

        #endregion

        #region Message Management

        /// <summary>
        /// Agrega un mensaje del usuario
        /// </summary>
        public void AddUserMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            GameObject messageBubble = InstantiateMessageBubble(userMessagePrefab);
            SetMessageText(messageBubble, message);
            messageObjects.Add(messageBubble);

            ScrollToBottom();
        }

        /// <summary>
        /// Agrega un mensaje de la IA
        /// </summary>
        public void AddAIMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            GameObject messageBubble = InstantiateMessageBubble(aiMessagePrefab);

            // Efecto de escritura si está habilitado
            if (config != null && config.typingSpeed > 0)
            {
                StartCoroutine(TypeMessage(messageBubble, message));
            }
            else
            {
                SetMessageText(messageBubble, message);
            }

            messageObjects.Add(messageBubble);
            ScrollToBottom();
        }

        /// <summary>
        /// Agrega un mensaje del sistema
        /// </summary>
        public void AddSystemMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            GameObject messageBubble = InstantiateMessageBubble(systemMessagePrefab);
            SetMessageText(messageBubble, message);
            messageObjects.Add(messageBubble);

            ScrollToBottom();
        }

        /// <summary>
        /// Limpia todos los mensajes
        /// </summary>
        public void ClearMessages()
        {
            foreach (GameObject msg in messageObjects)
            {
                if (msg != null)
                {
                    Destroy(msg);
                }
            }

            messageObjects.Clear();
        }

        #endregion

        #region Private Methods

        private GameObject InstantiateMessageBubble(GameObject prefab)
        {
            if (prefab == null || messagesContainer == null)
            {
                Debug.LogError("ChatPanel: Prefab o container no configurado");
                return null;
            }

            GameObject bubble = Instantiate(prefab, messagesContainer);

            // Animación de aparición
            bubble.transform.localScale = Vector3.zero;
            bubble.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

            return bubble;
        }

        private void SetMessageText(GameObject messageBubble, string text)
        {
            if (messageBubble == null) return;

            TextMeshProUGUI textComponent = messageBubble.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }

        private IEnumerator TypeMessage(GameObject messageBubble, string fullMessage)
        {
            if (messageBubble == null) yield break;

            isTyping = true;

            TextMeshProUGUI textComponent = messageBubble.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent == null)
            {
                isTyping = false;
                yield break;
            }

            textComponent.text = "";

            float delay = 1f / config.typingSpeed;

            foreach (char c in fullMessage)
            {
                textComponent.text += c;
                yield return new WaitForSeconds(delay);

                // Auto-scroll mientras escribe
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                }
            }

            isTyping = false;
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null) return;

            StartCoroutine(ScrollToBottomCoroutine());
        }

        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame();

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void GenerateQuickActions()
        {
            if (quickActionsContainer == null || quickActionButtonPrefab == null) return;

            string[] actions = {
                "¿Qué elemento debo estudiar?",
                "Explícame un concepto",
                "¿Cómo voy en mi progreso?",
                "Dame una pista"
            };

            foreach (string action in actions)
            {
                GameObject buttonObj = Instantiate(quickActionButtonPrefab, quickActionsContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = action;
                }

                if (button != null)
                {
                    string actionText = action; // Capturar en closure
                    button.onClick.AddListener(() => OnQuickActionClicked(actionText));
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnSendButtonClicked()
        {
            SendMessage();
        }

        private void OnInputSubmit(string text)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (inputField == null || uiController == null) return;

            string message = inputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            // Enviar mensaje
            uiController.SendUserMessage(message);

            // Limpiar input
            inputField.text = "";
            inputField.ActivateInputField();
        }

        private void OnVoiceButtonClicked()
        {
            // TODO: Implementar grabación de voz
            Debug.Log("ChatPanel: Voice input no implementado aún");
            AddSystemMessage("La entrada por voz estará disponible próximamente.");
        }

        private void OnQuickActionClicked(string action)
        {
            if (uiController != null)
            {
                uiController.SendUserMessage(action);
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Limpiar animaciones
            DOTween.Kill(canvasGroup);
            DOTween.Kill(panelContainer);
        }

        #endregion
    }
}
