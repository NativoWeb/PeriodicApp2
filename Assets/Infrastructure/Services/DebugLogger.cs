using UnityEngine;
using System.Diagnostics;

namespace Infrastructure.Services
{
    /// <summary>
    /// Clase de utilidad para logging condicional.
    /// Los logs solo se ejecutan en Editor o builds de desarrollo.
    /// Esto mejora el rendimiento en builds de producción.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Log normal - Solo en development builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Log con contexto - Solo en development builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message, Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        /// <summary>
        /// Warning - Solo en development builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// Warning con contexto - Solo en development builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(object message, Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Error - SIEMPRE se muestra (incluso en producción)
        /// Los errores son críticos y deben ser visibles siempre
        /// </summary>
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        /// <summary>
        /// Error con contexto - SIEMPRE se muestra
        /// </summary>
        public static void LogError(object message, Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }

        /// <summary>
        /// Exception - SIEMPRE se muestra
        /// </summary>
        public static void LogException(System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        /// <summary>
        /// Exception con contexto - SIEMPRE se muestra
        /// </summary>
        public static void LogException(System.Exception exception, Object context)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }
}
