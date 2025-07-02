using UnityEngine;
using System.Collections.Generic;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    // El archivo CSV que contiene las traducciones
    public TextAsset localizationFile;

    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
    private bool isReady = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Llama a esta función al inicio para cargar el idioma (p. ej., "es" o "en")
    public void LoadLocalizedText(string languageCode)
    {
        localizedTexts.Clear();
        string[] lines = localizationFile.text.Split('\n');

        // La primera línea es la cabecera (key,es,en,...)
        string[] header = lines[0].Trim().Split(',');
        int languageIndex = -1;

        // Encontramos el índice de la columna del idioma que queremos
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i] == languageCode)
            {
                languageIndex = i;
                break;
            }
        }

        if (languageIndex == -1)
        {
            Debug.LogError("Código de idioma no encontrado en el archivo CSV: " + languageCode);
            return;
        }

        // Leemos el resto de las líneas
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Trim().Split(',');
            if (values.Length > languageIndex)
            {
                string key = values[0];
                string value = values[languageIndex];
                localizedTexts[key] = value;
            }
        }

        isReady = true;
        Debug.Log("Traducciones cargadas para el idioma: " + languageCode);
    }

    public string GetLocalizedValue(string key)
    {
        if (!isReady)
        {
            Debug.LogWarning("LocalizationManager no está listo.");
            return key; // Devuelve la clave si aún no se ha cargado
        }

        if (localizedTexts.ContainsKey(key))
        {
            return localizedTexts[key];
        }

        Debug.LogWarning("Clave de traducción no encontrada: " + key);
        return key; // Devuelve la clave si no se encuentra la traducción
    }
}