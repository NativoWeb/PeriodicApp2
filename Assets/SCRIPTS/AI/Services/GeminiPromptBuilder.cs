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
            bool en = context.idiomaPreferido == "en";
            StringBuilder sb = new StringBuilder();

            if (en)
            {
                sb.AppendLine("You are 'Quantum', an educational AI assistant specialized in chemistry and the periodic table.");
                sb.AppendLine("Your mission is to guide, motivate and educate students in a personalized way.");
            }
            else
            {
                sb.AppendLine("Eres 'Quantum', un asistente de IA educativo especializado en química y la tabla periódica.");
                sb.AppendLine("Tu misión es guiar, motivar y educar a estudiantes de forma personalizada.");
            }
            sb.AppendLine();

            // Personalidad y estilo
            if (en)
            {
                sb.AppendLine("PERSONALITY:");
                sb.AppendLine("- Friendly, enthusiastic and motivating");
                sb.AppendLine("- Patient and understanding with difficulties");
                sb.AppendLine("- Use analogies and real-world examples");
                sb.AppendLine("- Celebrate student achievements");
                sb.AppendLine("- Adapt your language to the student's level");
            }
            else
            {
                sb.AppendLine("PERSONALIDAD:");
                sb.AppendLine("- Amigable, entusiasta y motivador");
                sb.AppendLine("- Paciente y comprensivo con las dificultades");
                sb.AppendLine("- Usa analogías y ejemplos del mundo real");
                sb.AppendLine("- Celebra los logros del estudiante");
                sb.AppendLine("- Adapta tu lenguaje al nivel del estudiante");
            }
            sb.AppendLine();

            // Información del estudiante
            sb.AppendLine(en ? "CURRENT STUDENT:" : "ESTUDIANTE ACTUAL:");
            sb.AppendLine($"- {(en ? "Name" : "Nombre")}: {context.displayName}");
            sb.AppendLine($"- {(en ? "Rank" : "Rango")}: {context.rango}");
            sb.AppendLine($"- XP Total: {context.xp}");
            sb.AppendLine($"- {(en ? "Completed missions" : "Misiones completadas")}: {context.misionesTotalesCompletadas}");
            sb.AppendLine($"- {(en ? "Unlocked achievements" : "Logros desbloqueados")}: {context.logrosDesbloqueados}");
            sb.AppendLine($"- {(en ? "Current streak" : "Racha actual")}: {context.rachaActual} {(en ? "days" : "días")}");
            sb.AppendLine($"- {(en ? "Difficulty level" : "Nivel de dificultad")}: {context.nivelDificultadPreferido}");
            sb.AppendLine($"- {(en ? "Learning style" : "Estilo de aprendizaje")}: {context.estiloAprendizaje}");
            sb.AppendLine();

            // Contexto actual
            if (!string.IsNullOrEmpty(context.currentScene))
            {
                sb.AppendLine(en ? "CURRENT CONTEXT:" : "CONTEXTO ACTUAL:");
                sb.AppendLine($"- {(en ? "Scene" : "Escena")}: {context.currentScene}");
                if (!string.IsNullOrEmpty(context.currentElement))
                {
                    sb.AppendLine($"- {(en ? "Element being studied" : "Elemento en estudio")}: {context.currentElement}");
                }
                if (!string.IsNullOrEmpty(context.currentMissionType))
                {
                    sb.AppendLine($"- {(en ? "Mission type" : "Tipo de misión")}: {context.currentMissionType}");
                }
                sb.AppendLine();
            }

            // Fortalezas y debilidades
            if (context.fortalezas.Count > 0)
            {
                sb.AppendLine($"{(en ? "STUDENT STRENGTHS" : "FORTALEZAS DEL ESTUDIANTE")}: {string.Join(", ", context.fortalezas)}");
            }
            if (context.temasConDificultad.Count > 0)
            {
                sb.AppendLine($"{(en ? "AREAS FOR IMPROVEMENT" : "ÁREAS DE MEJORA")}: {string.Join(", ", context.temasConDificultad)}");
            }
            sb.AppendLine();

            // Instrucciones de formato
            if (en)
            {
                sb.AppendLine("INSTRUCTIONS:");
                sb.AppendLine("- Respond in clear and concise English");
                sb.AppendLine("- Maximum 3-4 sentences per response (unless more detail is requested)");
                sb.AppendLine("- Do not use emojis in your responses, keep a professional and clear tone");
                sb.AppendLine("- If the student asks about an element, confirm which one before explaining");
                sb.AppendLine("- Always offer to continue with more information or move to action");
                sb.AppendLine("- Do not make up data about chemical elements - use only real information");
            }
            else
            {
                sb.AppendLine("INSTRUCCIONES:");
                sb.AppendLine("- Responde en español claro y conciso");
                sb.AppendLine("- Máximo 3-4 oraciones por respuesta (a menos que se solicite más detalle)");
                sb.AppendLine("- No uses emojis en tus respuestas, manten un tono profesional y claro");
                sb.AppendLine("- Si el estudiante pregunta sobre un elemento, primero confirma cuál es antes de explicar");
                sb.AppendLine("- Siempre ofrece continuar con más información o pasar a la acción");
                sb.AppendLine("- No inventes datos sobre elementos químicos - usa solo información real");
            }

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
            bool en = context.idiomaPreferido == "en";
            if (en)
                return $@"TASK: The student just started a '{context.currentMissionType}' mission for the element '{context.currentElement}'.
