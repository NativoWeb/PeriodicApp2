using System.Text;
using UnityEngine;
using QuantumAI.Models;

namespace QuantumAI.Services
{
    /// <summary>
    /// Constructor de prompts contextuales para Gemini
    /// Crea prompts especializados según el contexto del estudiante y tipo de intervención
    /// </summary>
    public class GeminiPromptBuilder : MonoBehaviour
    {
        /// <summary>
        /// Construye el prompt de sistema base con personalidad y contexto de Quantum
        /// </summary>
        public string BuildSystemPrompt(StudentContext context)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Eres 'Quantum', un asistente de IA educativo especializado en química y la tabla periódica.");
            sb.AppendLine("Tu misión es guiar, motivar y educar a estudiantes de forma personalizada.");
            sb.AppendLine();

            // Personalidad y estilo
            sb.AppendLine("PERSONALIDAD:");
            sb.AppendLine("- Amigable, entusiasta y motivador");
            sb.AppendLine("- Paciente y comprensivo con las dificultades");
            sb.AppendLine("- Usa analogías y ejemplos del mundo real");
            sb.AppendLine("- Celebra los logros del estudiante");
            sb.AppendLine("- Adapta tu lenguaje al nivel del estudiante");
            sb.AppendLine();

            // Información del estudiante
            sb.AppendLine("ESTUDIANTE ACTUAL:");
            sb.AppendLine($"- Nombre: {context.displayName}");
            sb.AppendLine($"- Rango: {context.rango}");
            sb.AppendLine($"- XP Total: {context.xp}");
            sb.AppendLine($"- Misiones completadas: {context.misionesTotalesCompletadas}");
            sb.AppendLine($"- Logros desbloqueados: {context.logrosDesbloqueados}");
            sb.AppendLine($"- Racha actual: {context.rachaActual} días");
            sb.AppendLine($"- Nivel de dificultad: {context.nivelDificultadPreferido}");
            sb.AppendLine($"- Estilo de aprendizaje: {context.estiloAprendizaje}");
            sb.AppendLine();

            // Contexto actual
            if (!string.IsNullOrEmpty(context.currentScene))
            {
                sb.AppendLine("CONTEXTO ACTUAL:");
                sb.AppendLine($"- Escena: {context.currentScene}");
                if (!string.IsNullOrEmpty(context.currentElement))
                {
                    sb.AppendLine($"- Elemento en estudio: {context.currentElement}");
                }
                if (!string.IsNullOrEmpty(context.currentMissionType))
                {
                    sb.AppendLine($"- Tipo de misión: {context.currentMissionType}");
                }
                sb.AppendLine();
            }

            // Fortalezas y debilidades
            if (context.fortalezas.Count > 0)
            {
                sb.AppendLine($"FORTALEZAS DEL ESTUDIANTE: {string.Join(", ", context.fortalezas)}");
            }
            if (context.temasConDificultad.Count > 0)
            {
                sb.AppendLine($"ÁREAS DE MEJORA: {string.Join(", ", context.temasConDificultad)}");
            }
            sb.AppendLine();

            // Instrucciones de formato
            sb.AppendLine("INSTRUCCIONES:");
            sb.AppendLine("- Responde en español claro y conciso");
            sb.AppendLine("- Máximo 3-4 oraciones por respuesta (a menos que se solicite más detalle)");
            sb.AppendLine("- Usa emojis ocasionalmente para hacer la conversación más amigable");
            sb.AppendLine("- Si el estudiante pregunta sobre un elemento, primero confirma cuál es antes de explicar");
            sb.AppendLine("- Siempre ofrece continuar con más información o pasar a la acción");
            sb.AppendLine("- No inventes datos sobre elementos químicos - usa solo información real");

            return sb.ToString();
        }

        /// <summary>
        /// Construye un prompt conversacional estándar
        /// </summary>
        public string BuildConversationalPrompt(string userInput, StudentContext context, string systemPrompt)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(systemPrompt);
            sb.AppendLine();

