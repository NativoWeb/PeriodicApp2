using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuantumAI.Models;
using QuantumAI.Config;
using QuantumAI.Services;

namespace QuantumAI.Core
{
    /// <summary>
    /// Núcleo central del sistema de IA Quantum
    /// Singleton persistente que coordina todas las funcionalidades de IA
    /// Persiste entre escenas y mantiene el contexto del estudiante
    /// </summary>
    public class QuantumAICore : MonoBehaviour
    {
        #region Singleton

        private static QuantumAICore _instance;
        public static QuantumAICore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<QuantumAICore>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("QuantumAICore");
                        _instance = go.AddComponent<QuantumAICore>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration

        [Header("Configuración")]
        [SerializeField] private AIConfig config;

        public AIConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = Resources.Load<AIConfig>("QuantumAIConfig");
                    if (config == null)
                    {
                        Debug.LogError("Quantum AI: No se encontró AIConfig. Crea uno en Assets > Create > Quantum AI > AI Config");
                    }
                }
                return config;
            }
        }

        #endregion

        #region Services

        private GeminiService geminiService;
        private MiniLMEmbedder localEmbedder;
        private ResponseCache responseCache;

        #endregion

        #region Student Context

        private StudentContext studentContext;
        public StudentContext Context => studentContext;

        #endregion

        #region Events

        // Eventos para que otros sistemas se suscriban
        public event Action<string> OnAIResponse;
        public event Action<string, string> OnMissionStarted;
        public event Action<string, bool> OnMissionCompleted;
        public event Action<int, string> OnXPGained;
        public event Action<string> OnAchievementUnlocked;
        public event Action<string> OnSceneChanged;

        #endregion

        #region Initialization

        private void Awake()
        {
            // Implementar patrón Singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Inicializar servicios
            InitializeServices();

            // Cargar contexto del estudiante
            LoadStudentContext();

            // Suscribirse a cambios de escena
            SceneManager.sceneLoaded += OnSceneLoaded;

            Log("Quantum AI Core inicializado correctamente");
        }

        private void InitializeServices()
        {
            // Inicializar servicio de Gemini
            geminiService = gameObject.AddComponent<GeminiService>();

            // Buscar el embedder local (MiniLM)
            localEmbedder = FindObjectOfType<MiniLMEmbedder>();
            if (localEmbedder == null)
            {
                Log("MiniLM Embedder no encontrado. Funcionará solo con Gemini API.", LogType.Warning);
            }

            // Inicializar cache
            responseCache = gameObject.AddComponent<ResponseCache>();

            Log("Servicios de IA inicializados");
        }

        private void LoadStudentContext()
        {
            studentContext = new StudentContext();

            // Cargar datos desde Firebase/PlayerPrefs
            if (DbConnexion.Instance != null)
            {
                LoadFromFirebase();
            }
            else
            {
                LoadFromPlayerPrefs();
            }

            Log($"Contexto cargado para: {studentContext.displayName}");
        }

        private void LoadFromPlayerPrefs()
        {
            studentContext.userId = PlayerPrefs.GetString("userId", "");
            studentContext.displayName = PlayerPrefs.GetString("DisplayName", "Estudiante");
            studentContext.xp = PlayerPrefs.GetInt("TempXP", 0);
            studentContext.rango = PlayerPrefs.GetString("Rango", "Aprendiz Atomico");
            studentContext.rachaActual = PlayerPrefs.GetInt("rachaActual", 0);
            studentContext.ocupacion = PlayerPrefs.GetString("TempOcupacion", "Estudiante");

            // Intentar cargar fecha de último login
            string fechaStr = PlayerPrefs.GetString("ultimaFecha", "");
            if (!string.IsNullOrEmpty(fechaStr) && DateTime.TryParse(fechaStr, out DateTime fecha))
            {
                studentContext.ultimaFechaLogin = fecha;
            }
        }

        private void LoadFromFirebase()
        {
            // TODO: Implementar carga desde Firebase cuando esté online
            // Por ahora, fallback a PlayerPrefs
            LoadFromPlayerPrefs();
        }

        #endregion

        #region Scene Management

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Actualizar contexto con nueva escena
            studentContext.currentScene = scene.name;
            studentContext.tiempoEnEscenaActual = 0f;

            Log($"Escena cargada: {scene.name}");

            // Notificar a listeners
            OnSceneChanged?.Invoke(scene.name);

            // Intervenir si es apropiado
            HandleSceneEntry(scene.name);
        }

        private void HandleSceneEntry(string sceneName)
        {
            // Lógica de intervención al entrar a una escena
            if (sceneName == "Inicio")
            {
                HandleDashboardEntry();
            }
            else if (sceneName.Contains("Categorías"))
            {
                HandleCategorySelection();
            }
            else if (sceneName.Contains("Vuforia"))
            {
                HandleARMissionStart();
            }
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            if (studentContext != null)
            {
                // Actualizar tiempo en escena
                studentContext.ActualizarTiempoEnEscena(Time.deltaTime);

                // Verificar si el estudiante está atascado
                if (studentContext.EstaAtascado())
                {
                    OnStudentStuck();
                }
            }
        }

        #endregion

        #region Public API - Mission Events

        /// <summary>
        /// Notifica que una misión ha comenzado
        /// </summary>
        public void NotifyMissionStarted(string elementName, string missionType, int missionId)
        {
            studentContext.currentElement = elementName;
            studentContext.currentMissionType = missionType;
            studentContext.currentMissionId = missionId;
            studentContext.tiempoEnEscenaActual = 0f;

            Log($"Misión iniciada: {missionType} para {elementName}");

            OnMissionStarted?.Invoke(elementName, missionType);

            // Generar mensaje de orientación
            StartCoroutine(GenerateMissionGuidance(elementName, missionType));
        }

        /// <summary>
        /// Notifica que una misión se completó
        /// </summary>
        public void NotifyMissionCompleted(string elementName, int missionId, bool success)
        {
            if (success)
            {
                studentContext.misionesTotalesCompletadas++;
                studentContext.RegistrarIntentoMision(missionId.ToString(), true);
            }

            Log($"Misión completada: {elementName} - Éxito: {success}");

            OnMissionCompleted?.Invoke(elementName, success);

            // Generar feedback
            StartCoroutine(GenerateMissionFeedback(elementName, success));
        }

        #endregion

        #region Public API - Game Events

        /// <summary>
        /// Notifica un fallo en un juego
        /// </summary>
        public void NotifyGameFailure(string gameType, string attemptDetails, int failureCount)
        {
            Log($"Fallo en juego {gameType}: {attemptDetails} (Fallos: {failureCount})");

            // Si supera el threshold, ofrecer ayuda
            if (failureCount >= Config.failureThreshold)
            {
                StartCoroutine(GenerateGameHint(gameType, attemptDetails));
            }
        }

        /// <summary>
        /// Notifica éxito en un juego
        /// </summary>
        public void NotifyGameSuccess(string gameType, float timeSpent, int score)
        {
            Log($"Juego completado: {gameType} - Tiempo: {timeSpent}s - Score: {score}");

            // Generar celebración o feedback positivo
            StartCoroutine(GenerateGameCelebration(gameType, score));
        }

        #endregion

        #region Public API - Progress Events

        /// <summary>
        /// Notifica ganancia de XP
        /// </summary>
        public void NotifyXPGained(int amount, string reason, int totalXP)
        {
            int xpAnterior = studentContext.xp;
            studentContext.xp = totalXP;

            Log($"+{amount} XP ({reason}). Total: {totalXP}");

            OnXPGained?.Invoke(amount, reason);

            // Verificar si está cerca de subir de rango
            int xpParaSiguiente = studentContext.XPParaSiguienteRango();
            if (xpParaSiguiente > 0 && xpParaSiguiente <= 100)
            {
                StartCoroutine(GenerateRankUpMotivation(xpParaSiguiente));
            }
        }

        /// <summary>
        /// Notifica desbloqueo de logro
        /// </summary>
        public void NotifyAchievementUnlocked(string achievementName, int xpAwarded)
        {
            studentContext.logrosDesbloqueados++;

            Log($"Logro desbloqueado: {achievementName} (+{xpAwarded} XP)");

            OnAchievementUnlocked?.Invoke(achievementName);

            // Generar celebración
            if (Config.enableCelebrations)
            {
                StartCoroutine(GenerateAchievementCelebration(achievementName, xpAwarded));
            }
        }

        #endregion

        #region Public API - Chat

        /// <summary>
        /// Procesa una pregunta del usuario
        /// </summary>
        public void ProcessUserQuestion(string question)
        {
            studentContext.AgregarConversacion(question, true);
            StartCoroutine(GenerateResponse(question));
        }

        #endregion

        #region AI Response Generation

        private IEnumerator GenerateResponse(string userInput)
        {
            // Decidir si usar Gemini o respuestas locales
            bool useGemini = ShouldUseGemini(userInput);

            if (useGemini)
            {
                yield return StartCoroutine(GenerateGeminiResponse(userInput));
            }
            else
            {
                string localResponse = GenerateLocalResponse(userInput);
                DeliverResponse(localResponse);
            }
        }

        private IEnumerator GenerateGeminiResponse(string userInput)
        {
            Log("Generando respuesta con Gemini...");

            yield return StartCoroutine(geminiService.GenerateResponse(
                userInput,
                studentContext,
                (response) =>
                {
                    if (!string.IsNullOrEmpty(response))
                    {
                        DeliverResponse(response);
                    }
                    else
                    {
                        // Fallback a respuesta local
                        string fallback = GenerateLocalResponse(userInput);
                        DeliverResponse(fallback);
                    }
                }
            ));
        }

        private string GenerateLocalResponse(string userInput)
        {
            // TODO: Implementar respuestas locales usando MiniLM
            // Por ahora, respuesta genérica
            return "Entiendo tu pregunta. ¿Puedes ser más específico sobre qué elemento químico te interesa?";
        }

        private void DeliverResponse(string response)
        {
            studentContext.AgregarConversacion(response, false);
            OnAIResponse?.Invoke(response);
            Log($"Respuesta entregada: {response.Substring(0, Math.Min(50, response.Length))}...");
        }

        #endregion

        #region Intervention Handlers

        private void HandleDashboardEntry()
        {
            if (!Config.enableProactiveRecommendations) return;

            StartCoroutine(GenerateDashboardGreeting());
        }

        private void HandleCategorySelection()
        {
            // Sugerencias al entrar a categorías
        }

        private void HandleARMissionStart()
        {
            // Instrucciones para misión AR
        }

        private void OnStudentStuck()
        {
            // El estudiante lleva mucho tiempo sin progresar
            if (Config.enableProactiveRecommendations)
            {
                StartCoroutine(GenerateStuckHint());
            }
        }

        #endregion

        #region Message Generators (placeholders)

        private IEnumerator GenerateMissionGuidance(string element, string missionType)
        {
            string message = $"Iniciando misión de {missionType} para {element}. ¡Buena suerte!";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateMissionFeedback(string element, bool success)
        {
            string message = success
                ? $"¡Excelente! Completaste la misión de {element}."
                : $"No te preocupes, inténtalo de nuevo con {element}.";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateGameHint(string gameType, string details)
        {
            string message = $"Veo que tienes dificultades en {gameType}. ¿Necesitas una pista?";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateGameCelebration(string gameType, int score)
        {
            string message = $"¡Increíble! Completaste {gameType} con {score} puntos.";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateRankUpMotivation(int xpNeeded)
        {
            string message = $"¡Estás a solo {xpNeeded} XP de subir de rango! ¿Seguimos?";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateAchievementCelebration(string achievement, int xp)
        {
            string message = $"🏆 ¡Logro desbloqueado: {achievement}! +{xp} XP";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateDashboardGreeting()
        {
            // Array de mensajes motivacionales aleatorios
            string[] mensajesMotivacionales = new string[]
            {
                $"¡Bienvenido de vuelta, {studentContext.displayName}! Cada misión te acerca más a dominar la química ⚗️",
                $"¡Sigue así, {studentContext.displayName}! Tu progreso es increíble 🌟",
                $"¡Listo para una nueva aventura química, {studentContext.displayName}? 🚀",
                $"¡Excelente verte de nuevo, {studentContext.displayName}! ¿Qué descubrirás hoy? 🔬",
                $"¡Tu dedicación inspira, {studentContext.displayName}! Continuemos aprendiendo 💪",
                $"¡Cada elemento aprendido es un paso hacia el éxito, {studentContext.displayName}! 🎯",
                $"¡Bienvenido, {studentContext.displayName}! La química espera por ti ✨"
            };

            // Seleccionar un mensaje aleatorio
            string message = mensajesMotivacionales[UnityEngine.Random.Range(0, mensajesMotivacionales.Length)];
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateStuckHint()
        {
            string message = "¿Necesitas ayuda con esta misión?";
            DeliverResponse(message);
            yield return null;
        }

        #endregion

        #region Utility Methods

        private bool ShouldUseGemini(string input)
        {
            if (!Config.useHybridApproach) return true;
            if (string.IsNullOrEmpty(Config.geminiApiKey)) return false;

            // Verificar cache primero
            if (Config.enableResponseCache && responseCache.HasResponse(input))
            {
                return false;
            }

            // Preguntas que claramente necesitan Gemini
            string lower = input.ToLower();

            // Preguntas con "por qué", "cómo", etc. siempre usan Gemini
            if (lower.Contains("por qué") || lower.Contains("por que") ||
                lower.Contains("cómo") || lower.Contains("como") ||
                lower.Contains("explica") || lower.Contains("explicame"))
            {
                Log("Usando Gemini: pregunta conceptual detectada");
                return true;
            }

            // Preguntas largas (más de 30 caracteres)
            if (input.Length > 30)
            {
                Log("Usando Gemini: pregunta larga detectada");
                return true;
            }

            Log("Usando respuesta local: pregunta simple");
            return false;
        }

        private void Log(string message, LogType type = LogType.Log)
        {
            if (!Config.enableDebugLogs) return;

            switch (type)
            {
                case LogType.Log:
                    Debug.Log($"[Quantum AI] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[Quantum AI] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[Quantum AI] {message}");
                    break;
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion
    }
}
