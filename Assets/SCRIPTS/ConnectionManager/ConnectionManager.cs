using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    public static bool isOnline = false;
    public static bool isOffline = false;
    public static bool IsInitialized { get; private set; } = false; // ✅ Bandera de inicialización

    public delegate void ConnectionChanged();
    public static event ConnectionChanged OnConnectionChanged;

    private bool lastConnectionState = false; // Guarda el último estado de conexión

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
        IsInitialized = true; // ✅ Marcar como listo
        Debug.Log("🔌 ConnectionManager inicializado. isOnline = " + isOnline + ", isOffline = " + isOffline);
    }

    public void CheckInternetConnection()
    {
        bool currentConnectionState = (Application.internetReachability != NetworkReachability.NotReachable);

        // Solo ejecutamos el código si el estado ha cambiado
        if (currentConnectionState != lastConnectionState)
        {
            lastConnectionState = currentConnectionState; // Actualizamos el estado

            if (!currentConnectionState)
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

            OnConnectionChanged?.Invoke(); // Disparamos el evento solo cuando hay un cambio
        }
    }

    // 🔑 Método público para consultar desde otros scripts
    public bool IsConnectedToInternet()
    {
        return isOnline;
    }
}
