using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
public class LoginController : MonoBehaviour
{
    [Header("UI Login")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;
    public Button loginButton;
    public Toggle toggleRememberMe;

    [Header("UI Recuperar Contraseña")]
    public Button btnResetPassword;
    public Button btnSendReset;
    public TMP_InputField emailResetInput;
    public TMP_Text txtResetStatus;
    public GameObject panelLogin;
    public GameObject panelRestablecerUI;
    public GameObject panelMessage;

    [Header("Otros")]
    [SerializeField] private GameObject sinInternetPopup;

    // UseCases
    private LoginUsuario loginUseCase;
    private ResetearPassword resetPasswordUseCase;
    private GestionarIntentosFallidos intentosFallidosUseCase;
    private VerificarEstadoUsuario verificarEstadoUsuarioUseCase;

    private async void Start()
    {
        bool listo = await FirebaseServiceLocator.InicializarFirebase();

        if (!listo)
        {
            Debug.LogError("Firebase no se inicializó correctamente.");
            return;
        }

        var authService = new FirebaseAuthService(FirebaseServiceLocator.Auth);
        var firestoreService = new FirestoreService(FirebaseServiceLocator.Firestore);
        var localStorage = new LocalStorageService();


        loginUseCase = new LoginUsuario(authService, localStorage);
        resetPasswordUseCase = new ResetearPassword(authService);
        intentosFallidosUseCase = new GestionarIntentosFallidos(localStorage);
        verificarEstadoUsuarioUseCase = new VerificarEstadoUsuario(firestoreService);

        loginButton.onClick.AddListener(OnLoginButtonClick);
        btnSendReset.onClick.AddListener(OnSendResetPasswordClick);
        btnResetPassword.onClick.AddListener(MostrarPanelRestablecer);

    }



    private async void OnLoginButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            sinInternetPopup.SetActive(true);
            return;
        }

        if (intentosFallidosUseCase.EstaBloqueado())
        {
            MostrarError($"Demasiados intentos fallidos. Intenta en {intentosFallidosUseCase.TiempoRestante()} segundos.");
            return;
        }

        if (VerificarCamposLoginVacios()) return;

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        var resultado = await loginUseCase.Ejecutar(email, password);

        if (resultado.Exito)
        {
            Debug.Log($"Usuario logueado: {resultado.UsuarioId}");

            PlayerPrefs.SetInt("rememberMe", 1);
            PlayerPrefs.SetString("userEmail", email);
            PlayerPrefs.SetString("userPassword", password);
            
            intentosFallidosUseCase.ResetearIntentos(); // Éxito: resetea intentos
            OnLoginSuccess();
        }
        else
        {
            intentosFallidosUseCase.RegistrarIntentoFallido();
            MostrarError(resultado.MensajeError);
        }
    }

    private async void OnSendResetPasswordClick()
    {
        string email = emailResetInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            MostrarResetError("Ingresa tu correo.", Color.red);
            return;
        }

        bool enviado = await resetPasswordUseCase.Ejecutar(email);

        if (enviado)
        {
            MostrarResetError("¡Correo enviado! Revisa tu bandeja de entrada.", Color.green);
            StartCoroutine(HideResetPanelAfterDelay(3));
        }
        else
        {
            MostrarResetError("Error al enviar el correo. Verifica tu email.", Color.red);
        }
    }

    private async void OnLoginSuccess()
    {
        string userId = FirebaseServiceLocator.Auth.CurrentUser?.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            await verificarEstadoUsuarioUseCase.Ejecutar(userId);
        }
        else
        {
            Debug.LogError("Usuario no autenticado.");
        }
    }

    private void MostrarPanelRestablecer()
    {
        panelLogin.SetActive(false);
        panelRestablecerUI.SetActive(true);
        txtResetStatus.text = "";
        emailResetInput.text = "";
    }

    private void MostrarError(string mensaje)
    {
        panelMessage.SetActive(true);
        errorText.text = mensaje;
    }

    private void MostrarResetError(string mensaje, Color color)
    {
        txtResetStatus.text = mensaje;
        txtResetStatus.color = color;
    }

    private bool VerificarCamposLoginVacios()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            MostrarError("Hay campos vacíos, por favor completa todos los campos.");
            return true;
        }

        return false;
    }

    private IEnumerator HideResetPanelAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        panelLogin.SetActive(true);
        panelRestablecerUI.SetActive(false);
    }
}
