using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore; // Importa Firestore
using TMPro; // Importa el espacio de nombres de TMP
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    void Start()
    {
        // Inicializar Firebase Auth y Firestore
        auth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;

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
                Debug.LogError($"Error: {task.Exception?.Message}");
                return;
            }

            AuthResult authResult = task.Result;
            FirebaseUser user = authResult.User;

            Debug.Log("✅ Inicio de sesión exitoso! Bienvenido, " + user.Email);

            // 🔹 Guardamos el userId en PlayerPrefs
            Debug.Log($"🆔 Guardando userId en PlayerPrefs: {user.UserId}");
            PlayerPrefs.SetString("userId", user.UserId);
            PlayerPrefs.Save();


            // 🔹 Verificar ocupación y encuesta en Firestore
            CheckUserStatus(user.UserId);
        });
    }

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

            // Obtener datos de Firestore
            string ocupacion = snapshot.GetValue<string>("Ocupacion");
            bool encuestaCompletada;
            if (snapshot.ContainsField("EncuestaCompletada"))
            {
                encuestaCompletada = snapshot.GetValue<bool>("EncuestaCompletada");
                Debug.Log($"📌 Estado Encuesta en Firestore: {encuestaCompletada}");
            }
            else
            {
                Debug.LogWarning("⚠️ Campo EncuestaCompletada no encontrado en Firestore. Asignando false.");
                encuestaCompletada = false;
            }


            Debug.Log($"📌 Usuario: {ocupacion}, EncuestaCompletada: {encuestaCompletada}");

            // 🔹 Redirigir según el tipo de usuario y estado de la encuesta
            if (ocupacion == "Estudiante")
            {
                if (encuestaCompletada)
                {
                    SceneManager.LoadScene("Inicio"); // Ir a la vista principal
                }
                else
                {
                    SceneManager.LoadScene("EcnuestaScen1e"); // Ir a la encuesta
                }
            }
            else if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor"); // Ir a la vista del profesor
            }
        });
    }
}
