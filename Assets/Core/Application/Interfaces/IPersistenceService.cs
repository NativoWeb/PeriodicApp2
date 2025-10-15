//using UnityEngine;

namespace PeriodicApp.Core.Application.Interfaces
{
    public interface IPersistenceService
    {
        string GetPersistentDataPath();

        string GetTemporaryDataPath();

        string GetStreamingAssetsPath();
    }
}
