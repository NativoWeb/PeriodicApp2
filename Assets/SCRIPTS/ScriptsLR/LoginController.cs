using System.Linq;  // Asegura que esté incluido
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text.RegularExpressions;

public class LoginController : MonoBehaviour
{

    /* -----------------  Necesario para restablecer contraseña  ----------------- */
    public Button btnResetPassword;
    public Button btnSendReset; // Botón para enviar el correo
    public TMP_InputField emailResetInput;
    public TMP_Text txtResetStatus;
    public GameObject PanelRestablecerUI;
    public GameObject PanelLogin;

    /* -----------------  Necesario para intentos erroneos  ----------------- */
    private int failedAttempts = 0;
    private const int maxAttempts = 3;
    private const int lockoutTime = 10; // 5 minutos en segundos


    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Toggle toggleRememberMe;
    public Button loginButton;
    public TMP_Text txtError;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    void Start()
    {
        StartCoroutine(WaitForFirebase());
        btnResetPassword.onClick.AddListener(MostrarPanelRestablecer);
        btnSendReset.onClick.AddListener(OnSendResetClick);
        CheckLockoutStatus(); // Verifica si el usuario está bloqueado
    }

    private IEnumerator WaitForFirebase()
    {
        float tiempoMaximoEspera = 10f;
        float tiempoEspera = 0f;

        while (!DbConnexion.Instance.IsFirebaseReady() || !StartAppManager.IsReady)
        {
            Debug.Log($"Esperando... Firebase: {DbConnexion.Instance.IsFirebaseReady()}, StartAppManager: {StartAppManager.IsReady}");
            yield return new WaitForSeconds(0.5f);
            tiempoEspera += 0.5f;

            if (tiempoEspera >= tiempoMaximoEspera)
            {
                Debug.LogError("Tiempo de espera excedido.");
                yield break;
            }
        }

        auth = DbConnexion.Instance.Auth;
        firestore = DbConnexion.Instance.Firestore;

        if (auth == null || firestore == null)
        {
            Debug.LogError("Error: No se pudo obtener las referencias de Firebase.");
            yield break;
        }

        
        loginButton.onClick.AddListener(OnLoginButtonClick);
       
    }

    public void OnLoginButtonClick()
    {

        if (IsLockedOut())
        {
            txtError.text = $"Demasiados intentos fallidos. Intenta en {GetRemainingLockoutTime()} segundos.";
            return;
        }

        string email = emailInput.text;
        string password = passwordInput.text;
        SignInUserWithEmail(email, password);
    }

    private void MostrarPanelRestablecer()
    {
        PanelLogin.SetActive(false);
        PanelRestablecerUI.SetActive(true);
        txtResetStatus.text = ""; // Limpiar mensaje anterior
        emailResetInput.text = ""; // Limpiar campo de texto
    }
    public void OnSendResetClick()
    {
        string email = emailResetInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("Ingresa tu correo.", Color.red);
            return;
        }

        // 🔹 Limpiar mensajes previos antes de verificar
        ShowMessage("Verificando correo...", Color.yellow);

