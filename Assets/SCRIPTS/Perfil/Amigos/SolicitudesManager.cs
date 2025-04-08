using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;

public class SolicitudesManager : MonoBehaviour
{
    public GameObject requestPrefab;  // Prefab con botones "Aceptar" y "Rechazar"
    public Transform requestContainer; // Contenedor en la UI

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = auth.CurrentUser.UserId;

        LoadPendingRequests();
    }

    void LoadPendingRequests()
    {
        db.Collection("Solicitudes_Amistad")
          .WhereEqualTo("Usuarios." + currentUserId + "status", "Pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  foreach (Transform child in requestContainer) Destroy(child.gameObject);

                  QuerySnapshot snapshot = task.Result;
                  foreach (DocumentSnapshot document in snapshot.Documents)
                  {
                      string fromUserId = document.Id; // ID del remitente
                      Dictionary<string, object> usuarios = document.GetValue<Dictionary<string, object>>("Usuarios");

                      if (usuarios.ContainsKey(currentUserId))
                      {
                          Dictionary<string, object> userInfo = usuarios[currentUserId] as Dictionary<string, object>;
                          string fromUserName = userInfo["DisplayName"].ToString();
                          CreateRequestUI(fromUserId, fromUserName);
                      }
                  }
              }
          });
    }

    void CreateRequestUI(string fromUserId, string fromUserName)
    {
        GameObject requestItem = Instantiate(requestPrefab, requestContainer);
        requestItem.transform.Find("UserNameText").GetComponent<Text>().text = fromUserName;

        requestItem.transform.Find("AcceptButton").GetComponent<Button>().onClick.AddListener(() => AcceptRequest(fromUserId));
        requestItem.transform.Find("RejectButton").GetComponent<Button>().onClick.AddListener(() => RejectRequest(fromUserId));
    }

    void AcceptRequest(string fromUserId)
    {
        DocumentReference senderRef = db.Collection("Solicitudes_Amistad").Document(fromUserId);
        senderRef.UpdateAsync("Usuarios." + currentUserId + "status", "Aceptada").ContinueWithOnMainThread(task => LoadPendingRequests());
    }

    void RejectRequest(string fromUserId)
    {
        DocumentReference senderRef = db.Collection("Solicitudes_Amistad").Document(fromUserId);
        senderRef.UpdateAsync("Usuarios." + currentUserId + "status", "Rechazada").ContinueWithOnMainThread(task => LoadPendingRequests());
    }
}
