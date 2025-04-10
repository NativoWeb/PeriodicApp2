﻿using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using Vuforia;
using System.Text.RegularExpressions;
using System;

public class EmailController : MonoBehaviour
{

    // validar wifi
    private bool hayInternet = false;
    

    //pop up 
    [SerializeField] private GameObject m_SinInternetUI = null;

    /* -----------------  VALIDAR CONTRASEÑA  ----------------- */

    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public GameObject requirementsPanel;  // Panel con los requisitos
    public TMP_Text minLengthText, uppercaseText, lowercaseText, specialCharText; // Textos de cada requisito
    public TMP_Text txtMessage;

    public Texture2D imagenActiva;
    public Texture2D imagenInactiva;



    public RawImage Caracteres;
    public RawImage Mayusculas;
    public RawImage Minusculas;
    public RawImage Especiales;



    public TMP_InputField emailInput;
    public TMP_InputField verificationCodeInput;
    public Button registerButton;
    public Button verifyButton;
    public Button editButton;
    public TMP_Text verificationMessage;
    public TMP_Text CorreoMessage;
    public GameObject registroPanel;
    public GameObject verificacionPanel;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    private string generatedCode;
    private string userEmail;

    // Lista de dominios permitidos
    private string[] allowedDomains = {"gmail.com", "outlook.com", "outlook.es", "yahoo.com", "hotmail.com", "icloud.com", "aol.com", "zoho.com", "mail.com"};

    private const string apiKey = "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";  // Reemplaza con tu API Key de Brevo
    private const string url = "https://api.brevo.com/v3/smtp/email";

    void Start()
    {
        /* -----------------  VALIDAR CONTRASEÑA----------------- */
        passwordInput.onSelect.AddListener(ShowRequirements);
        passwordInput.onValueChanged.AddListener(ValidatePassword);
        passwordInput.onDeselect.AddListener(HideRequirements);
        requirementsPanel.SetActive(false);

        verificacionPanel.SetActive(false);
        registroPanel.SetActive(true);
        StartCoroutine(WaitForFirebase());
    }

    private IEnumerator WaitForFirebase()
    {
        float tiempoMaximoEspera = 3f;
        float tiempoEspera = 0f;

        while (!DbConnexion.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.5f);
            tiempoEspera += 0.5f;

            if (tiempoEspera >= tiempoMaximoEspera)
            {
                Debug.LogError("🚨 Tiempo de espera excedido. Firebase no está listo.");
                yield break;
            }
        }

        auth = DbConnexion.Instance.Auth;
        firestore = DbConnexion.Instance.Firestore;