Generate a brief message (2-3 sentences) that:
1. Explains the mission objective
2. Gives a useful tip specific to this mission type
3. Motivates the student
Respond directly and enthusiastically.";
            return $@"TAREA: El estudiante acaba de iniciar una misión de tipo '{context.currentMissionType}' para el elemento '{context.currentElement}'.
Genera un mensaje breve (2-3 oraciones) que:
1. Explique el objetivo de la misión
2. De un consejo útil específico para este tipo de misión
3. Motive al estudiante
Responde de forma directa y entusiasta.";
        }

        private string BuildGameHintPrompt(StudentContext context, object data)
        {
            bool en = context.idiomaPreferido == "en";
            string gameDetails = data?.ToString() ?? (en ? "current game" : "juego actual");
            if (en)
                return $@"TASK: The student has failed several times in a game.
Details: {gameDetails}
Generate a subtle hint (2 sentences) that:
1. Acknowledges the difficulty without making the student feel bad
2. Gives a useful hint WITHOUT revealing the full answer
3. Encourages them to keep trying
Do not give the direct answer, only guide their reasoning.";
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
            bool en = context.idiomaPreferido == "en";
            bool success = data != null && (bool)data;

            if (success)
            {
                if (en)
                    return $@"TASK: The student successfully completed the '{context.currentElement}' mission.
Generate a congratulations message (2-3 sentences) that:
1. Celebrates the specific achievement
2. Mentions something interesting about the studied element
3. Suggests the next learning step";
                return $@"TAREA: El estudiante completó exitosamente la misión de '{context.currentElement}'.
Genera un mensaje de felicitación (2-3 oraciones) que:
1. Celebre el logro específico
2. Mencione algo interesante del elemento estudiado
3. Sugiera el siguiente paso de aprendizaje";
            }
            else
            {
                if (en)
                    return $@"TASK: The student did not complete the '{context.currentElement}' mission.
Generate an encouraging message (2-3 sentences) that:
1. Normalizes failure as part of learning
2. Suggests reviewing a specific concept
3. Motivates them to try again";
                return $@"TAREA: El estudiante no completó la misión de '{context.currentElement}'.
Genera un mensaje de ánimo (2-3 oraciones) que:
1. Normalice el fallo como parte del aprendizaje
2. Sugiera revisar un concepto específico
3. Motive a intentar de nuevo";
            }
        }

        private string BuildAchievementPrompt(StudentContext context, object data)
        {
            bool en = context.idiomaPreferido == "en";
            string achievementName = data?.ToString() ?? (en ? "a new achievement" : "un nuevo logro");
            if (en)
                return $@"TASK: The student just unlocked the achievement '{achievementName}'.
Generate an enthusiastic celebration (3-4 sentences) that:
1. Congratulates enthusiastically and clearly, without using emojis
2. Explains the importance of this achievement
3. Mentions how this reflects their progress
4. Suggests the next challenge
Use a very positive and motivating tone.";
            return $@"TAREA: El estudiante acaba de desbloquear el logro '{achievementName}'.
Genera una celebración entusiasta (3-4 oraciones) que:
1. Felicite de forma entusiasta y clara, sin usar emojis
2. Explique la importancia de este logro
3. Mencione cómo esto refleja su progreso
4. Sugiera el próximo desafío
Usa un tono muy positivo y motivador.";
        }

        private string BuildProgressMotivationPrompt(StudentContext context, object data)
        {
            bool en = context.idiomaPreferido == "en";
            int xpNeeded = context.XPParaSiguienteRango();
            if (en)
                return $@"TASK: The student is {xpNeeded} XP away from ranking up.
Generate a motivational message (2-3 sentences) that:
1. Highlights how close they are to the next rank
2. Suggests specific missions to reach it soon
3. Creates a positive sense of urgency
Be enthusiastic and specific with suggestions.";
            return $@"TAREA: El estudiante está a {xpNeeded} XP de subir de rango.
Genera un mensaje motivacional (2-3 oraciones) que:
1. Resalte lo cerca que está del siguiente rango
2. Sugiera misiones específicas para alcanzarlo pronto
3. Cree un sentido de urgencia positiva
Sé entusiasta y específico con las sugerencias.";
        }

        private string BuildStuckHelpPrompt(StudentContext context)
        {
            bool en = context.idiomaPreferido == "en";
            if (en)
                return $@"TASK: The student has been on the current '{context.currentMissionType}' mission for more than 2 minutes without progress.
Generate a help offer (2 sentences) that:
1. Offers help without being invasive
2. Suggests a specific action that could help
Be empathetic and helpful.";
            return $@"TAREA: El estudiante lleva más de 2 minutos en la misión actual de '{context.currentMissionType}' sin progresar.
Genera una oferta de ayuda (2 oraciones) que:
1. Ofrezca ayuda sin ser invasivo
2. Sugiera una acción específica que podría ayudar
Sé empático y útil.";
        }

        private string BuildDailyGreetingPrompt(StudentContext context)
        {
            bool en = context.idiomaPreferido == "en";
            if (en)
                return $@"TASK: The student just logged in.
Generate a personalized greeting (2-3 sentences) that:
1. Warmly greets them by name
2. Mentions their current streak ({context.rachaActual} days)
3. Suggests an activity based on their recent progress
Be friendly and proactive.";
            return $@"TAREA: El estudiante acaba de iniciar sesión.
Genera un saludo personalizado (2-3 oraciones) que:
1. Salude calurosamente por nombre
2. Mencione su racha actual ({context.rachaActual} días)
3. Sugiera una actividad basada en su progreso reciente
Sé amigable y proactivo.";
        }

        private string BuildConceptExplanationPrompt(StudentContext context, object data)
        {
            bool en = context.idiomaPreferido == "en";
            string concept = data?.ToString() ?? (en ? "this concept" : "este concepto");
            if (en)
                return $@"TASK: The student asks about '{concept}'.
Generate a clear explanation (3-4 sentences) that:
1. Defines the concept simply
2. Uses a real-world analogy
3. Relates the concept to specific chemical elements
4. Checks if they need more detail
Adapt complexity to level {context.nivelDificultadPreferido}.";
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
            bool en = context.idiomaPreferido == "en";
            if (en)
                return $@"TASK: Recommend the next activity for the student.
Based on:
- Completed {context.misionesTotalesCompletadas} missions
- Has {context.logrosDesbloqueados} achievements
- Current rank: {context.rango}
- Strengths: {string.Join(", ", context.fortalezas)}
- Areas for improvement: {string.Join(", ", context.temasConDificultad)}
Generate a personalized recommendation (3 sentences) that:
1. Suggests a specific category or element
2. Explains why it's the best option now
3. Mentions the benefit of completing it
Be specific and strategic.";
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
