using System.Text;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class EmailSenderBrevoService : IEmailSender
{
    private const string apiKey = "TU_API_KEY_DE_BREVO";
    private const string urlBrevo = "https://api.brevo.com/v3/smtp/email";

    public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string contenidoHtml)
    {
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
            Debug.LogError(request.downloadHandler.text);
            return false;

        }
    }
}
