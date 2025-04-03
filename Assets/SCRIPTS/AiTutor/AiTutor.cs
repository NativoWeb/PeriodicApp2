using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
//using System.Security.Policy;


public class AiTutor : MonoBehaviour
{
    string apiKey = "AIzaSyBx7SXpAy3o2fCKa1vT0bj_5fJCmth6Kyc";
    string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void PreguntarAI(string pregunta)
    {
        StartCoroutine(SendQuestion(pregunta));
    }

    IEnumerator SendQuestion(string input)
    {
        // Formato JSON requerido por Gemini
        string json = "{\"contents\":[{\"parts\":[{\"text\":\"" + input + "\"}]}]}";

        using (UnityWebRequest request = new UnityWebRequest(endpoint + apiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Respuesta: " + request.downloadHandler.text);

                // Aquí puedes extraer el contenido de la respuesta
                string respuesta = ProcesarRespuesta(request.downloadHandler.text);
                Debug.Log("Respuesta procesada: " + respuesta);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    string ProcesarRespuesta(string json)
    {
        // Extrae el texto de la respuesta con una búsqueda sencilla
        int index = json.IndexOf("\"text\":\"") + 8;
        int end = json.IndexOf("\"", index);
        if (index > 0 && end > index)
        {
            return json.Substring(index, end - index).Replace("\\n", "\n");
        }
        return "No se pudo procesar la respuesta.";
    }

}
