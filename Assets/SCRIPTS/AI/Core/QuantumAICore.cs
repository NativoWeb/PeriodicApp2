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

            string learningStyle = PlayerPrefs.GetString("LearningStyleMapped", "");
            if (!string.IsNullOrEmpty(learningStyle))
            {
                if (Enum.TryParse<LearningStyle>(learningStyle, out var parsed))
                    studentContext.estiloAprendizaje = parsed;
            }

            string fechaStr = PlayerPrefs.GetString("ultimaFecha", "");
            if (!string.IsNullOrEmpty(fechaStr) && DateTime.TryParse(fechaStr, out DateTime fecha))
            {
                studentContext.ultimaFechaLogin = fecha;
            }

            string appIdioma = PlayerPrefs.GetString("appIdioma", "español");
            studentContext.idiomaPreferido = (appIdioma == "ingles" || appIdioma == "english") ? "en" : "es";
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
            studentContext.currentScene = scene.name;
            studentContext.tiempoEnEscenaActual = 0f;
            _stuckNotified = false;

            Log($"Escena cargada: {scene.name}");
            OnSceneChanged?.Invoke(scene.name);
            HandleSceneEntry(scene.name);
        }

        private void HandleSceneEntry(string sceneName)
        {
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
            else if (sceneName.Contains("Encuesta") || sceneName.Contains("SeleccionarEncuesta"))
            {
                HandleEncuestaEntry(sceneName);
            }
            else if (sceneName.Contains("Quiz") || sceneName.Contains("Cuestionario"))
            {
                HandleQuizEntry();
            }
            else if (sceneName.Contains("Combate") || sceneName.Contains("TablaManía"))
            {
                HandleGameEntry(sceneName);
            }
        }

        #endregion

        #region Update Loop

        private bool _stuckNotified;
        private float _lastInterventionTime;
        private const float InterventionCooldown = 30f;

        private void Update()
        {
            if (studentContext == null) return;

            studentContext.ActualizarTiempoEnEscena(Time.deltaTime);

            if (!_stuckNotified && studentContext.EstaAtascado() && CanIntervene())
            {
                _stuckNotified = true;
                OnStudentStuck();
            }
        }

        private bool CanIntervene()
        {
            if (Time.time - _lastInterventionTime < InterventionCooldown) return false;
            _lastInterventionTime = Time.time;
            return true;
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
            if (!Config.enableProactiveRecommendations) return;

            string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");
            if (string.IsNullOrEmpty(categoria)) return;

            string prompt = $"El estudiante {studentContext.displayName} (rango: {studentContext.rango}, " +
                $"{studentContext.misionesTotalesCompletadas} misiones completadas) acaba de entrar a la categoria '{categoria}'. " +
                $"Dale un consejo breve (1-2 oraciones) sobre que encontrara en esta categoria y como abordarla. " +
                $"No uses emojis. Se directo y util.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Estas explorando {categoria}. Revisa cada elemento y completa sus misiones en orden."));
        }

        private void HandleARMissionStart()
        {
            if (!Config.enableProactiveRecommendations) return;

            string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
            string message = string.IsNullOrEmpty(elemento)
                ? "Apunta la camara al marcador del elemento para verlo en realidad aumentada."
                : $"Busca el marcador de {elemento} y apunta la camara hacia el. Podras ver su estructura atomica en 3D.";
            DeliverResponse(message);
        }

        private void HandleEncuestaEntry(string sceneName)
        {
            if (!Config.enableProactiveRecommendations) return;

            if (sceneName.Contains("SeleccionarEncuesta"))
                DeliverResponse("Completa las encuestas iniciales para personalizar tu experiencia de aprendizaje.");
            else
                DeliverResponse("Responde con sinceridad. Esto nos ayuda a adaptar el contenido a tu estilo de aprendizaje.");
        }

        private void HandleQuizEntry()
        {
            if (!Config.enableProactiveRecommendations) return;

            string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
            if (!string.IsNullOrEmpty(elemento))
            {
                string prompt = $"El estudiante va a hacer un quiz sobre el elemento '{elemento}'. " +
                    $"Dale un tip breve (1 oracion) sobre algo clave que deberia recordar de este elemento. " +
                    $"No uses emojis. Se conciso.";
                StartCoroutine(GenerateGeminiOrFallback(prompt,
                    $"Recuerda las propiedades principales de {elemento} antes de responder. Lee cada pregunta con calma."));
            }
        }

        private void HandleGameEntry(string sceneName)
        {
            if (!Config.enableProactiveRecommendations) return;

            string gameName = sceneName.Contains("Combate") ? "Combate Quimico" : "TablaManía";
            DeliverResponse($"Iniciando {gameName}. Usa lo que has aprendido sobre los elementos para ganar.");
        }

        private void OnStudentStuck()
        {
            if (!Config.enableProactiveRecommendations) return;

            string elemento = studentContext.currentElement ?? "";
            string tipo = studentContext.currentMissionType ?? "";

            string prompt = $"El estudiante {studentContext.displayName} lleva mas de 2 minutos atascado " +
                $"en una mision de tipo '{tipo}' sobre el elemento '{elemento}'. " +
                $"Dale una pista concreta y util (1-2 oraciones) sin darle la respuesta directa. " +
                $"No uses emojis. Se alentador pero practico.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Si tienes dificultades, revisa la informacion de {elemento} en la seccion de datos. Puedes volver e intentarlo de nuevo."));
        }

        #endregion

        #region New Intervention Points

        public void NotifyRankUp(string newRank, string previousRank)
        {
            Log($"Rank up: {previousRank} -> {newRank}");

            string prompt = $"El estudiante {studentContext.displayName} acaba de subir de rango. " +
                $"Paso de '{previousRank}' a '{newRank}'. Tiene {studentContext.xp} XP y " +
                $"{studentContext.misionesTotalesCompletadas} misiones completadas. " +
                $"Genera una felicitacion genuina (2-3 oraciones) por este logro. " +
                $"Menciona el nuevo rango y motivalo a seguir. No uses emojis.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Has alcanzado el rango de {newRank}. Tu progreso constante esta dando resultados. Sigue asi."));
        }

        public void NotifyStreakMilestone(int streakDays)
        {
            if (streakDays < 3) return;

            string prompt = $"El estudiante {studentContext.displayName} tiene una racha de {streakDays} dias consecutivos " +
                $"usando la app. Genera un reconocimiento breve (1-2 oraciones) por su constancia. " +
                $"No uses emojis. Se genuino.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Llevas {streakDays} dias consecutivos aprendiendo. La constancia es la clave del dominio."));
        }

        public void NotifyCategoryCompleted(string categoryName)
        {
            Log($"Categoria completada: {categoryName}");

            string prompt = $"El estudiante {studentContext.displayName} acaba de completar TODAS las misiones " +
                $"de la categoria '{categoryName}'. Tiene {studentContext.misionesTotalesCompletadas} misiones " +
                $"completadas en total. Genera una celebracion significativa (2-3 oraciones) que reconozca " +
                $"el esfuerzo de dominar una categoria completa. Sugiere la siguiente categoria. No uses emojis.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Has completado toda la categoria {categoryName}. Eso demuestra un dominio solido de estos elementos. Explora la siguiente categoria."));
        }

        public void NotifyFirstTimeElement(string elementName)
        {
            string prompt = $"El estudiante {studentContext.displayName} esta viendo el elemento '{elementName}' " +
                $"por primera vez. Dale un dato curioso breve (1 oracion) sobre este elemento que despierte su interes. " +
                $"No uses emojis. Usa informacion real.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Es tu primera vez con {elementName}. Explora sus misiones para conocer sus propiedades."));
        }

        public void NotifyQuizPerfectScore(string quizName, int totalQuestions)
        {
            string message = $"Puntuacion perfecta en {quizName}: {totalQuestions}/{totalQuestions} correctas. Dominas este tema.";
            DeliverResponse(message);
        }

        public void NotifyReturnAfterAbsence(int daysAway)
        {
            if (daysAway < 2) return;

            string prompt = $"El estudiante {studentContext.displayName} vuelve despues de {daysAway} dias sin usar la app. " +
                $"Su rango es '{studentContext.rango}' con {studentContext.misionesTotalesCompletadas} misiones completadas. " +
                $"Dale la bienvenida de forma breve (1-2 oraciones) y sugiere retomar donde lo dejo. " +
                $"No uses emojis. Se acogedor sin ser condescendiente.";
            StartCoroutine(GenerateGeminiOrFallback(prompt,
                $"Han pasado {daysAway} dias. Bienvenido de vuelta, {studentContext.displayName}. Retoma donde lo dejaste."));
        }

        #endregion

        #region Message Generators

        private IEnumerator GenerateGeminiOrFallback(string prompt, string fallback)
        {
            if (!string.IsNullOrEmpty(Config.geminiApiKey))
            {
                yield return StartCoroutine(geminiService.GenerateResponse(
                    prompt, studentContext,
                    (response) =>
                    {
                        DeliverResponse(!string.IsNullOrEmpty(response) ? response : fallback);
                    }
                ));
            }
            else
            {
                DeliverResponse(fallback);
            }
        }

        private IEnumerator GenerateMissionGuidance(string element, string missionType)
        {
            string prompt = $"El estudiante {studentContext.displayName} inicia una mision de tipo '{missionType}' " +
                $"para el elemento '{element}'. Dale una orientacion breve (1-2 oraciones) sobre que esperar " +
                $"y como completarla. No uses emojis.";
            yield return GenerateGeminiOrFallback(prompt,
                $"Mision de {missionType} para {element}. Presta atencion a los detalles para completarla.");
        }

        private IEnumerator GenerateMissionFeedback(string element, bool success)
        {
            if (success)
            {
                string prompt = $"El estudiante {studentContext.displayName} completo exitosamente una mision sobre '{element}'. " +
                    $"Lleva {studentContext.misionesTotalesCompletadas} misiones completadas. " +
                    $"Felicitalo brevemente (1 oracion) y sugiere el siguiente paso. No uses emojis.";
                yield return GenerateGeminiOrFallback(prompt,
                    $"Mision de {element} completada. Buen trabajo. Continua con la siguiente mision.");
            }
            else
            {
                string prompt = $"El estudiante {studentContext.displayName} no logro completar una mision sobre '{element}'. " +
                    $"Dale animo breve (1 oracion) y un consejo practico para su proximo intento. No uses emojis.";
                yield return GenerateGeminiOrFallback(prompt,
                    $"No te desanimes. Revisa la informacion de {element} y vuelve a intentarlo.");
            }
        }

        private IEnumerator GenerateGameHint(string gameType, string details)
        {
            string prompt = $"El estudiante fallo varias veces en el juego '{gameType}'. Detalles: {details}. " +
                $"Dale una pista util (1 oracion) sin revelar la respuesta. No uses emojis.";
            yield return GenerateGeminiOrFallback(prompt,
                $"Revisa los conceptos basicos del tema antes de volver a intentar {gameType}.");
        }

        private IEnumerator GenerateGameCelebration(string gameType, int score)
        {
            string message = $"Completaste {gameType} con {score} puntos. Bien hecho.";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateRankUpMotivation(int xpNeeded)
        {
            string message = $"Estas a solo {xpNeeded} XP de subir de rango. Una mision mas y lo consigues.";
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateAchievementCelebration(string achievement, int xp)
        {
            string prompt = $"El estudiante {studentContext.displayName} desbloqueo el logro '{achievement}' " +
                $"y gano {xp} XP. Genera una felicitacion breve (1-2 oraciones). No uses emojis.";
            yield return GenerateGeminiOrFallback(prompt,
                $"Logro desbloqueado: {achievement}. +{xp} XP. Sigue acumulando logros.");
        }

        private IEnumerator GenerateDashboardGreeting()
        {
            string[] saludos = new string[]
            {
                $"Bienvenido de vuelta, {studentContext.displayName}. Tienes {studentContext.misionesTotalesCompletadas} misiones completadas.",
                $"{studentContext.displayName}, tu rango actual es {studentContext.rango}. Veamos que puedes lograr hoy.",
                $"Bienvenido, {studentContext.displayName}. Continua desde donde lo dejaste.",
                $"{studentContext.displayName}, cada sesion cuenta. Elige tu siguiente mision.",
            };

            string message = saludos[UnityEngine.Random.Range(0, saludos.Length)];
            DeliverResponse(message);
            yield return null;
        }

        private IEnumerator GenerateStuckHint()
        {
            string elemento = studentContext.currentElement ?? "este tema";
            string tipo = studentContext.currentMissionType ?? "mision";

            string prompt = $"El estudiante lleva mas de 2 minutos atascado en {tipo} sobre '{elemento}'. " +
                $"Dale una pista practica y breve (1 oracion). No uses emojis.";
            yield return GenerateGeminiOrFallback(prompt,
                $"Si necesitas ayuda, revisa la seccion de informacion de {elemento}.");
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