        // 🔍 Verificar si el correo está registrado en Firebase Firestore
        firestore.Collection("users").WhereEqualTo("Email", email).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                ShowMessage("Error de conexión. Inténtalo de nuevo.", Color.red);
                return;
            }

            QuerySnapshot snapshot = task.Result;

            if (!snapshot.Documents.Any())
            {
                ShowMessage("Correo no registrado.", Color.red);
                return;
            }

            // 📩 Si el correo existe, enviar el enlace de restablecimiento
            auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(resetTask =>
            {
                if (resetTask.IsCanceled || resetTask.IsFaulted)
                {
                    ShowMessage("Error al enviar el correo. Verifica el email.", Color.red);
                    return;
                }

                ShowMessage("¡Correo enviado! Revisa tu bandeja de entrada.", Color.green);
                StartCoroutine(HideResetPanelAfterDelay(3));
            });
        });
    }

    private void ShowMessage(string message, Color color)
    {
        txtResetStatus.text = message;
        txtResetStatus.color = color;
    }

    private IEnumerator HideResetPanelAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        PanelLogin.SetActive(true);
        PanelRestablecerUI.SetActive(false);
    }

    private void SignInUserWithEmail(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                failedAttempts++;

                txtError.text = "El usuario o la contraseña no son correctas. Inténtelo de nuevo." + "\n\n Intentos restantes: " + (maxAttempts - failedAttempts);
                PlayerPrefs.SetInt("FailedAttempts", failedAttempts);

                if (failedAttempts >= maxAttempts)
                {
                    LockUser();
                    return;
                }

                return;
            }

            // ✅ Inicio de sesión exitoso: restablecer intentos
            failedAttempts = 0;
            PlayerPrefs.SetInt("failedAttempts", 0);
            PlayerPrefs.DeleteKey("LockoutTime");
            PlayerPrefs.Save();

            FirebaseUser user = task.Result.User;
            Debug.Log("✅ Inicio de sesión exitoso: " + user.Email);

            PlayerPrefs.SetString("userId", user.UserId);
            PlayerPrefs.SetString("DisplayName", user.DisplayName);
            PlayerPrefs.SetString("Estadouser", "nube");

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

            CheckAndDownloadMisiones(user.UserId);
        });
    }

  
    /* -----------------  MÉTODOS PARA BLOQUEAR USUARIO  ----------------- */
    private void LockUser()
    {
        int lockoutEndTime = GetCurrentUnixTimestamp() + lockoutTime;
        PlayerPrefs.SetInt("LockoutTime", lockoutEndTime);
        PlayerPrefs.Save();
        txtError.text = $"⏳ Demasiados intentos fallidos. Intenta en {lockoutTime} segundos.";
        emailInput.interactable = false;
        passwordInput.interactable = false;
        loginButton.interactable = false;
        StartCoroutine(UnlockUserAfterDelay(lockoutTime));
    }

    private bool IsLockedOut()
    {
        if (!PlayerPrefs.HasKey("LockoutTime"))
            return false;

        int lockoutEndTime = PlayerPrefs.GetInt("LockoutTime");
        int currentTime = GetCurrentUnixTimestamp();

        return currentTime < lockoutEndTime;
    }

    private int GetRemainingLockoutTime()
    {
        int lockoutEndTime = PlayerPrefs.GetInt("LockoutTime");
        int currentTime = GetCurrentUnixTimestamp();
        return Mathf.Max(0, lockoutEndTime - currentTime);
    }

    private IEnumerator UnlockUserAfterDelay(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        PlayerPrefs.DeleteKey("LockoutTime");
        PlayerPrefs.SetInt("failedAttempts", 0);
        PlayerPrefs.Save();
        failedAttempts = 0;
        emailInput.interactable = true;
        passwordInput.interactable = true;
        loginButton.interactable = true;
        txtError.text = "";
    }

    private int GetCurrentUnixTimestamp()
    {
        return (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }

    private void CheckLockoutStatus()
    {
        if (IsLockedOut())
        {
            int remainingTime = GetRemainingLockoutTime();
            txtError.text = $"Demasiados intentos fallidos. Intenta en {remainingTime} segundos.";
            emailInput.interactable = false;
            passwordInput.interactable = false;
            loginButton.interactable = false;
            StartCoroutine(UnlockUserAfterDelay(remainingTime));
        }
    }


    /* ------------------------ 🔥 NUEVA FUNCIÓN PARA DESCARGAR MISIONES 🔥 ------------------------ */
    private void CheckAndDownloadMisiones(string userId)
    {
        DocumentReference userDoc = firestore.Collection("users").Document(userId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("❌ Error al obtener los datos del usuario.");
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists || !snapshot.ContainsField("misiones"))
            {
                Debug.Log("📌 No hay misiones en Firestore. Continuando con el login normal.");
                CheckUserStatus(userId);
                return;
            }

            string misionesJson = snapshot.GetValue<string>("misiones");

            if (!string.IsNullOrEmpty(misionesJson))
            {
                PlayerPrefs.SetString("misionesJSON", misionesJson);
                PlayerPrefs.Save();
                Debug.Log("✅ Misiones descargadas y guardadas localmente.");
            }

            CheckUserStatus(userId);
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

            string ocupacion = snapshot.GetValue<string>("Ocupacion");


            //bool estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje")? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;  

            bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            //bool estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento")? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;  // Valor por defecto si el campo no existe


            Debug.Log($"📌 Usuario: {ocupacion}, Estado Encuesta Aprendizaje: {estadoencuestaaprendizaje}, Estado Encuesta Conocimiento: {estadoencuestaconocimiento}");

            if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor");
            }
            else if (ocupacion == "Estudiante")
            {
                if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
                {
                    SceneManager.LoadScene("Categorías");
                }
                else
                {
                    SceneManager.LoadScene("SeleccionarEncuesta");
                }
            }
           
        });
    }

}
