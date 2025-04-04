using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;

public class AiTutor : MonoBehaviour
{
    [Header("Clave API")]
    string apiKey = "AIzaSyBx7SXpAy3o2fCKa1vT0bj_5fJCmth6Kyc";
    string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";



    [Header("Referencias UI")]
    public TMP_InputField inputPregunta;
    public TextMeshProUGUI respuestaTexto;
    public GameObject panelChatTutor;

    void Start()
    {
        
        panelChatTutor.SetActive(false);
    }

    public void EnviarPregunta()
    {
        string pregunta = inputPregunta.text;
        if (!string.IsNullOrEmpty(pregunta))
        {
            Debug.Log("Pregunta enviada: " + pregunta);
            StartCoroutine(SolicitarRespuestaIA(pregunta));
            inputPregunta.text = "";
        }
        else
        {
            Debug.LogWarning("El campo de pregunta está vacío.");
        }
    }

    IEnumerator SolicitarRespuestaIA(string pregunta)
    {
        string jsonBody = "{ \"contents\": [ { \"role\": \"user\", \"parts\": [ { \"text\": \"" + pregunta + "\" } ] } ] }";
        

        UnityWebRequest request = new UnityWebRequest(endpoint + apiKey, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Código de respuesta HTTP: " + request.responseCode);
        Debug.Log("Respuesta completa:\n" + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                RespuestaGemini respuesta = JsonUtility.FromJson<RespuestaGemini>(request.downloadHandler.text);
                string texto = respuesta.candidates[0].content.parts[0].text;
                respuestaTexto.text = texto;
                Debug.Log("Respuesta IA: " + texto);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al parsear respuesta de Gemini: " + e.Message);
                respuestaTexto.text = "Error al procesar respuesta IA.";
            }
        }
        else
        {
            Debug.LogError("Error en la petición: " + request.error);
            respuestaTexto.text = "Error: " + request.error;
        }
    }

    public void ToggleChatPanel()
    {
        if (panelChatTutor != null)
        {
            bool estadoActual = panelChatTutor.activeSelf;
            panelChatTutor.SetActive(!estadoActual);
            
        }
        else
        {
            Debug.LogError("panelChatTutor no está asignado.");
        }
    }



    [System.Serializable]
    public class MensajeGemini
    {
        public Content content;
        public MensajeGemini(string texto)
        {
            content = new Content(texto);
        }
    }

    [System.Serializable]
    public class Content
    {
        public Part[] parts;
        public string role = "user";
        public Content(string texto)
        {
            parts = new Part[] { new Part { text = texto } };
        }
    }

    [System.Serializable]
    public class Part
    {
        public string text;
    }

    [System.Serializable]
    public class RespuestaGemini
    {
        public Candidate[] candidates;
    }

    [System.Serializable]
    public class Candidate
    {
        public Content content;
    }
}
