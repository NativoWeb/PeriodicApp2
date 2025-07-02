using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    private TMP_Text textField;

    void Start()
    {
        // Obtener la referencia al componente de texto.
        // GetComponent se hace aquí para asegurar que siempre lo tengamos.
        if (textField == null)
        {
            textField = GetComponent<TMP_Text>();
        }

        UpdateText();
    }

    public void UpdateText()
    {
        // Asegurarse de que el textField no sea nulo antes de usarlo.
        if (textField == null)
        {
            textField = GetComponent<TMP_Text>();
        }

        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(localizationKey))
        {
            textField.text = LocalizationManager.Instance.GetLocalizedValue(localizationKey);
        }
    }
}