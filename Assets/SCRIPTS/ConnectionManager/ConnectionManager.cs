using UnityEngine;
using UnityEngine.Events;
using PeriodicApp.UnityAdapters.Connectivity;

public sealed class ConnectionManager : MonoBehaviour
{
    [SerializeField] private ConnectionMonitorBehaviour connectionMonitor;
    [SerializeField] private UnityEvent<bool> onConnectionChanged = new UnityEvent<bool>();

    private void Awake()
    {
        if (connectionMonitor == null)
        {
            connectionMonitor = GetComponentInChildren<ConnectionMonitorBehaviour>();
        }
    }

    private void OnEnable()
    {
        if (connectionMonitor != null)
        {
            connectionMonitor.ConnectionChanged += HandleConnectionChanged;
        }
    }

    private void OnDisable()
    {
        if (connectionMonitor != null)
        {
            connectionMonitor.ConnectionChanged -= HandleConnectionChanged;
        }
    }

    public bool IsConnectedToInternet()
    {
        return connectionMonitor != null && connectionMonitor.IsConnected;
    }

    private void HandleConnectionChanged(bool isConnected)
    {
        onConnectionChanged?.Invoke(isConnected);
    }
}
