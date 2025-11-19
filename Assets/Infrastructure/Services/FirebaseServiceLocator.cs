using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine;
using Infrastructure.Services;

public static class FirebaseServiceLocator
{
    private static bool initialized = false;
    private static Task<bool> initializationTask; // Cambiamos el tipo para que devuelva nuestro resultado
    private static FirebaseConfig config;

    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseFirestore Firestore { get; private set; }
    public static FirebaseUser CurrentUser => Auth?.CurrentUser;

    // La firma del método sigue igual, pero su interior cambia
    public static Task<bool> InicializarFirebase()
    {
        // Si ya tenemos una tarea de inicialización en curso o completada,
        // simplemente la devolvemos para no ejecutar el proceso dos veces.
        if (initializationTask != null)
        {
            return initializationTask;
        }

        // Creamos e iniciamos la tarea asíncrona y la guardamos.
        initializationTask = InicializarAsync();
        return initializationTask;
    }

    // Creamos un método 'privado' que contiene la lógica async/await real
    private static async Task<bool> InicializarAsync()
    {
        try
        {
            // Cargar la configuración desde Resources
            if (config == null)
            {
                config = Resources.Load<FirebaseConfig>("FirebaseConfig");
                if (config == null)
                {
                    Debug.LogError("FirebaseConfig no encontrado en Resources. Por favor crea un FirebaseConfig en Assets/Resources/");
                    return false;
                }

                if (!config.IsValid())
                {
                    Debug.LogError("FirebaseConfig inválido. Por favor configura todas las credenciales necesarias.");
                    return false;
                }
            }

            // 'await' pausa este método, libera el hilo principal,
            // y cuando la tarea termina, el código que sigue se ejecuta
            // de vuelta en el HILO PRINCIPAL de Unity.
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Configurar las opciones de Firebase usando el ScriptableObject
                var options = new Firebase.AppOptions
                {
                    ApiKey = config.apiKey,
                    AppId = config.appId,
                    ProjectId = config.projectId,
                    StorageBucket = config.storageBucket,
                    DatabaseUrl = string.IsNullOrEmpty(config.databaseUrl)
                        ? null
                        : new System.Uri(config.databaseUrl)
                };

                // Crear la app con las opciones configuradas
                FirebaseApp app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    app = FirebaseApp.Create(options);
                }

                // ¡ESTO AHORA ES SEGURO!
                // Estamos de vuelta en el hilo principal, por lo que podemos
                // acceder a las APIs de Unity y Firebase sin problemas.
                Auth = FirebaseAuth.GetAuth(app);

                // Desactivar explícitamente el uso del emulador de autenticación
                // Esto fuerza a Firebase a conectarse al servicio real en la nube
                System.Environment.SetEnvironmentVariable("USE_AUTH_EMULATOR", null);

                Firestore = FirebaseFirestore.GetInstance(app);
                initialized = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (config.enableFirebaseLogs)
                {
                    Debug.Log("Firebase inicializado correctamente (ServiceLocator) con servicio en la nube.");
                }
#endif
                return true;
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError($"Error al inicializar Firebase: {dependencyStatus}");
#endif
                initialized = false;
                return false;
            }
        }
        catch (System.Exception ex)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"Excepción al inicializar Firebase: {ex.Message}\n{ex.StackTrace}");
#endif
            initialized = false;
            return false;
        }
    }

    public static bool EstaListo()
    {
        return initialized && Auth != null && Firestore != null;
    }
}