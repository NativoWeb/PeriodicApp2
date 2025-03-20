using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class DbConnexion : MonoBehaviour
{
    public static DbConnexion Instance { get; private set; }

    public FirebaseAuth Auth { get; private set; }
    public FirebaseFirestore Firestore { get; private set; }
    public FirebaseUser User { get; set; } // Usuario actual conectado

    private bool firebaseInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persistir entre escenas
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject); // Evitar duplicados
        }
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            if (task.Result == DependencyStatus.Available)
            {
                Auth = FirebaseAuth.DefaultInstance;
                Firestore = FirebaseFirestore.DefaultInstance;
                firebaseInitialized = true;
                Debug.Log("✅ Firebase Inicializado correctamente");
            }
            else
            {
                Debug.LogError("❌ No se pudo inicializar Firebase: " + task.Result);
            }
        });
    }

    // Método para consultar si Firebase está listo
    public bool IsFirebaseReady()
    {
        return firebaseInitialized;
    }
}
