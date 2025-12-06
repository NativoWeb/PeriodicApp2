using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using QuantumAI.Models;
using QuantumAI.Config;
using SimpleJSON;

namespace QuantumAI.Services
{
    /// <summary>
    /// Servicio de comunicación con la API de Google Gemini
    /// Maneja requests, respuestas, errores y fallbacks
    /// </summary>
    public class GeminiService : MonoBehaviour
    {
        private AIConfig config;
        private GeminiPromptBuilder promptBuilder;

        private void Awake()
        {
            promptBuilder = gameObject.AddComponent<GeminiPromptBuilder>();
        }

        /// <summary>
        /// Genera una respuesta usando Gemini API
        /// </summary>
        public IEnumerator GenerateResponse(
            string userInput,
            StudentContext context,
            Action<string> onComplete)
        {
            config = Core.QuantumAICore.Instance.Config;

            if (config == null || string.IsNullOrEmpty(config.geminiApiKey))
            {
                Debug.LogError("Gemini API Key no configurada");
                onComplete?.Invoke(null);
                yield break;
            }

            // Construir el prompt contextual
            string systemPrompt = promptBuilder.BuildSystemPrompt(context);
            string fullPrompt = promptBuilder.BuildConversationalPrompt(userInput, context, systemPrompt);

            if (config.showGeminiPrompts)
            {
                Debug.Log($"Prompt enviado a Gemini:\n{fullPrompt}");
            }

            // Simular respuesta si está en modo simulación
            if (config.simulateGeminiResponses)
            {
                yield return new WaitForSeconds(0.5f);
                onComplete?.Invoke(GetSimulatedResponse(userInput));
                yield break;
            }

            // Preparar request
            string endpoint = config.GetGeminiEndpoint();
            Debug.Log("⚠️ USANDO URL HARDCODED: " + endpoint);
            string requestBody = BuildRequestBody(fullPrompt);

            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = config.apiTimeout;

                // Enviar request
                yield return request.SendWebRequest();

                // Manejar respuesta
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = ParseGeminiResponse(request.downloadHandler.text);
                    onComplete?.Invoke(response);
                }
                else
                {
                    Debug.LogError($"Error en Gemini API: {request.error}");
                    Debug.LogError($"Response Code: {request.responseCode}");
                    Debug.LogError($"Response: {request.downloadHandler.text}");
                    onComplete?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Construye el cuerpo del request para Gemini API
        /// </summary>
        private string BuildRequestBody(string prompt)
        {
            JSONObject requestJson = new JSONObject();

            // Contents array
            JSONArray contentsArray = new JSONArray();
            JSONObject contentObj = new JSONObject();
            JSONArray partsArray = new JSONArray();
            JSONObject partObj = new JSONObject();
            partObj.Add("text", prompt);
            partsArray.Add(partObj);
            contentObj.Add("parts", partsArray);
            contentsArray.Add(contentObj);
            requestJson.Add("contents", contentsArray);

            // Generation config
            JSONObject generationConfig = new JSONObject();
            generationConfig.Add("temperature", config.temperature);
            generationConfig.Add("maxOutputTokens", config.maxTokens);
            requestJson.Add("generationConfig", generationConfig);

            // Safety settings (permitir contenido educativo)
            JSONArray safetySettings = new JSONArray();
            string[] categories = {
                "HARM_CATEGORY_HARASSMENT",
                "HARM_CATEGORY_HATE_SPEECH",
                "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                "HARM_CATEGORY_DANGEROUS_CONTENT"
            };

            foreach (string category in categories)
            {
                JSONObject safety = new JSONObject();
                safety.Add("category", category);
                safety.Add("threshold", "BLOCK_ONLY_HIGH");
                safetySettings.Add(safety);
            }
            requestJson.Add("safetySettings", safetySettings);

            return requestJson.ToString();
        }

        /// <summary>
        /// Parsea la respuesta de Gemini
        /// </summary>
        private string ParseGeminiResponse(string jsonResponse)
        {
            try
            {
                JSONNode responseNode = JSON.Parse(jsonResponse);

                // Navegar por la estructura de respuesta de Gemini
                if (responseNode["candidates"] != null && responseNode["candidates"].Count > 0)
                {
                    var candidate = responseNode["candidates"][0];
                    if (candidate["content"] != null && candidate["content"]["parts"] != null)
                    {
                        var parts = candidate["content"]["parts"];
                        if (parts.Count > 0 && parts[0]["text"] != null)
                        {
                            return parts[0]["text"].Value;
                        }
                    }
                }

                Debug.LogWarning("No se pudo extraer texto de la respuesta de Gemini");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parseando respuesta de Gemini: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Genera una respuesta simulada para testing sin gastar API
        /// </summary>
        private string GetSimulatedResponse(string userInput)
        {
            string lower = userInput.ToLower();

            if (lower.Contains("hola") || lower.Contains("hello"))
            {
                return "¡Hola! Soy Quantum, tu asistente de aprendizaje de química. ¿En qué puedo ayudarte hoy?";
            }

            if (lower.Contains("oxígeno") || lower.Contains("oxigeno"))
            {
                return "El Oxígeno (O) es un elemento fascinante. Es el tercer elemento más abundante del universo y esencial para la vida. Tiene número atómico 8 y está en el grupo 16 de la tabla periódica. ¿Quieres saber más sobre sus propiedades o sus usos?";
            }

            if (lower.Contains("ayuda") || lower.Contains("help"))
            {
                return "¡Claro! Puedo ayudarte con:\n- Información sobre elementos químicos\n- Explicaciones de conceptos\n- Guía en misiones y juegos\n- Recomendaciones personalizadas\n¿Qué te interesa explorar?";
            }

            return "[Respuesta simulada de Gemini] Entiendo tu pregunta. En modo de producción, Gemini generaría una respuesta personalizada y contextual basada en tu perfil de aprendizaje.";
        }

        /// <summary>
        /// Genera una respuesta específica para un tipo de intervención
        /// </summary>
        public IEnumerator GenerateInterventionResponse(
            InterventionType type,
            StudentContext context,
            object additionalData,
            Action<string> onComplete)
        {
            string prompt = promptBuilder.BuildInterventionPrompt(type, context, additionalData);

            yield return GenerateResponse(prompt, context, onComplete);
        }
    }

    /// <summary>
    /// Tipos de intervenciones de la IA
    /// </summary>
    public enum InterventionType
    {
        MissionGuidance,        // Guía al iniciar misión
        GameHint,               // Pista en juego
        MissionFeedback,        // Feedback al completar misión
        AchievementCelebration, // Celebración de logro
        ProgressMotivation,     // Motivación de progreso
        StuckHelp,              // Ayuda cuando está atascado
        DailyGreeting,          // Saludo diario
        ConceptExplanation,     // Explicación de concepto
        Recommendation          // Recomendación personalizada
    }
}