            // Incluir historial reciente (últimas 5 interacciones)
            if (context.historialConversacion.Count > 0)
            {
                sb.AppendLine("HISTORIAL RECIENTE:");
                int start = Mathf.Max(0, context.historialConversacion.Count - 5);
                for (int i = start; i < context.historialConversacion.Count; i++)
                {
                    var entry = context.historialConversacion[i];
                    string role = entry.esUsuario ? "Estudiante" : "Quantum";
                    sb.AppendLine($"{role}: {entry.mensaje}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"Estudiante: {userInput}");
            sb.AppendLine("Quantum:");

            return sb.ToString();
        }

        /// <summary>
        /// Construye un prompt para una intervención específica
        /// </summary>
        public string BuildInterventionPrompt(InterventionType type, StudentContext context, object additionalData)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(BuildSystemPrompt(context));
            sb.AppendLine();

            switch (type)
            {
                case InterventionType.MissionGuidance:
                    sb.AppendLine(BuildMissionGuidancePrompt(context, additionalData));
                    break;

                case InterventionType.GameHint:
                    sb.AppendLine(BuildGameHintPrompt(context, additionalData));
                    break;

                case InterventionType.MissionFeedback:
                    sb.AppendLine(BuildMissionFeedbackPrompt(context, additionalData));
                    break;

                case InterventionType.AchievementCelebration:
                    sb.AppendLine(BuildAchievementPrompt(context, additionalData));
                    break;

                case InterventionType.ProgressMotivation:
                    sb.AppendLine(BuildProgressMotivationPrompt(context, additionalData));
                    break;

                case InterventionType.StuckHelp:
                    sb.AppendLine(BuildStuckHelpPrompt(context));
                    break;

                case InterventionType.DailyGreeting:
                    sb.AppendLine(BuildDailyGreetingPrompt(context));
                    break;

                case InterventionType.ConceptExplanation:
                    sb.AppendLine(BuildConceptExplanationPrompt(context, additionalData));
                    break;

                case InterventionType.Recommendation:
                    sb.AppendLine(BuildRecommendationPrompt(context));
                    break;
            }

            return sb.ToString();
        }

        #region Specific Prompt Builders

        private string BuildMissionGuidancePrompt(StudentContext context, object data)
        {
            return $@"TAREA: El estudiante acaba de iniciar una misión de tipo '{context.currentMissionType}'
para el elemento '{context.currentElement}'.

Genera un mensaje breve (2-3 oraciones) que:
1. Explique el objetivo de la misión
2. De un consejo útil específico para este tipo de misión
3. Motive al estudiante

Responde de forma directa y entusiasta.";
        }

        private string BuildGameHintPrompt(StudentContext context, object data)
        {
            string gameDetails = data?.ToString() ?? "juego actual";

            return $@"TAREA: El estudiante ha fallado varias veces en un juego.
Detalles: {gameDetails}

Genera una pista sutil (2 oraciones) que:
1. Reconozca la dificultad sin hacer sentir mal al estudiante
2. De una pista útil SIN revelar la respuesta completa
3. Anime a seguir intentando

No des la respuesta directa, solo guía el razonamiento.";
        }

        private string BuildMissionFeedbackPrompt(StudentContext context, object data)
        {
            bool success = data != null && (bool)data;

            if (success)
            {
                return $@"TAREA: El estudiante completó exitosamente la misión de '{context.currentElement}'.

Genera un mensaje de felicitación (2-3 oraciones) que:
1. Celebre el logro específico
2. Mencione algo interesante del elemento estudiado
3. Sugiera el siguiente paso de aprendizaje";
            }
            else
            {
                return $@"TAREA: El estudiante no completó la misión de '{context.currentElement}'.

Genera un mensaje de ánimo (2-3 oraciones) que:
1. Normalice el fallo como parte del aprendizaje
2. Sugiera revisar un concepto específico
3. Motive a intentar de nuevo";
            }
        }

        private string BuildAchievementPrompt(StudentContext context, object data)
        {
            string achievementName = data?.ToString() ?? "un nuevo logro";

            return $@"TAREA: El estudiante acaba de desbloquear el logro '{achievementName}'.

Genera una celebración entusiasta (3-4 oraciones) que:
1. Felicite efusivamente con emojis apropiados
2. Explique la importancia de este logro
3. Mencione cómo esto refleja su progreso
4. Sugiera el próximo desafío

Usa un tono muy positivo y motivador.";
        }

        private string BuildProgressMotivationPrompt(StudentContext context, object data)
        {
            int xpNeeded = context.XPParaSiguienteRango();

            return $@"TAREA: El estudiante está a {xpNeeded} XP de subir de rango.

Genera un mensaje motivacional (2-3 oraciones) que:
1. Resalte lo cerca que está del siguiente rango
2. Sugiera misiones específicas para alcanzarlo pronto
3. Cree un sentido de urgencia positiva

Sé entusiasta y específico con las sugerencias.";
        }

        private string BuildStuckHelpPrompt(StudentContext context)
        {
            return $@"TAREA: El estudiante lleva más de 2 minutos en la misión actual de '{context.currentMissionType}'
sin progresar.

Genera una oferta de ayuda (2 oraciones) que:
1. Ofrezca ayuda sin ser invasivo
2. Sugiera una acción específica que podría ayudar

Sé empático y útil.";
        }

        private string BuildDailyGreetingPrompt(StudentContext context)
        {
            return $@"TAREA: El estudiante acaba de iniciar sesión.

Genera un saludo personalizado (2-3 oraciones) que:
1. Salude calurosamente por nombre
2. Mencione su racha actual ({context.rachaActual} días)
3. Sugiera una actividad basada en su progreso reciente

Sé amigable y proactivo.";
        }

        private string BuildConceptExplanationPrompt(StudentContext context, object data)
        {
            string concept = data?.ToString() ?? "este concepto";

            return $@"TAREA: El estudiante pregunta sobre '{concept}'.

Genera una explicación clara (3-4 oraciones) que:
1. Defina el concepto de forma simple
2. Use una analogía del mundo real
3. Relate el concepto con elementos químicos específicos
4. Verifique si necesita más detalle

Adapta la complejidad al nivel {context.nivelDificultadPreferido}.";
        }

        private string BuildRecommendationPrompt(StudentContext context)
        {
            return $@"TAREA: Recomienda la siguiente actividad para el estudiante.

Basándote en:
- Ha completado {context.misionesTotalesCompletadas} misiones
- Tiene {context.logrosDesbloqueados} logros
- Está en rango {context.rango}
- Sus fortalezas: {string.Join(", ", context.fortalezas)}
- Áreas de mejora: {string.Join(", ", context.temasConDificultad)}

Genera una recomendación personalizada (3 oraciones) que:
1. Sugiera una categoría o elemento específico
2. Explique por qué es la mejor opción ahora
3. Mencione el beneficio de completarlo

Sé específico y estratégico.";
        }

        #endregion
    }
}
