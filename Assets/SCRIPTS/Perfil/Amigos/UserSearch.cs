using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using System.Linq;
using System.Threading.Tasks;
using System;


public class SearchUsers : MonoBehaviour
{
    public TMP_InputField searchInput;
    public Button searchButton;
    public Transform resultsContainer;
    public GameObject userResultPrefab; // Prefab con nombre y botón de agregar
    public TMP_Text messageText; // Texto para mostrar mensajes

    FirebaseFirestore db;
    private FirebaseUser currentUser;
    private FirebaseAuth auth;
    string currentUserId;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
        }

        searchButton.onClick.AddListener(() => SearchUser(searchInput.text));

        // Mostrar usuarios aleatorios al inicio
        ShowRandomUsers();
    }

    void ShowRandomUsers()
    {
        // Limpiar resultados anteriores
        foreach (Transform child in resultsContainer)
        {
            Destroy(child.gameObject);
        }

        messageText.text = "Cargando usuarios...";

        db.Collection("users")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error cargando usuarios: " + task.Exception);
                    ShowMessage("Hubo un error al cargar usuarios. Inténtalo de nuevo.");
                    return;
                }

                var allUsers = task.Result.Documents
                    .Where(doc => doc.Id != currentUserId) // Excluir al usuario actual
                    .ToList();

                if (allUsers.Count == 0)
                {
                    ShowMessage("No hay otros usuarios registrados.");
                    return;
                }

                // Seleccionar 5 usuarios aleatorios (o menos si no hay suficientes)
                var randomUsers = GetRandomUsers(allUsers, Math.Min(5, allUsers.Count));

                foreach (DocumentSnapshot doc in randomUsers)
                {
                    string userId = doc.Id;
                    string name = doc.GetValue<string>("DisplayName");
                    string rank = doc.GetValue<string>("Rango");

                    // Instanciar el prefab
                    GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

                    // Configurar los elementos de UI
                    TMP_Text nameText = userEntry.transform.Find("NombreText").GetComponent<TMP_Text>();
                    TMP_Text rankText = userEntry.transform.Find("RangoText").GetComponent<TMP_Text>();
                    Button addButton = userEntry.transform.Find("AñadirBtn").GetComponent<Button>();

                    nameText.text = name;
                    rankText.text = "Rango: " + rank;

                    // Verificar estado de amistad
                    CheckFriendStatus(userId, addButton);

                    // Configurar evento del botón
                    addButton.onClick.AddListener(() => AddFriend(userId, name, addButton));
                }

                ShowMessage("Usuarios sugeridos:");
            });
    }

    List<DocumentSnapshot> GetRandomUsers(List<DocumentSnapshot> allUsers, int count)
    {
        // Si hay menos usuarios que el count solicitado, devolver todos
        if (allUsers.Count <= count)
            return allUsers;

        // Selección aleatoria sin repetición
        System.Random rand = new System.Random();
        return allUsers.OrderBy(x => rand.Next()).Take(count).ToList();
    }

    void SearchUser(string username)
    {
        // Si el input está vacío, mostrar un mensaje
        if (string.IsNullOrEmpty(username))
        {
            ShowRandomUsers();
            return;
        }

        // Limpiar resultados anteriores
        foreach (Transform child in resultsContainer)
        {
            Destroy(child.gameObject);
        }

        messageText.text = "Buscando..."; // Mostrar estado de búsqueda

        db.Collection("users")
            .WhereGreaterThanOrEqualTo("DisplayName", username)
            .WhereLessThanOrEqualTo("DisplayName", username + "\uf8ff")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error buscando usuarios: " + task.Exception);
                    ShowMessage("Hubo un error al buscar. Inténtalo de nuevo.");
                    return;
                }

                Debug.Log($"Usuarios encontrados: {task.Result.Count}");

                int userCount = 0; // Contador para saber si hay resultados válidos

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    string userId = doc.Id;

                    // Omitir el usuario actual
                    if (userId == currentUserId)
                        continue;

                    string name = doc.GetValue<string>("DisplayName");
                    string rank = doc.GetValue<string>("Rango");

                    Debug.Log($"Usuario encontrado: {name} - Rango: {rank}");

                    // Instanciar el prefab
                    GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

                    // Buscar los elementos de UI dentro del prefab
                    TMP_Text nameText = userEntry.transform.Find("NombreText").GetComponent<TMP_Text>();
                    TMP_Text rankText = userEntry.transform.Find("RangoText").GetComponent<TMP_Text>();
                    Button addButton = userEntry.transform.Find("AñadirBtn").GetComponent<Button>();

                    // Asignar datos al prefab
                    nameText.text = name;
                    rankText.text = "Rango: " + rank;

                    // Verificar si ya se envió la solicitud o si ya son amigos
                    CheckFriendStatus(userId, addButton);

                    // Configurar evento del botón
                    addButton.onClick.AddListener(() => AddFriend(userId, name, addButton));

                    userCount++; // Incrementar contador de usuarios válidos
                }

                // Si no se encontraron usuarios válidos
                if (userCount == 0)
                {
                    ShowMessage("No se encontraron usuarios con ese nombre.");
                }
                else
                {
                    ShowMessage(""); // Limpiar mensaje si hay resultados
                }
            });
    }

    void CheckFriendStatus(string userId, Button button)
    {
        // Verificar en ambas direcciones: currentUser -> userId Y userId -> currentUser
        var query1 = db.Collection("SolicitudesAmistad")
            .WhereEqualTo("idRemitente", currentUserId)
            .WhereEqualTo("idDestinatario", userId);

        var query2 = db.Collection("SolicitudesAmistad")
            .WhereEqualTo("idRemitente", userId)
            .WhereEqualTo("idDestinatario", currentUserId);

        // Ejecutar ambas consultas en paralelo
        Task.WhenAll(query1.GetSnapshotAsync(), query2.GetSnapshotAsync())
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error al verificar estado de amistad.");
                    return;
                }

                var snapshots = task.Result;
                var allDocs = snapshots[0].Documents.Concat(snapshots[1].Documents).ToList();

                if (allDocs.Count == 0)
                {
                    // No hay solicitudes en ninguna dirección
                    SetButtonState(button, new Color(0.215f, 0.741f, 0.968f), "Agregar amigo", true);
                }
                else
                {
                    // Verificar el estado de la primera solicitud encontrada (debería ser la única)
                    var solicitud = allDocs.FirstOrDefault();
                    string estado = solicitud.GetValue<string>("estado");
                    string remitenteId = solicitud.GetValue<string>("idRemitente");

                    if (estado == "pendiente")
                    {
                        if (remitenteId == currentUserId)
                        {
                            SetButtonState(button, Color.white, "Solicitud enviada", false);
                        }
                        else
                        {
                            SetButtonState(button, Color.yellow, "Aceptar solicitud", true);
                            // Aquí podrías cambiar el listener del botón para aceptar la solicitud
                            AcceptRequest(userId);
                            button.onClick.RemoveAllListeners();
                            button.onClick.AddListener(() => AcceptFriendRequest(userId, button));
                        }
                    }
                    else if (estado == "aceptada")
                    {
                        SetButtonState(button, Color.green, "Amigos", false);
                    }
                }
            });
    }
    void AcceptRequest(string documentId)
    {
        Debug.Log("Aceptando solicitud con ID: " + documentId);
        db.Collection("SolicitudesAmistad").Document(documentId)
          .UpdateAsync("estado", "aceptada")
          .ContinueWithOnMainThread(updateTask =>
          {
              if (updateTask.IsCompleted)
              {
                  Debug.Log("Solicitud aceptada.");
                 
              }
              else
              {
                  Debug.LogError("Error al aceptar solicitud: " + updateTask.Exception);
              }
          });
    }
    void AddFriend(string friendId, string friendName, Button button)
    {
        string solicitudId = currentUserId + "_" + friendId;

        var solicitudData = new Dictionary<string, object>
        {
            { "idRemitente", currentUserId },
            { "nombreRemitente", currentUser.DisplayName },
            { "idDestinatario", friendId },
            { "nombreDestinatario", friendName },
            { "estado", "pendiente" },
        };

        db.Collection("SolicitudesAmistad").Document(solicitudId).SetAsync(solicitudData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    SetButtonState(button, Color.white, "Solicitud enviada", false);
                }
                else
                {
                    Debug.LogError("Error al enviar solicitud: " + task.Exception);
                }
            });
    }

    void AcceptFriendRequest(string friendId, Button button)
    {
        string solicitudId1 = currentUserId + "_" + friendId;
        string solicitudId2 = friendId + "_" + currentUserId;

        // Actualizar ambas posibles solicitudes (por si acaso)
        var batch = db.StartBatch();

        // Actualizar solicitud donde currentUser es el remitente
        var docRef1 = db.Collection("SolicitudesAmistad").Document(solicitudId1);
        batch.Update(docRef1, new Dictionary<string, object> { { "estado", "aceptada" } });

        // Actualizar solicitud donde friend es el remitente
        var docRef2 = db.Collection("SolicitudesAmistad").Document(solicitudId2);
        batch.Update(docRef2, new Dictionary<string, object> { { "estado", "aceptada" } });

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                SetButtonState(button, Color.green, "Amigos", false);
                Debug.Log("Solicitud de amistad aceptada");
            }
            else
            {
                Debug.LogError("Error al aceptar solicitud: " + task.Exception);
            }
        });
    }


    void SetButtonState(Button button, Color color, string text, bool interactable)
    {
        button.GetComponent<Image>().color = color;
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = interactable;
    }

    void ShowMessage(string message)
    {
        messageText.text = message;
    }
}
