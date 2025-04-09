using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;

public class SolicitudesManager : MonoBehaviour
{
    public GameObject requestPrefab;
    public Transform requestContainer;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;
    private string currentUserName;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser == null)
        {
            Debug.LogError("Usuario no autenticado.");
            return;
        }

        currentUserId = auth.CurrentUser.UserId;
        Debug.Log("Usuario actual: " + currentUserId);

        LoadPendingRequests();
    }
    void LoadPendingRequests()
    {
        Debug.Log("Cargando solicitudes pendientes...");

        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.Result == null)
              {
                  Debug.LogError("Error al obtener solicitudes: " + (task.Exception != null ? task.Exception.ToString() : "Sin resultados"));
                  return;
              }

              Debug.Log("Solicitudes obtenidas correctamente. Total: " + task.Result.Count);

              foreach (Transform child in requestContainer) Destroy(child.gameObject);

              foreach (DocumentSnapshot document in task.Result.Documents)
              {
                  string fromUserId = document.ContainsField("idRemitente") ? document.GetValue<string>("idRemitente") : "Desconocido";
                  string fromUserName = document.ContainsField("nombreRemitente") ? document.GetValue<string>("nombreRemitente") : "Desconocido";

                  Debug.Log($"Solicitud de {fromUserName} ({fromUserId})");

                  CreateRequestUI(fromUserId, fromUserName);
              }
          });
    }


    void CreateRequestUI(string fromUserId, string fromUserName)
    {
        Debug.Log("Creando UI para solicitud de: " + fromUserName);
        GameObject requestItem = Instantiate(requestPrefab, requestContainer);

        requestItem.transform.Find("NombreText").GetComponent <TMP_Text>().text = fromUserName;
        requestItem.transform.Find("AceptarBtn").GetComponent<Button>().onClick.AddListener(() => AcceptRequest(fromUserId));
        requestItem.transform.Find("RechazarBtn").GetComponent<Button>().onClick.AddListener(() => RejectRequest(fromUserId));
    }

    void AcceptRequest(string fromUserId)
    {
        Debug.Log("Aceptando solicitud de: " + fromUserId);
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", fromUserId)
          .WhereEqualTo("idDestinatario", currentUserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && task.Result.Count > 0)
              {
                  foreach (var document in task.Result.Documents)
                  {
                      db.Collection("SolicitudesAmistad").Document(document.Id)
                        .UpdateAsync("estado", "aceptada")
                        .ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.IsCompleted)
                            {
                                Debug.Log("Solicitud aceptada.");
                                LoadPendingRequests();
                            }
                            else
                            {
                                Debug.LogError("Error al aceptar solicitud: " + updateTask.Exception);
                            }
                        });
                  }
              }
          });
    }


    void RejectRequest(string fromUserId)
    {
        Debug.Log("Rechazando solicitud de: " + fromUserId);
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", fromUserId)
          .WhereEqualTo("idDestinatario", currentUserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && task.Result.Count > 0)
              {
                  foreach (var document in task.Result.Documents)
                  {
                      db.Collection("SolicitudesAmistad").Document(document.Id).UpdateAsync("estado", "rechazada").ContinueWithOnMainThread(updateTask =>
                      {
                          if (updateTask.IsCompleted)
                          {
                              Debug.Log("Solicitud rechazada con éxito.");
                              LoadPendingRequests();
                          }
                          else
                          {
                              Debug.LogError("Error al rechazar solicitud: " + updateTask.Exception);
                          }
                      });
                  }
              }
          });
    }
}
