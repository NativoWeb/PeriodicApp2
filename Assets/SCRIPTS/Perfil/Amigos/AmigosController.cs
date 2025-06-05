using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class AmigosController : MonoBehaviour
{
    public GameObject amigoPrefab;
    public Transform contentPanel;
    public TMP_InputField inputBuscar;
    public Button botonBuscar;  // Podemos mantenerlo como respaldo
    public TMP_Text messageText; // Nuevo campo para mensajes de estado
    public Button agregarAmigosButton; // Botón para agregar amigos
    public float liveSearchDelay = 0.3f; // Tiempo de espera en segundos para iniciar la búsqueda

    // Evitar Duplicados...
    private bool isLoading = false;
    private int consultasCompletadas = 0;
    private Coroutine liveSearchCoroutine;

    [SerializeField] public GameObject m_AgregarAmigosUI = null;
    [SerializeField] public GameObject m_SolicitudesUI = null;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    private int amigosCargados = 0;

    // Variables para el proceso de eliminación
    private string amigoIdSeleccionado;
    private string amigoNombreSeleccionado;
    private string documentoSolicitudSeleccionado;

    // Panel de confirmación para eliminar amigos
    public GameObject panelConfirmacionEliminar;
    public Button botonConfirmarEliminar;
    public Button botonCancelarEliminar;
    public TMP_Text textoConfirmacion;

    private Color defaultColor;

    void Start()
    {
        if (messageText != null)
        {
            defaultColor = messageText.color; // Guardamos el color desde el Inspector
        }

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        // Verificar conexión al inicio
        if (!HayConexion())
        {
            ShowMessage("No hay conexión a internet. Algunas funciones pueden no estar disponibles.", true);
        }

        // verificar si tiene que mostrar alguna scena en particular
        if (PlayerPrefs.GetInt("MostrarSolicitudes", 0) == 1)
        {
            ActivarPanelSolicitudes();
            PlayerPrefs.SetInt("MostrarSolicitudes", 0); // Limpia después de usar
        }
        else if (PlayerPrefs.GetInt("MostrarSugerencias", 0) == 1)
        {
            ActivarPanelAgregarAmigos();
            PlayerPrefs.SetInt("MostrarSugerencias", 0); // Limpia después de usar
        }

        // Inicializar panel de confirmación
        if (panelConfirmacionEliminar != null)
        {
            panelConfirmacionEliminar.SetActive(false);

            if (botonConfirmarEliminar != null)
                botonConfirmarEliminar.onClick.AddListener(EliminarAmigoConfirmado);

            if (botonCancelarEliminar != null)
                botonCancelarEliminar.onClick.AddListener(() => panelConfirmacionEliminar.SetActive(false));
        }

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId;
            ShowMessage("Cargando amigos...");
            CargarAmigos("");

            // Configurar el live search
            if (inputBuscar != null)
            {
                inputBuscar.onValueChanged.AddListener(OnSearchInputChanged);
            }

            // Mantener el botón de búsqueda como respaldo
            if (botonBuscar != null)
            {
                botonBuscar.onClick.AddListener(() => {
                    string nombreBuscar = inputBuscar.text.Trim();
                    ShowMessage($"Buscando: {nombreBuscar}");
                    CargarAmigos(nombreBuscar);
                });
            }

            // Configurar el botón de agregar amigos
            if (agregarAmigosButton != null)
            {
                agregarAmigosButton.onClick.AddListener(ActivarPanelAgregarAmigos);
            }
        }
        else
        {
            ShowMessage("No autenticado", true);
            Debug.LogError("No hay usuario autenticado.");
        }
        
    }

    void OnSearchInputChanged(string searchText)
    {
        // Detener cualquier búsqueda en progreso
        if (liveSearchCoroutine != null)
        {
            StopCoroutine(liveSearchCoroutine);
        }

        // Iniciar una nueva búsqueda con delay
        liveSearchCoroutine = StartCoroutine(DelayedSearch(searchText));
    }

    IEnumerator DelayedSearch(string searchText)
    {
        // Esperar un breve momento para permitir que el usuario termine de escribir
        yield return new WaitForSeconds(liveSearchDelay);

        // Si estamos en modo de eliminación, no realizar búsqueda
        if (panelConfirmacionEliminar != null && panelConfirmacionEliminar.activeSelf)
        {
            yield break;
        }

        // Realizar la búsqueda
        ShowMessage($"Buscando: {searchText}");
        CargarAmigos(searchText);
    }

    void CargarAmigos(string filtroNombre)
    {
        if (isLoading) return;

        // Verificar conexión antes de cargar amigos
        if (!HayConexion())
        {
            ShowMessage("No hay conexión a internet.", true);
           
        }

        isLoading = true;
        consultasCompletadas = 0;
        amigosCargados = 0;
        ClearFriendList();

        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: ID de usuario vacío", true);
            isLoading = false;
            return;
        }

        HashSet<string> amigosMostrados = new HashSet<string>();

        // Consulta amigos donde el usuario es remitente
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", userId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  ProcessFriends(task.Result.Documents, true, filtroNombre, amigosMostrados);
              }
              else
              {
                  ShowMessage("Error al cargar amigos", true);
              }
              ConsultaCompletada(filtroNombre);
          });

        // Consulta amigos donde el usuario es destinatario
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", userId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted && !task.IsFaulted)
              {
                  ProcessFriends(task.Result.Documents, false, filtroNombre, amigosMostrados);
              }
              ConsultaCompletada(filtroNombre);
          });
    }

    void ConsultaCompletada(string filtroNombre)
    {
        consultasCompletadas++;

        // Solo cuando ambas consultas terminen
        if (consultasCompletadas == 2)
        {
            CheckSearchCompletion(filtroNombre);
            isLoading = false;
        }
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
                    CreateFriendCard(amigoId, nombreAmigo, document.Id);
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

    void CreateFriendCard(string amigoId, string nombreAmigo, string documentId)
    {
        GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);
        nuevoAmigo.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = nombreAmigo;

        // Configurar estado
        var panelEstado = nuevoAmigo.transform.Find("EstadoPanel").gameObject;
        panelEstado.GetComponent<Image>().color = new Color32(0x52, 0xD9, 0x99, 0xFF);
        nuevoAmigo.transform.Find("Estadotxt").GetComponent<TMP_Text>().text = "Amigos";

        // Añadir botón de eliminar amigo
        Button btnEliminar = nuevoAmigo.transform.Find("BtnEliminar")?.GetComponent<Button>();
        if (btnEliminar != null)
        {
            btnEliminar.onClick.AddListener(() => MostrarConfirmacionEliminar(amigoId, nombreAmigo, documentId));
        }

        // Cargar rango
        LoadFriendRank(amigoId, nuevoAmigo);
    }

    void MostrarConfirmacionEliminar(string amigoId, string nombreAmigo, string documentId)
    {
        if (panelConfirmacionEliminar == null) return;

        // Verificar conexión antes de mostrar el panel de confirmación
        if (!HayConexion())
        {
            ShowMessage("No puedes eliminar amigos sin conexión a internet", true);
            return;
        }

        amigoIdSeleccionado = amigoId;
        amigoNombreSeleccionado = nombreAmigo;
        documentoSolicitudSeleccionado = documentId;

        if (textoConfirmacion != null)
            textoConfirmacion.text = $"¿Estás seguro que deseas eliminar a {nombreAmigo} de tu lista de amigos?";

        panelConfirmacionEliminar.SetActive(true);

        // Seleccionar el botón de cancelar por defecto para mejor UX
        EventSystem.current.SetSelectedGameObject(botonCancelarEliminar.gameObject);
    }

    void EliminarAmigoConfirmado()
    {
        if (string.IsNullOrEmpty(documentoSolicitudSeleccionado)) return;

        ShowMessage($"Eliminando a {amigoNombreSeleccionado}...");

        // Actualizar el estado a "eliminada" en lugar de borrar el documento
        DocumentReference docRef = db.Collection("SolicitudesAmistad").Document(documentoSolicitudSeleccionado);
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "estado", "eliminada" }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task => {
            if (task.IsCompleted && !task.IsFaulted)
            {
                ShowMessage($"{amigoNombreSeleccionado} ha sido eliminado de tu lista de amigos");
                panelConfirmacionEliminar.SetActive(false);

                // Recargar la lista de amigos
                CargarAmigos(inputBuscar.text.Trim());
            }
            else
            {
                ShowMessage("Error al eliminar amigo", true);
                Debug.LogError($"Error: {task.Exception}");
                panelConfirmacionEliminar.SetActive(false);
            }
        });
    }

    void LoadFriendRank(string amigoId, GameObject amigoUI)
    {
        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string rango = task.Result.GetValue<string>("Rango") ?? "Novato de laboratorio";
                var rangoText = amigoUI.transform.Find("Rangotxt")?.GetComponent<TMP_Text>();
                if (rangoText != null) rangoText.text = rango;
            }
        });
    }

    void CheckSearchCompletion(string filtroNombre)
    {
        if (amigosCargados == 0)
        {
            if (!string.IsNullOrEmpty(filtroNombre))
            {
                ShowMessage("No se encontraron amigos con ese nombre. ¡Prueba a agregar nuevos amigos!");
            }
            else
            {
                ShowMessage("No tienes amigos aún. ¡Agrega algunos amigos para comenzar!");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(filtroNombre))
            {
                ShowMessage($"{amigosCargados} amigos encontrados");
            }
            else
            {
                ShowMessage($"{amigosCargados} amigos cargados");
            }
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
            // Cambiar color según si es error o no
            messageText.color = isError ? Color.red : defaultColor;

            // Si el mensaje está vacío, ocultar el texto
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
    }

    public void ActivarPanelAgregarAmigos()
    {
        m_AgregarAmigosUI.SetActive(true);
        m_SolicitudesUI.SetActive(false);
        ShowMessage(""); // Limpiar mensaje al cambiar de panel
    }

    public void ActivarPanelSolicitudes()
    {
        m_AgregarAmigosUI.SetActive(false);
        m_SolicitudesUI.SetActive(true);
        ShowMessage(""); // Limpiar mensaje al cambiar de panel
    }

    private void OnDestroy()
    {
        // Asegurarse de detener las corrutinas al destruir el objeto
        if (liveSearchCoroutine != null)
        {
            StopCoroutine(liveSearchCoroutine);
        }
    }
    public bool HayConexion()
    {
        bool hayConexion = Application.internetReachability != NetworkReachability.NotReachable;

        // Mostrar u ocultar mensaje de conexión
        if (!hayConexion)
        {
            ShowMessage("No hay conexión a internet. Algunas funciones pueden no estar disponibles.", true);
        }

        return hayConexion;
    }
}