using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;

public class SolicitudesManager : MonoBehaviour
{
    public GameObject requestPrefab;
    public Transform requestContainer;
    public TMP_InputField searchInput;
    public Button searchButton;
    public TMP_Text messageText; // Texto para mostrar mensajes de estado

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;
    private List<FriendRequest> allRequests = new List<FriendRequest>();

    private class FriendRequest
    {
        public string fromUserId;
        public string fromUserName;
        public string fromUserRank; // Nuevo campo para el rango
        public string documentId;
    }

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

        searchButton.onClick.AddListener(() => FilterRequests(searchInput.text));
        LoadPendingRequests();
    }

    void LoadPendingRequests()
    {
        ShowMessage("Cargando solicitudes...");
        Debug.Log("Cargando solicitudes pendientes...");

        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", currentUserId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.Result == null)
              {
                  string errorMsg = "Error al obtener solicitudes: " + (task.Exception?.Message ?? "Sin resultados");
                  ShowMessage(errorMsg, true);
                  Debug.LogError(errorMsg);
                  return;
              }

              allRequests.Clear();
              var userFetchTasks = new List<Task>();

              foreach (DocumentSnapshot document in task.Result.Documents)
              {
                  string fromUserId = document.GetValue<string>("idRemitente");
                  string fromUserName = document.GetValue<string>("nombreRemitente");

                  // Crear tarea para obtener el rango del usuario
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

              // Esperar a que todas las tareas de obtención de rango terminen
              Task.WhenAll(userFetchTasks).ContinueWithOnMainThread(finalTask =>
              {
                  if (allRequests.Count == 0)
                  {
                      ShowMessage("No tienes solicitudes pendientes");
                  }
                  else
                  {
                      ShowMessage($"{allRequests.Count} solicitudes encontradas");
                  }
                  FilterRequests("");
              });
          });
    }

    void FilterRequests(string searchText)
    {
        // Limpiar el contenedor
        foreach (Transform child in requestContainer) Destroy(child.gameObject);

        if (allRequests.Count == 0)
        {
            ShowMessage("No hay solicitudes para mostrar");
            return;
        }

        int matches = 0;
        foreach (var request in allRequests)
        {
            if (string.IsNullOrEmpty(searchText) ||
                request.fromUserName.ToLower().Contains(searchText.ToLower()))
            {
                CreateRequestUI(request.fromUserId, request.fromUserName, request.fromUserRank, request.documentId);
                matches++;
            }
        }

        if (matches == 0)
        {
            ShowMessage("No se encontraron coincidencias");
        }
        else if (!string.IsNullOrEmpty(searchText))
        {
            ShowMessage($"{matches} solicitudes coinciden con tu búsqueda");
        }
    }

    void CreateRequestUI(string fromUserId, string fromUserName, string userRank, string documentId)
    {
        GameObject requestItem = Instantiate(requestPrefab, requestContainer);

        // Configurar los textos
        requestItem.transform.Find("NombreText").GetComponent<TMP_Text>().text = fromUserName;

        // Buscar y configurar el texto del rango
        TMP_Text rankText = requestItem.transform.Find("RangoText")?.GetComponent<TMP_Text>();
        if (rankText != null)
        {
            rankText.text = userRank;

        }
        else
        {
            Debug.LogWarning("No se encontró el componente RangoText en el prefab");
        }

        // Configurar botones
        requestItem.transform.Find("AceptarBtn").GetComponent<Button>().onClick.AddListener(() => AcceptRequest(documentId));
        requestItem.transform.Find("RechazarBtn").GetComponent<Button>().onClick.AddListener(() => RejectRequest(documentId));
    }

    void AcceptRequest(string documentId)
    {
        ShowMessage("Procesando solicitud...");
        db.Collection("SolicitudesAmistad").Document(documentId)
          .UpdateAsync("estado", "aceptada")
          .ContinueWithOnMainThread(updateTask =>
          {
              if (updateTask.IsCompleted)
              {
                  ShowMessage("Solicitud aceptada con éxito");
                  LoadPendingRequests();
              }
              else
              {
                  ShowMessage("Error al aceptar solicitud", true);
                  Debug.LogError("Error al aceptar solicitud: " + updateTask.Exception);
              }
          });
    }

    void RejectRequest(string documentId)
    {
        ShowMessage("Procesando solicitud...");
        db.Collection("SolicitudesAmistad").Document(documentId)
          .UpdateAsync("estado", "rechazada")
          .ContinueWithOnMainThread(updateTask =>
          {
              if (updateTask.IsCompleted)
              {
                  ShowMessage("Solicitud rechazada");
                  LoadPendingRequests();
              }
              else
              {
                  ShowMessage("Error al rechazar solicitud", true);
                  Debug.LogError("Error al rechazar solicitud: " + updateTask.Exception);
              }
          });
    }

    // Método para mostrar mensajes de estado
    void ShowMessage(string message, bool isError = false)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = isError ? Color.red : Color.white;
        }
    }
}