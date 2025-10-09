//using UnityEngine;

namespace PeriodicApp.Core.Application.Interfaces
{
    
    /// Servicio para registrar logs y mensajes de debug
    
    public interface ILoggingService
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
