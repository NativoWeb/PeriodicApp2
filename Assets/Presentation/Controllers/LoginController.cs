using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Infrastructure.Services;
using PeriodicApp.Presentation;

public class LoginController : MonoBehaviour
{
    [Header("UI Login")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;
    public Button loginButton;
    public Toggle toggleRememberMe;

    [Header("UI de Idiomas")]
    public Image RawEspañol;
    public Image RawIngles;
    public GameObject contenedorIdiomas;
    public Button btnIdiomas;
    public Button btnEspañol;
    public Button btnIngles;
    public TMP_Text txtIdiomas;

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

    private void Start()
    {
        StartCoroutine(InicializarAsync());
    }

    private IEnumerator InicializarAsync()
    {
        // Verificar ServiceLocator y crearlo si no existe
        if (!ServiceLocator.AreServicesInitialized())
        {
            Debug.LogWarning("ServiceLocator no está inicializado. Creando instancia...");

            ServiceLocator existingLocator = FindObjectOfType<ServiceLocator>();

            if (existingLocator == null)
            {
                GameObject serviceLocatorObj = new GameObject("ServiceLocator");
                serviceLocatorObj.AddComponent<ServiceLocator>();
                Debug.Log("ServiceLocator creado exitosamente");
            }

            yield return null;

            if (!ServiceLocator.AreServicesInitialized())
            {
                Debug.LogError("No se pudo inicializar ServiceLocator");
                yield break;
            }
        }

        // Inicializar Firebase
        var firebaseTask = FirebaseServiceLocator.InicializarFirebase();
        while (!firebaseTask.IsCompleted)
        {
            yield return null;
        }

        bool listo = firebaseTask.Result;

        if (!listo)
        {
            Debug.LogError("Firebase no se inicializó correctamente.");
            yield break;
        }

        // Inicializar servicios
        var authService = new FirebaseAuthService(FirebaseServiceLocator.Auth);
        var firestoreService = new FirestoreService(FirebaseServiceLocator.Firestore);
        var localStorage = new LocalStorageService();

        loginUseCase = new LoginUsuario(authService, localStorage);
        resetPasswordUseCase = new ResetearPassword(authService);
        intentosFallidosUseCase = new GestionarIntentosFallidos(
            localStorage,
            ServiceLocator.PlayerPrefs
        );

        verificarEstadoUsuarioUseCase = new VerificarEstadoUsuario(
            firestoreService,
            ServiceLocator.PlayerPrefs,
            ServiceLocator.Network,
            ServiceLocator.Logger,
            ServiceLocator.Scene,
            ServiceLocator.Persistence,
            ServiceLocator.ResourceLoader
        );

        // Configurar botones
        loginButton.onClick.AddListener(() => StartCoroutine(OnLoginButtonClickCoroutine()));
        btnSendReset.onClick.AddListener(() => StartCoroutine(OnSendResetPasswordClickCoroutine()));
        btnResetPassword.onClick.AddListener(MostrarPanelRestablecer);

        // Configurar idioma
        int locale = ServiceLocator.PlayerPrefs.GetInt("LocaleKey", 0);
        switch (locale)
        {
            case 0:
                txtIdiomas.text = "Español";
                RawIngles.gameObject.SetActive(false);
                RawEspañol.gameObject.SetActive(true);
                break;
            case 1:
                txtIdiomas.text = "English";
                RawEspañol.gameObject.SetActive(false);
                RawIngles.gameObject.SetActive(true);
                break;
        }

        btnIdiomas.onClick.AddListener(abrirPanelIdiomas);
        btnEspañol.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(0));
        btnIngles.onClick.AddListener(() => CambiarIdiomaY_CerrarPanel(1));
    }

    public void abrirPanelIdiomas()
    {
        contenedorIdiomas.SetActive(true);
    }

    private void CambiarIdiomaY_CerrarPanel(int id)
    {
        if (ControladorIdioma.instancia != null)
        {
            ControladorIdioma.instancia.ChangeLocale(id);
        }
        switch (id)
        {
            case 0:
                txtIdiomas.text = "Español";
                RawIngles.gameObject.SetActive(false);
                RawEspañol.gameObject.SetActive(true);
                break;
            case 1:
                txtIdiomas.text = "English";
                RawEspañol.gameObject.SetActive(false);
                RawIngles.gameObject.SetActive(true);
                break;
        }
        contenedorIdiomas.SetActive(false);
    }

    private IEnumerator OnLoginButtonClickCoroutine()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            sinInternetPopup.SetActive(true);
            yield break;
        }

        if (intentosFallidosUseCase.EstaBloqueado())
        {
            MostrarError($"Demasiados intentos fallidos. Intenta en {intentosFallidosUseCase.TiempoRestante()} segundos.");
            yield break;
        }

        if (VerificarCamposLoginVacios())
            yield break;

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        var loginTask = loginUseCase.EjecutarAsync(email, password);

        while (!loginTask.IsCompleted)
        {
            yield return null;
        }

        var resultado = loginTask.Result;

        if (resultado.EsExitoso)
        {
            Debug.Log($"Usuario logueado: {resultado.UsuarioId}");

            PlayerPrefs.SetInt("rememberMe", 1);
            PlayerPrefs.SetString("userEmail", email);
            PlayerPrefs.SetString("userPassword", password);
            PlayerPrefs.Save();

            intentosFallidosUseCase.ResetearIntentos();

            StartCoroutine(OnLoginSuccessCoroutine());
        }
        else
        {
            intentosFallidosUseCase.RegistrarIntentoFallido();
            MostrarError(resultado.MensajeError);
        }
    }

    private IEnumerator OnSendResetPasswordClickCoroutine()
    {
        string email = emailResetInput.text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            MostrarResetError("Ingresa tu correo.", Color.red);
            yield break;
        }

        var resetTask = resetPasswordUseCase.EjecutarAsync(email);

        while (!resetTask.IsCompleted)
        {
            yield return null;
        }

        bool enviado = resetTask.Result;

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

    private IEnumerator OnLoginSuccessCoroutine()
    {
        string userId = FirebaseServiceLocator.Auth.CurrentUser?.UserId;

        if (!string.IsNullOrEmpty(userId))
        {
            var verificarTask = verificarEstadoUsuarioUseCase.Ejecutar(userId);

            while (!verificarTask.IsCompleted)
            {
                yield return null;
            }
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