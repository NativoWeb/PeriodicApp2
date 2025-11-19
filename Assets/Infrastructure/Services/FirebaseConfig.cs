using UnityEngine;

namespace Infrastructure.Services
{
    /// <summary>
    /// Configuración de Firebase almacenada como ScriptableObject.
    /// IMPORTANTE: No commitear este archivo con credenciales reales al repositorio.
    /// Usar .gitignore para excluir los archivos .asset generados.
    /// </summary>
    [CreateAssetMenu(fileName = "FirebaseConfig", menuName = "PeriodicApp/Firebase Configuration", order = 1)]
    public class FirebaseConfig : ScriptableObject
    {
        [Header("Firebase Credentials")]
        [Tooltip("API Key de Firebase (obtener de Firebase Console)")]
        public string apiKey = "";

        [Tooltip("App ID de Firebase")]
        public string appId = "";

        [Tooltip("Project ID de Firebase")]
        public string projectId = "";

        [Tooltip("Storage Bucket de Firebase")]
        public string storageBucket = "";

        [Tooltip("Database URL de Firebase")]
        public string databaseUrl = "";

        [Header("Debug Settings")]
        [Tooltip("Habilitar logs de Firebase en builds de desarrollo")]
        public bool enableFirebaseLogs = true;

        /// <summary>
        /// Valida que todas las credenciales necesarias estén configuradas.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(apiKey) &&
                   !string.IsNullOrEmpty(appId) &&
                   !string.IsNullOrEmpty(projectId);
        }
    }
}
