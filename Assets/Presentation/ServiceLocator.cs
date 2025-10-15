using PeriodicApp.Core.Application;
using PeriodicApp.Core.Application.Interfaces;
using PeriodicApp.Infrastructure.Services;
using UnityEngine;

namespace PeriodicApp.Presentation
{
    public class ServiceLocator : MonoBehaviour
    {
        /// Service Locator para acceder a servicios desde cualquier parte de la aplicación
        /// Este script debe estar en un GameObject en la primera escena
        private static ServiceLocator _instance;

        // Servicios disponibles
        public static IPersistenceService Persistence { get; private set; }
        public static IPlayerPrefsService PlayerPrefs { get; private set; }
        public static INetworkService Network { get; private set; }
        public static IJsonService Json { get; private set; }
        public static ILoggingService Logger { get; private set; }
        public static ISceneService Scene { get; private set; }
        public static IResourceLoaderService ResourceLoader { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServices();
                Debug.Log("[ServiceLocator] Services initialized successfully");
            }
            else
            {
                Debug.LogWarning("[ServiceLocator] Instance already exists, destroying duplicate");
                Destroy(gameObject);
            }
        }

        private void InitializeServices()
        {
            // Instanciar las implementaciones concretas
            Persistence = new UnityPersistenceService();
            PlayerPrefs = new UnityPlayerPrefsService();
            Network = new UnityNetworkService();
            Json = new UnityJsonService();
            Logger = new UnityLoggingService();
            Scene = new UnitySceneService();
            ResourceLoader = new UnityResourceLoaderService();
        }

        /// Verifica si los servicios están inicializados
        public static bool AreServicesInitialized()
        {
            return _instance != null &&
                   Persistence != null &&
                   PlayerPrefs != null &&
                   Network != null &&
                   Json != null &&
                   Logger != null &&
                   Scene != null &&
                   ResourceLoader != null;
        }
    }
}
