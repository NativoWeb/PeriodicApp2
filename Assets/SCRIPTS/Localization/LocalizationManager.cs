using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using TMPro;

public class LocalizationManager : MonoBehaviour
{
    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        UpdateAllTexts();
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateAllTexts();
    }

    public void UpdateAllTexts()
    {
        LocalizedTextUI[] textsToLocalize = FindObjectsOfType<LocalizedTextUI>();

        foreach (LocalizedTextUI textObject in textsToLocalize)
        {
            TextMeshProUGUI tmpText = textObject.GetComponent<TextMeshProUGUI>();
            var localizedString = new LocalizedString("UI_Text", textObject.localizationKey);

            var asyncOperation = localizedString.GetLocalizedStringAsync();
            if (asyncOperation.IsDone)
            {
                tmpText.text = asyncOperation.Result;
            }
            else
            {
                asyncOperation.Completed += (op) => tmpText.text = op.Result;
            }
        }
    }
}
