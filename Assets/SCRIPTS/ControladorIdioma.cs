using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
        int ID = PlayerPrefs.GetInt("LocaleKey", 0);
           
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
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        PlayerPrefs.SetInt("LocaleKey", localeID);
        if (localeID == 0)
            PlayerPrefs.SetString("appIdioma", "español");
        else
            PlayerPrefs.SetString("appIdioma", "ingles");
        _active = false;
    }
}
