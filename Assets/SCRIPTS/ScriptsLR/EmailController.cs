using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using UnityEngine.Networking;
using System.Collections;

public class EmailController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_InputField verificationCodeInput;
    public Button registerButton;
    public Button verifyButton;
    public TMP_Text verificationMessage;

    public GameObject registroPanel;  // Panel de registro
    public GameObject verificacionPanel;  // Panel de verificación

    private FirebaseAuth auth;
    private string generatedCode;
    private string registeredEmail;
    private string registeredPassword;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        verificacionPanel.SetActive(false);
        registroPanel.SetActive(true);

        registerButton.onClick.AddListener(OnRegisterButtonClick);
        verifyButton.onClick.AddListener(OnVerifyButtonClick);
    }

    public void OnRegisterButtonClick()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        if (password != confirmPassword)
        {
            verificationMessage.text = "Las contraseñas no coinciden.";
            return;
        }

        registeredEmail = email;
        registeredPassword = password;

        // Generar código de verificación aleatorio
        generatedCode = Random.Range(100000, 999999).ToString();

        // Llamar a la API para enviar el correo
        FindObjectOfType<Api>().SendVerificationEmail(email, generatedCode);

        verificationMessage.text = "Se ha enviado un código de verificación a tu correo.";

        // Cambiar al panel de verificación
        registroPanel.SetActive(false);
        verificacionPanel.SetActive(true);
    }

    public void OnVerifyButtonClick()
    {
        string userCode = verificationCodeInput.text;

        if (userCode == generatedCode)
        {
            verificationMessage.text = "Codigo de verificación exitoso.";
            RegisterUserInFirebase(registeredEmail, registeredPassword);
            StartCoroutine(registrar());
        }
        else
        {
            verificationMessage.text = "Código incorrecto. Verifica e intenta de nuevo.";
        }
    }

    private void RegisterUserInFirebase(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                Debug.Log("✅ Usuario registrado en Firebase.");
            }
            else
            {
                verificationMessage.text = "Codigo de verificación incorrecto.";
                Debug.LogError("❌ Error al registrar usuario en Firebase: " + task.Exception);
            }
        });
    }

    IEnumerator registrar()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Registrar");
    }
}
