using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using System.Linq;

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
        ShowMessage("Por favor, escribe un nombre para buscar.");
    }

    void SearchUser(string username)
    {
        // Si el input está vacío, mostrar un mensaje
        if (string.IsNullOrEmpty(username))
        {
            ShowMessage("Por favor, escribe un nombre para buscar.");
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
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", currentUserId)
          .WhereEqualTo("idDestinatario", userId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.IsFaulted)
              {
                  Debug.LogError("Error al obtener solicitudes de amistad.");
                  return;
              }

              var snapshot = task.Result;
              if (snapshot.Documents.Count() == 0)
              {
                  // No hay solicitudes pendientes
                  SetButtonState(button, Color.blue, "Agregar amigo", true);
              }
              else
              {
                  // Obtener el primer documento encontrado
                  var solicitud = snapshot.Documents.FirstOrDefault();
                  if (solicitud != null)
                  {
                      string estado = solicitud.GetValue<string>("estado");

                      if (estado == "pendiente")
                      {
                          SetButtonState(button, Color.white, "Solicitud enviada", false);
                      }
                      else if (estado == "aceptada")
                      {
                          SetButtonState(button, Color.green, "Amigos", false);
                      }
                  }
              }
          });
    }


    void AddFriend(string friendId, string friendName, Button button)
    {
        string currentUserName = currentUser.DisplayName;

        string solicitudId = currentUserId + "_" + friendId; // ID único basado en ambos usuarios

        var solicitudData = new Dictionary<string, object>
    {
        { "idRemitente", currentUserId },
        { "nombreRemitente", currentUserName }, // Nombre del remitente
        { "idDestinatario", friendId },
        { "nombreDestinatario", friendName }, // Nombre del destinatario
        { "estado", "pendiente" }
    };

        // Verificar si ya existe una solicitud antes de agregarla
        db.Collection("SolicitudesAmistad").Document(solicitudId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("Ya existe una solicitud pendiente para este usuario.");
                SetButtonState(button, Color.gray, "Solicitud ya enviada", false);
            }
            else
            {
                // Si no existe, la agregamos con el ID único
                db.Collection("SolicitudesAmistad").Document(solicitudId).SetAsync(solicitudData).ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsCompleted)
                    {
                        Debug.Log("Solicitud de amistad enviada de " + currentUserName + " a " + friendName);
                        SetButtonState(button, Color.white, "Solicitud enviada", false);
                    }
                    else
                    {
                        Debug.LogError("Error al enviar solicitud: " + setTask.Exception);
                    }
                });
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
