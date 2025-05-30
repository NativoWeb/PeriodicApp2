using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;

public class SolicitudesAmistadManager : MonoBehaviour
{
    public GameObject[] solicitudPanels; // Asigna los paneles en el inspector
    
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;

    private class FriendRequest
    {
        public string fromUserId;
        public string fromUserName;
        public string fromUserRank;
        public string documentId;
    }

    private List<FriendRequest> allRequests = new List<FriendRequest>();

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
        LoadPendingRequests();
    }

    void LoadPendingRequests()
    {
        

        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.Result == null)
              {
                  
                  return;
              }

              allRequests.Clear();
              var userFetchTasks = new List<Task>();

              foreach (var document in task.Result.Documents)
              {
                  string fromUserId = document.GetValue<string>("idRemitente");
                  string fromUserName = document.GetValue<string>("nombreRemitente");

                  var fetchTask = db.Collection("users").Document(fromUserId).GetSnapshotAsync()
                      .ContinueWithOnMainThread(userTask =>
                      {
                          if (userTask.IsCompleted && userTask.Result.Exists)
                          {
                              string rank = userTask.Result.ContainsField("Rango") ?
                                  userTask.Result.GetValue<string>("Rango") : "Novato de laboratorio";

                              allRequests.Add(new FriendRequest
                              {
                                  fromUserId = fromUserId,
                                  fromUserName = fromUserName,
                                  fromUserRank = rank,
                                  documentId = document.Id
                              });
                          }
                      });

                  userFetchTasks.Add(fetchTask);
              }

              Task.WhenAll(userFetchTasks).ContinueWithOnMainThread(finalTask =>
              {
                  DisplayRequests();
              });
          });
    }

    void DisplayRequests()
    {
        // Desactivar todos los paneles primero
        foreach (var panel in solicitudPanels)
        {
            panel.SetActive(false);
        }

        if (allRequests.Count == 0)
        {
            if (solicitudPanels.Length > 0)
            {
                solicitudPanels[0].SetActive(true);
                solicitudPanels[0].transform.Find("NombreText").GetComponent<TMP_Text>().text = "Sin solicitudes";
                solicitudPanels[0].transform.Find("RangoText").GetComponent<TMP_Text>().text = "Invita a tus amigos";
                solicitudPanels[0].transform.Find("AceptarBtn").gameObject.SetActive(false);
                solicitudPanels[0].transform.Find("RechazarBtn").gameObject.SetActive(false);
            }
            return;
        }


        int toShow = Mathf.Min(3, allRequests.Count);

        for (int i = 0; i < toShow; i++)
        {
            var request = allRequests[i];
            GameObject panel = solicitudPanels[i];
            panel.SetActive(true);

            panel.transform.Find("NombreText").GetComponent<TMP_Text>().text = request.fromUserName;
            panel.transform.Find("RangoText").GetComponent<TMP_Text>().text = request.fromUserRank;

            // Limpiar listeners anteriores
            Button aceptarBtn = panel.transform.Find("AceptarBtn").GetComponent<Button>();
            Button rechazarBtn = panel.transform.Find("RechazarBtn").GetComponent<Button>();

            aceptarBtn.onClick.RemoveAllListeners();
            rechazarBtn.onClick.RemoveAllListeners();

            aceptarBtn.gameObject.SetActive(true);
            rechazarBtn.gameObject.SetActive(true);

            string docId = request.documentId;
            aceptarBtn.onClick.AddListener(() => AcceptRequest(docId));
            rechazarBtn.onClick.AddListener(() => RejectRequest(docId));
        }
    }

    void AcceptRequest(string documentId)
    {
        db.Collection("SolicitudesAmistad").Document(documentId)
          .UpdateAsync("estado", "aceptada")
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  
                  LoadPendingRequests();
              }
              else
              {
                  
              }
          });
    }

    void RejectRequest(string documentId)
    {
        db.Collection("SolicitudesAmistad").Document(documentId)
          .UpdateAsync("estado", "rechazada")
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                 
                  LoadPendingRequests();
              }
              else
              {
                  
              }
          });
    }

   
}
