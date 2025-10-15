//using UnityEngine;

namespace PeriodicApp.Core.Application.Interfaces
{
    
        public enum NetworkStatus
        {
            NotReachable = 0,
            ReachableViaCarrierDataNetwork = 1,
            ReachableViaLocalAreaNetwork = 2
        }

        public interface INetworkService
        {
            /// <summary>
            /// Obtiene el estado actual de conectividad
            /// </summary>
            NetworkStatus GetNetworkStatus();

            /// <summary>
            /// Verifica si hay conexión a internet disponible
            /// </summary>
            bool IsConnected();
        }
    
}
