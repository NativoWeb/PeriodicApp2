using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using QuantumAI.Core;

namespace QuantumAI.UI
{
    /// <summary>
    /// Indicador visual permanente que muestra que Quantum AI está activo.
    /// Aparece como un pequeño icono que pulsa suavemente.
    /// </summary>
    public class QuantumIndicator : MonoBehaviour
    {
        [Header("Configuración Visual")]
        [Tooltip("Imagen del indicador (logo de Quantum)")]
        [SerializeField] private Image indicatorImage;

        [Tooltip("Color cuando está idle")]
        [SerializeField] private Color idleColor = new Color(0.4f, 0.7f, 1f, 0.7f); // Azul claro

        [Tooltip("Color cuando está procesando")]
        [SerializeField] private Color processingColor = new Color(0.2f, 1f, 0.3f, 1f); // Verde brillante

        [Header("Animación")]
        [Tooltip("Escala máxima del pulso idle")]
        [SerializeField] private float idlePulseScale = 1.1f;

        [Tooltip("Duración del pulso idle")]
        [SerializeField] private float idlePulseDuration = 2f;

        [Tooltip("Velocidad de rotación cuando procesa")]
        [SerializeField] private float processingRotationSpeed = 2f;

        private bool isProcessing = false;
        private Tween idleTween;
        private Tween processingTween;

        private void Awake()
        {
            if (indicatorImage == null)
            {
                indicatorImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            // Suscribirse a eventos de Quantum AI
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse += OnQuantumResponse;
                QuantumAICore.Instance.OnMissionStarted += OnMissionStarted;
                QuantumAICore.Instance.OnMissionCompleted += OnMissionCompleted;
                QuantumAICore.Instance.OnXPGained += OnXPGained;
                QuantumAICore.Instance.OnAchievementUnlocked += OnAchievementUnlocked;

                Debug.Log("💠 [QuantumIndicator] Indicador visual conectado");
            }

            // Iniciar animación idle
            StartIdleAnimation();
        }

        private void StartIdleAnimation()
        {
            if (isProcessing) return;

            // Cancelar animaciones previas
            StopAllAnimations();

            // Color idle
            if (indicatorImage != null)
            {
                indicatorImage.color = idleColor;
            }

            // Animación de pulso suave
            idleTween = transform.DOScale(idlePulseScale, idlePulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StartProcessingAnimation()
        {
            if (isProcessing) return;

            isProcessing = true;

            // Cancelar animaciones idle
            StopAllAnimations();

            // Color de procesamiento
            if (indicatorImage != null)
            {
                indicatorImage.DOColor(processingColor, 0.3f);
            }

            // Rotación continua
            processingTween = transform.DORotate(new Vector3(0, 0, 360), processingRotationSpeed, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);

            // Pulso rápido
            transform.DOScale(1.2f, 0.2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }

        private void StopProcessing()
        {
            if (!isProcessing) return;

            isProcessing = false;

            // Cancelar animaciones de procesamiento
            StopAllAnimations();

            // Resetear rotación
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // Volver a idle
            StartIdleAnimation();
        }

        private void StopAllAnimations()
        {
            // Cancelar todas las animaciones activas
            transform.DOKill();
            if (indicatorImage != null)
            {
                indicatorImage.DOKill();
            }
        }

        #region Event Handlers

        private void OnQuantumResponse(string response)
        {
            // Breve flash al recibir respuesta
            Flash();
        }

        private void OnMissionStarted(string element, string missionType)
        {
            StartProcessingAnimation();
            // Volver a idle después de 2 segundos
            Invoke(nameof(StopProcessing), 2f);
        }

        private void OnMissionCompleted(string element, bool success)
        {
            if (success)
            {
                SuccessPulse();
            }
        }

        private void OnXPGained(int amount, string reason)
        {
            Flash();
        }

        private void OnAchievementUnlocked(string achievement)
        {
            CelebrationAnimation();
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Flash rápido del indicador
        /// </summary>
        private void Flash()
        {
            if (indicatorImage == null) return;

            Color currentColor = indicatorImage.color;
            indicatorImage.DOColor(Color.white, 0.1f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => indicatorImage.color = currentColor);
        }

        /// <summary>
        /// Pulso de éxito (verde)
        /// </summary>
        private void SuccessPulse()
        {
            if (indicatorImage == null) return;

            Color currentColor = indicatorImage.color;
            Color successColor = new Color(0.2f, 1f, 0.3f, 1f);

            indicatorImage.DOColor(successColor, 0.3f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => indicatorImage.color = currentColor);

            transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 5, 0.5f);
        }

        /// <summary>
        /// Animación de celebración (dorado brillante)
        /// </summary>
        private void CelebrationAnimation()
        {
            if (indicatorImage == null) return;

            Color currentColor = indicatorImage.color;
            Color celebrationColor = new Color(1f, 0.8f, 0.1f, 1f);

            indicatorImage.DOColor(celebrationColor, 0.2f)
                .SetLoops(6, LoopType.Yoyo)
                .OnComplete(() => indicatorImage.color = currentColor);

            transform.DOPunchScale(Vector3.one * 0.5f, 1f, 8, 0.8f);
        }

        #endregion

        private void OnDestroy()
        {
            // Limpiar eventos
            if (QuantumAICore.Instance != null)
            {
                QuantumAICore.Instance.OnAIResponse -= OnQuantumResponse;
                QuantumAICore.Instance.OnMissionStarted -= OnMissionStarted;
                QuantumAICore.Instance.OnMissionCompleted -= OnMissionCompleted;
                QuantumAICore.Instance.OnXPGained -= OnXPGained;
                QuantumAICore.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
            }

            // Cancelar animaciones
            StopAllAnimations();
        }
    }
}
