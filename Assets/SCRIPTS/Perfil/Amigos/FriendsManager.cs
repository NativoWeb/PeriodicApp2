using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;


public class FriendsManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform scrollContent;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser currentUser;
    private string userId;
    private string myCity;

    public Button btnVerAmigosSugeridos;

    private HashSet<string> excludedUsers = new HashSet<string>();
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
        btnVerAmigosSugeridos.onClick.AddListener(VerTodosUsuariosSugeridos);
        LoadExcludedUsers();
    }

    public void LoadExcludedUsers()
    {
        excludedUsers.Clear();
        excludedUsers.Add(userId);

        Query query = firestore.Collection("SolicitudesAmistad")
            .Where(Filter.Or(
                Filter.EqualTo("idRemitente", userId),
                Filter.EqualTo("idDestinatario", userId)
            ));

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error obteniendo solicitudes de amistad: " + task.Exception);
                return;
            }

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                if (doc.Exists)
                {
                    string idRemitente = doc.GetValue<string>("idRemitente");
                    string idDestinatario = doc.GetValue<string>("idDestinatario");
                    excludedUsers.Add(idRemitente);
                    excludedUsers.Add(idDestinatario);
                }
            }

            Debug.Log($"Total de usuarios excluidos: {excludedUsers.Count}");
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
        Debug.Log($"Buscando usuarios en {myCity}. Excluyendo: {string.Join(", ", excludedUsers)}");

        // Primero cargamos usuarios de la misma ciudad
        firestore.Collection("users")
            .WhereEqualTo("Ciudad", myCity)
            .Limit(10)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                List<DocumentSnapshot> localUsers = new List<DocumentSnapshot>();

                if (!task.IsFaulted && !task.IsCanceled)
                {
                    localUsers = task.Result.Documents
                        .Where(doc => doc.Exists && !excludedUsers.Contains(doc.Id))
                        .ToList();
                }

                Debug.Log($"Usuarios locales encontrados en {myCity}: {localUsers.Count}");

                // Calculamos cuántos usuarios adicionales necesitamos para llegar a 10
                int neededUsers = Mathf.Max(0, 10 - localUsers.Count);

                if (neededUsers > 0)
                {
                    Debug.Log($"Necesitamos {neededUsers} usuarios más de otras ciudades");
                    LoadRandomUsers(neededUsers, localUsers);
                }
                else
                {
                    // Si ya tenemos 10 o más usuarios locales, mostramos solo 10
                    CreateUserCards(localUsers.Take(10).ToList());
                }
            });
    }


    void LoadRandomUsers(int cantidad, List<DocumentSnapshot> currentUsers)
    {
        // Aumentamos el límite para asegurar que encontremos suficientes usuarios únicos
        int limit = Mathf.Max(cantidad * 3, 20); // Mínimo 20 usuarios para buscar

        firestore.Collection("users")
            .Limit(limit)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error cargando usuarios aleatorios.");
                    // Mostramos los usuarios locales que tengamos
                    CreateUserCards(currentUsers);
                    return;
                }
                // Verificamos que la tarea se completó correctamente
                if (task.IsCompleted)
                {
                    QuerySnapshot snapshot = task.Result;

                    // Añadimos los logs de depuración
                    Debug.Log($"Total usuarios en BD: {snapshot.Documents.Count()}");
                    Debug.Log($"Usuarios excluidos: {excludedUsers.Count}");
                    Debug.Log($"IDs de usuarios excluidos: {string.Join(", ", excludedUsers)}");

                    var allUsers = snapshot.Documents;
                    var availableUsers = allUsers
                        .Where(doc => doc.Exists && !excludedUsers.Contains(doc.Id) && !currentUsers.Any(u => u.Id == doc.Id))
                        .ToList();

                    Debug.Log($"Usuarios aleatorios disponibles: {availableUsers.Count}");

                    var randomUsers = availableUsers
                        .OrderBy(x => Random.Range(0, int.MaxValue))
                        .Take(cantidad)
                        .ToList();

                    currentUsers.AddRange(randomUsers);

                    Debug.Log($"Total de usuarios a mostrar: {currentUsers.Count}");
                    CreateUserCards(currentUsers.Take(10).ToList());
                }
                else
                {
                    Debug.LogWarning("La tarea no se completó correctamente");
                    CreateUserCards(currentUsers);
                }
            });
    }

    void CreateUserCards(List<DocumentSnapshot> users)
    {
        // Limpiar el contenido actual
        foreach (Transform child in scrollContent)
        {
            Destroy(child.gameObject);
        }

        if (users.Count == 0)
        {
            Debug.Log("No hay usuarios para mostrar");
            return;
        }

        foreach (DocumentSnapshot doc in users)
        {
            string suggestedUserId = doc.Id;
            string nombre = doc.GetValue<string>("DisplayName");
            string rango = doc.ContainsField("Rango") ? doc.GetValue<string>("Rango") : "Novato de laboratorio";
            string avatar = ObtenerAvatarPorRango(rango);
            Sprite avatarSprite = Resources.Load<Sprite>(avatar) ?? Resources.Load<Sprite>("Avatares/defecto");

            GameObject newCard = Instantiate(cardPrefab, scrollContent);
            TMP_Text nombreText = newCard.transform.Find("NombreText")?.GetComponent<TMP_Text>();
            TMP_Text rangoText = newCard.transform.Find("RangoText")?.GetComponent<TMP_Text>();
            Image AvatarUsuario = newCard.transform.Find("AvatarImage")?.GetComponent<Image>();
            Button agregarAmigoButton = newCard.transform.Find("BtnAgregarAmigo")?.GetComponent<Button>();

            if (nombreText != null) nombreText.text = nombre;
            if (rangoText != null) rangoText.text = rango;
            if (AvatarUsuario != null) AvatarUsuario.sprite = avatarSprite;

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
        string solicitudId = userId + "_" + friendId;

        var solicitudData = new Dictionary<string, object>
        {
            { "idRemitente", userId },
            { "nombreRemitente", currentUserName },
            { "idDestinatario", friendId },
            { "nombreDestinatario", friendName },
            { "estado", "pendiente" }
        };

        firestore.Collection("SolicitudesAmistad").Document(solicitudId).SetAsync(solicitudData)
            .ContinueWithOnMainThread(setTask =>
            {
                if (setTask.IsCompleted)
                {
                    Debug.Log("Solicitud de amistad enviada de " + currentUserName + " a " + friendName);
                    SetButtonState(button, Color.cyan, "Solicitud enviada", false);
                }
                else
                {
                    Debug.LogError("Error al enviar solicitud: " + setTask.Exception);
                }
            });
    }

    void SetButtonState(Button button, Color color, string text, bool interactable)
    {
        button.GetComponent<Image>().color = color;
        button.GetComponentInChildren<TMP_Text>().text = text;
        button.interactable = interactable;
    }

    private string ObtenerAvatarPorRango(string rangos)
    {
        switch (rangos)
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
}