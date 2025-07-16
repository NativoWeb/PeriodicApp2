using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class FriendsManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cardPrefab;
    public Transform scrollContent;
    public Button btnVerAmigosSugeridos;
    public TMP_Text messageText; // Para mostrar mensajes como "No hay sugerencias"

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    private string userId;
    private string myCity;

    private HashSet<string> excludedUsers = new HashSet<string>();

    // MODIFICADO: Variables de localización
    private string appIdioma;
    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // MODIFICADO: Inicializar idioma y textos
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InitializeLocalizedTexts();

        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
            userId = auth.CurrentUser.UserId;
        }
        else
        {
            Debug.LogError(localizedTexts["noAuthUser"]);
            ShowMessage(localizedTexts["noAuthUser"]);
            return;
        }

        firestore = FirebaseFirestore.DefaultInstance;
        btnVerAmigosSugeridos.onClick.AddListener(VerTodosUsuariosSugeridos);
        LoadExcludedUsers();
    }

    // MODIFICADO: Nuevo método para centralizar las traducciones
    void InitializeLocalizedTexts()
    {
        if (appIdioma == "ingles")
        {
            localizedTexts["noAuthUser"] = "No authenticated user.";
            localizedTexts["requestsError"] = "Error getting friend requests: ";
            localizedTexts["cityError"] = "Error getting user's city: ";
            localizedTexts["randomUsersError"] = "Error loading random users.";
            localizedTexts["noSuggestions"] = "No suggestions available at this time.";
            localizedTexts["defaultRank"] = "Lab Newbie";
            localizedTexts["addFriend"] = "Add Friend";
            localizedTexts["requestSent"] = "Request Sent";
            localizedTexts["sendRequestError"] = "Error sending request: ";
            localizedTexts["rankLabel"] = "Rank: {0}";
        }
        else // Español por defecto
        {
            localizedTexts["noAuthUser"] = "No hay usuario autenticado.";
            localizedTexts["requestsError"] = "Error obteniendo solicitudes de amistad: ";
            localizedTexts["cityError"] = "Error obteniendo la ciudad del usuario: ";
            localizedTexts["randomUsersError"] = "Error cargando usuarios aleatorios.";
            localizedTexts["noSuggestions"] = "No hay sugerencias disponibles en este momento.";
            localizedTexts["defaultRank"] = "Novato de laboratorio";
            localizedTexts["addFriend"] = "Agregar Amigo";
            localizedTexts["requestSent"] = "Solicitud enviada";
            localizedTexts["sendRequestError"] = "Error al enviar solicitud: ";
            localizedTexts["rankLabel"] = "Rango: {0}";
        }
    }

    public void LoadExcludedUsers()
    {
        excludedUsers.Clear();
        excludedUsers.Add(userId);

        Query query = firestore.Collection("SolicitudesAmistad")
            .Where(Filter.Or(Filter.EqualTo("idRemitente", userId), Filter.EqualTo("idDestinatario", userId)));

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(localizedTexts["requestsError"] + task.Exception);
                return;
            }
            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                if (doc.Exists)
                {
                    excludedUsers.Add(doc.GetValue<string>("idRemitente"));
                    excludedUsers.Add(doc.GetValue<string>("idDestinatario"));
                }
            }
            LoadUserCity();
        });
    }

    void LoadUserCity()
    {
        firestore.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(localizedTexts["cityError"] + task.Exception);
                LoadRandomUsers(10, new List<DocumentSnapshot>());
                return;
            }
            if (task.Result.Exists && task.Result.ContainsField("Ciudad"))
            {
                myCity = task.Result.GetValue<string>("Ciudad");
                if (!string.IsNullOrEmpty(myCity)) LoadSuggestedUsers();
                else LoadRandomUsers(10, new List<DocumentSnapshot>());
            }
            else
            {
                LoadRandomUsers(10, new List<DocumentSnapshot>());
            }
        });
    }

    void LoadSuggestedUsers()
    {
        if (string.IsNullOrEmpty(myCity))
        {
            LoadRandomUsers(10, new List<DocumentSnapshot>());
            return;
        }

        firestore.Collection("users").WhereEqualTo("Ciudad", myCity).Limit(10).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            List<DocumentSnapshot> localUsers = new List<DocumentSnapshot>();
            if (!task.IsFaulted)
            {
                localUsers = task.Result.Documents.Where(doc => doc.Exists && !excludedUsers.Contains(doc.Id)).ToList();
            }

            if (localUsers.Count >= 10)
            {
                CreateUserCards(localUsers.Take(10).ToList());
            }
            else
            {
                LoadRandomUsers(10 - localUsers.Count, localUsers);
            }
        });
    }

    void LoadRandomUsers(int cantidad, List<DocumentSnapshot> currentUsers)
    {
        if (cantidad <= 0)
        {
            CreateUserCards(currentUsers);
            return;
        }

        int limit = Mathf.Max(cantidad * 3, 20);
        firestore.Collection("users").Limit(limit).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(localizedTexts["randomUsersError"]);
                CreateUserCards(currentUsers);
                return;
            }
            if (task.IsCompleted)
            {
                var availableUsers = task.Result.Documents
                    .Where(doc => doc.Exists && !excludedUsers.Contains(doc.Id) && !currentUsers.Any(u => u.Id == doc.Id))
                    .ToList();

                var randomUsers = availableUsers.OrderBy(x => Random.value).Take(cantidad).ToList();
                currentUsers.AddRange(randomUsers);
                CreateUserCards(currentUsers.Take(10).ToList());
            }
            else
            {
                CreateUserCards(currentUsers);
            }
        });
    }

    void CreateUserCards(List<DocumentSnapshot> users)
    {
        foreach (Transform child in scrollContent) Destroy(child.gameObject);

        if (users.Count == 0)
        {
            ShowMessage(localizedTexts["noSuggestions"]);
            return;
        }
        else
        {
            ShowMessage(""); // Ocultar mensaje si hay usuarios
        }

        foreach (DocumentSnapshot doc in users)
        {
            string suggestedUserId = doc.Id;
            string nombre = doc.GetValue<string>("DisplayName");
            string rango = doc.ContainsField("Rango") ? doc.GetValue<string>("Rango") : localizedTexts["defaultRank"];
            string avatar = ObtenerAvatarPorRango(rango);
            Sprite avatarSprite = Resources.Load<Sprite>(avatar) ?? Resources.Load<Sprite>("Avatares/Rango1");

            GameObject newCard = Instantiate(cardPrefab, scrollContent);
            newCard.transform.Find("NombreText")?.GetComponent<TMP_Text>().SetText(nombre);
            // MODIFICADO: Añadir etiqueta de rango traducida
            newCard.transform.Find("RangoText")?.GetComponent<TMP_Text>().SetText(string.Format(localizedTexts["rankLabel"], rango));
            Image AvatarUsuario = newCard.transform.Find("AvatarImage")?.GetComponent<Image>();
            if (AvatarUsuario != null)
            {
                AvatarUsuario.sprite = avatarSprite;
            }

            Button agregarAmigoButton = newCard.transform.Find("BtnAgregarAmigo")?.GetComponent<Button>();
            if (agregarAmigoButton != null)
            {
                // MODIFICADO: Establecer estado inicial del botón
                SetButtonState(agregarAmigoButton, new Color(0.2f, 0.6f, 1f), localizedTexts["addFriend"], true);
                agregarAmigoButton.onClick.AddListener(() => AddFriend(suggestedUserId, nombre, agregarAmigoButton));
            }
        }
    }

    void AddFriend(string friendId, string friendName, Button button)
    {
        string solicitudId = userId + "_" + friendId;
        var solicitudData = new Dictionary<string, object>
        {
            { "idRemitente", userId },
            { "nombreRemitente", currentUser.DisplayName },
            { "idDestinatario", friendId },
            { "nombreDestinatario", friendName },
            { "estado", "pendiente" }
        };

        firestore.Collection("SolicitudesAmistad").Document(solicitudId).SetAsync(solicitudData).ContinueWithOnMainThread(setTask =>
        {
            if (setTask.IsCompleted)
            {
                SetButtonState(button, Color.cyan, localizedTexts["requestSent"], false);
            }
            else
            {
                Debug.LogError(localizedTexts["sendRequestError"] + setTask.Exception);
            }
        });
    }

    void SetButtonState(Button button, Color color, string text, bool interactable)
    {
        button.GetComponent<Image>().color = color;
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = interactable;
    }

    private string ObtenerAvatarPorRango(string rango)
    {
        // La lógica depende de los nombres en español de la DB.
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

    void VerTodosUsuariosSugeridos()
    {
        PlayerPrefs.SetInt("MostrarSugerencias", 1);
        SceneManager.LoadScene("Amigos");
    }

    void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }
}