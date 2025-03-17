﻿using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore; // Importa Firestore
using TMPro; // Importa TMP
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Toggle toggleRememberMe;
    public Button loginButton;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    void Start()
    {
        // Esperar a que Firebase esté listo
        if (DbConnexion.Instance.IsFirebaseReady())
        {
            auth = DbConnexion.Instance.Auth;
            firestore = DbConnexion.Instance.Firestore;
        }
        else
        {
            Debug.LogError("❌ Firebase no está listo. Asegúrate de que DbConnexion esté inicializado.");
            return;
        }

        AutoLogin(); // Intenta login automático
        loginButton.onClick.AddListener(OnLoginButtonClick); // Escuchar botón login
    }

    /*------------------------ CUANDO SE OPRIME EL BOTÓN DE LOGIN ------------------------*/
    public void OnLoginButtonClick()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        SignInUserWithEmail(email, password); // Llama login normal
    }

    /*------------------------ LOGIN CON FIREBASE ------------------------*/
    private void SignInUserWithEmail(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("❌ La solicitud fue cancelada.");
                TryOfflineLogin(email, password); // Intentar login offline
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Error: {task.Exception?.Message}");
                TryOfflineLogin(email, password); // Intentar login offline
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser user = authResult.User;

            Debug.Log("✅ Inicio de sesión exitoso! Bienvenido, " + user.Email);

            // 🔹 Guardar datos en PlayerPrefs
            PlayerPrefs.SetString("userId", user.UserId);
            if (toggleRememberMe.isOn)
            {
                PlayerPrefs.SetString("userEmail", email);
                PlayerPrefs.SetString("userPassword", password);
                PlayerPrefs.SetInt("rememberMe", 1);
            }
            else
            {
                PlayerPrefs.DeleteKey("userEmail");
                PlayerPrefs.DeleteKey("userPassword");
                PlayerPrefs.SetInt("rememberMe", 0);
            }
            PlayerPrefs.Save();

            // 🔹 Verificar ocupación y encuesta en Firestore
            CheckUserStatus(user.UserId);
        });
    }

    /*------------------------ LOGIN AUTOMÁTICO CON REMEMBER ME ------------------------*/
    void AutoLogin()
    {
        if (PlayerPrefs.GetInt("rememberMe") == 1)
        {
            string savedEmail = PlayerPrefs.GetString("userEmail");
            string savedPassword = PlayerPrefs.GetString("userPassword");

            auth.SignInWithEmailAndPasswordAsync(savedEmail, savedPassword).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    Debug.Log("✅ Login automático exitoso");
                    FirebaseUser user = task.Result.User;
                    PlayerPrefs.SetString("userId", user.UserId);
                    PlayerPrefs.Save();

                    CheckUserStatus(user.UserId); // Ir según ocupación
                }
                else
                {
                    Debug.LogError("❌ Error en login automático: " + task.Exception);
                    // Intentar login offline
                    TryOfflineLogin(savedEmail, savedPassword);
                }
            });
        }
    }

    /*------------------------ REVISAR STATUS EN FIRESTORE ------------------------*/
    private void CheckUserStatus(string userId)
    {
        DocumentReference docRef = firestore.Collection("users").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Error al obtener los datos del usuario.");
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogError("❌ No se encontraron datos para este usuario.");
                return;
            }

            // Datos Firestore
            string ocupacion = snapshot.GetValue<string>("Ocupacion");
            bool encuestaCompletada = snapshot.ContainsField("EncuestaCompletada")
                ? snapshot.GetValue<bool>("EncuestaCompletada")
                : false;

            Debug.Log($"📌 Usuario: {ocupacion}, EncuestaCompletada: {encuestaCompletada}");

            // 🔹 Ir a escena según ocupación
            if (ocupacion == "Estudiante")
            {
                SceneManager.LoadScene(encuestaCompletada ? "Inicio" : "EncuestaScene1");
            }
            else if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor");
            }
        });
    }

    /*------------------------ LOGIN SIN INTERNET --------------------------------------------------------*/
    private void TryOfflineLogin(string email, string password)
    {
        // Verificar si hay datos guardados y coinciden
        if (PlayerPrefs.HasKey("userEmail") && PlayerPrefs.HasKey("userPassword") && PlayerPrefs.HasKey("userId"))
        {
            string savedEmail = PlayerPrefs.GetString("userEmail");
            string savedPassword = PlayerPrefs.GetString("userPassword");
            string savedUserId = PlayerPrefs.GetString("userId");

            if (email == savedEmail && password == savedPassword)
            {
                Debug.Log("📴 ✅ Inicio de sesión sin conexión exitoso.");

                SceneManager.LoadScene("InicioOffline");
            }
            else
            {
                Debug.LogError("📴 ❌ Datos incorrectos para el inicio de sesión offline.");
            }
        }
        else
        {
            Debug.LogError("📴 ❌ No hay datos guardados para inicio de sesión offline.");
        }
    }
}
