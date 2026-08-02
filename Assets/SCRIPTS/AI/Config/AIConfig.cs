using UnityEngine;
using QuantumAI.Models;

namespace QuantumAI.Config
{
    /// <summary>
    /// Configuración centralizada del sistema de IA Quantum
    /// Crea una instancia desde: Assets > Create > Quantum AI > AI Config
    /// </summary>
    [CreateAssetMenu(fileName = "QuantumAIConfig", menuName = "Quantum AI/AI Config", order = 1)]
    public class AIConfig : ScriptableObject
    {
        #region API Configuration

        [Header("Gemini API Configuration")]
        [Tooltip("API Key de Google Gemini (obtener en https://aistudio.google.com/app/apikey)")]
        public string geminiApiKey = "";

        [Tooltip("Modelo de Gemini a usar")]
        public string geminiModel = "gemini-1.5-flash";

        [Tooltip("URL base de la API de Gemini")]
        public string geminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        [Tooltip("Timeout para requests API en segundos")]
        public int apiTimeout = 15;

        [Tooltip("Máximo de tokens por respuesta")]
        public int maxTokens = 150;

        [Tooltip("Temperatura para generación (0.0 = determinista, 1.0 = creativo)")]
        [Range(0f, 1f)]
        public float temperature = 0.7f;

        #endregion

        #region Hybrid AI Strategy

        [Header("Estrategia Híbrida Local/API")]
        [Tooltip("Usar Gemini para respuestas complejas, MiniLM local para simples")]
        public bool useHybridApproach = true;

        [Tooltip("Priorizar respuestas locales para ahorrar costos")]
        public bool prioritizeLocalResponses = true;

        [Tooltip("Usar cache para respuestas comunes")]
        public bool enableResponseCache = true;

        [Tooltip("Tiempo de cache en minutos")]
        public int cacheDurationMinutes = 60;

        [Tooltip("Máximo de entradas en cache")]
        public int maxCacheEntries = 100;

        #endregion

        #region Intervention Settings

        [Header("Configuración de Intervenciones")]
        [Tooltip("Frecuencia de intervención de la IA")]
        public InteractionPreference defaultInteractionPreference = InteractionPreference.Equilibrado;

        [Tooltip("Tiempo de inactividad antes de intervenir (segundos)")]
        public float inactivityThreshold = 10f;

        [Tooltip("Tiempo máximo en misión antes de ofrecer ayuda (segundos)")]
        public float stuckThreshold = 120f;

        [Tooltip("Número de fallos antes de intervenir")]
        public int failureThreshold = 3;

        [Tooltip("Mostrar celebraciones al desbloquear logros")]
        public bool enableCelebrations = true;

        [Tooltip("Mostrar recomendaciones proactivas")]
        public bool enableProactiveRecommendations = true;

        #endregion

        #region UI Settings

        [Header("Configuración de UI")]
        [Tooltip("Posición del icono flotante")]
        public FloatingIconPosition iconPosition = FloatingIconPosition.BottomRight;

        [Tooltip("Tamaño del icono flotante")]
        [Range(40f, 100f)]
        public float iconSize = 60f;

        [Tooltip("Activar animaciones del icono")]
        public bool enableIconAnimations = true;

        [Tooltip("Mostrar badge de notificaciones")]
        public bool showNotificationBadge = true;

        [Tooltip("Velocidad de tipeo para efecto de escritura (caracteres por segundo)")]
        [Range(10f, 100f)]
        public float typingSpeed = 50f;

        #endregion

        #region Voice Settings

        [Header("Configuración de Voz")]
        [Tooltip("Activar narración por voz")]
        public bool enableVoiceNarration = true;

        [Tooltip("Usar voz en misiones AR")]
        public bool voiceInAR = true;

        [Tooltip("Usar voz en juegos")]
        public bool voiceInGames = false;

        [Tooltip("Volumen de la voz (0-1)")]
        [Range(0f, 1f)]
        public float voiceVolume = 0.8f;

        [Tooltip("Velocidad de la voz (0.5-2.0)")]
        [Range(0.5f, 2f)]
        public float voiceSpeed = 1f;

        #endregion

        #region Analytics Settings

        [Header("Configuración de Analítica")]
        [Tooltip("Activar detección de estilo de aprendizaje")]
        public bool enableLearningStyleDetection = true;

        [Tooltip("Activar análisis de rendimiento")]
        public bool enablePerformanceAnalysis = true;

        [Tooltip("Mínimo de misiones para detectar patrones")]
        public int minMissionsForPatternDetection = 5;

        [Tooltip("Activar recomendaciones personalizadas")]
        public bool enablePersonalizedRecommendations = true;

        #endregion

        #region Debug Settings

        [Header("Configuración de Debug")]
        [Tooltip("Activar logs detallados")]
        public bool enableDebugLogs = true;

        [Tooltip("Mostrar prompts enviados a Gemini")]
        public bool showGeminiPrompts = false;

        [Tooltip("Simular respuestas de Gemini (sin gastar API)")]
        public bool simulateGeminiResponses = false;

        #endregion

        #region Localization

        [Header("Localización")]
        [Tooltip("Idioma por defecto")]
        public string defaultLanguage = "es";

        [Tooltip("Idiomas soportados")]
        public string[] supportedLanguages = { "es", "en" };

        #endregion

        /// <summary>
        /// Valida la configuración
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(geminiApiKey))
            {
                Debug.LogWarning("Quantum AI: API Key de Gemini no configurada. Solo funcionará en modo local.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Obtiene la URL completa de la API con el modelo
        /// </summary>
        public string GetGeminiEndpoint()
        {
            string cleanUrl = geminiApiUrl.Trim();
            string cleanModel = geminiModel.Trim();
            string cleanKey = geminiApiKey.Trim();

            // Asegura que la URL termine en '/' si no lo tiene
            if (!cleanUrl.EndsWith("/"))
            {
                cleanUrl += "/";
            }

            return $"{geminiApiUrl}{geminiModel}:generateContent?key={geminiApiKey}";
        }
    }

    /// <summary>
    /// Posición del icono flotante
    /// </summary>
    public enum FloatingIconPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom
    }
}
