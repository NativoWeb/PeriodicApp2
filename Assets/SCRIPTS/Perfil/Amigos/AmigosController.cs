using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;
using System.Threading.Tasks;

public class AmigosController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject amigoPrefab;
    public Transform contentPanel;
    public TMP_InputField inputBuscar;
    public Button botonBuscar;
    public TMP_Text messageText;
    public Button agregarAmigosButton;
    public GameObject panelConfirmacionEliminar;
    public Button botonConfirmarEliminar;
    public Button botonCancelarEliminar;
    public TMP_Text textoConfirmacion;

    [Header("Panel References")]
    [SerializeField] public GameObject m_AgregarAmigosUI = null;
    [SerializeField] public GameObject m_SolicitudesUI = null;

    [Header("Live Search Settings")]
    public float liveSearchDelay = 0.3f;
    private Coroutine liveSearchCoroutine;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;

    private bool isLoading = false;
    private int amigosCargados = 0;

    // Variables para el proceso de eliminación
    private string amigoIdSeleccionado;
    private string amigoNombreSeleccionado;
    private string documentoSolicitudSeleccionado;

    private Color defaultColor;

    // MODIFICADO: Variables de localización
    private string appIdioma;
    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // MODIFICADO: Inicializar idioma y textos
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InitializeLocalizedTexts();

        if (messageText != null) defaultColor = messageText.color;

        if (!HayConexion())
        {
            ShowMessage(localizedTexts["noConnection"], true);
        }

        if (PlayerPrefs.GetInt("MostrarSolicitudes", 0) == 1)
        {
            ActivarPanelSolicitudes();
            PlayerPrefs.SetInt("MostrarSolicitudes", 0);
        }
        else if (PlayerPrefs.GetInt("MostrarSugerencias", 0) == 1)
        {
            ActivarPanelAgregarAmigos();
            PlayerPrefs.SetInt("MostrarSugerencias", 0);
        }

        if (panelConfirmacionEliminar != null)
        {
            panelConfirmacionEliminar.SetActive(false);
            if (botonCancelarEliminar != null)
                botonCancelarEliminar.onClick.AddListener(() => panelConfirmacionEliminar.SetActive(false));
        }

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
            ShowMessage(localizedTexts["loadingFriends"]);
            CargarAmigos("");

            if (inputBuscar != null) inputBuscar.onValueChanged.AddListener(OnSearchInputChanged);
            if (botonBuscar != null) botonBuscar.onClick.AddListener(() => CargarAmigos(inputBuscar.text.Trim()));
            if (agregarAmigosButton != null) agregarAmigosButton.onClick.AddListener(ActivarPanelAgregarAmigos);
        }
        else
        {
            ShowMessage(localizedTexts["notAuthenticated"], true);
            Debug.LogError("No hay usuario autenticado.");
        }
    }

    // MODIFICADO: Nuevo método para centralizar las traducciones
    void InitializeLocalizedTexts()
    {
        if (appIdioma == "ingles")
        {
            localizedTexts["noConnection"] = "No internet connection. Some features may not be available.";
            localizedTexts["loadingFriends"] = "Loading friends...";
            localizedTexts["searching"] = "Searching: {0}";
            localizedTexts["notAuthenticated"] = "Not authenticated";
            localizedTexts["emptyUserIdError"] = "Error: User ID is empty";
            localizedTexts["loadError"] = "Error loading friends.";
            localizedTexts["friendshipInfoError"] = "Error loading friendship information.";
            localizedTexts["noMatches"] = "No friends found with that name. Try adding new friends!";
            localizedTexts["noFriendsYet"] = "You don't have any friends yet. Add some to get started!";
            localizedTexts["friendsFound"] = "{0} friends found";
            localizedTexts["friendsLoaded"] = "{0} friends loaded";
            localizedTexts["unknown"] = "Unknown";
            localizedTexts["statusFriends"] = "Friends";
            localizedTexts["defaultRank"] = "Lab Newbie";
            localizedTexts["cantDeleteOffline"] = "You can't remove friends without an internet connection";
            localizedTexts["deleteConfirm"] = "Are you sure you want to remove {0} from your friends list?";
            localizedTexts["deleteIncompleteData"] = "Incomplete data to remove friend";
            localizedTexts["deleting"] = "Removing {0}...";
            localizedTexts["deleteSuccess"] = "{0} has been removed from your friends list";
            localizedTexts["deleteError"] = "Error removing friend";
            localizedTexts["rankLabel"] = "Rank: {0}";
        }
        else // Español por defecto
        {
            localizedTexts["noConnection"] = "No hay conexión a internet. Algunas funciones pueden no estar disponibles.";
            localizedTexts["loadingFriends"] = "Cargando amigos...";
            localizedTexts["searching"] = "Buscando: {0}";
            localizedTexts["notAuthenticated"] = "No autenticado";
            localizedTexts["emptyUserIdError"] = "Error: ID de usuario vacío";
            localizedTexts["loadError"] = "Error al cargar amigos.";
            localizedTexts["friendshipInfoError"] = "Error al cargar información de amistad.";
            localizedTexts["noMatches"] = "No se encontraron amigos con ese nombre. ¡Prueba a agregar nuevos amigos!";
            localizedTexts["noFriendsYet"] = "No tienes amigos aún. ¡Agrega algunos amigos para comenzar!";
            localizedTexts["friendsFound"] = "{0} amigos encontrados";
            localizedTexts["friendsLoaded"] = "{0} amigos cargados";
            localizedTexts["unknown"] = "Desconocido";
            localizedTexts["statusFriends"] = "Amigos";
            localizedTexts["defaultRank"] = "Novato de laboratorio";
            localizedTexts["cantDeleteOffline"] = "No puedes eliminar amigos sin conexión a internet";
            localizedTexts["deleteConfirm"] = "¿Estás seguro que deseas eliminar a {0} de tu lista de amigos?";
            localizedTexts["deleteIncompleteData"] = "Datos incompletos para eliminar amigo";
            localizedTexts["deleting"] = "Eliminando a {0}...";
            localizedTexts["deleteSuccess"] = "{0} ha sido eliminado de tu lista de amigos";
            localizedTexts["deleteError"] = "Error al eliminar amigo";
            localizedTexts["rankLabel"] = "Rango: {0}";
        }
    }


    void OnSearchInputChanged(string searchText)
    {
        if (liveSearchCoroutine != null) StopCoroutine(liveSearchCoroutine);
        liveSearchCoroutine = StartCoroutine(DelayedSearch(searchText));
    }

    IEnumerator DelayedSearch(string searchText)
    {
        yield return new WaitForSeconds(liveSearchDelay);
        if (panelConfirmacionEliminar != null && panelConfirmacionEliminar.activeSelf) yield break;
        ShowMessage(string.Format(localizedTexts["searching"], searchText));
        CargarAmigos(searchText);
    }

    void CargarAmigos(string filtroNombre)
    {
        if (isLoading) return;
        if (!HayConexion())
        {
            ShowMessage(localizedTexts["noConnection"], true);
            return;
        }

        isLoading = true;
        amigosCargados = 0;
        ClearFriendList();

        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage(localizedTexts["emptyUserIdError"], true);
            isLoading = false;
            return;
        }

        db.Collection("users").Document(userId).Collection("amigos")
            .GetSnapshotAsync().ContinueWithOnMainThread(amigosTask =>
            {
                if (amigosTask.IsFaulted)
                {
                    Debug.LogError("Error al cargar amigos: " + amigosTask.Exception);
                    ShowMessage(localizedTexts["loadError"], true);
                    isLoading = false;
                    return;
                }

                Dictionary<string, string> solicitudesDict = new Dictionary<string, string>();
                HashSet<string> amigosMostrados = new HashSet<string>();

                foreach (DocumentSnapshot amigoDoc in amigosTask.Result.Documents)
                {
                    string amigoId = amigoDoc.GetValue<string>("userId");
                    string nombreAmigo = amigoDoc.GetValue<string>("DisplayName");

                    if (!amigosMostrados.Contains(amigoId) && ShouldShowFriend(nombreAmigo, filtroNombre))
                    {
                        amigosMostrados.Add(amigoId);
                        CreateFriendCard(amigoId, ""); // documentId se buscará después si es necesario
                        amigosCargados++;
                    }
                }
                CheckSearchCompletion(filtroNombre);
                isLoading = false;
            });
    }

    bool ShouldShowFriend(string nombreAmigo, string filtroNombre)
    {
        return string.IsNullOrEmpty(filtroNombre) || nombreAmigo.ToLower().Contains(filtroNombre.ToLower());
    }

    void CreateFriendCard(string amigoId, string documentId)
    {
        GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);

        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string nombreAmigo = task.Result.GetValue<string>("DisplayName") ?? localizedTexts["unknown"];
                nuevoAmigo.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = nombreAmigo;

                var panelEstado = nuevoAmigo.transform.Find("EstadoPanel").gameObject;
                panelEstado.GetComponent<Image>().color = new Color32(0x52, 0xD9, 0x99, 0xFF);
                nuevoAmigo.transform.Find("Estadotxt").GetComponent<TMP_Text>().text = localizedTexts["statusFriends"];

                Button btnEliminar = nuevoAmigo.transform.Find("BtnEliminar")?.GetComponent<Button>();
                if (btnEliminar != null)
                {
                    btnEliminar.onClick.AddListener(() => MostrarConfirmacionEliminar(amigoId, nombreAmigo, documentId));
                }

                LoadFriendRankAndAvatar(amigoId, nuevoAmigo);
            }
        });
    }

    void LoadFriendRankAndAvatar(string amigoId, GameObject amigoUI)
    {
        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string rango = task.Result.GetValue<string>("Rango") ?? localizedTexts["defaultRank"];
                var rangoText = amigoUI.transform.Find("Rangotxt")?.GetComponent<TMP_Text>();
                if (rangoText != null) rangoText.text = string.Format(localizedTexts["rankLabel"], rango);

                string avatarPath = ObtenerAvatarPorRango(rango);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");
                Transform avatarTransform = amigoUI.transform.Find("AvatarImage");
                if (avatarTransform != null) avatarTransform.GetComponent<Image>().sprite = avatarSprite;
            }
        });
    }

    void MostrarConfirmacionEliminar(string amigoId, string nombreAmigo, string documentId)
    {
        if (panelConfirmacionEliminar == null) return;
        if (!HayConexion())
        {
            ShowMessage(localizedTexts["cantDeleteOffline"], true);
            return;
        }

        amigoIdSeleccionado = amigoId;
        amigoNombreSeleccionado = nombreAmigo;
        // Para la eliminación, necesitamos encontrar el ID del documento de la solicitud de amistad
        FindFriendshipDocumentId(amigoId, (solicitudId) =>
        {
            if (solicitudId != null)
            {
                documentoSolicitudSeleccionado = solicitudId;
                if (textoConfirmacion != null)
                    textoConfirmacion.text = string.Format(localizedTexts["deleteConfirm"], nombreAmigo);

                panelConfirmacionEliminar.SetActive(true);
                if (botonConfirmarEliminar != null)
                {
                    botonConfirmarEliminar.onClick.RemoveAllListeners(); // Limpiar listeners antiguos
                    botonConfirmarEliminar.onClick.AddListener(EliminarAmigoConfirmado);
                }
            }
            else
            {
                ShowMessage(localizedTexts["friendshipInfoError"], true);
            }
        });
    }

    void FindFriendshipDocumentId(string amigoId, Action<string> callback)
    {
        var query1 = db.Collection("SolicitudesAmistad").WhereEqualTo("idRemitente", userId).WhereEqualTo("idDestinatario", amigoId).WhereEqualTo("estado", "aceptada");
        var query2 = db.Collection("SolicitudesAmistad").WhereEqualTo("idRemitente", amigoId).WhereEqualTo("idDestinatario", userId).WhereEqualTo("estado", "aceptada");

        Task.WhenAll(query1.GetSnapshotAsync(), query2.GetSnapshotAsync()).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                callback(null);
                return;
            }
            var doc = task.Result[0].Documents.FirstOrDefault() ?? task.Result[1].Documents.FirstOrDefault();
            callback(doc?.Id);
        });
    }

    void EliminarAmigoConfirmado()
    {
        if (string.IsNullOrEmpty(documentoSolicitudSeleccionado) || string.IsNullOrEmpty(amigoIdSeleccionado))
        {
            ShowMessage(localizedTexts["deleteIncompleteData"], true);
            return;
        }

        ShowMessage(string.Format(localizedTexts["deleting"], amigoNombreSeleccionado));

        WriteBatch batch = db.StartBatch();
        batch.Delete(db.Collection("SolicitudesAmistad").Document(documentoSolicitudSeleccionado));
        batch.Delete(db.Collection("users").Document(userId).Collection("amigos").Document(amigoIdSeleccionado));
        batch.Delete(db.Collection("users").Document(amigoIdSeleccionado).Collection("amigos").Document(userId));

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            panelConfirmacionEliminar.SetActive(false);
            if (task.IsCompleted && !task.IsFaulted)
            {
                ShowMessage(string.Format(localizedTexts["deleteSuccess"], amigoNombreSeleccionado));
                CargarAmigos(inputBuscar.text.Trim());
            }
            else
            {
                ShowMessage(localizedTexts["deleteError"], true);
                Debug.LogError($"Error al eliminar amigo: {task.Exception}");
            }
        });
    }

    void CheckSearchCompletion(string filtroNombre)
    {
        if (amigosCargados == 0)
        {
            ShowMessage(string.IsNullOrEmpty(filtroNombre) ? localizedTexts["noFriendsYet"] : localizedTexts["noMatches"]);
        }
        else
        {
            ShowMessage(string.IsNullOrEmpty(filtroNombre)
                ? string.Format(localizedTexts["friendsLoaded"], amigosCargados)
                : string.Format(localizedTexts["friendsFound"], amigosCargados));
        }
    }

    void ClearFriendList()
    {
        foreach (Transform child in contentPanel) Destroy(child.gameObject);
    }

    void ShowMessage(string message, bool isError = false)
    {
        if (messageText == null) return;
        messageText.text = message;
        messageText.color = isError ? Color.red : defaultColor;
        messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    public void ActivarPanelAgregarAmigos()
    {
        m_AgregarAmigosUI.SetActive(true);
        m_SolicitudesUI.SetActive(false);
        ShowMessage("");
    }

    public void ActivarPanelSolicitudes()
    {
        m_AgregarAmigosUI.SetActive(false);
        m_SolicitudesUI.SetActive(true);
        ShowMessage("");
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

    public bool HayConexion()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void OnDestroy()
    {
        if (liveSearchCoroutine != null) StopCoroutine(liveSearchCoroutine);
    }
}