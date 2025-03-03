using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class EmailController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Button registerButton;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    public static string currentUserID;
    // Referencia al componente Api
    public Api api;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (app != null)
            {
                auth = FirebaseAuth.DefaultInstance;
                firestore = FirebaseFirestore.DefaultInstance;
                registerButton.onClick.AddListener(OnRegisterButtonClick);
            }
            else
            {
                Debug.LogError("Firebase no se ha podido inicializar.");
            }
        });
    }

    public void OnRegisterButtonClick()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        if (password != confirmPassword)
        {
            Debug.LogError("Las contraseñas no coinciden.");
            return;
        }

        // Crear usuario con correo y contraseña
        CreateUserWithEmail(email, password);
    }

    private void CreateUserWithEmail(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("La solicitud fue cancelada.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error al registrar usuario: " + task.Exception?.Message);
                return;
            }

            // Si todo va bien, obtenemos el FirebaseUser desde el AuthResult
            AuthResult authResult = task.Result;
            FirebaseUser newUser = authResult.User;


            // Generar código de verificación aleatorio
            string verificationCode = GenerateVerificationCode();
            Debug.Log(verificationCode);

            // Guardar el código en Firestore
            SaveVerificationCode(newUser.UserId, verificationCode);

            // Enviar correo con el código de verificación
            SendVerificationEmail(newUser.Email, verificationCode);

            // Ir a la escena de verificación
            SceneManager.LoadScene("Codigo");
        });
    }

    private string GenerateVerificationCode()
    {
        // Usar System.Random para generar un código aleatorio
        System.Random random = new System.Random();
        return random.Next(100000, 999999).ToString();
    }

    private void SaveVerificationCode(string userId, string verificationCode)
    {

        DocumentReference docRef = firestore.Collection("users").Document(userId);
        docRef.SetAsync(new { VerificationCode = verificationCode }).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Error al guardar el código de verificación.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error al guardar el código de verificación: " + task.Exception?.Message);
                return;
            }
            Debug.Log("Código de verificación guardado en Firestore.");
        });
    }

    private void SendVerificationEmail(string email, string verificationCode)
    {
        // Verifica que la referencia al componente Api esté asignada
        if (api != null)
        {
            api.SendVerificationEmail(email, verificationCode);
        }
        else
        {
            Debug.LogError("La referencia al componente Api no está asignada.");
        }
    }
}