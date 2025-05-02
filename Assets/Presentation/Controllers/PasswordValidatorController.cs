using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

/// 
/// Controlador visual que muestra el estado de los requisitos de la contraseña
/// mientras el usuario la escribe. No valida reglas de negocio.
/// 
public class PasswordValidatorController : MonoBehaviour
{
    [Header("Entradas")]
    public TMP_InputField passwordInput;

    [Header("Panel de Requisitos")]
    public GameObject requirementsPanel;

    [Header("Textos de requisitos")]
    public TMP_Text minLengthText;
    public TMP_Text uppercaseText;
    public TMP_Text lowercaseText;
    public TMP_Text specialCharText;

    [Header("Íconos visuales")]
    public RawImage Caracteres;
    public RawImage Mayusculas;
    public RawImage Minusculas;
    public RawImage Especiales;
    public Texture2D imagenActiva;
    public Texture2D imagenInactiva;

    private void Start()
    {
        requirementsPanel.SetActive(false);

        passwordInput.onSelect.AddListener(ShowRequirements);
        passwordInput.onDeselect.AddListener(HideRequirements);
        passwordInput.onValueChanged.AddListener(UpdateVisualFeedback);
    }

    private void ShowRequirements(string _) => requirementsPanel.SetActive(true);
    private void HideRequirements(string _) => requirementsPanel.SetActive(false);

    private void UpdateVisualFeedback(string password)
    {
        bool hasMinLength = password.Length >= 6;
        bool hasUppercase = Regex.IsMatch(password, "[A-Z]");
        bool hasLowercase = Regex.IsMatch(password, "[a-z]");
        bool hasSpecialChar = Regex.IsMatch(password, @"[\W_]");

        // Cambiar colores y texturas según validación
        minLengthText.color = hasMinLength ? Color.green : Color.white;
        Caracteres.texture = hasMinLength ? imagenActiva : imagenInactiva;

        uppercaseText.color = hasUppercase ? Color.green : Color.white;
        Mayusculas.texture = hasUppercase ? imagenActiva : imagenInactiva;

        lowercaseText.color = hasLowercase ? Color.green : Color.white;
        Minusculas.texture = hasLowercase ? imagenActiva : imagenInactiva;

        specialCharText.color = hasSpecialChar ? Color.green : Color.white;
        Especiales.texture = hasSpecialChar ? imagenActiva : imagenInactiva;
    }
}
