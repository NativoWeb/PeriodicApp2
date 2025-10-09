using PeriodicApp.Core.Application.Interfaces;
using UnityEngine;

namespace PeriodicApp.Infrastructure.Services
{
    /// Implementación de ILoggingService usando Unity Debug
    public class UnityLoggingService : ILoggingService
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
