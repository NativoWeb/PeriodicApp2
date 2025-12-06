using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QuantumAI.Core;
using DG.Tweening;

namespace QuantumAI.UI
{
    /// <summary>
    /// Badge de notificación para el botón de chat.
    /// Se activa cuando Quantum AI envía un mensaje.
    /// </summary>
    public class ChatNotificationBadge : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("Objeto que contiene el badge (círculo rojo con número)")]
        [SerializeField] private GameObject badgeObject;

        [Tooltip("Texto que muestra el número de mensajes sin leer")]
        [SerializeField] private TextMeshProUGUI badgeText;

        [Header("Configuración Visual")]
        [Tooltip("Color del badge cuando hay notificaciones")]
        [SerializeField] private Color notificationColor = new Color(1f, 0.2f, 0.2f); // Rojo

        [Tooltip("Escala de la animación de pulso")]
        [SerializeField] private float pulseScale = 1.2f;

        [Tooltip("Duración de la animación de pulso")]
        [SerializeField] private float pulseDuration = 0.5f;

        private int unreadMessages = 0;
        private Image badgeImage;
        private Button chatButton;

        private void Awake()
        {
            // Obtener referencias
            chatButton = GetComponent<Button>();

            if (badgeObject != null)
            {
                badgeImage = badgeObject.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.color = notificationColor;
                }
            }
        }

        private void Start()
        {
            // Suscribirse a eventos de Quantum AI
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse += OnQuantumMessage;
                Debug.Log("💬 [ChatNotificationBadge] Conectado con Quantum AI");
            }
            else
            {
                Debug.LogWarning("⚠️ [ChatNotificationBadge] QuantumAICore no encontrado");
            }

            // Ocultar badge al inicio
            HideBadge();

            // Limpiar contador cuando se abre el chat
            if (chatButton != null)
            {
                chatButton.onClick.AddListener(OnChatOpened);
            }
        }

        private void OnQuantumMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Incrementar contador de mensajes no leídos
            unreadMessages++;
            UpdateBadge();

            // Animar el badge para llamar la atención
            AnimateBadge();
        }

        private void UpdateBadge()
        {
            if (badgeObject == null) return;

            if (unreadMessages > 0)
            {
                badgeObject.SetActive(true);

                if (badgeText != null)
                {
                    // Mostrar número o "+" si son muchos
                    badgeText.text = unreadMessages > 9 ? "9+" : unreadMessages.ToString();
                }
            }
            else
            {
                HideBadge();
            }
        }

        private void AnimateBadge()
        {
            if (badgeObject == null) return;

            // Animación de pulso con DOTween
            badgeObject.transform.DOKill(); // Cancelar animaciones previas
            badgeObject.transform.localScale = Vector3.one;

            badgeObject.transform
                .DOScale(pulseScale, pulseDuration / 2)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo);

            // También animar el botón de chat
            if (chatButton != null)
            {
                chatButton.transform.DOKill();
                chatButton.transform.localScale = Vector3.one;

                chatButton.transform
                    .DOShakeScale(pulseDuration, 0.1f, 3, 90f)
                    .SetEase(Ease.OutQuad);
            }
        }

        private void OnChatOpened()
        {
            // Cuando se abre el chat, resetear el contador
            unreadMessages = 0;
            UpdateBadge();
        }

        private void HideBadge()
        {
            if (badgeObject != null)
            {
                badgeObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Limpiar eventos
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse -= OnQuantumMessage;
            }

            if (chatButton != null)
            {
                chatButton.onClick.RemoveListener(OnChatOpened);
            }
        }

        #region Métodos Públicos para Testing

        /// <summary>
        /// Para testing: simular un mensaje de IA
        /// </summary>
        public void SimulateMessage()
        {
            OnQuantumMessage("Test message");
        }

        /// <summary>
        /// Resetear manualmente el contador
        /// </summary>
        public void ResetCounter()
        {
            unreadMessages = 0;
            UpdateBadge();
        }

        #endregion
    }
}
