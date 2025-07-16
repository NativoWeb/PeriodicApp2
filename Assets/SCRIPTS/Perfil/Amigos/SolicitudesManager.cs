using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Extensions;
using TMPro;
using System.Threading.Tasks;
using System;
using System.Linq;

public class SolicitudesManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject requestPrefab;
    public Transform requestContainer;
    public TMP_InputField searchInput;
    public Button searchButton;
    public TMP_Text messageText;

    [Header("Live Search Settings")]
    public float searchDelay = 0.3f;
    public int minSearchChars = 2;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentUserId;
    private List<FriendRequest> allRequests = new List<FriendRequest>();

    private string lastSearchText = "";
    private float lastSearchTime;
    private bool searchScheduled = false;

    // MODIFICADO: Variables de localización
    private string appIdioma;
    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();

    private class FriendRequest
    {
        public string fromUserId;
        public string fromUserName;
        public string fromUserRank;
        public string documentId;
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // MODIFICADO: Inicializar idioma y textos
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InitializeLocalizedTexts();

        if (auth.CurrentUser == null)
        {
            Debug.LogError(localizedTexts["noAuthUser"]);
            ShowMessage(localizedTexts["noAuthUser"]);
            return;
        }

        currentUserId = auth.CurrentUser.UserId;

        searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        searchButton.onClick.AddListener(() => FilterRequests(searchInput.text));

        LoadPendingRequests();
    }

    // MODIFICADO: Nuevo método para centralizar las traducciones
    void InitializeLocalizedTexts()
    {
        if (appIdioma == "ingles")
        {
            localizedTexts["loading"] = "Loading requests...";
            localizedTexts["loadError"] = "Error loading requests: ";
            localizedTexts["noPending"] = "You have no pending requests";
            localizedTexts["requestsFound"] = "{0} requests found";
            localizedTexts["noRequestsToShow"] = "No requests to display";
            localizedTexts["noMatches"] = "No matches found";
            localizedTexts["matchesFound"] = "{0} requests match your search";
            localizedTexts["showingRequests"] = "Showing {0} requests";
            localizedTexts["processing"] = "Processing request...";
            localizedTexts["getRequestError"] = "Error getting request data";
            localizedTexts["updateRequestError"] = "Error updating request status";
            localizedTexts["requestAccepted"] = "Request accepted and friend added successfully";
            localizedTexts["addFriendError"] = "Error adding friend";
            localizedTexts["requestRejected"] = "Request rejected";
            localizedTexts["rejectRequestError"] = "Error rejecting request";
            localizedTexts["unknownUser"] = "Unknown";
            localizedTexts["unnamedUser"] = "Unnamed User";
            localizedTexts["defaultRank"] = "Lab Newbie"; // Aunque la lógica se basa en español, el default en UI puede cambiar
            localizedTexts["noAuthUser"] = "User not authenticated.";
            localizedTexts["rankLabel"] = "Rank: {0}";
        }
        else // Español (por defecto)
        {
            localizedTexts["loading"] = "Cargando solicitudes...";
            localizedTexts["loadError"] = "Error al obtener solicitudes: ";
            localizedTexts["noPending"] = "No tienes solicitudes pendientes";
            localizedTexts["requestsFound"] = "{0} solicitudes encontradas";
            localizedTexts["noRequestsToShow"] = "No hay solicitudes para mostrar";
            localizedTexts["noMatches"] = "No se encontraron coincidencias";
            localizedTexts["matchesFound"] = "{0} solicitudes coinciden con tu búsqueda";
            localizedTexts["showingRequests"] = "Mostrando {0} solicitudes";
            localizedTexts["processing"] = "Procesando solicitud...";
            localizedTexts["getRequestError"] = "Error al obtener datos de la solicitud";
            localizedTexts["updateRequestError"] = "Error al actualizar estado de solicitud";
            localizedTexts["requestAccepted"] = "Solicitud aceptada y amigo agregado con éxito";
            localizedTexts["addFriendError"] = "Error al agregar amigo";
            localizedTexts["requestRejected"] = "Solicitud rechazada";
            localizedTexts["rejectRequestError"] = "Error al rechazar solicitud";
            localizedTexts["unknownUser"] = "Desconocido";
            localizedTexts["unnamedUser"] = "Usuario sin nombre";
            localizedTexts["defaultRank"] = "Novato de laboratorio";
            localizedTexts["noAuthUser"] = "Usuario no autenticado.";
            localizedTexts["rankLabel"] = "Rango: {0}";
        }
    }

    void Update()
    {
        if (searchScheduled && Time.time >= lastSearchTime + searchDelay)
        {
            searchScheduled = false;
            FilterRequests(lastSearchText);
        }
    }

    void OnSearchInputChanged(string text)
    {
        lastSearchText = text;
        if (string.IsNullOrEmpty(text))
        {
            FilterRequests("");
            return;
        }
        if (text.Length < minSearchChars) return;

        lastSearchTime = Time.time;
        searchScheduled = true;
    }

    async void LoadPendingRequests()
    {
        ShowMessage(localizedTexts["loading"]);

        var query = db.Collection("SolicitudesAmistad")
                      .WhereEqualTo("idDestinatario", currentUserId)
                      .WhereEqualTo("estado", "pendiente");

        var snapshot = await query.GetSnapshotAsync();

        allRequests.Clear();
        var userFetchTasks = new List<Task<DocumentSnapshot>>();

        foreach (var doc in snapshot.Documents)
        {
            string fromUserId = doc.GetValue<string>("idRemitente");
            userFetchTasks.Add(db.Collection("users").Document(fromUserId).GetSnapshotAsync());
        }

        var userSnapshots = await Task.WhenAll(userFetchTasks);

        for (int i = 0; i < userSnapshots.Length; i++)
        {
            var userDoc = userSnapshots[i];
            var requestDoc = snapshot.Documents.ElementAt(i);

            if (userDoc.Exists)
            {
                allRequests.Add(new FriendRequest
                {
                    fromUserId = userDoc.Id,
                    fromUserName = userDoc.GetValue<string>("DisplayName") ?? localizedTexts["unknownUser"],
                    fromUserRank = userDoc.GetValue<string>("Rango") ?? localizedTexts["defaultRank"],
                    documentId = requestDoc.Id
                });
            }
        }

        if (allRequests.Count == 0)
        {
            ShowMessage(localizedTexts["noPending"]);
        }
        else
        {
            ShowMessage(string.Format(localizedTexts["requestsFound"], allRequests.Count));
        }

        FilterRequests("");
    }


    void FilterRequests(string searchText)
    {
        foreach (Transform child in requestContainer) Destroy(child.gameObject);

        if (allRequests.Count == 0)
        {
            ShowMessage(localizedTexts["noRequestsToShow"]);
            return;
        }

        int matches = 0;
        string lowerSearchText = searchText.ToLower();

        foreach (var request in allRequests)
        {
            if (string.IsNullOrEmpty(lowerSearchText) || request.fromUserName.ToLower().Contains(lowerSearchText))
            {
                CreateRequestUI(request.fromUserId, request.fromUserName, request.fromUserRank, request.documentId);
                matches++;
            }
        }

        if (matches == 0 && !string.IsNullOrEmpty(searchText))
        {
            ShowMessage(localizedTexts["noMatches"]);
        }
        else if (matches > 0 && !string.IsNullOrEmpty(searchText))
        {
            ShowMessage(string.Format(localizedTexts["matchesFound"], matches));
        }
        else if (matches > 0 && string.IsNullOrEmpty(searchText))
        {
            ShowMessage(string.Format(localizedTexts["showingRequests"], matches));
        }
    }

    void CreateRequestUI(string fromUserId, string fromUserName, string userRank, string documentId)
    {
        GameObject requestItem = Instantiate(requestPrefab, requestContainer);

        requestItem.transform.Find("NombreText").GetComponent<TMP_Text>().text = fromUserName;
        requestItem.transform.Find("RangoText").GetComponent<TMP_Text>().text = string.Format(localizedTexts["rankLabel"], userRank);

        string avatarPath = ObtenerAvatarPorRango(userRank);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);
        requestItem.transform.Find("AvatarImage").GetComponent<Image>().sprite = avatarSprite;

        requestItem.transform.Find("AceptarBtn").GetComponent<Button>().onClick.AddListener(() => AcceptRequest(documentId));
        requestItem.transform.Find("RechazarBtn").GetComponent<Button>().onClick.AddListener(() => RejectRequest(documentId));
    }

    async void AcceptRequest(string documentId)
    {
        ShowMessage(localizedTexts["processing"]);

        DocumentReference requestDocRef = db.Collection("SolicitudesAmistad").Document(documentId);
        var requestSnapshot = await requestDocRef.GetSnapshotAsync();

        if (!requestSnapshot.Exists)
        {
            ShowMessage(localizedTexts["getRequestError"]);
            return;
        }

        string fromUserId = requestSnapshot.GetValue<string>("idRemitente");
        string fromUserName = requestSnapshot.GetValue<string>("nombreRemitente");

        var friendDataForCurrentUser = new Dictionary<string, object>
        {
            { "userId", fromUserId },
            { "DisplayName", fromUserName },
            { "fechaAmistad", Timestamp.GetCurrentTimestamp() }
        };

        var currentUserDataForFriend = new Dictionary<string, object>
        {
            { "userId", currentUserId },
            { "DisplayName", auth.CurrentUser.DisplayName ?? localizedTexts["unnamedUser"] },
            { "fechaAmistad", Timestamp.GetCurrentTimestamp() }
        };

        var batch = db.StartBatch();
        batch.Update(requestDocRef, "estado", "aceptada");
        batch.Set(db.Collection("users").Document(currentUserId).Collection("amigos").Document(fromUserId), friendDataForCurrentUser);
        batch.Set(db.Collection("users").Document(fromUserId).Collection("amigos").Document(currentUserId), currentUserDataForFriend);

        try
        {
            await batch.CommitAsync();
            ShowMessage(localizedTexts["requestAccepted"]);
            LoadPendingRequests();
        }
        catch (Exception e)
        {
            ShowMessage(localizedTexts["addFriendError"]);
            Debug.LogError("Error en batch de aceptación: " + e.Message);
        }
    }

    async void RejectRequest(string documentId)
    {
        ShowMessage(localizedTexts["processing"]);
        try
        {
            await db.Collection("SolicitudesAmistad").Document(documentId).DeleteAsync();
            ShowMessage(localizedTexts["requestRejected"]);
            LoadPendingRequests();
        }
        catch (Exception e)
        {
            ShowMessage(localizedTexts["rejectRequestError"]);
            Debug.LogError("Error al rechazar solicitud: " + e.Message);
        }
    }

    void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private string ObtenerAvatarPorRango(string rango)
    {
        // Esta función depende de los valores en español de la base de datos.
        // La traducción se maneja en la etiqueta de la UI, no aquí.
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
}