using System;
using UnityEngine;
using UnityEngine.Events;
using PeriodicApp.Core.Application.Interfaces;
using PeriodicApp.Infrastructure.Connectivity;

namespace PeriodicApp.UnityAdapters.Connectivity
{
    public sealed class ConnectionMonitorBehaviour : MonoBehaviour
    {
        [SerializeField] private UnityEvent<bool> onConnectionStateChanged = new UnityEvent<bool>();

        private readonly IConnectionMonitor connectionMonitor = new ApplicationReachabilityConnectionMonitor();

        public event Action<bool>? ConnectionChanged;

        public bool IsConnected => connectionMonitor.IsConnected;

        private void Update()
        {
            connectionMonitor.Refresh();
        }

        private void OnEnable()
        {
            connectionMonitor.ConnectionStateChanged += HandleConnectionChanged;
        }

        private void OnDisable()
        {
            connectionMonitor.ConnectionStateChanged -= HandleConnectionChanged;
        }

        private void HandleConnectionChanged(bool isConnected)
        {
            onConnectionStateChanged?.Invoke(isConnected);
            ConnectionChanged?.Invoke(isConnected);
        }
    }
}
