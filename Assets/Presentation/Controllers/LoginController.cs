using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using System.Collections;
using UnityEngine.SceneManagement;
using Infrastructure.Services;
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

    private async void Start()
    {
        bool listo = await FirebaseServiceLocator.InicializarFirebase();

        if (!listo)
        {
            Debug.LogError("Firebase no se inicializó correctamente.");
            return;
        }

        var authService = new FirebaseAuthService(FirebaseServiceLocator.Auth);
        var localStorage = new LocalStorageService();

        loginUseCase = new LoginUsuario(authService, localStorage);
        resetPasswordUseCase = new ResetearPassword(authService);
        intentosFallidosUseCase = new GestionarIntentosFallidos(localStorage);

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
            Debug.Log($"✅ Usuario logueado: {resultado.UsuarioId}");

            // Opcional: Aquí puedes cargar escena o seguir flujo
            SceneManager.LoadScene("Inicio"); // O ajusta según tu lógica
            intentosFallidosUseCase.ResetearIntentos(); // Éxito: resetea intentos
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
