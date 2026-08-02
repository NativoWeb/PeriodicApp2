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
            ServiceLocator existingLocator = FindObjectOfType<ServiceLocator>();

            if (existingLocator == null)
            {
                GameObject serviceLocatorObj = new GameObject("ServiceLocator");
                serviceLocatorObj.AddComponent<ServiceLocator>();
            }

            StartCoroutine(InicializarDespuesDeServiceLocator());
            return;
        }

        int ID = ServiceLocator.PlayerPrefs.GetInt("LocaleKey", 0);
        ForzarLocale(ID);
    }

    private IEnumerator InicializarDespuesDeServiceLocator()
    {
        // Esperar un frame para que ServiceLocator se inicialice
        yield return null;

        if (!ServiceLocator.AreServicesInitialized())
        {
            Debug.LogError("No se pudo inicializar ServiceLocator en ControladorIdioma");
            yield break;
        }

        int ID = ServiceLocator.PlayerPrefs.GetInt("LocaleKey", 0);
        ForzarLocale(ID);
    }

    private void ForzarLocale(int localeID)
    {
        _active = false;
        StartCoroutine(SetLocale(localeID));
    }

    public void ChangeLocale(int localeID)
    {
        if (_active) return;
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

        // Usar ServiceLocator.PlayerPrefs si está disponible, sino usar PlayerPrefs directo
        if (ServiceLocator.AreServicesInitialized())
        {
            ServiceLocator.PlayerPrefs.SetInt("LocaleKey", localeID);
            if (localeID == 0)
                ServiceLocator.PlayerPrefs.SetString("appIdioma", "español");
            else
                ServiceLocator.PlayerPrefs.SetString("appIdioma", "ingles");
        }
        else
        {
            // Fallback: usar PlayerPrefs nativo de Unity
            PlayerPrefs.SetInt("LocaleKey", localeID);
            if (localeID == 0)
                PlayerPrefs.SetString("appIdioma", "español");
            else
                PlayerPrefs.SetString("appIdioma", "ingles");
        }

        _active = false;
    }
}
