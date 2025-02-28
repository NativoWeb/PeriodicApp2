using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;  // Si est�s usando Newtonsoft.Json

using System.Collections;

public class Api : MonoBehaviour
{
    private const string SendGridApiKey = "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";  // Sustituye con tu clave API de SendGrid

    // M�todo para enviar correo con c�digo de verificaci�n
    public void SendVerificationEmail(string toEmail, string verificationCode)
    {
        StartCoroutine(SendEmailCoroutine(toEmail, verificationCode));
    }

    IEnumerator SendEmailCoroutine(string toEmail, string verificationCode)
    {
        string emailSubject = "C�digo de Verificaci�n";
        string emailBody = $"Hola Bienvenido a PeriodicApp, Tu c�digo de verificaci�n es: {verificationCode}";

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

                // Mostrar m�s detalles sobre la respuesta de SendGrid
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError("Detalles de la respuesta de SendGrid: " + www.downloadHandler.text);
                }
            }
        }
    }
}
