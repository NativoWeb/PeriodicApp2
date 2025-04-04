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
using SimpleJSON;
using Google.MiniJSON;
using System.Threading.Tasks;

public class LoginController : MonoBehaviour
{

    /* -----------------  Necesario para restablecer contraseña  ----------------- */
    public Button btnResetPassword;
    public Button btnSendReset; // Botón para enviar el correo
    public TMP_InputField emailResetInput;
    public TMP_Text txtResetStatus;
    public GameObject PanelRestablecerUI;
    public GameObject PanelLogin;
    public GameObject PanelMessage;

    /* -----------------  Necesario para intentos erroneos  ----------------- */
    private int failedAttempts = 0;
    private const int maxAttempts = 3;
    private const int lockoutTime = 10; // 5 minutos en segundos


    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Toggle toggleRememberMe;
    public Button loginButton;
    public Button messageButton;
    public TMP_Text txtError;


    // pop up
    [SerializeField] private GameObject m_SinInternetUI = null;

    // Verificar internet
    private bool hayInternet = false;



    private FirebaseAuth auth;
    private FirebaseFirestore firestore;

    void Start()
    {
        StartCoroutine(WaitForFirebase());
        btnResetPassword.onClick.AddListener(MostrarPanelRestablecer);
        messageButton.onClick.AddListener(ClosePanelMessage);
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
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            if (IsLockedOut())
            {
                PanelMessage.SetActive(true);
                txtError.text = $"Demasiados intentos fallidos. Intenta en {GetRemainingLockoutTime()} segundos.";
                return;
            }

            if (VerificarCamposLoginVacios()) return; // 👈 Validación de campos vacíos

            string email = emailInput.text;
            string password = passwordInput.text;
            SignInUserWithEmail(email, password);
        }
        else
        {
            m_SinInternetUI.SetActive(true);
        }
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

                PanelMessage.SetActive(true);
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
        PanelMessage.SetActive(true);
        txtError.text = $"Demasiados intentos fallidos. Intenta en {lockoutTime} segundos.";
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
        PanelMessage.SetActive(false);
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
            PanelMessage.SetActive(true);
            txtError.text = "Demasiados intentos fallidos. Intenta en {remainingTime} segundos.";
            emailInput.interactable = false;
            passwordInput.interactable = false;
            loginButton.interactable = false;
            StartCoroutine(UnlockUserAfterDelay(remainingTime));
        }
    }


    /* ------------------------ 🔥 NUEVA FUNCIÓN PARA DESCARGAR MISIONES 🔥 ------------------------ */
    private void CheckAndDownloadMisiones(string userId)
    {
        // Referencias a los documentos de Firestore
        DocumentReference categoriasDoc = firestore
            .Collection("users").Document(userId)
            .Collection("datos").Document("categorias");

        DocumentReference misionesDoc = firestore
            .Collection("users").Document(userId)
            .Collection("datos").Document("misiones");

        // Ejecutar ambas consultas en paralelo
        Task<DocumentSnapshot> categoriasTask = categoriasDoc.GetSnapshotAsync();
        Task<DocumentSnapshot> misionesTask = misionesDoc.GetSnapshotAsync();

        Task.WhenAll(categoriasTask, misionesTask).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("❌ Error al obtener datos de Firestore.");
                return;
            }

            // Obtener resultados de las tareas
            DocumentSnapshot categoriasSnapshot = categoriasTask.Result;
            DocumentSnapshot misionesSnapshot = misionesTask.Result;

            // Verificar existencia de documentos
            if (!categoriasSnapshot.Exists || !misionesSnapshot.Exists)
            {
                Debug.LogWarning("⚠️ No se encontraron categorías o misiones en Firestore.");
                CheckUserStatus(userId);
                return;
            }

            // Obtener datos de Firestore
            string categoriasJson = categoriasSnapshot.ContainsField("categorias") ? categoriasSnapshot.GetValue<string>("categorias") : null;
            string misionesJson = misionesSnapshot.ContainsField("misiones") ? misionesSnapshot.GetValue<string>("misiones") : null;


            // Validar si hay datos antes de guardar
            if (!string.IsNullOrEmpty(categoriasJson))
            {
                PlayerPrefs.SetString("CategoriasOrdenadas", categoriasJson);
            }
            else
            {
                Debug.LogWarning("⚠️ No se guardaron categorías porque están vacías.");
            }

            PlayerPrefs.Save();
            Debug.Log("✅ Misiones y categorías guardadas en PlayerPrefs.");
            CheckUserStatus(userId);
        });
    }

    private void CheckUserStatus(string userId)
    {

        // verificar si hay wifi
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // instanciar variables 
        bool estadoencuestaaprendizaje = false;
        bool estadoencuestaconocimiento = false;

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

            if (hayInternet)
            {
                estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;
                estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;  // Valor por defecto si el campo no existe
            }
            else
            {
                estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
                estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            }


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

    private void ClosePanelMessage()
    {
        PanelMessage.SetActive(false);
    }

    private bool VerificarCamposLoginVacios()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            txtError.text = "Hay campos vacíos, por favor completa todos los campos.";
            PanelMessage.SetActive(true);
            return true; // Sí están vacíos
        }

        return false; // No están vacíos
    }

}
