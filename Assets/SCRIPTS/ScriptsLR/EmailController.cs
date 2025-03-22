using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.Networking;

public class EmailController : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_InputField verificationCodeInput;
    public Button registerButton;
    public Button verifyButton;
    public TMP_Text verificationMessage;
    public GameObject registroPanel;
    public GameObject verificacionPanel;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    private string generatedCode;
    private string userEmail;

    private const string apiKey = "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";  // Reemplaza con tu API Key de Brevo
    private const string url = "https://api.brevo.com/v3/smtp/email";

    void Start()
    {
        verificacionPanel.SetActive(false);
        registroPanel.SetActive(true);
        StartCoroutine(WaitForFirebase());
    }

    private IEnumerator WaitForFirebase()
    {
        float tiempoMaximoEspera = 3f;
        float tiempoEspera = 0f;

        while (!DbConnexion.Instance.IsFirebaseReady())
        {
            yield return new WaitForSeconds(0.5f);
            tiempoEspera += 0.5f;

            if (tiempoEspera >= tiempoMaximoEspera)
            {
                Debug.LogError("🚨 Tiempo de espera excedido. Firebase no está listo.");
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
        registerButton.onClick.AddListener(OnRegisterButtonClick);
        verifyButton.onClick.AddListener(OnVerifyButtonClick);
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

            currentUser = auth.CurrentUser;
            userEmail = email;
            generatedCode = Random.Range(100000, 999999).ToString();
            SendVerificationEmail(userEmail, generatedCode);
        });
    }

    private void SendVerificationEmail(string email, string code)
    {
        StartCoroutine(SendEmailCoroutine(email, code));
    }

    private IEnumerator SendEmailCoroutine(string email, string code)
    {
        string jsonPayload = "{ " +
                    "\"sender\": { \"name\": \"PeriodicApp\", \"email\": \"periodicappoficial@gmail.com\" }, " +
                    "\"to\": [{ \"email\": \"" + email + "\", \"name\": \"Usuario\" }], " +
                    "\"subject\": \"🔐 Código de Verificación - PeriodicApp\", " +
                    "\"htmlContent\": \"" +
                    "<div style='font-family: Arial, sans-serif; text-align: center; background-color: #f4f4f4; padding: 20px;'>" +
                        "<div style='max-width: 500px; margin: auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);'>" +
                            "<h2 style='color: #332C85;'>🔐 Código de Verificación</h2>" +
                            "<p style='font-size: 16px; color: #333;'>¡Hola! Gracias por registrarte en <strong>PeriodicApp</strong>. Para continuar, usa el siguiente código de verificación:</p>" +
                            "<div style='font-size: 24px; font-weight: bold; color: #ffffff; background: #332C85; padding: 10px; display: inline-block; border-radius: 5px; margin: 10px 0;'>" +
                                code + "</div>" +
                            "<p style='font-size: 14px; color: #666;'>Este código expirará en 10 minutos.</p>" +
                            "<hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>" +
                            "<p style='font-size: 12px; color: #777;'>Si no solicitaste este código, puedes ignorar este mensaje.</p>" +
                        "</div>" +
                    "</div>" +
                    "\" }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("api-key", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Correo enviado con éxito");
                registroPanel.SetActive(false);
                verificacionPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("❌ Error al enviar el correo: " + request.responseCode + " - " + request.error);
                Debug.LogError("❌ Respuesta: " + request.downloadHandler.text);
            }
        }
    }

    public void OnVerifyButtonClick()
    {
        Debug.Log(generatedCode);
        Debug.Log(verificationCodeInput.text);
        if (verificationCodeInput.text == generatedCode)
        {
            Debug.Log("✅ Código verificado correctamente. Avanzando a la siguiente escena...");
            SceneManager.LoadScene("Registrar");
        }
        else
        {
            Debug.LogError("❌ Código incorrecto. Intenta de nuevo.");
            verificationMessage.text = "⚠️ Código incorrecto. Intenta nuevamente.";
        }
    }
}
