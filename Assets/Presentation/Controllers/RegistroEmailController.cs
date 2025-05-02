using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Firebase.Auth;

public class RegistroEmailController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_Text feedbackText;
    public GameObject panelError;
    public GameObject panelSinInternet;

    [Header("Verificación")]
    public GameObject panelVerificacion;
    public TMP_Text correoVerificadoText;
    public TMP_Text mensajeVerificacionText;
    public GameObject panelCorreoInfo;
    public TMP_Text tiempoRestanteText;

    [Header("Botones")]
    public Button btnRegistrar;
    public Button btnVolver;
    public Button btnCerrarError;
    public Button btnVerificar;

    [Header("Validador visual de contraseña")]
    public PasswordValidatorController passwordValidatorController;


    private string codigoVerificacion;
    private float tiempoRestante = 180f;
    private bool codigoExpirado = false;

    // Servicios y casos de uso
    private IEmailSender emailSender;
    private RegistrarUsuario useCaseRegistro;
    private ValidarRegistroUsuario useCaseValidacion;

    void Start()
    {
        // Inyección manual de dependencias
        var firebaseAuthService = new FirebaseAuthService(FirebaseAuth.DefaultInstance);
        emailSender = new EmailSenderBrevoService();
        useCaseRegistro = new RegistrarUsuario(firebaseAuthService);
        useCaseValidacion = new ValidarRegistroUsuario();

        btnRegistrar.onClick.AddListener(OnClickRegistrar);
        btnCerrarError.onClick.AddListener(() => panelError.SetActive(false));
        btnVolver.onClick.AddListener(VolverARegistro);

        panelVerificacion.SetActive(false);
        panelCorreoInfo.SetActive(false);
    }

    void Update()
    {
        if (panelVerificacion.activeSelf && tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarTemporizador();
        }
        else if (!codigoExpirado && tiempoRestante <= 0)
        {
            tiempoRestante = 0;
            codigoExpirado = true;
            MostrarError("El código ha expirado. Intenta registrar de nuevo.");
            VolverARegistro();
        }
    }

    public async void OnClickRegistrar()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            panelSinInternet.SetActive(true);
            return;
        }

        string correo = emailInput.text.Trim();
        string pass = passwordInput.text;
        string confirm = confirmPasswordInput.text;

        var resultado = useCaseValidacion.Ejecutar(correo, pass, confirm);
        if (!resultado.EsValido)
        {
            MostrarError(resultado.Mensaje);
            return;
        }

        try
        {
            Usuario usuario = await useCaseRegistro.Ejecutar(correo, pass);

            PlayerPrefs.SetString("UsuarioEliminar", usuario.UserId);
            PlayerPrefs.SetString("userEmail", usuario.Email);
            PlayerPrefs.SetString("userPassword", pass);
            PlayerPrefs.Save();

            // ✅ Enviar correo de verificación
            codigoVerificacion = UnityEngine.Random.Range(100000, 999999).ToString();
            string html = $"<p>Tu código de verificación es:</p><h2>{codigoVerificacion}</h2><p>Expira en 3 minutos.</p>";

            bool enviado = await emailSender.EnviarCorreoAsync(correo, "Código de Verificación", html);

            if (!enviado)
            {
                MostrarError("Error al enviar el correo.");
                return;
            }

            // Mostrar panel de verificación
            tiempoRestante = 180f;
            panelCorreoInfo.SetActive(true);
            panelVerificacion.SetActive(true);
            correoVerificadoText.text = "Correo enviado a: " + correo;
            mensajeVerificacionText.text = "Por favor revisa tu bandeja de entrada.";
        }
        catch (Exception ex)
        {
            MostrarError("Error al registrar usuario: " + ex.Message);
        }
    }

    private void VolverARegistro()
    {
        panelVerificacion.SetActive(false);
        panelCorreoInfo.SetActive(false);
        PlayerPrefs.DeleteKey("UsuarioEliminar");

        emailInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }

    private void MostrarError(string mensaje)
    {
        panelError.SetActive(true);
        feedbackText.text = mensaje;
        feedbackText.color = Color.red;
    }

    private void ActualizarTemporizador()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        tiempoRestanteText.text = $"Quedan {minutos:00}:{segundos:00}";
    }

    public void ResetearFormulario()
    {
        emailInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        feedbackText.text = "";
        panelError.SetActive(false);
        panelCorreoInfo.SetActive(false);
        panelVerificacion.SetActive(false);
    }

}
