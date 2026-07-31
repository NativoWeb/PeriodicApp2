using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using QuantumAI.Core;

namespace QuantumAI.UI
{
    /// <summary>
    /// Sistema de notificaciones toast para mostrar mensajes breves de Quantum AI.
    /// Aparece temporalmente en la pantalla y desaparece automáticamente.
    /// PERSISTE ENTRE ESCENAS automáticamente.
    /// </summary>
    public class QuantumToastNotification : MonoBehaviour
    {
        #region Singleton

        private static QuantumToastNotification _instance;
        public static QuantumToastNotification Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<QuantumToastNotification>();
                }
                return _instance;
            }
        }

        #endregion

        [Header("Prefab de Toast")]
        [Tooltip("Prefab del toast que se instanciará")]
        [SerializeField] private GameObject toastPrefab;

        [Header("Configuración")]
        [Tooltip("Duración que el toast permanece visible (segundos)")]
        [SerializeField] private float displayDuration = 3f;

        [Tooltip("Tiempo de animación de entrada/salida")]
        [SerializeField] private float animationDuration = 0.3f;

        [Tooltip("Máximo de toasts simultáneos")]
        [SerializeField] private int maxSimultaneousToasts = 3;

        [Tooltip("Espacio vertical entre toasts")]
        [SerializeField] private float toastSpacing = 120f;

        [Header("Tipos de Notificación")]
        [SerializeField] private bool showMissionNotifications = true;
        [SerializeField] private bool showXPNotifications = true;
        [SerializeField] private bool showAchievementNotifications = true;
        [SerializeField] private bool showGameHints = true;
        [SerializeField] private bool showAIResponses = true;

        private Queue<ToastData> toastQueue = new Queue<ToastData>();
        private List<GameObject> activeToasts = new List<GameObject>();
        private RectTransform canvasRect;
        private Canvas canvas;

        private void Awake()
        {
            // Implementar patrón Singleton con persistencia
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // ¡PERSISTE ENTRE ESCENAS!

            canvasRect = GetComponent<RectTransform>();

            // Usar SOLO el Canvas propio del toast (nunca el canvas padre)
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Por encima de todo, sin afectar el canvas padre

            Debug.Log("🔔 [QuantumToastNotification] Sistema persistente inicializado");
        }

        private void Start()
        {
            // Suscribirse a eventos de Quantum AI
            if (QuantumAICore.Instance != null)
            {
                if (showMissionNotifications)
                {
                    QuantumAICore.Instance.OnMissionStarted += OnMissionStarted;
                    QuantumAICore.Instance.OnMissionCompleted += OnMissionCompleted;
                }

                if (showXPNotifications)
                {
                    QuantumAICore.Instance.OnXPGained += OnXPGained;
                }

                if (showAchievementNotifications)
                {
                    QuantumAICore.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
                }

                if (showAIResponses)
                {
                    QuantumAICore.Instance.OnAIResponse += OnAIResponse;
                }

                Debug.Log("🔔 [QuantumToastNotification] Sistema de notificaciones inicializado");
            }
        }

        #region Event Handlers

        private void OnMissionStarted(string elementName, string missionType)
        {
            string icon = GetMissionIcon(missionType);
            string message = $"{icon} Misión iniciada: {elementName}";
            ShowToast(message, ToastType.Info);
        }

        private void OnMissionCompleted(string elementName, bool success)
        {
            if (success)
            {
                ShowToast($"¡Misión de {elementName} completada!", ToastType.Success);
            }
            else
            {
                // Generar mensaje motivacional con IA
                if (QuantumAICore.Instance != null)
                {
                    // Delegar a Quantum para generar una frase motivacional
                    string contexto = $"El estudiante no logró completar la misión sobre el elemento {elementName}. Genera UNA frase muy corta (máximo 15 palabras) de motivación y un consejo breve para mejorar. Sé empático y positivo.";
                    QuantumAICore.Instance.ProcessUserQuestion(contexto);
                }
                else
                {
                    // Fallback si Quantum no está disponible
                    ShowToast($"¡No te rindas! Revisa los conceptos de {elementName} e inténtalo de nuevo.", ToastType.Warning);
                }
            }
        }

        private void OnXPGained(int amount, string reason)
        {
            ShowToast($"+{amount} XP - {reason}", ToastType.XP);
        }

        private void OnAchievementUnlocked(string achievementName)
        {
            ShowToast($"Logro: {achievementName}", ToastType.Achievement, 8f);
        }

        private void OnAIResponse(string response)
        {
            // Truncar el mensaje si es muy largo
            string displayMessage = response;
            if (displayMessage.Length > 180)
            {
                displayMessage = displayMessage.Substring(0, 177) + "...";
            }

            ShowToast(displayMessage, ToastType.Info, 8f);
            Debug.Log($"[QuantumToastNotification] Mostrando respuesta de IA: {displayMessage}");
        }

        #endregion

        #region Toast Display

        public void ShowToast(string message, ToastType type = ToastType.Info, float? customDuration = null)
        {
            ToastData data = new ToastData
            {
                message = message,
                type = type,
                duration = customDuration ?? displayDuration
            };

            // SOLO UN TOAST: Si ya hay uno activo, destruirlo primero
            if (activeToasts.Count > 0)
            {
                foreach (var toast in activeToasts)
                {
                    if (toast != null)
                    {
                        DOTween.Kill(toast.transform);
                        Destroy(toast);
                    }
                }
                activeToasts.Clear();
            }

            // Mostrar el nuevo toast
            StartCoroutine(DisplayToast(data));
        }

        private IEnumerator DisplayToast(ToastData data)
        {
            if (toastPrefab == null)
            {
                Debug.LogError("❌ [QuantumToastNotification] No hay prefab de toast asignado");
                yield break;
            }

            // Instanciar el toast
            GameObject toastObj = Instantiate(toastPrefab, transform);
            activeToasts.Add(toastObj);

            // Configurar el toast
            ConfigureToast(toastObj, data);

            // Posicionar el toast
            RectTransform toastRect = toastObj.GetComponent<RectTransform>();
            PositionToast(toastRect, activeToasts.Count - 1);

            // Animación de entrada (desde la derecha)
            Vector2 startPos = toastRect.anchoredPosition;
            startPos.x += 500f; // Fuera de pantalla a la derecha
            toastRect.anchoredPosition = startPos;

            toastRect.DOAnchorPosX(startPos.x - 500f, animationDuration)
                .SetEase(Ease.OutBack);

            // Fade in
            CanvasGroup canvasGroup = toastObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = toastObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1f, animationDuration);

            // Esperar
            yield return new WaitForSeconds(data.duration);

            // Animación de salida
            toastRect.DOAnchorPosX(startPos.x, animationDuration).SetEase(Ease.InBack);
            canvasGroup.DOFade(0f, animationDuration);

            yield return new WaitForSeconds(animationDuration);

            // Destruir y remover de la lista
            activeToasts.Remove(toastObj);
            Destroy(toastObj);

            // Reposicionar toasts restantes
            RepositionToasts();

            // Mostrar siguiente en cola si hay
            if (toastQueue.Count > 0)
            {
                ToastData nextToast = toastQueue.Dequeue();
                StartCoroutine(DisplayToast(nextToast));
            }
        }

        private void ConfigureToast(GameObject toastObj, ToastData data)
        {
            // Buscar el texto
            TextMeshProUGUI text = toastObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = data.message;
            }

            // Buscar el fondo y aplicar color según tipo
            Image background = toastObj.GetComponent<Image>();
            if (background != null)
            {
                background.color = GetColorForType(data.type);
            }

            // Buscar icono si existe
            Transform iconTransform = toastObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    // Aquí podrías asignar sprites diferentes según el tipo
                    icon.color = Color.white;
                }
            }
        }

        private void PositionToast(RectTransform toastRect, int index)
        {
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);

            float safeAreaTop = Screen.height - Screen.safeArea.yMax;
            float yOffset = safeAreaTop > 10f ? safeAreaTop + 10f : 20f;
            float yPos = -yOffset - (index * toastSpacing);
            toastRect.anchoredPosition = new Vector2(0, yPos);
        }

        private void RepositionToasts()
        {
            for (int i = 0; i < activeToasts.Count; i++)
            {
                RectTransform rect = activeToasts[i].GetComponent<RectTransform>();
                float yPos = -100f - (i * toastSpacing);
                rect.DOAnchorPosY(yPos, 0.3f).SetEase(Ease.OutQuad);
            }
        }

        #endregion

        #region Helpers

        private Color GetColorForType(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success:
                    return new Color(0.2f, 0.8f, 0.2f, 0.95f); // Verde
                case ToastType.Achievement:
                    return new Color(1f, 0.7f, 0.1f, 0.95f); // Dorado
                case ToastType.XP:
                    return new Color(0.4f, 0.6f, 1f, 0.95f); // Azul
                case ToastType.Warning:
                    return new Color(1f, 0.5f, 0.1f, 0.95f); // Naranja
                case ToastType.Info:
                default:
                    return new Color(1f, 1f, 1f, 0.95f); // Blanco (cambiado de gris)
            }
        }

        private string GetMissionIcon(string missionType)
        {
            switch (missionType)
            {
                case "AR": return "[AR]";
                case "QR": return "[QR]";
                case "Juego": return "[Juego]";
                case "Quiz": return "[Quiz]";
                default: return "[Mision]";
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Mostrar un mensaje personalizado de info
        /// </summary>
        public void ShowInfo(string message)
        {
            ShowToast(message, ToastType.Info);
        }

        /// <summary>
        /// Mostrar un mensaje de éxito
        /// </summary>
        public void ShowSuccess(string message)
        {
            ShowToast(message, ToastType.Success);
        }

        /// <summary>
        /// Mostrar un mensaje de advertencia
        /// </summary>
        public void ShowWarning(string message)
        {
            ShowToast(message, ToastType.Warning);
        }

        #endregion

        private void OnDestroy()
        {
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnMissionStarted -= OnMissionStarted;
                QuantumAICore.Instance.OnMissionCompleted -= OnMissionCompleted;
                QuantumAICore.Instance.OnXPGained -= OnXPGained;
                QuantumAICore.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
                QuantumAICore.Instance.OnAIResponse -= OnAIResponse;
            }

            // Limpiar toasts activos
            foreach (var toast in activeToasts)
            {
                if (toast != null) Destroy(toast);
            }
        }
    }

    #region Data Classes

    [System.Serializable]
    public class ToastData
    {
        public string message;
        public ToastType type;
        public float duration;
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Achievement,
        XP
    }

    #endregion
}
