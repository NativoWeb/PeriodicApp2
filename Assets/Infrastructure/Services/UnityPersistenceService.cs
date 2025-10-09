using UnityEngine;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Infrastructure.Services
{
    public class UnityPersistenceService : IPersistenceService
    {
        public string GetPersistentDataPath()
        {
            return Application.persistentDataPath;
        }

        public string GetTemporaryDataPath()
        {
            return Application.temporaryCachePath;
        }

        public string GetStreamingAssetsPath()
        {
            return Application.streamingAssetsPath;
        }
    }
}
