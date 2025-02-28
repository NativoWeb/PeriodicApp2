using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;  // Si estás usando Newtonsoft.Json

using System.Collections;

public class Api : MonoBehaviour
{
    private const string SendGridApiKey = "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";  // Sustituye con tu clave API de SendGrid

    // Método para enviar correo con código de verificación
    public void SendVerificationEmail(string toEmail, string verificationCode)
    {
        StartCoroutine(SendEmailCoroutine(toEmail, verificationCode));
    }

    IEnumerator SendEmailCoroutine(string toEmail, string verificationCode)
    {
        string emailSubject = "Código de Verificación";
        string emailBody = $"Hola Bienvenido a PeriodicApp, Tu código de verificación es: {verificationCode}";

        // Crear el JSON para la solicitud
        var emailData = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[]
                    {
                        new
                        {
                            email = toEmail
                        }
                    },
                    subject = emailSubject
                }
            },
            from = new
            {
                email = "periodicappoficial@gmail.com"  // Sustituye con tu correo de SendGrid
            },
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = emailBody
                }
            }
        };

        // Convertir el objeto a JSON
        string emailJson = JsonConvert.SerializeObject(emailData);

        // Crear la solicitud POST
        using (UnityWebRequest www = new UnityWebRequest("https://api.sendgrid.com/v3/mail/send", "POST"))
        {
            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(emailJson);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Authorization", "Bearer " + SendGridApiKey);
            www.SetRequestHeader("Content-Type", "application/json");

            // Enviar la solicitud y esperar la respuesta
            yield return www.SendWebRequest();

            // Verificar el resultado de la solicitud
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Correo enviado correctamente.");
            }
            else
            {
                Debug.LogError("Error al enviar el correo: " + www.error);
                Debug.LogError("Respuesta del servidor: " + www.downloadHandler.text);

                // Mostrar más detalles sobre la respuesta de SendGrid
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError("Detalles de la respuesta de SendGrid: " + www.downloadHandler.text);
                }
            }
        }
    }
}
