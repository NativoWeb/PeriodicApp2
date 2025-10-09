//using UnityEngine;

namespace PeriodicApp.Core.Application.Interfaces
{
    public interface IPlayerPrefsService
    {
        // String methods
        void SetString(string key, string value);
        string GetString(string key, string defaultValue = "");

        // Int methods
        void SetInt(string key, int value);
        int GetInt(string key, int defaultValue = 0);

        // Float methods
        void SetFloat(string key, float value);
        float GetFloat(string key, float defaultValue = 0f);

        // Bool methods (se guardan como int)
        void SetBool(string key, bool value);
        bool GetBool(string key, bool defaultValue = false);

        // Utility methods
        bool HasKey(string key);
        void DeleteKey(string key);
        void DeleteAll();
        void Save();
    }
}
