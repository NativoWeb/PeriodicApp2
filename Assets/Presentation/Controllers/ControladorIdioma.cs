using System.Collections;
using System.Collections.Generic;
using PeriodicApp.Presentation;
using UnityEngine;

using UnityEngine.Localization.Settings;

public class ControladorIdioma : MonoBehaviour
{
    private bool _active = false;
    public static ControladorIdioma instancia;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Evitar duplicados
        }
    }

    void Start()
    {
        if (!ServiceLocator.AreServicesInitialized())
        {
            Debug.LogError("ServiceLocator no inicializado en ControladorIdioma");
            return;
        }
        int ID = ServiceLocator.PlayerPrefs.GetInt("LocaleKey", 0);

        ChangeLocale(ID);
    }

    public void ChangeLocale(int localeID)
    {
        if (_active)
        {
            return;
        }
        StartCoroutine(SetLocale(localeID));
    }

    private IEnumerator SetLocale(int localeID)
    {
        _active = true;
        yield return LocalizationSettings.InitializationOperation;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (localeID < 0 || localeID >= locales.Count)
        {
            Debug.LogWarning($"Locale ID {localeID} is out of range. Falling back to default locale.");
            localeID = 0;
        }

        LocalizationSettings.SelectedLocale = locales[localeID];
        PlayerPrefs.SetInt("LocaleKey", localeID);
        if (localeID == 0)
            PlayerPrefs.SetString("appIdioma", "español");
        else
            PlayerPrefs.SetString("appIdioma", "ingles");
        _active = false;
    }
}
