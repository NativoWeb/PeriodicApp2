using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class PasswordValidator : MonoBehaviour
{
    public TMP_InputField passwordInput;
    public GameObject requirementsPanel;  // Panel con los requisitos
    public TMP_Text minLengthText, uppercaseText, lowercaseText, specialCharText; // Textos de cada requisito

    private void Start()
    {
        passwordInput.onSelect.AddListener(ShowRequirements);
        passwordInput.onValueChanged.AddListener(ValidatePassword);
        passwordInput.onDeselect.AddListener(HideRequirements);
        requirementsPanel.SetActive(false);
    }

    void ShowRequirements(string text)
    {
        requirementsPanel.SetActive(true);
    }

    void HideRequirements(string text)
    {
        requirementsPanel.SetActive(false);
    }

    void ValidatePassword(string password)
    {
        // Expresiones regulares para cada criterio
        bool hasMinLength = password.Length >= 6;
        bool hasUppercase = Regex.IsMatch(password, "[A-Z]");
        bool hasLowercase = Regex.IsMatch(password, "[a-z]");
        bool hasSpecialChar = Regex.IsMatch(password, @"[\^\$\*\.\[\]\{\}\(\)\?\""!@#%&/\\,><':;|_~`]");

        // Cambiar color según validación
        minLengthText.color = hasMinLength ? Color.green : Color.red;
        uppercaseText.color = hasUppercase ? Color.green : Color.red;
        lowercaseText.color = hasLowercase ? Color.green : Color.red;
        specialCharText.color = hasSpecialChar ? Color.green : Color.red;
    }
}
