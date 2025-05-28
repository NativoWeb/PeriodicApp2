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
    [Header("Texts")]
    public TMP_Text messageText; // Texto para mostrar mensajes
    public TMP_InputField searchInput;

    [Header("Buttons")]
    public Button searchButton;

    [Header("Prefab")]
    public Transform resultsContainer;
    public GameObject userResultPrefab; // Prefab con nombre y botón de agregar

    [Header("Live Search Settings")]
    public float searchDelay = 0.3f; // Retraso en segundos antes de ejecutar la búsqueda
    public int minSearchChars = 2; // Mínimo de caracteres para iniciar búsqueda

   

    FirebaseFirestore db;
    private FirebaseUser currentUser;
    private FirebaseAuth auth;
    string currentUserId;

    // Variables para live search
    private string lastSearchText = "";
    private float lastSearchTime;
    private bool searchScheduled = false;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
        }

        // Configurar listener para el input field
        searchInput.onValueChanged.AddListener(OnSearchInputChanged);

        // Mantener el botón de búsqueda tradicional también
        searchButton.onClick.AddListener(() => SearchUser(searchInput.text));

        // Mostrar usuarios aleatorios al inicio
        ShowRandomUsers();
    }

    void Update()
    {
        // Manejar la búsqueda programada
        if (searchScheduled && Time.time >= lastSearchTime + searchDelay)
        {
            searchScheduled = false;
            SearchUser(lastSearchText);
        }
    }

    void OnSearchInputChanged(string text)
    {
        lastSearchText = text;

        // Si está vacío, mostrar usuarios aleatorios
        if (string.IsNullOrEmpty(text))
        {
            ShowRandomUsers();
            return;
        }

        // Si no tiene suficientes caracteres, no buscar aún
        if (text.Length < minSearchChars)
        {
            messageText.text = $"Escribe al menos {minSearchChars} caracteres para buscar";
            return;
        }

        // Programar búsqueda después del retraso
        lastSearchTime = Time.time;
        searchScheduled = true;
        messageText.text = "Escribiendo...";
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

        // Si no tiene suficientes caracteres, mostrar mensaje
        if (username.Length < minSearchChars)
        {
            messageText.text = $"Escribe al menos {minSearchChars} caracteres para buscar";
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
                // Verificar si esta respuesta corresponde con la última búsqueda
                if (username != lastSearchText)
                {
                    // Los resultados ya no son relevantes para lo que está escrito ahora
                    return;
                }

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
                            SetButtonState(button, new Color(1f, 0.84f, 0f), "Te ha enviado solicitud", false);
                        }
                    }
                    else if (estado == "aceptada")
                    {
                        SetButtonState(button, Color.green, "Amigos", false);
                    }
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