        if (auth == null || firestore == null)
        {
            Debug.LogError("🚨 Error: No se pudo obtener las referencias de Firebase.");
            yield break;
        }
        registerButton.onClick.AddListener(OnRegisterButtonClick);
        verifyButton.onClick.AddListener(OnVerifyButtonClick);
    }



    /* -----------------  MÉTODOS PARA VALIDAR CONTRASEÑA----------------- */

    void ShowRequirements(string text)
    {
        txtMessage.text = "";
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
        minLengthText.color = hasMinLength ? Color.green : Color.white;
        Caracteres.texture = hasMinLength ? imagenActiva : imagenInactiva;
        uppercaseText.color = hasUppercase ? Color.green : Color.white;
        Mayusculas.texture = hasUppercase ? imagenActiva : imagenInactiva;
        lowercaseText.color = hasLowercase ? Color.green : Color.white;
        Minusculas.texture = hasLowercase ? imagenActiva : imagenInactiva;
        specialCharText.color = hasSpecialChar ? Color.green : Color.white;
        Especiales.texture = hasSpecialChar ? imagenActiva : imagenInactiva;
    }


    public void OnRegisterButtonClick()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {


            string email = emailInput.text.Trim();
            string password = passwordInput.text;
            string confirmPassword = confirmPasswordInput.text;

            // Verificar si los campos están vacíos
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                txtMessage.text = "Por favor, completa todos los campos.";
                txtMessage.color = Color.red;
                return;
            }

            // Validar el formato del correo
            if (!IsValidEmail(email))
            {
                txtMessage.text = "El correo ingresado no tiene un formato válido.";
                txtMessage.color = Color.red;
                return;
            }

            // Validar si el dominio es permitido
            if (!IsAllowedDomain(email))
            {
                txtMessage.text = "El dominio del correo es invalido.";
                txtMessage.color = Color.red;
                return;
            }

            txtMessage.text = "Correo válido.";
            txtMessage.color = Color.green;

            // Verificar si las contraseñas coinciden
            if (password != confirmPassword)
            {
                txtMessage.text = "Las contraseñas no coinciden.";
                txtMessage.color = Color.red;
                return;
            }

            // Validar contraseña
            bool hasMinLength = password.Length >= 6;
            bool hasUppercase = Regex.IsMatch(password, "[A-Z]");
            bool hasLowercase = Regex.IsMatch(password, "[a-z]");
            bool hasSpecialChar = Regex.IsMatch(password, @"[\^\$\*\.\[\]\{\}\(\)\?\""!@#%&/\\,><':;|_~`]");

            if (!hasMinLength || !hasUppercase || !hasLowercase || !hasSpecialChar)
            {
                txtMessage.text = "La contraseña no cumple con los requisitos solicitados.";
                txtMessage.color = Color.red;
                return;
            }

            // Si todo está correcto, registrar usuario
            txtMessage.text = "Registrando usuario...";
            txtMessage.color = Color.green;
            CreateUserWithEmail(email, password);

            // acá guardo los player para si todo sale bien, guarde en registercontroller

            PlayerPrefs.SetString("userEmail", email);
            PlayerPrefs.SetString("userPassword", password);
        }
        else
        {
            m_SinInternetUI.SetActive(true);
        }
    }

    // Método para validar el formato del correo electrónico
    private bool IsValidEmail(string email)
    {
        string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }

    // Método para validar si el dominio del correo es permitido
    private bool IsAllowedDomain(string email)
    {
        string domain = email.Split('@')[1]; // Extrae el dominio después del '@'

        foreach (string allowedDomain in allowedDomains)
        {
            if (domain.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase))
            {
                return true; // El dominio es válido
            }
        }
        return false; // El dominio no está en la lista permitida
    }

    private void CreateUserWithEmail(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                txtMessage.text = "Correo electronico en uso.";
                txtMessage.color = Color.red;
                return;
            }

            currentUser = auth.CurrentUser;
            userEmail = email;

            // Guardar el ID del usuario recién creado en PlayerPrefs para la siguiente escena
            PlayerPrefs.SetString("UsuarioEliminar", currentUser.UserId);
            PlayerPrefs.Save();

            System.Random random = new System.Random();
            generatedCode = random.Next(100000, 999999).ToString();
            SendVerificationEmail(userEmail, generatedCode);
        });
    }

    private void SendVerificationEmail(string email, string code)
    {
        StartCoroutine(SendEmailCoroutine(email, code));
    }

    private IEnumerator SendEmailCoroutine(string email, string code)
    {
        string jsonPayload = "{ " +
                    "\"sender\": { \"name\": \"PeriodicApp\", \"email\": \"periodicappoficial@gmail.com\" }, " +
                    "\"to\": [{ \"email\": \"" + email + "\", \"name\": \"Usuario\" }], " +
                    "\"subject\": \"🔐 Código de Verificación - PeriodicApp\", " +
                    "\"htmlContent\": \"" +
                    "<div style='font-family: Arial, sans-serif; text-align: center; background-color: #f4f4f4; padding: 20px;'>" +
                        "<div style='max-width: 500px; margin: auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);'>" +
                            "<h2 style='color: #332C85;'>🔐 Código de Verificación</h2>" +
                            "<p style='font-size: 16px; color: #333;'>¡Hola! Gracias por registrarte en <strong>PeriodicApp</strong>. Para continuar, usa el siguiente código de verificación:</p>" +
                            "<div style='font-size: 24px; font-weight: bold; color: #ffffff; background: #332C85; padding: 10px; display: inline-block; border-radius: 5px; margin: 10px 0;'>" +
                                code + "</div>" +
                            "<p style='font-size: 14px; color: #666;'>Este código expirará en 10 minutos.</p>" +
                            "<hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>" +
                            "<p style='font-size: 12px; color: #777;'>Si no solicitaste este código, puedes ignorar este mensaje.</p>" +
                        "</div>" +
                    "</div>" +
                    "\" }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("api-key", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                verificationMessage.text = "Ingresa el código que te enviamos a tu correo.";
                Color warningColor = new Color(1f, 0.65f, 0f); // Naranja fuerte
                verificationMessage.color = warningColor;
                registroPanel.SetActive(false);
                CorreoMessage.text = "Correo enviado a: " + email;
                CorreoMessage.color = Color.white;
                editButton.onClick.AddListener(VolverEmail);
                verificacionPanel.SetActive(true);
                
            }
            else
            {
                Debug.LogError("Error al enviar el correo: " + request.responseCode + " - " + request.error);
                Debug.LogError("Respuesta: " + request.downloadHandler.text);
            }
        }
    }

    void VolverEmail()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        verificacionPanel.SetActive(false);
        registroPanel.SetActive(true);
        emailInput.text = "";
        passwordInput.text = passwordInput.text;


        if (hayInternet)
        {
            StartCoroutine(DeleteAccount());
        }

    }
    private IEnumerator DeleteAccount()
    {

        string UserEliminarId = PlayerPrefs.GetString("UsuarioEliminar", "");

        if (!string.IsNullOrEmpty(UserEliminarId))
        {
            FirebaseUser user = auth.CurrentUser;
            if (user != null && user.UserId == UserEliminarId)
            {
                var deleteTask = user.DeleteAsync();
                yield return new WaitUntil(() => deleteTask.IsCompleted);

                if (deleteTask.IsCompletedSuccessfully)
                {
                    Debug.Log("Cuenta eliminada por falta de conexión.");

                    PlayerPrefs.DeleteKey("UsuarioEliminar");
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.LogError("Error al eliminar la cuenta.");
                }
            }
        }
        else
        {
            Debug.Log("No hay Cuentas pendientes por eliminar. Desde StartApp");
        }
    }

    public void OnVerifyButtonClick()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {


            Debug.Log(generatedCode);
            Debug.Log(verificationCodeInput.text);
            if (verificationCodeInput.text == generatedCode)
            {
                verificationMessage.text = "Código verificado correctamente. Avanzando a la siguiente escena...";
                verificationMessage.color = Color.green;
                SceneManager.LoadScene("Registrar");
            }
            else
            {
                verificationMessage.text = "Código incorrecto. Intenta nuevamente.";
            }
        }
        else
        {
            string usuarioaeliminar = PlayerPrefs.GetString("UsuarioEliminar", "");
            PlayerPrefs.Save();
            m_SinInternetUI.SetActive(true);
        }

    }
}
