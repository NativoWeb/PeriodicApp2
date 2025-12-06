using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuantumAI.Models
{
    /// <summary>
    /// Contexto completo del estudiante para personalización de IA
    /// Contiene toda la información necesaria para que Quantum tome decisiones inteligentes
    /// </summary>
    [System.Serializable]
    public class StudentContext
    {
        #region Perfil Básico

        public string userId;
        public string displayName;
        public int xp;
        public string rango;
        public int rachaActual;
        public string ocupacion; // Estudiante/Profesor
        public DateTime ultimaFechaLogin;

        #endregion

        #region Ubicación Actual

        public string currentScene;
        public string currentCategory;
        public string currentElement;
        public int currentMissionId;
        public string currentMissionType; // AR, Juego, Quiz, Evaluacion
        public float tiempoEnEscenaActual;

        #endregion

        #region Progreso Académico

        public int misionesTotalesCompletadas;
        public int logrosDesbloqueados;
        public Dictionary<string, CategoryProgress> progressoPorCategoria;
        public List<string> elementosDominados; // Elementos con todas las misiones completadas
        public List<string> categoriasDominadas; // Categorías con todos los elementos completados

        #endregion

        #region Comportamiento y Rendimiento

        public Dictionary<string, int> intentosPorMision; // missionId -> intentos
        public List<string> temasConDificultad; // Temas donde falla consistentemente
        public List<string> fortalezas; // Temas donde sobresale
        public float tiempoPromedioMision;
        public float tasaExito; // % de misiones completadas en primer intento
        public DateTime ultimaInteraccionConIA;

        #endregion

        #region Personalización

        public LearningStyle estiloAprendizaje;
        public DifficultyLevel nivelDificultadPreferido;
        public InteractionPreference preferenciaInteraccion;
        public bool vozActivada;
        public string idiomaPreferido; // "es" o "en"

        #endregion

        #region Social

        public int posicionRanking;
        public List<string> amigosIds;
        public Dictionary<string, int> comparacionConAmigos; // amigoId -> diferencia XP

        #endregion

        #region Historial Conversacional

        public List<ConversationEntry> historialConversacion;
        public string ultimoElementoDiscutido;
        public string ultimaIntencionDetectada;

        #endregion

        // Constructor
        public StudentContext()
        {
            progressoPorCategoria = new Dictionary<string, CategoryProgress>();
            elementosDominados = new List<string>();
            categoriasDominadas = new List<string>();
            intentosPorMision = new Dictionary<string, int>();
            temasConDificultad = new List<string>();
            fortalezas = new List<string>();
            historialConversacion = new List<ConversationEntry>();
            amigosIds = new List<string>();
            comparacionConAmigos = new Dictionary<string, int>();

            // Valores por defecto
            estiloAprendizaje = LearningStyle.Mixto;
            nivelDificultadPreferido = DifficultyLevel.Intermedio;
            preferenciaInteraccion = InteractionPreference.Equilibrado;
            vozActivada = true;
            idiomaPreferido = "es";
        }

        /// <summary>
        /// Actualiza el tiempo en la escena actual
        /// </summary>
        public void ActualizarTiempoEnEscena(float deltaTime)
        {
            tiempoEnEscenaActual += deltaTime;
        }

        /// <summary>
        /// Registra un intento de misión
        /// </summary>
        public void RegistrarIntentoMision(string misionId, bool exitoso)
        {
            if (!intentosPorMision.ContainsKey(misionId))
            {
                intentosPorMision[misionId] = 0;
            }
            intentosPorMision[misionId]++;

            // Actualizar tasa de éxito
            RecalcularTasaExito();
        }

        /// <summary>
        /// Recalcula la tasa de éxito del estudiante
        /// </summary>
        private void RecalcularTasaExito()
        {
            if (intentosPorMision.Count == 0) return;

            int misionesEnPrimerIntento = 0;
            foreach (var intento in intentosPorMision.Values)
            {
                if (intento == 1) misionesEnPrimerIntento++;
            }

            tasaExito = (float)misionesEnPrimerIntento / intentosPorMision.Count;
        }

        /// <summary>
        /// Agrega una entrada al historial conversacional
        /// </summary>
        public void AgregarConversacion(string mensaje, bool esUsuario)
        {
            historialConversacion.Add(new ConversationEntry
            {
                mensaje = mensaje,
                esUsuario = esUsuario,
                timestamp = DateTime.Now
            });

            // Mantener solo las últimas 50 entradas para no sobrecargar
            if (historialConversacion.Count > 50)
            {
                historialConversacion.RemoveAt(0);
            }

            ultimaInteraccionConIA = DateTime.Now;
        }

        /// <summary>
        /// Obtiene el progreso de una categoría específica
        /// </summary>
        public CategoryProgress ObtenerProgresoCategoría(string categoria)
        {
            if (!progressoPorCategoria.ContainsKey(categoria))
            {
                progressoPorCategoria[categoria] = new CategoryProgress(categoria);
            }
            return progressoPorCategoria[categoria];
        }

        /// <summary>
        /// Verifica si el estudiante está atascado (mucho tiempo sin progresar)
        /// </summary>
        public bool EstaAtascado()
        {
            return tiempoEnEscenaActual > 120f && // Más de 2 minutos en la escena
                   (currentMissionType == "Juego" || currentMissionType == "Quiz");
        }

        /// <summary>
        /// Calcula XP necesario para el siguiente rango
        /// </summary>
        public int XPParaSiguienteRango()
        {
            var rangos = new Dictionary<string, int>
            {
                { "Aprendiz Atomico", 300 },
                { "Explorador de Elementos", 900 },
                { "Científico en Formación", 2000 },
                { "Experto Molecular", 4000 },
                { "Maestro de Laboratorio", 7500 },
                { "Sabio de la tabla", 13000 },
                { "Leyenda química", 25000 },
                { "Alquimista Supremo", int.MaxValue }
            };

            foreach (var rango in rangos)
            {
                if (xp < rango.Value)
                {
                    return rango.Value - xp;
                }
            }

            return 0; // Ya está en el rango máximo
        }
    }

    /// <summary>
    /// Progreso por categoría
    /// </summary>
    [System.Serializable]
    public class CategoryProgress
    {
        public string nombreCategoria;
        public int elementosCompletados;
        public int elementosTotales;
        public int misionesCompletadas;
        public int misionesTotales;
        public bool logroCategoriaDesbloqueado;
        public float porcentajeCompletado => elementosTotales > 0
            ? (float)elementosCompletados / elementosTotales * 100f
            : 0f;

        public CategoryProgress(string nombre)
        {
            nombreCategoria = nombre;
            elementosCompletados = 0;
            elementosTotales = 0;
            misionesCompletadas = 0;
            misionesTotales = 0;
            logroCategoriaDesbloqueado = false;
        }
    }

    /// <summary>
    /// Entrada de conversación
    /// </summary>
    [System.Serializable]
    public class ConversationEntry
    {
        public string mensaje;
        public bool esUsuario;
        public DateTime timestamp;
    }

    /// <summary>
    /// Estilo de aprendizaje detectado
    /// </summary>
    public enum LearningStyle
    {
        Visual,         // Prefiere AR y visualizaciones
        Verbal,         // Prefiere chat y explicaciones textuales
        Kinestésico,    // Prefiere juegos interactivos
        Mixto           // Balance de todos
    }

    /// <summary>
    /// Nivel de dificultad preferido
    /// </summary>
    public enum DifficultyLevel
    {
        Principiante,   // Explicaciones simples, muchas ayudas
        Intermedio,     // Balance estándar
        Avanzado        // Explicaciones técnicas, menos ayudas
    }

    /// <summary>
    /// Preferencia de interacción con la IA
    /// </summary>
    public enum InteractionPreference
    {
        Proactivo,      // IA interviene frecuentemente con sugerencias
        Equilibrado,    // IA interviene en momentos clave
        Reactivo        // IA solo responde cuando se le pregunta
    }
}
