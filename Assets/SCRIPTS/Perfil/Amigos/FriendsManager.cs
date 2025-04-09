using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FriendsManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform scrollContent;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    private string userId;
    private string myCity;

    private HashSet<string> excludedUsers = new HashSet<string>(); // Para almacenar amigos y solicitudes pendientes

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
            userId = auth.CurrentUser.UserId;
           
            Debug.Log($"Usuario autenticado: {userId}");
        }
        else
        {
            Debug.LogError("No hay usuario autenticado.");
            return;
        }

        firestore = FirebaseFirestore.DefaultInstance;

        // Obtener la lista de amigos y solicitudes pendientes
        LoadExcludedUsers();
    }
    // nuevos cambios

    void LoadExcludedUsers()
    {
        excludedUsers.Clear();

        // Consultar solicitudes enviadas por el usuario
        firestore.Collection("SolicitudesAmistad")
            .WhereEqualTo("idRemitente", userId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error obteniendo solicitudes enviadas: {task.Exception}");
                    return;
                }

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    if (doc.Exists)
                    {
                        string friendId = doc.GetValue<string>("idDestinatario");
                        string status = doc.GetValue<string>("estado");

                        // Excluir si la solicitud está aceptada o pendiente
                        if (status == "Aceptada" || status == "pendiente")
                        {
                            excludedUsers.Add(friendId);
                        }
                    }
                }

                // Consultar solicitudes recibidas por el usuario
                firestore.Collection("SolicitudesAmistad")
                    .WhereEqualTo("idDestinatario", userId)
                    .GetSnapshotAsync()
                    .ContinueWithOnMainThread(task2 =>
                    {
                        if (task2.IsFaulted)
                        {
                            Debug.LogError($"Error obteniendo solicitudes recibidas: {task2.Exception}");
                            return;
                        }

                        foreach (DocumentSnapshot doc in task2.Result.Documents)
                        {
                            if (doc.Exists)
                            {
                                string friendId = doc.GetValue<string>("idRemitente");
                                string status = doc.GetValue<string>("estado");

                                // Excluir si la solicitud está aceptada o pendiente
                                if (status == "Aceptada" || status == "pendiente")
                                {
                                    excludedUsers.Add(friendId);
                                }
                            }
                        }

                        // Ahora que tenemos la lista de excluidos, cargamos la ciudad
                        LoadUserCity();
                    });
            });
    }



    void LoadUserCity()
    {
        DocumentReference userDoc = firestore.Collection("users").Document(userId);
        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Error obteniendo la ciudad del usuario: {task.Exception}");
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists && snapshot.ContainsField("Ciudad"))
            {
                myCity = snapshot.GetValue<string>("Ciudad");
                Debug.Log($"Ciudad obtenida: {myCity}");

                if (!string.IsNullOrEmpty(myCity))
                {
                    LoadSuggestedUsers();
                }
            }
        });
    }

    void LoadSuggestedUsers()
    {
        firestore.Collection("users")
            .WhereEqualTo("Ciudad", myCity)
            .Limit(10)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error obteniendo usuarios sugeridos: {task.Exception}");
                    return;
                }

                List<DocumentSnapshot> suggestedUsers = task.Result.Documents
                    .Where(doc => doc.Exists && doc.Id != userId && !excludedUsers.Contains(doc.Id))
                    .ToList();

                int cantidadFaltante = 5 - suggestedUsers.Count;

                if (cantidadFaltante > 0)
                {
                    Debug.Log($"Faltan {cantidadFaltante} usuarios, buscando aleatorios...");
                    LoadRandomUsers(cantidadFaltante, suggestedUsers);
                }
                else
                {
                    CreateUserCards(suggestedUsers);
                }
            });
    }

    void LoadRandomUsers(int cantidadFaltante, List<DocumentSnapshot> currentUsers)
    {
        firestore.Collection("users")
            .Limit(20) // Obtener más para mayor aleatoriedad
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error obteniendo usuarios aleatorios: {task.Exception}");
                    return;
                }

                List<DocumentSnapshot> allUsers = task.Result.Documents
                    .Where(doc => doc.Exists && doc.Id != userId && !excludedUsers.Contains(doc.Id))
                    .ToList();

                if (allUsers.Count > 0)
                {
                    System.Random rand = new System.Random();
                    List<DocumentSnapshot> randomUsers = allUsers.OrderBy(x => rand.Next()).Take(cantidadFaltante).ToList();
                    currentUsers.AddRange(randomUsers);
                }

                CreateUserCards(currentUsers);
            });
    }


    void LoadRandomUsers()
    {
        firestore.Collection("users")
            .Limit(20) // Traemos más usuarios para aumentar la aleatoriedad
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error obteniendo usuarios aleatorios: {task.Exception}");
                    return;
                }

                List<DocumentSnapshot> allUsers = task.Result.Documents
                    .Where(doc => doc.Exists && doc.Id != userId && !excludedUsers.Contains(doc.Id))
                    .ToList();

                if (allUsers.Count > 0)
                {
                    System.Random rand = new System.Random();
                    List<DocumentSnapshot> randomUsers = allUsers.OrderBy(x => rand.Next()).Take(3).ToList();
                    CreateUserCards(randomUsers);
                }
                else
                {
                    Debug.Log("No hay suficientes usuarios para mostrar.");
                }
            });
    }

    void CreateUserCards(List<DocumentSnapshot> users)
    {
        foreach (DocumentSnapshot doc in users)
        {
            string suggestedUserId = doc.Id;
            string nombre = doc.GetValue<string>("DisplayName");
            string rango = doc.ContainsField("Rango") ? doc.GetValue<string>("Rango") : "Novato de laboratorio";

            GameObject newCard = Instantiate(cardPrefab, scrollContent);
            TMP_Text nombreText = newCard.transform.Find("NombreText")?.GetComponent<TMP_Text>();
            TMP_Text rangoText = newCard.transform.Find("RangoText")?.GetComponent<TMP_Text>();
            Button agregarAmigoButton = newCard.transform.Find("BtnAgregarAmigo")?.GetComponent<Button>();

            if (nombreText != null) nombreText.text = nombre;
            if (rangoText != null) rangoText.text = rango;

            if (agregarAmigoButton != null)
            {
                agregarAmigoButton.onClick.AddListener(() =>
                    AddFriend(suggestedUserId, nombre, agregarAmigoButton)
                );
            }
        }
    }


    void AddFriend(string friendId, string friendName, Button button)
    {
        string currentUserName = currentUser.DisplayName;

        string solicitudId = userId + "_" + friendId; // ID único basado en ambos usuarios

        var solicitudData = new Dictionary<string, object>
    {
        { "idRemitente", userId },
        { "nombreRemitente", currentUserName }, // Nombre del remitente
        { "idDestinatario", friendId },
        { "nombreDestinatario", friendName }, // Nombre del destinatario
        { "estado", "pendiente" }
    };

        // Verificar si ya existe una solicitud antes de agregarla
        firestore.Collection("SolicitudesAmistad").Document(solicitudId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("Ya existe una solicitud pendiente para este usuario.");
                SetButtonState(button, Color.gray, "Solicitud ya enviada", false);
            }
            else
            {
                // Si no existe, la agregamos con el ID único
                firestore.Collection("SolicitudesAmistad").Document(solicitudId).SetAsync(solicitudData).ContinueWithOnMainThread(setTask =>
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
}
