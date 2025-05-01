using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class PasswordValidatorController : MonoBehaviour
{
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public GameObject requirementsPanel;

    public TMP_Text minLengthText, uppercaseText, lowercaseText, specialCharText;
    public RawImage Caracteres, Mayusculas, Minusculas, Especiales;
    public Texture2D imagenActiva;
    public Texture2D imagenInactiva;

    void Start()
    {
        requirementsPanel.SetActive(false);
        passwordInput.onSelect.AddListener(ShowRequirements);
        passwordInput.onDeselect.AddListener(HideRequirements);
        passwordInput.onValueChanged.AddListener(ValidatePassword);
    }

    void ShowRequirements(string _) => requirementsPanel.SetActive(true);
    void HideRequirements(string _) => requirementsPanel.SetActive(false);

    void ValidatePassword(string password)
    {
        bool hasMinLength = password.Length >= 6;
        bool hasUppercase = Regex.IsMatch(password, "[A-Z]");
        bool hasLowercase = Regex.IsMatch(password, "[a-z]");
        bool hasSpecialChar = Regex.IsMatch(password, @"[\^\$\*\.\[\]\{\}\(\)\?\""!@#%&/\\,><':;|_~`]");

        minLengthText.color = hasMinLength ? Color.green : Color.white;
        Caracteres.texture = hasMinLength ? imagenActiva : imagenInactiva;

        uppercaseText.color = hasUppercase ? Color.green : Color.white;
        Mayusculas.texture = hasUppercase ? imagenActiva : imagenInactiva;

        lowercaseText.color = hasLowercase ? Color.green : Color.white;
        Minusculas.texture = hasLowercase ? imagenActiva : imagenInactiva;

        specialCharText.color = hasSpecialChar ? Color.green : Color.white;
        Especiales.texture = hasSpecialChar ? imagenActiva : imagenInactiva;
    }

    public bool CumpleRequisitos(string password)
    {
        return password.Length >= 6 &&
               Regex.IsMatch(password, "[A-Z]") &&
               Regex.IsMatch(password, "[a-z]") &&
               Regex.IsMatch(password, @"[\^\$\*\.\[\]\{\}\(\)\?\""!@#%&/\\,><':;|_~`]");
    }
}
