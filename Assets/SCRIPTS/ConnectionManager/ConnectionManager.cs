using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    public static bool isOnline = false;
    public static bool isOffline = false;

    public delegate void ConnectionChanged();
    public static event ConnectionChanged OnConnectionChanged;

    // Hacer Singleton
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        CheckInternetConnection();
    }

    public void CheckInternetConnection()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            isOffline = true;
            isOnline = false;
            Debug.Log("❌ Sin conexión a Internet");
        }
        else
        {
            isOnline = true;
            isOffline = false;
            Debug.Log("✅ Conexión a Internet");
        }

        OnConnectionChanged?.Invoke();
    }

    // 🔑 Método público para consultar desde otros scripts
    public bool IsConnectedToInternet()
    {
        return isOnline;
    }
}
