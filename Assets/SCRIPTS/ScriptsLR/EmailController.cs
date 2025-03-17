using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class EmailController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Button registerButton;
    public Button verifyButton;
    public TMP_Text verificationMessage;

    public GameObject registroPanel;  // Panel de registro
    public GameObject verificacionPanel;  // Panel de verificación

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;

    void Start()
    {
        // Aseguramos que Firebase esté inicializado desde DbConnexion
        if (DbConnexion.Instance.IsFirebaseReady())
        {
            auth = DbConnexion.Instance.Auth;
            firestore = DbConnexion.Instance.Firestore;
            registerButton.onClick.AddListener(OnRegisterButtonClick);
            verifyButton.onClick.AddListener(OnVerifyButtonClick);

            // Asegurarse de que el panel de verificación esté oculto al inicio
            verificacionPanel.SetActive(false);
            registroPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Firebase no está inicializado correctamente.");
        }
    }

    public void OnRegisterButtonClick()
    {
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        if (password != confirmPassword)
        {
            Debug.LogError("❌ Las contraseñas no coinciden.");
            return;
        }

        // Crear usuario con correo y contraseña
        CreateUserWithEmail(email, password);
    }

    private void CreateUserWithEmail(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Error al registrar usuario: " + task.Exception?.Message);
                return;
            }

            // Usuario registrado exitosamente
            currentUser = auth.CurrentUser;

            // Enviar correo de verificación
            SendVerificationEmail();
        });
    }

    private void SendVerificationEmail()
    {
        if (currentUser != null)
        {
            currentUser.SendEmailVerificationAsync().ContinueWithOnMainThread(task => {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("❌ Error al enviar el correo de verificación: " + task.Exception?.Message);
                    return;
                }

                Debug.Log("✅ Correo de verificación enviado.");

                // Mostrar el mensaje en el TMP_Text
                verificationMessage.text = "📩 Se ha enviado un correo de verificación. Por favor, revisa tu bandeja de entrada y haz clic en el enlace.";

                // 🔄 Cambiar de panel: Ocultar el de registro y mostrar el de verificación
                registroPanel.SetActive(false);
                verificacionPanel.SetActive(true);
            });
        }
        else
        {
            Debug.LogError("❌ No hay un usuario autenticado.");
        }
    }

    public void OnVerifyButtonClick()
    {
        StartCoroutine(VerifyEmailRoutine());
    }

    private IEnumerator VerifyEmailRoutine()
    {
        verificationMessage.text = "🔄 Verificando email...";
        verifyButton.interactable = false;

        while (true)
        {
            Debug.Log("🔄 Recargando usuario...");
            var reloadTask = currentUser.ReloadAsync();
            yield return new WaitUntil(() => reloadTask.IsCompleted);

            if (reloadTask.IsFaulted)
            {
                Debug.LogError($"❌ Error al recargar usuario: {reloadTask.Exception?.Message}");
                verificationMessage.text = "⚠️ Error al verificar el correo. Intenta nuevamente.";
                verifyButton.interactable = true;
                yield break;
            }

            Debug.Log($"🔍 Estado de verificación: {currentUser.IsEmailVerified}");

            if (currentUser.IsEmailVerified)
            {
                Debug.Log("✅ Email verificado correctamente. Avanzando a la siguiente escena...");
                SceneManager.LoadScene("Registrar");
                yield break;
            }

            yield return new WaitForSeconds(3);
        }
    }
}
