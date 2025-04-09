using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    private Dictionary<string, DocumentSnapshot> userCache = new Dictionary<string, DocumentSnapshot>();
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
        List<Task<QuerySnapshot>> tasks = new List<Task<QuerySnapshot>>();

        // Consultar solicitudes enviadas
        tasks.Add(firestore.Collection("SolicitudesAmistad")
            .WhereEqualTo("idRemitente", userId)
            .GetSnapshotAsync());

        // Consultar solicitudes recibidas
        tasks.Add(firestore.Collection("SolicitudesAmistad")
            .WhereEqualTo("idDestinatario", userId)
            .GetSnapshotAsync());

        Task.WhenAll(tasks.ToArray()).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error obteniendo solicitudes de amistad: " + task.Exception);
                return;
            }

            foreach (var querySnapshot in task.Result)
            {
                foreach (DocumentSnapshot doc in querySnapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        string idRemitente = doc.GetValue<string>("idRemitente");
                        string idDestinatario = doc.GetValue<string>("idDestinatario");
                        string status = doc.GetValue<string>("estado");

                        if (status == "aceptada" || status == "pendiente")
                        {
                            // Excluir ambos usuarios para evitar que aparezcan en sugerencias
                            excludedUsers.Add(idRemitente);
                            excludedUsers.Add(idDestinatario);

                            Debug.Log($"Excluyendo usuario {idRemitente} y {idDestinatario} por estado {status}");
                        }
                    }
                }
            }

            // Evitar que el usuario actual aparezca en sugerencias
            excludedUsers.Add(userId);
            Debug.Log($"Excluyéndome a mí mismo: {userId}");

            // Ahora que excludedUsers está lleno, cargar la ciudad del usuario
            LoadUserCity();
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
            .Limit(10) // Mantenemos un límite razonable
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

                if (suggestedUsers.Count >= 5)
                {
                    // Si hay 5 o más usuarios, mostramos todos los que haya (hasta 10)
                    Debug.Log($"Mostrando {suggestedUsers.Count} usuarios de {myCity}");
                    CreateUserCards(suggestedUsers);
                }
                else
                {
                    // Si hay menos de 5, completamos con usuarios aleatorios
                    Debug.Log($"Solo {suggestedUsers.Count} usuarios en {myCity}, completando con aleatorios...");
                    int cantidadFaltante = 5 - suggestedUsers.Count;
                    LoadRandomUsers(cantidadFaltante, suggestedUsers);
                }
            });
    }

    void LoadRandomUsers(int cantidadFaltante, List<DocumentSnapshot> currentUsers)
    {
        firestore.Collection("users")
            .Limit(20) // Obtener más usuarios para aumentar aleatoriedad
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error obteniendo usuarios aleatorios: {task.Exception}");
                    return;
                }

                List<DocumentSnapshot> allUsers = task.Result.Documents
                    .Where(doc => doc.Exists && doc.Id != userId && !excludedUsers.Contains(doc.Id)) // Asegurar exclusión
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
