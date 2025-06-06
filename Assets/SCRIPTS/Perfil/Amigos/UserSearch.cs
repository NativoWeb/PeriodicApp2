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
    public GameObject userResultPrefab; // Prefab con nombre y bot�n de agregar

    [Header("Live Search Settings")]
    public float searchDelay = 0.3f; // Retraso en segundos antes de ejecutar la b�squeda
    public int minSearchChars = 2; // M�nimo de caracteres para iniciar b�squeda

   

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
        try
        {
            // Inicializaci�n de Firebase
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser != null)
            {
                currentUser = auth.CurrentUser;
                currentUserId = currentUser.UserId;
                Debug.Log($"Usuario actual: {currentUserId}");
            }
            else
            {
                Debug.LogWarning("No hay usuario autenticado");
            }

            // Verificaci�n de componentes UI
            if (searchInput == null)
            {
                Debug.LogError("searchInput no est� asignado en el inspector!");
                return;
            }

            if (searchButton == null)
            {
                Debug.LogError("searchButton no est� asignado en el inspector!");
                return;
            }

            if (resultsContainer == null)
            {
                Debug.LogError("resultsContainer no est� asignado en el inspector!");
                return;
            }

            if (userResultPrefab == null)
            {
                Debug.LogError("userResultPrefab no est� asignado en el inspector!");
                return;
            }

            // Configuraci�n robusta de listeners
            searchInput.onValueChanged.RemoveAllListeners(); // Limpiar listeners previos
            searchInput.onValueChanged.AddListener(OnSearchInputChanged);

            searchButton.onClick.RemoveAllListeners();
            searchButton.onClick.AddListener(() => {
                Debug.Log("Bot�n de b�squeda presionado");
                SearchUser(searchInput.text);
            });

            // Verificaci�n de listeners
            Debug.Log($"Listeners en searchInput: {searchInput.onValueChanged.GetPersistentEventCount()}");
            Debug.Log($"Listeners en searchButton: {searchButton.onClick.GetPersistentEventCount()}");

            // Mostrar usuarios aleatorios al inicio
            Debug.Log("Mostrando usuarios aleatorios iniciales...");
            ShowRandomUsers();

            Debug.Log("BuscarUsuarios iniciado correctamente");
            SearchUser("");

        }
        catch (Exception e)
        {
            Debug.LogError($"Error en Start(): {e.Message}\n{e.StackTrace}");
        }
    }
    void Update()
    {
        if (searchScheduled)
        {
            Debug.Log($"searchScheduled: true | Tiempo restante: {(lastSearchTime + searchDelay) - Time.time}");

            if (Time.time >= lastSearchTime + searchDelay)
            {
                Debug.Log("Ejecutando b�squeda programada");
                searchScheduled = false;
                SearchUser(lastSearchText);
            }
        }
    }

    void OnSearchInputChanged(string text)
    {
        Debug.Log($"OnSearchInputChanged: '{text}' (Length: {text?.Length})");
        lastSearchText = text;

        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("Mostrando usuarios aleatorios por texto vac�o");
            ShowRandomUsers();
            return;
        }

        if (text.Length < minSearchChars)
        {
            Debug.Log($"Texto demasiado corto ({text.Length} < {minSearchChars})");
            messageText.text = $"Escribe al menos {minSearchChars} caracteres para buscar";
            return;
        }

        Debug.Log($"Programando b�squeda para '{text}'");
        lastSearchTime = Time.time;
        searchScheduled = true;
        messageText.text = "Escribiendo...";
    }
    void ShowRandomUsers()
    {
        Debug.Log(" 11111");
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
                    ShowMessage("Hubo un error al cargar usuarios. Int�ntalo de nuevo.");
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
                    Button addButton = userEntry.transform.Find("A�adirBtn").GetComponent<Button>();
                    Image avatarimage = userEntry.transform.Find("AvatarImage").GetComponent<Image>();

                    string avatarPath = ObtenerAvatarPorRango(rank);
                    Debug.Log($"Intentando cargar avatar: {avatarPath}");

                    // 1. Cargar sprite
                    Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);
                    avatarimage.sprite = avatarSprite;

                    // asignamos 
                    nameText.text = name;
                    rankText.text = "Rango: " + rank;

                    // Verificar estado de amistad
                    CheckFriendStatus(userId, addButton);

                    // Configurar evento del bot�n
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

        // Selecci�n aleatoria sin repetici�n
        System.Random rand = new System.Random();
        return allUsers.OrderBy(x => rand.Next()).Take(count).ToList();
    }

    void SearchUser(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("Username vac�o - mostrando usuarios aleatorios");
            ShowRandomUsers();
            return;
        }

        if (username.Length < minSearchChars)
        {
            Debug.Log($"Username demasiado corto ({username.Length} < {minSearchChars})");
            messageText.text = $"Escribe al menos {minSearchChars} caracteres para buscar";
            return;
        }

        Debug.Log("Limpiando resultados anteriores");
        // Limpiar resultados anteriores
        foreach (Transform child in resultsContainer)
        {
            Destroy(child.gameObject);
        }

        messageText.text = "Buscando...";
        Debug.Log($"Buscando usuarios con nombre: {username}");


        db.Collection("users")
            .WhereGreaterThanOrEqualTo("DisplayName", username)
            .WhereLessThanOrEqualTo("DisplayName", username + "\uf8ff")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (username != lastSearchText) return;

                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Error buscando usuarios: " + task.Exception);
                    ShowMessage("Hubo un error al buscar. Int�ntalo de nuevo.");
                    return;
                }

                Debug.Log($"Usuarios encontrados: {task.Result.Count}");

                int userCount = 0;

                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    string userId = doc.Id;
                    if (userId == currentUserId) continue;

                    string name = doc.GetValue<string>("DisplayName");
                    string rank = doc.GetValue<string>("Rango");
                    Debug.Log($"Mostrando usuario: {name} (Rango: {rank})");

                    // Instanciar prefab
                    GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

                    try
                    {
                        // 1. Configurar texto b�sico
                        userEntry.transform.Find("NombreText").GetComponent<TMP_Text>().text = name;
                        userEntry.transform.Find("RangoText").GetComponent<TMP_Text>().text = $"Rango: {rank}";

                        Debug.Log("ppppppppp");
                        // 2. Configurar avatar (sistema mejorado)
                        ConfigureAvatar(userEntry, rank);

                        // 3. Configurar bot�n
                        Button addButton = userEntry.transform.Find("A�adirBtn").GetComponent<Button>();
                        CheckFriendStatus(userId, addButton);
                        addButton.onClick.AddListener(() => AddFriend(userId, name, addButton));

                        userCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error mostrando usuario {name}: {e.Message}");
                        Destroy(userEntry); // Limpiar instancia si hay error
                    }
                }

                ShowMessage(userCount == 0 ? "No se encontraron usuarios" : "");
            });
    }

    // Nueva funci�n auxiliar para avatares
    private void ConfigureAvatar(GameObject userEntry, string rank)
    {
        Debug.Log("Iniciando ConfigureAvatar");

        // Verifica la jerarqu�a completa (opcional, para depuraci�n)
        PrintHierarchy(userEntry.transform);

        string avatarPath = ObtenerAvatarPorRango(rank);
        Debug.Log($"Intentando cargar avatar: {avatarPath}");

        // 1. Cargar sprite
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);
        if (avatarSprite == null)
        {
            Debug.LogWarning($"Avatar no encontrado en {avatarPath}, usando predeterminado");
            avatarSprite = Resources.Load<Sprite>("Avatares/Rango1");
        }

        // 2. Buscar directamente el componente Image llamado "AvatarImage"
        Image avatarImg = userEntry.transform.Find("AvatarImage")?.GetComponent<Image>();
        if (avatarImg == null)
        {
            Debug.LogError("No se encontr� el componente Image llamado 'AvatarImage' en el prefab");
            return;
        }

        // 3. Asignar sprite
        avatarImg.sprite = avatarSprite;
        Debug.Log($"Avatar asignado correctamente: {avatarSprite.name}");
    }
    // M�todo auxiliar para imprimir jerarqu�a
    private void PrintHierarchy(Transform parent, string indent = "")
    {
        Debug.Log($"{indent}{parent.name} ({parent.GetType()})");
        foreach (Transform child in parent)
        {
            PrintHierarchy(child, indent + "  ");
        }
    }

    void CheckFriendStatus(string userId, Button button)
    {
        Debug.Log("4444");
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
                    // No hay solicitudes en ninguna direcci�n
                    SetButtonState(button, new Color(0.215f, 0.741f, 0.968f), "Agregar amigo", true);
                }
                else
                {
                    // Verificar el estado de la primera solicitud encontrada (deber�a ser la �nica)
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
    
    private string ObtenerAvatarPorRango(string rango)
    {
        Debug.Log("555555");
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda qu�mica": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }
}