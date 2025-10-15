

namespace PeriodicApp.Core.Application.Interfaces
{
    public interface IJsonService
    {
        string ToJson<T>(T obj, bool prettyPrint = false);

        T FromJson<T>(string json);

        void FromJsonOverwrite<T>(string json, T objectToOverwrite);
    }
}
