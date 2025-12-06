using UnityEngine;
using UnityEngine.UI;
using QuantumAI.Core;

namespace QuantumAI.UI
{
    /// <summary>
    /// Adaptador para reutilizar un botón de chat existente con Quantum
    /// Úsalo en lugar de FloatingAssistant si ya tienes un botón de chat diseñado
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuantumChatButtonAdapter : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private GameObject notificationBadge;

        [Header("Configuración")]
        [SerializeField] private bool createChatPanelAutomatically = true;
        [SerializeField] private bool disableSceneChange = true;

        private Button button;
        private int unreadMessages = 0;

        private void Awake()
        {
            button = GetComponent<Button>();

            if (disableSceneChange)
            {
                // Remover todos los listeners existentes (como cambiarescena)
                button.onClick.RemoveAllListeners();
            }

            // Agregar el nuevo comportamiento
            button.onClick.AddListener(OnButtonClicked);
        }

        private void Start()
        {
            // Buscar o crear ChatPanel
            if (chatPanel == null && createChatPanelAutomatically)
            {
                chatPanel = FindObjectOfType<ChatPanel>();

                if (chatPanel == null)
                {
                    Debug.LogWarning("QuantumChatButtonAdapter: ChatPanel no encontrado. Créalo manualmente o activa createChatPanelAutomatically.");
                }
            }

            // Suscribirse a eventos de Quantum
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse += OnAIResponse;
            }

            // Ocultar badge inicialmente
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(false);
            }
        }

        private void OnButtonClicked()
        {
            Debug.Log("QuantumChatButtonAdapter: Botón clickeado");

            if (chatPanel != null)
            {
                // Abrir/cerrar el panel de chat
                if (chatPanel.IsVisible())
                {
                    chatPanel.Hide();
                }
                else
                {
                    chatPanel.Show();
                    HideNotificationBadge();
                }
            }
            else
            {
                Debug.LogError("QuantumChatButtonAdapter: No hay ChatPanel asignado");
            }
        }

        private void OnAIResponse(string response)
        {
            // Si el chat está cerrado, mostrar notificación
            if (chatPanel != null && !chatPanel.IsVisible())
            {
                ShowNotificationBadge();
            }
        }

        private void ShowNotificationBadge()
        {
            unreadMessages++;

            if (notificationBadge != null)
            {
                notificationBadge.SetActive(true);
            }
        }

        private void HideNotificationBadge()
        {
            unreadMessages = 0;

            if (notificationBadge != null)
            {
                notificationBadge.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse -= OnAIResponse;
            }
        }
    }
}
