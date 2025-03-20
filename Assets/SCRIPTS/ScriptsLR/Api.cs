using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;

public class Api : MonoBehaviour
{
    private const string ResendApiKey = "t6PTE0e2QTRMswZ7iuniOOYt3pA3";  // Sustituye con tu clave API de Resend

    public void SendVerificationEmail(string toEmail, string verificationCode)
    {
        StartCoroutine(SendEmailCoroutine(toEmail, verificationCode));
    }

    IEnumerator SendEmailCoroutine(string toEmail, string verificationCode)
    {
        string emailSubject = "Código de Verificación";
        string emailBody = $"Hola Bienvenido a PeriodicApp, tu código de verificación es: {verificationCode}";

        // JSON para Resend
        var emailData = new
        {
            from = "periodicappoficial@gmail.com",
            to = new[] { toEmail },
            subject = emailSubject,
            html = $"<p>{emailBody}</p>"
        };

        string emailJson = JsonConvert.SerializeObject(emailData);

        using (UnityWebRequest www = new UnityWebRequest("https://api.resend.com/emails", "POST"))
        {
            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(emailJson);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", "Bearer " + ResendApiKey);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Correo enviado correctamente con Resend.");
            }
            else
            {
                Debug.LogError("❌ Error al enviar el correo: " + www.error);
                Debug.LogError("🔍 Respuesta del servidor: " + www.downloadHandler.text);
            }
        }
    }
}
