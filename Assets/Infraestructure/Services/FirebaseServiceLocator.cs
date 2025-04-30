using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine;


public static class FirebaseServiceLocator
{
    private static bool initialized = false;
    private static Task initializationTask;

    public static FirebaseAuth Auth { get; private set; }
    public static FirebaseFirestore Firestore { get; private set; }
    public static FirebaseUser CurrentUser => Auth?.CurrentUser;

    public static async Task<bool> InicializarFirebase()
    {
        if (initialized) return true;

        if (initializationTask == null)
        {
            initializationTask = FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    Auth = FirebaseAuth.DefaultInstance;
                    Firestore = FirebaseFirestore.DefaultInstance;
                    initialized = true;
                    Debug.Log("Firebase inicializado correctamente (ServiceLocator).");
                }
                else
                {
                    Debug.LogError($"Error al inicializar Firebase: {task.Result}");
                    initialized = false;
                }
            });
        }

        await initializationTask;
        return initialized;
    }

    public static bool EstaListo()
    {
        return initialized && Auth != null && Firestore != null;
    }
}

