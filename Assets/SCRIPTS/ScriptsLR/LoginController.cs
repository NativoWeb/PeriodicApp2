using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

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
        StartCoroutine(WaitForFirebase());
    }

    private IEnumerator WaitForFirebase()
    {
        float tiempoMaximoEspera = 10f;
        float tiempoEspera = 0f;

        while (!DbConnexion.Instance.IsFirebaseReady() || !StartAppManager.IsReady)
        {
            Debug.Log($"⏳ Esperando... Firebase: {DbConnexion.Instance.IsFirebaseReady()}, StartAppManager: {StartAppManager.IsReady}");
            yield return new WaitForSeconds(0.5f);
            tiempoEspera += 0.5f;

            if (tiempoEspera >= tiempoMaximoEspera)
            {
                Debug.LogError("🚨 Tiempo de espera excedido.");
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

        AutoLogin();
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
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("❌ Error en inicio de sesión.");
                TryOfflineLogin(email, password);
                return;
            }

            FirebaseUser user = task.Result.User;
            Debug.Log("✅ Inicio de sesión exitoso: " + user.Email);

            PlayerPrefs.SetString("userId", user.UserId);

            //guardar el Display name para luego mostrarlo nuevamente
            PlayerPrefs.SetString("DisplayName", user.DisplayName);

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
            PlayerPrefs.SetString("Estadouser", "nube");
            PlayerPrefs.Save();

            // 🔹 Verificar y descargar misiones antes de continuar
            CheckAndDownloadMisiones(user.UserId);
        });
    }

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
                    PlayerPrefs.SetString("Estadouser", "nube");
                    PlayerPrefs.Save();

                    CheckAndDownloadMisiones(user.UserId);
                }
                else
                {
                    Debug.LogError("❌ Error en login automático.");
                    TryOfflineLogin(savedEmail, savedPassword);
                }
            });
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
            bool encuestaCompletada = snapshot.ContainsField("EncuestaCompletada")
                ? snapshot.GetValue<bool>("EncuestaCompletada")
                : false;

            Debug.Log($"📌 Usuario: {ocupacion}, EncuestaCompletada: {encuestaCompletada}");

            if (ocupacion == "Estudiante")
            {
                SceneManager.LoadScene(encuestaCompletada ? "Categorías" : "EcnuestaScen1e");
            }
            else if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor");
            }
        });
    }

    private void TryOfflineLogin(string email, string password)
    {
        if (PlayerPrefs.HasKey("userEmail") && PlayerPrefs.HasKey("userPassword") && PlayerPrefs.HasKey("userId"))
        {
            string savedEmail = PlayerPrefs.GetString("userEmail");
            string savedPassword = PlayerPrefs.GetString("userPassword");
            string savedUserId = PlayerPrefs.GetString("userId");

            if (email == savedEmail && password == savedPassword)
            {
                Debug.Log("📴 ✅ Inicio de sesión sin conexión exitoso.");
                SceneManager.LoadScene("Categorías");
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
