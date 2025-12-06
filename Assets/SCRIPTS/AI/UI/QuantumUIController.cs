using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QuantumAI.Core;
using QuantumAI.Config;

namespace QuantumAI.UI
{
    /// <summary>
    /// Controlador principal de la interfaz de Quantum
    /// Gestiona el icono flotante, panel de chat y notificaciones
    /// </summary>
    public class QuantumUIController : MonoBehaviour
    {
        #region UI References

        [Header("UI Components")]
        [SerializeField] private FloatingAssistant floatingIcon;
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private Canvas mainCanvas;

        #endregion

        #region Configuration

        private AIConfig config;

        #endregion

        #region Initialization

        private void Awake()
        {
            config = QuantumAICore.Instance.Config;

            // Si no hay componentes asignados, intentar encontrarlos
            if (floatingIcon == null)
            {
                floatingIcon = GetComponentInChildren<FloatingAssistant>();
            }

            if (chatPanel == null)
            {
                chatPanel = GetComponentInChildren<ChatPanel>();
            }

            // Suscribirse a eventos del core
            SubscribeToEvents();
        }

        private void Start()
        {
            // Configurar UI inicial
            SetupUI();

            // Mostrar icono flotante
            if (floatingIcon != null)
            {
                floatingIcon.Show();
            }

            // Ocultar chat panel inicialmente
            if (chatPanel != null)
            {
                chatPanel.Hide();
            }
        }

        private void SubscribeToEvents()
        {
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse += HandleAIResponse;
                QuantumAICore.Instance.OnMissionStarted += HandleMissionStarted;
                QuantumAICore.Instance.OnMissionCompleted += HandleMissionCompleted;
                QuantumAICore.Instance.OnXPGained += HandleXPGained;
                QuantumAICore.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
                QuantumAICore.Instance.OnSceneChanged += HandleSceneChanged;
            }
        }

        private void SetupUI()
        {
            // Configurar canvas si no existe
            if (mainCanvas == null)
            {
                mainCanvas = GetComponent<Canvas>();
                if (mainCanvas == null)
                {
                    mainCanvas = gameObject.AddComponent<Canvas>();
                }
            }

            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 1000; // Asegurar que esté por encima de otros UI

            // Agregar CanvasScaler si no existe
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Agregar GraphicRaycaster si no existe
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        #endregion

        #region Event Handlers

        private void HandleAIResponse(string response)
        {
            // Mostrar respuesta en el chat panel
            if (chatPanel != null)
            {
                chatPanel.AddAIMessage(response);

                // Si el panel está oculto, mostrar notificación en el icono
                if (!chatPanel.IsVisible())
                {
                    floatingIcon?.ShowNotificationBadge();
                }
            }
        }

        private void HandleMissionStarted(string elementName, string missionType)
        {
            // Animación del icono para indicar actividad
            floatingIcon?.PlayStartMissionAnimation();
        }

        private void HandleMissionCompleted(string elementName, bool success)
        {
            if (success)
            {
                floatingIcon?.PlaySuccessAnimation();
            }
        }

        private void HandleXPGained(int amount, string reason)
        {
            // Mostrar notificación flotante de XP
            // TODO: Implementar notificación flotante
        }

        private void HandleAchievementUnlocked(string achievementName)
        {
            floatingIcon?.PlayCelebrationAnimation();
        }

        private void HandleSceneChanged(string sceneName)
        {
            // Asegurar que la UI persista entre escenas
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Alterna la visibilidad del panel de chat
        /// </summary>
        public void ToggleChatPanel()
        {
            if (chatPanel != null)
            {
                if (chatPanel.IsVisible())
                {
                    chatPanel.Hide();
                }
                else
                {
                    chatPanel.Show();
                    floatingIcon?.HideNotificationBadge();
                }
            }
        }

        /// <summary>
        /// Muestra el panel de chat
        /// </summary>
        public void ShowChatPanel()
        {
            chatPanel?.Show();
            floatingIcon?.HideNotificationBadge();
        }

        /// <summary>
        /// Oculta el panel de chat
        /// </summary>
        public void HideChatPanel()
        {
            chatPanel?.Hide();
        }

        /// <summary>
        /// Envía un mensaje del usuario
        /// </summary>
        public void SendUserMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // Agregar mensaje al chat
            chatPanel?.AddUserMessage(message);

            // Procesar con IA
            QuantumAICore.Instance?.ProcessUserQuestion(message);
        }

        /// <summary>
        /// Muestra un mensaje de sistema
        /// </summary>
        public void ShowSystemMessage(string message)
        {
            chatPanel?.AddSystemMessage(message);
        }

        /// <summary>
        /// Activa/desactiva la UI completa
        /// </summary>
        public void SetUIEnabled(bool enabled)
        {
            gameObject.SetActive(enabled);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse -= HandleAIResponse;
                QuantumAICore.Instance.OnMissionStarted -= HandleMissionStarted;
                QuantumAICore.Instance.OnMissionCompleted -= HandleMissionCompleted;
                QuantumAICore.Instance.OnXPGained -= HandleXPGained;
                QuantumAICore.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
                QuantumAICore.Instance.OnSceneChanged -= HandleSceneChanged;
            }
        }

        #endregion
    }
}
