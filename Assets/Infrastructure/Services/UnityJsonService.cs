using UnityEngine;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Infrastructure.Services
{
    public class UnityJsonService : IJsonService
    {
        public string ToJson<T>(T obj, bool prettyPrint = false)
        {
            return JsonUtility.ToJson(obj, prettyPrint);
        }

        public T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        public void FromJsonOverwrite<T>(string json, T objectToOverwrite)
        {
            JsonUtility.FromJsonOverwrite(json, objectToOverwrite);
        }
    }
}
