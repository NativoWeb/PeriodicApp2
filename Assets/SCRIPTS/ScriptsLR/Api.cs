using UnityEngine;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Api : MonoBehaviour
{
    private string apiKey = "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"; // Sustituye con tu API Key real
    private const string url = "https://api.sendgrid.com/v3/mail/send";

    public void SendVerificationEmail(string email, string code)
    {
        StartCoroutine(SendEmailCoroutine(email, code));
    }

    private IEnumerator SendEmailCoroutine(string email, string code)
    {
        string jsonPayload = "{ " +
            "\"from\": { \"email\": \"periodicappoficial@gmail.com\" }, " +
            "\"personalizations\": [{ \"to\": [{ \"email\": \"" + email + "\" }] }], " +
            "\"subject\": \"🔐 Código de Verificación - PeriodicApp\", " +
            "\"content\": [{ \"type\": \"text/html\", \"value\": \"" +
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
            "\" }] " +
        "}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("📨 Enviando correo a: " + email);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Correo enviado con éxito: " + request.downloadHandler.text);

                // 🔄 Esperar unos segundos antes de cambiar de escena
                yield return new WaitForSeconds(2f);

                // ⏭️ Ir a la escena de verificación
                Debug.Log("⏭️ Cambiando a la escena de verificación...");
                SceneManager.LoadScene("Registrar");
            }
            else
            {
                Debug.LogError("❌ Error al enviar el correo: " + request.responseCode + " - " + request.error);
                Debug.LogError("🔍 Respuesta del servidor: " + request.downloadHandler.text);
            }
        }
    }
}
