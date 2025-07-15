using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

public class SearchUsers : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text messageText;
    public TMP_InputField searchInput;
    public Button searchButton;
    public Transform resultsContainer;
    public GameObject userResultPrefab;

    [Header("Live Search Settings")]
    public float searchDelay = 0.3f;
    public int minSearchChars = 2;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string currentUserId;

    private string lastSearchText = "";
    private float lastSearchTime;
    private bool searchScheduled = false;

    // MODIFICADO: Variables de localización
    private string appIdioma;
    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();

    void Start()
    {
        try
        {
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;

            // MODIFICADO: Inicializar idioma y textos localizados
            appIdioma = PlayerPrefs.GetString("appIdioma", "español");
            InitializeLocalizedTexts();

            if (auth.CurrentUser != null)
            {
                currentUser = auth.CurrentUser;
                currentUserId = currentUser.UserId;
            }
            else
            {
                Debug.LogWarning(localizedTexts["noAuthUser"]);
            }

            if (searchInput == null || searchButton == null || resultsContainer == null || userResultPrefab == null)
            {
                Debug.LogError("Uno o más componentes de la UI no están asignados en el inspector.");
                return;
            }

            searchInput.onValueChanged.AddListener(OnSearchInputChanged);
            searchButton.onClick.AddListener(() => SearchUser(searchInput.text));

            ShowRandomUsers();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error en Start(): {e.Message}\n{e.StackTrace}");
        }
    }

    // MODIFICADO: Nuevo método para centralizar la traducción
    void InitializeLocalizedTexts()
    {
        if (appIdioma == "ingles")
        {
            localizedTexts["minChars"] = "Type at least {0} characters to search";
            localizedTexts["writing"] = "Typing...";
            localizedTexts["loadingUsers"] = "Loading users...";
            localizedTexts["loadError"] = "Error loading users. Please try again.";
            localizedTexts["noOtherUsers"] = "No other users found.";
            localizedTexts["suggestedUsers"] = "Suggested users:";
            localizedTexts["searching"] = "Searching...";
            localizedTexts["searchError"] = "An error occurred while searching. Please try again.";
            localizedTexts["noUsersFound"] = "No users found";
            localizedTexts["noAuthUser"] = "No authenticated user";
            localizedTexts["rankLabel"] = "Rank: {0}";
            // Estados de botones de amistad
            localizedTexts["friends"] = "Friends";
            localizedTexts["addFriend"] = "Add Friend";
            localizedTexts["requestSent"] = "Request Sent";
            localizedTexts["requestReceived"] = "Friend Request";
        }
        else // Español por defecto
        {
            localizedTexts["minChars"] = "Escribe al menos {0} caracteres para buscar";
            localizedTexts["writing"] = "Escribiendo...";
            localizedTexts["loadingUsers"] = "Cargando usuarios...";
            localizedTexts["loadError"] = "Hubo un error al cargar usuarios. Inténtalo de nuevo.";
            localizedTexts["noOtherUsers"] = "No hay otros usuarios registrados.";
            localizedTexts["suggestedUsers"] = "Usuarios sugeridos:";
            localizedTexts["searching"] = "Buscando...";
            localizedTexts["searchError"] = "Hubo un error al buscar. Inténtalo de nuevo.";
            localizedTexts["noUsersFound"] = "No se encontraron usuarios";
            localizedTexts["noAuthUser"] = "No hay usuario autenticado";
            localizedTexts["rankLabel"] = "Rango: {0}";
            // Estados de botones de amistad
            localizedTexts["friends"] = "Amigos";
            localizedTexts["addFriend"] = "Agregar amigo";
            localizedTexts["requestSent"] = "Solicitud enviada";
            localizedTexts["requestReceived"] = "Te ha enviado solicitud";
        }
    }

    void Update()
    {
        if (searchScheduled && Time.time >= lastSearchTime + searchDelay)
        {
            searchScheduled = false;
            SearchUser(lastSearchText);
        }
    }

    void OnSearchInputChanged(string text)
    {
        lastSearchText = text;

        if (string.IsNullOrEmpty(text))
        {
            ShowRandomUsers();
            return;
        }

        if (text.Length < minSearchChars)
        {
            messageText.text = string.Format(localizedTexts["minChars"], minSearchChars);
            return;
        }

        lastSearchTime = Time.time;
        searchScheduled = true;
        messageText.text = localizedTexts["writing"];
    }

    void ShowRandomUsers()
    {
        foreach (Transform child in resultsContainer) Destroy(child.gameObject);
        messageText.text = localizedTexts["loadingUsers"];

        db.Collection("users").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error cargando usuarios: " + task.Exception);
                ShowMessage(localizedTexts["loadError"]);
                return;
            }

            var allUsers = task.Result.Documents.Where(doc => doc.Id != currentUserId).ToList();
            if (allUsers.Count == 0)
            {
                ShowMessage(localizedTexts["noOtherUsers"]);
                return;
            }

            var randomUsers = GetRandomUsers(allUsers, Math.Min(5, allUsers.Count));
            foreach (DocumentSnapshot doc in randomUsers)
            {
                InstantiateUserEntry(doc);
            }
            ShowMessage(localizedTexts["suggestedUsers"]);
        });
    }

    List<DocumentSnapshot> GetRandomUsers(List<DocumentSnapshot> allUsers, int count)
    {
        if (allUsers.Count <= count) return allUsers;
        System.Random rand = new System.Random();
        return allUsers.OrderBy(x => rand.Next()).Take(count).ToList();
    }

    void SearchUser(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            ShowRandomUsers();
            return;
        }

        if (username.Length < minSearchChars)
        {
            messageText.text = string.Format(localizedTexts["minChars"], minSearchChars);
            return;
        }

        foreach (Transform child in resultsContainer) Destroy(child.gameObject);
        messageText.text = localizedTexts["searching"];

        db.Collection("users")
            .WhereGreaterThanOrEqualTo("DisplayName", username)
            .WhereLessThanOrEqualTo("DisplayName", username + "\uf8ff")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (username != lastSearchText) return;
                if (task.IsFaulted)
                {
                    Debug.LogError("Error buscando usuarios: " + task.Exception);
                    ShowMessage(localizedTexts["searchError"]);
                    return;
                }

                int userCount = 0;
                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    if (doc.Id != currentUserId)
                    {
                        InstantiateUserEntry(doc);
                        userCount++;
                    }
                }
                ShowMessage(userCount == 0 ? localizedTexts["noUsersFound"] : "");
            });
    }

    void InstantiateUserEntry(DocumentSnapshot doc)
    {
        try
        {
            string userId = doc.Id;
            string name = doc.GetValue<string>("DisplayName");
            string rank = doc.GetValue<string>("Rango");

            GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

            userEntry.transform.Find("NombreText").GetComponent<TMP_Text>().text = name;
            // MODIFICADO: Usar texto localizado para el rango
            userEntry.transform.Find("RangoText").GetComponent<TMP_Text>().text = string.Format(localizedTexts["rankLabel"], rank);

            ConfigureAvatar(userEntry, rank);

            Button addButton = userEntry.transform.Find("AñadirBtn").GetComponent<Button>();
            CheckFriendStatus(userId, addButton);
            addButton.onClick.AddListener(() => AddFriend(userId, name, addButton));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error instanciando entrada de usuario: {e.Message}");
        }
    }

    private void ConfigureAvatar(GameObject userEntry, string rank)
    {
        string avatarPath = ObtenerAvatarPorRango(rank);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/Rango1");
        Image avatarImg = userEntry.transform.Find("AvatarImage")?.GetComponent<Image>();
        if (avatarImg != null) avatarImg.sprite = avatarSprite;
    }

    void CheckFriendStatus(string userId, Button button)
    {
        var amigoDocRef = db.Collection("users").Document(currentUserId).Collection("amigos").Document(userId);
        amigoDocRef.GetSnapshotAsync().ContinueWithOnMainThread(amigoTask =>
        {
            if (amigoTask.IsFaulted) return;
            if (amigoTask.Result.Exists)
            {
                SetButtonState(button, Color.green, localizedTexts["friends"], false);
            }
            else
            {
                var q1 = db.Collection("SolicitudesAmistad").WhereEqualTo("idRemitente", currentUserId).WhereEqualTo("idDestinatario", userId);
                var q2 = db.Collection("SolicitudesAmistad").WhereEqualTo("idRemitente", userId).WhereEqualTo("idDestinatario", currentUserId);

                Task.WhenAll(q1.GetSnapshotAsync(), q2.GetSnapshotAsync()).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted) return;
                    var allDocs = task.Result[0].Documents.Concat(task.Result[1].Documents).ToList();

                    if (allDocs.Count == 0)
                    {
                        SetButtonState(button, new Color(0.215f, 0.741f, 0.968f), localizedTexts["addFriend"], true);
                    }
                    else
                    {
                        var solicitud = allDocs.First();
                        if (solicitud.GetValue<string>("estado") == "pendiente")
                        {
                            if (solicitud.GetValue<string>("idRemitente") == currentUserId)
                                SetButtonState(button, Color.white, localizedTexts["requestSent"], false);
                            else
                                SetButtonState(button, new Color(1f, 0.84f, 0f), localizedTexts["requestReceived"], false);
                        }
                    }
                });
            }
        });
    }

    void AddFriend(string friendId, string friendName, Button button)
    {
        var solicitudData = new Dictionary<string, object>
        {
            { "idRemitente", currentUserId },
            { "nombreRemitente", currentUser.DisplayName },
            { "idDestinatario", friendId },
            { "nombreDestinatario", friendName },
            { "estado", "pendiente" },
            { "fechaSolicitud", Timestamp.GetCurrentTimestamp() }
        };

        db.Collection("SolicitudesAmistad").Document().SetAsync(solicitudData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
                SetButtonState(button, Color.white, localizedTexts["requestSent"], false);
            else
                Debug.LogError("Error al enviar solicitud: " + task.Exception);
        });
    }

    void SetButtonState(Button button, Color color, string text, bool interactable)
    {
        if (button == null) return;
        button.GetComponent<Image>().color = color;
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = interactable;
    }

    void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
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

    private void PrintHierarchy(Transform parent, string indent = "")
    {
        // Este método es para depuración y puede ser eliminado en la versión final
        // Debug.Log($"{indent}{parent.name}");
        foreach (Transform child in parent)
        {
            PrintHierarchy(child, indent + "  ");
        }
    }
}