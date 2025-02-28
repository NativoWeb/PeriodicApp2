using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro; // Importa el espacio de nombres de TMP
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    public TMP_InputField emailInput;        // Cambiado a TMP_InputField
    public TMP_InputField passwordInput;     // Cambiado a TMP_InputField
    public Button loginButton;

    private FirebaseAuth auth;

    void Start()
    {
        // Inicializar Firebase Auth
        auth = FirebaseAuth.DefaultInstance;

        loginButton.onClick.AddListener(OnLoginButtonClick);
    }

    public void OnLoginButtonClick()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        SignInUserWithEmail(email, password);
    }

    private void SignInUserWithEmail(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("La solicitud fue cancelada.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error: {task.Exception?.Message}");
                return;
            }

            // Accede al AuthResult y luego al FirebaseUser
            AuthResult authResult = task.Result;
            FirebaseUser user = authResult.User;

            Debug.Log("Inicio de sesi�n exitoso! Bienvenido, " + user.Email);
            // Aqu� puedes navegar a la pantalla principal o men� de tu juego.

            SceneManager.LoadScene("Inicio");
        });
    }
}
