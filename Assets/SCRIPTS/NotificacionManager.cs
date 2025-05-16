using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NotificacionManager : MonoBehaviour
{
    public static NotificacionManager instancia;
    private string backendURL = "http://localhost:3000/notificar-turno"; // ⚠️ Cambiar por URL real si subes a hosting

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnviarNotificacionTurno(string uidDestino, string nombreOponente)
    {
        StartCoroutine(EnviarSolicitudAlBackend(uidDestino, nombreOponente));
    }

    private IEnumerator EnviarSolicitudAlBackend(string uidDestino, string nombreOponente)
    {
        UIDWrapper data = new UIDWrapper
        {
            uid = uidDestino,
            uidDesafiante = nombreOponente
        };

        string json = JsonUtility.ToJson(data);

        UnityWebRequest request = new UnityWebRequest(backendURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Notificación enviada desde backend: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("❌ Error al contactar el backend: " + request.error + "\n" + request.downloadHandler.text);
        }
    }


    [System.Serializable]
    private class UIDWrapper
    {
        public string uid;
        public string uidDesafiante;
    }

}
