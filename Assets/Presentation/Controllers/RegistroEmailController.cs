using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Firebase.Auth;
using UnityEngine.SceneManagement;

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
    public TMP_InputField inputCodigoVerificacion;

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
        btnVerificar.onClick.AddListener(OnClickVerificarCodigo);


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
            string html = $"<div style='font-family: Arial, sans-serif; text-align: center; background-color: #f4f4f4; padding: 20px;'>" +
        "<div style='max-width: 500px; margin: auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);'>" +
        "<h2 style='color: #332C85;'>🔐 Código de Verificación</h2>" +
        "<p style='font-size: 16px; color: #333;'>¡Hola! Gracias por registrarte en <strong>PeriodicApp</strong>. Para continuar, usa el siguiente código de verificación:</p>" +
        $"<div style='font-size: 24px; font-weight: bold; color: #ffffff; background: #332C85; padding: 10px; display: inline-block; border-radius: 5px; margin: 10px 0;'>{codigoVerificacion}</div>" +
        "<p style='font-size: 14px; color: #666;'>Este código expirará en 10 minutos.</p>" +
        "<hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>" +
        "<p style='font-size: 12px; color: #777;'>Si no solicitaste este código, puedes ignorar este mensaje.</p>" +
        "</div></div>";


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

    private void OnClickVerificarCodigo()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            panelSinInternet.SetActive(true);
            return;
        }

        string codigoIngresado = inputCodigoVerificacion.text.Trim();

        if (string.IsNullOrEmpty(codigoIngresado))
        {
            MostrarError("Debes ingresar el código de verificación.");
            return;
        }

        if (codigoIngresado == codigoVerificacion)
        {
            Debug.Log("✅ Código correcto. Registro finalizado.");
            // Puedes cargar una nueva escena o mostrar mensaje
            SceneManager.LoadScene("Registrar");
        }
        else
        {
            MostrarError("Código incorrecto. Intenta nuevamente.");
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
