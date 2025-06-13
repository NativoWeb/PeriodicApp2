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

    [Header("Live Search Settings")]
    public float searchDelay = 0.3f; // Retraso en segundos antes de ejecutar la búsqueda
    public int minSearchChars = 2; // Mínimo de caracteres para iniciar búsqueda

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;
    private List<FriendRequest> allRequests = new List<FriendRequest>();

    // Variables para live search
    private string lastSearchText = "";
    private float lastSearchTime;
    private bool searchScheduled = false;

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

        // Configurar el listener para el campo de búsqueda
        searchInput.onValueChanged.AddListener(OnSearchInputChanged);

        // Mantener el botón de búsqueda tradicional también
        searchButton.onClick.AddListener(() => FilterRequests(searchInput.text));

        LoadPendingRequests();
    }

    void Update()
    {
        // Ejecutar la búsqueda programada cuando pase el tiempo de retraso
        if (searchScheduled && Time.time >= lastSearchTime + searchDelay)
        {
            searchScheduled = false;
            FilterRequests(lastSearchText);
        }
    }

    void OnSearchInputChanged(string text)
    {
        lastSearchText = text;

        // Si está vacío, mostrar todas las solicitudes
        if (string.IsNullOrEmpty(text))
        {
            FilterRequests("");
            return;
        }

        // Si no tiene suficientes caracteres, no hacer nada todavía
        if (text.Length < minSearchChars)
        {
            return;
        }

        // Programar la búsqueda después del retraso
        lastSearchTime = Time.time;
        searchScheduled = true;
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
                 

                  // Crear tarea para obtener el rango del usuario
                  var fetchTask = db.Collection("users").Document(fromUserId).GetSnapshotAsync()
                      .ContinueWithOnMainThread(userTask =>
                      {
                          if (userTask.IsCompleted && userTask.Result.Exists)
                          {
                              string rank = userTask.Result.ContainsField("Rango") ?
                              userTask.Result.GetValue<string>("Rango") : "Novato de laboratorio";

                              string fromUserName = userTask.Result.ContainsField("DisplayName") ?
                              userTask.Result.GetValue<string>("DisplayName") : "Desconocido";

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
        else
        {
            ShowMessage($"Mostrando {matches} solicitudes");
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

        // buscamos y configuramos la imagen del avatar
        Image avatarImage = requestItem.transform.Find("AvatarImage")?.GetComponent<Image>();

        string avatarPath = ObtenerAvatarPorRango(userRank);
        Debug.Log($"Intentando cargar avatar: {avatarPath}");

        // 1. Cargar sprite
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

        // cargamos la imagen al prefab
        avatarImage.sprite = avatarSprite;

        // Configurar botones
        requestItem.transform.Find("AceptarBtn").GetComponent<Button>().onClick.AddListener(() => AcceptRequest(documentId));
        requestItem.transform.Find("RechazarBtn").GetComponent<Button>().onClick.AddListener(() => RejectRequest(documentId));
    }
    private string ObtenerAvatarPorRango(string rango)
    {
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }

    void AcceptRequest(string documentId)
    {
        ShowMessage("Procesando solicitud...");

        // Primero obtenemos la información de la solicitud
        var requestDoc = db.Collection("SolicitudesAmistad").Document(documentId);
        requestDoc.GetSnapshotAsync().ContinueWithOnMainThread(getTask =>
        {
            if (!getTask.IsCompleted || !getTask.Result.Exists)
            {
                ShowMessage("Error al obtener datos de la solicitud", true);
                Debug.LogError("Error al obtener solicitud: " + getTask.Exception);
                return;
            }

            // Obtenemos los datos de la solicitud
            string fromUserId = getTask.Result.GetValue<string>("idRemitente");
            string fromUserName = getTask.Result.GetValue<string>("nombreRemitente");

            // Actualizamos el estado de la solicitud
            requestDoc.UpdateAsync("estado", "aceptada").ContinueWithOnMainThread(updateTask =>
            {
                if (!updateTask.IsCompleted)
                {
                    ShowMessage("Error al actualizar estado de solicitud", true);
                    Debug.LogError("Error al actualizar solicitud: " + updateTask.Exception);
                    return;
                }

                // Creamos la referencia a la subcolección de amigos del usuario actual
                var currentUserFriendsRef = db.Collection("users").Document(currentUserId).Collection("amigos");

                // Creamos la referencia a la subcolección de amigos del otro usuario
                var otherUserFriendsRef = db.Collection("users").Document(fromUserId).Collection("amigos");

                // Creamos un diccionario con los datos del amigo a agregar
                var friendData = new Dictionary<string, object>
            {
                { "userId", fromUserId },
                { "DisplayName", fromUserName },
                { "fechaAmistad", FieldValue.ServerTimestamp }
            };

                // Creamos un diccionario con los datos del usuario actual para el otro usuario
                var currentUserData = new Dictionary<string, object>
            {
                { "userId", currentUserId },
                { "DisplayName", auth.CurrentUser.DisplayName ?? "Usuario sin nombre" },
                { "fechaAmistad", FieldValue.ServerTimestamp }
            };

                // Batch para ejecutar ambas operaciones atómicamente
                var batch = db.StartBatch();

                // Agregamos el amigo al usuario actual
                batch.Set(currentUserFriendsRef.Document(fromUserId), friendData);

                // Agregamos el usuario actual como amigo del otro usuario
                batch.Set(otherUserFriendsRef.Document(currentUserId), currentUserData);

                // Ejecutamos el batch
                batch.CommitAsync().ContinueWithOnMainThread(batchTask =>
                {
                    if (batchTask.IsCompleted)
                    {
                        ShowMessage("Solicitud aceptada y amigo agregado con éxito");
                        LoadPendingRequests();
                    }
                    else
                    {
                        ShowMessage("Error al agregar amigo", true);
                        Debug.LogError("Error en batch: " + batchTask.Exception);
                    }
                });
            });
        });
    }

    void RejectRequest(string documentId)
    {
        ShowMessage("Procesando solicitud...");
        db.Collection("SolicitudesAmistad").Document(documentId)
          .DeleteAsync()
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

        }
    }
}   