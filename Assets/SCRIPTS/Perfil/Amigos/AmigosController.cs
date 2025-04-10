using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;

public class AmigosController : MonoBehaviour
{
    public GameObject amigoPrefab;
    public Transform contentPanel;
    public TMP_InputField inputBuscar;
    public Button botonBuscar;
    public TMP_Text messageText; // Nuevo campo para mensajes de estado

    [SerializeField] public GameObject m_AgregarAmigosUI = null;
    [SerializeField] public GameObject m_SolicitudesUI = null;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    private int amigosCargados = 0;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
            ShowMessage("Cargando amigos...");
            CargarAmigos("");

            botonBuscar.onClick.AddListener(() => {
                string nombreBuscar = inputBuscar.text.Trim();
                ShowMessage($"Buscando: {nombreBuscar}");
                CargarAmigos(nombreBuscar);
            });
        }
        else
        {
            ShowMessage("No autenticado", true);
            Debug.LogError("No hay usuario autenticado.");
        }
    }

    void CargarAmigos(string filtroNombre)
    {
        amigosCargados = 0;
        ClearFriendList();

        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: ID de usuario vacío", true);
            return;
        }

        HashSet<string> amigosMostrados = new HashSet<string>();

        // Consulta amigos donde el usuario es remitente
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", userId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  ProcessFriends(task.Result.Documents, true, filtroNombre, amigosMostrados);
                  CheckSearchCompletion(filtroNombre);
              }
              else
              {
                  ShowMessage("Error al cargar amigos", true);
              }
          });

        // Consulta amigos donde el usuario es destinatario
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", userId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  ProcessFriends(task.Result.Documents, false, filtroNombre, amigosMostrados);
                  CheckSearchCompletion(filtroNombre);
              }
          });
    }

    void ProcessFriends(IEnumerable<DocumentSnapshot> documents, bool isSender, string filtroNombre, HashSet<string> amigosMostrados)
    {
        foreach (DocumentSnapshot document in documents)
        {
            string amigoId = isSender ? document.GetValue<string>("idDestinatario") : document.GetValue<string>("idRemitente");
            string nombreAmigo = isSender ? document.GetValue<string>("nombreDestinatario") : document.GetValue<string>("nombreRemitente");

            if (!amigosMostrados.Contains(amigoId))
            {
                amigosMostrados.Add(amigoId);
                if (ShouldShowFriend(nombreAmigo, filtroNombre))
                {
                    CreateFriendCard(amigoId, nombreAmigo);
                    amigosCargados++;
                }
            }
        }
    }

    bool ShouldShowFriend(string nombreAmigo, string filtroNombre)
    {
        return string.IsNullOrEmpty(filtroNombre) ||
               nombreAmigo.ToLower().Contains(filtroNombre.ToLower());
    }

    void CreateFriendCard(string amigoId, string nombreAmigo)
    {
        GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);
        nuevoAmigo.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = nombreAmigo;

        // Configurar estado
        var panelEstado = nuevoAmigo.transform.Find("EstadoPanel").gameObject;
        panelEstado.GetComponent<Image>().color = new Color32(0x52, 0xD9, 0x99, 0xFF);
        nuevoAmigo.transform.Find("Estadotxt").GetComponent<TMP_Text>().text = "Amigos";

        // Cargar rango
        LoadFriendRank(amigoId, nuevoAmigo);
    }

    void LoadFriendRank(string amigoId, GameObject amigoUI)
    {
        db.Collection("usuarios").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string rango = task.Result.GetValue<string>("Rango") ?? "Novato";
                var rangoText = amigoUI.transform.Find("Rangotxt")?.GetComponent<TMP_Text>();
                if (rangoText != null) rangoText.text = rango;
            }
        });
    }

    void CheckSearchCompletion(string filtroNombre)
    {
        if (!string.IsNullOrEmpty(filtroNombre))
        {
            if (amigosCargados > 0)
            {
                ShowMessage($"{amigosCargados} amigos encontrados");
            }
            else
            {
                ShowMessage("No se encontraron amigos con ese nombre");
            }
        }
        else if (amigosCargados > 0)
        {
            ShowMessage($"{amigosCargados} amigos cargados");
        }
    }

    void ClearFriendList()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }

    void ShowMessage(string message, bool isError = false)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = isError ? Color.red : Color.white;
        }
    }

    public void ActivarPanelAgregarAmigos()
    {
        m_AgregarAmigosUI.SetActive(true);
    }

    public void ActivarPanelSolicitudes()
    {
        m_AgregarAmigosUI.SetActive(false);
        m_SolicitudesUI.SetActive(true);
    }
}