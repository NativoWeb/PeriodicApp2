using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using PeriodicApp.Core.Domain.Interfaces;

public class EmailSenderBrevoService : IEmailSender
{
    private const string urlBrevo = "https://api.brevo.com/v3/smtp/email";

    private string ObtenerApiKey()
    {
        string key = PlayerPrefs.GetString("BREVO_API_KEY", "");

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("BREVO_API_KEY no está configurada. Usa el script ConfigurarBrevo para configurarla.");
        }

        return key;
    }

    public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string contenidoHtml)
    {
        string apiKey = ObtenerApiKey();

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("No se puede enviar el correo: BREVO_API_KEY no está configurada");
            return false;
        }

        Debug.Log($"[Brevo] Enviando correo a: {destinatario}");
        Debug.Log($"[Brevo] API Key (primeros 20 chars): {apiKey.Substring(0, Mathf.Min(20, apiKey.Length))}...");
        Debug.Log($"[Brevo] API Key (longitud total): {apiKey.Length} caracteres");

        string jsonPayload = $@"
            {{
                ""sender"": {{""name"": ""PeriodicApp"", ""email"": ""periodicappoficial@gmail.com""}},
                ""to"": [{{""email"": ""{destinatario}"", ""name"": ""Usuario""}}],
                ""subject"": ""{asunto}"",
                ""htmlContent"": ""{contenidoHtml.Replace("\"", "\\\"")}""
            }}";
        using (UnityWebRequest request = new UnityWebRequest(urlBrevo, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("api-key", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Correo enviado correctamente a " + destinatario);
                return true;
            }

            Debug.LogError($"Error al enviar correo: {request.responseCode} - {request.error}");
            Debug.LogError($"Respuesta de Brevo: {request.downloadHandler.text}");

            // Mostrar los headers enviados para debugging
            var headers = request.GetRequestHeader("api-key");
            Debug.LogError($"Header 'api-key' enviado: {(string.IsNullOrEmpty(headers) ? "NO SE ENVIÓ" : headers.Substring(0, Mathf.Min(20, headers.Length)) + "...")}");

            return false;

        }
    }
}
