using System;
using UnityEngine;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Infrastructure.Connectivity
{
    public sealed class ApplicationReachabilityConnectionMonitor : IConnectionMonitor
    {
        private bool isConnected;

        public event Action<bool>? ConnectionStateChanged;

        public bool IsConnected => isConnected;

        public void Refresh()
        {
            bool current = Application.internetReachability != NetworkReachability.NotReachable;
            if (current == isConnected)
            {
                return;
            }

            isConnected = current;
            ConnectionStateChanged?.Invoke(isConnected);
        }
    }
}
