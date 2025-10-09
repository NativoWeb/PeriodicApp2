using UnityEngine;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Infrastructure.Services
{
    public class UnityNetworkService : INetworkService
    {
        public NetworkStatus GetNetworkStatus()
        {
            return Application.internetReachability switch
            {
                NetworkReachability.NotReachable => NetworkStatus.NotReachable,
                NetworkReachability.ReachableViaCarrierDataNetwork => NetworkStatus.ReachableViaCarrierDataNetwork,
                NetworkReachability.ReachableViaLocalAreaNetwork => NetworkStatus.ReachableViaLocalAreaNetwork,
                _ => NetworkStatus.NotReachable
            };
        }

        public bool IsConnected()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }
    }
}
