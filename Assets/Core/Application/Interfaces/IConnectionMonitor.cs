using System;

namespace PeriodicApp.Core.Application.Interfaces
{
    public interface IConnectionMonitor
    {
        event Action<bool> ConnectionStateChanged;

        bool IsConnected { get; }

        void Refresh();
    }
}
