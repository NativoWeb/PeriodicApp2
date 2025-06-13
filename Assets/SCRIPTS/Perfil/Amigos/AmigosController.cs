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
using System.Threading.Tasks;

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

        if (!HayConexion())
        {
            ShowMessage("No hay conexión a internet.", true);
            return;
        }

        isLoading = true;
        amigosCargados = 0;
        ClearFriendList();

        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: ID de usuario vacío", true);
            isLoading = false;
            return;
        }

        // Primero cargamos la lista de amigos
        db.Collection("users").Document(userId).Collection("amigos")
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al cargar amigos: " + task.Exception);
                    ShowMessage("Error al cargar amigos.", true);
                    isLoading = false;
                    return;
                }

                // Consulta para solicitudes donde el usuario es remitente
                Query queryRemitente = db.Collection("SolicitudesAmistad")
                    .WhereEqualTo("estado", "aceptada")
                    .WhereEqualTo("idRemitente", userId);

                // Consulta para solicitudes donde el usuario es destinatario
                Query queryDestinatario = db.Collection("SolicitudesAmistad")
                    .WhereEqualTo("estado", "aceptada")
                    .WhereEqualTo("idDestinatario", userId);

                // Ejecutar ambas consultas
                Task.WhenAll(
                    queryRemitente.GetSnapshotAsync(),
                    queryDestinatario.GetSnapshotAsync()
                ).ContinueWithOnMainThread(combinedTask =>
                {
                    if (combinedTask.IsFaulted)
                    {
                        Debug.LogError("Error al cargar solicitudes: " + combinedTask.Exception);
                        ShowMessage("Error al cargar información de amistad.", true);
                        isLoading = false;
                        return;
                    }

                    // Combinar resultados de ambas consultas
                    var resultados = combinedTask.Result;
                    QuerySnapshot remitenteSnapshot = resultados[0];
                    QuerySnapshot destinatarioSnapshot = resultados[1];

                    // Diccionario para mapear amigoId -> documentId de solicitud
                    Dictionary<string, string> solicitudesDict = new Dictionary<string, string>();

                    // Procesar solicitudes donde el usuario es remitente
                    foreach (DocumentSnapshot solicitudDoc in remitenteSnapshot.Documents)
                    {
                        string destinatario = solicitudDoc.GetValue<string>("idDestinatario");
                        solicitudesDict[destinatario] = solicitudDoc.Id;
                    }

                    // Procesar solicitudes donde el usuario es destinatario
                    foreach (DocumentSnapshot solicitudDoc in destinatarioSnapshot.Documents)
                    {
                        string remitente = solicitudDoc.GetValue<string>("idRemitente");
                        solicitudesDict[remitente] = solicitudDoc.Id;
                    }

                    // Procesar los amigos con sus documentIds
                    HashSet<string> amigosMostrados = new HashSet<string>();
                    foreach (DocumentSnapshot amigoDoc in task.Result.Documents)
                    {
                        string amigoId = amigoDoc.GetValue<string>("userId");
                        string nombreAmigo = amigoDoc.GetValue<string>("DisplayName");
                        string solicitudId = solicitudesDict.TryGetValue(amigoId, out var id) ? id : null;

                        if (!amigosMostrados.Contains(amigoId))
                        {
                            amigosMostrados.Add(amigoId);

                            if (ShouldShowFriend(nombreAmigo, filtroNombre))
                            {
                                CreateFriendCard(amigoId, solicitudId);
                                amigosCargados++;
                            }
                        }
                    }

                    CheckSearchCompletion(filtroNombre);
                    isLoading = false;
                });
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
                    CreateFriendCard(amigoId, document.Id);
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

    void CreateFriendCard(string amigoId, string documentId)
    {
        
        GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);

        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                // Cargar nombre del amigo
                string nombreAmigo = task.Result.GetValue<string>("DisplayName") ?? "Desconocido";
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

                // Cargar rango y avatar
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
                // Cargar rango
                string rango = task.Result.GetValue<string>("Rango") ?? "Novato de laboratorio";
                var rangoText = amigoUI.transform.Find("Rangotxt")?.GetComponent<TMP_Text>();
                if (rangoText != null) rangoText.text = rango;

                // Cargar avatar
                string avatarPath = ObtenerAvatarPorRango(rango);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

                // Buscar el componente Image del avatar en el prefab
                Transform avatarTransform = amigoUI.transform.Find("AvatarImage"); // Asegúrate de que este es el nombre correcto en tu prefab
                if (avatarTransform != null)
                {
                    Image avatarImage = avatarTransform.GetComponent<Image>();
                    if (avatarImage != null)
                    {
                        avatarImage.sprite = avatarSprite;
                    }
                }
            }
        });
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
        Debug.Log($" amigo seleccionado{amigoIdSeleccionado}");
        Debug.Log($"amigo seleccionado{amigoNombreSeleccionado}");
        Debug.Log($" documento seleccionado {documentoSolicitudSeleccionado}");



        if (textoConfirmacion != null)
            textoConfirmacion.text = $"¿Estás seguro que deseas eliminar a {nombreAmigo} de tu lista de amigos?";

        panelConfirmacionEliminar.SetActive(true);

        if (botonConfirmarEliminar!= null)
        {
            botonConfirmarEliminar.onClick.AddListener(() => EliminarAmigoConfirmado(documentoSolicitudSeleccionado,amigoIdSeleccionado));
        }

        // Seleccionar el botón de cancelar por defecto para mejor UX
        EventSystem.current.SetSelectedGameObject(botonCancelarEliminar.gameObject);
    }

    void EliminarAmigoConfirmado(string documentoSolicitudSeleccionado, string amigoIdSeleccionado)
    {

        if (string.IsNullOrEmpty(documentoSolicitudSeleccionado) || string.IsNullOrEmpty(amigoIdSeleccionado))
        {
            ShowMessage("Datos incompletos para eliminar amigo", true);
            return;
        }

        ShowMessage($"Eliminando a {amigoNombreSeleccionado}...");

        // Referencias a los documentos
        DocumentReference solicitudRef = db.Collection("SolicitudesAmistad").Document(documentoSolicitudSeleccionado);

        // Referencias a las subcolecciones de amigos de ambos usuarios
        DocumentReference miAmigoRef = db.Collection("users")
                                        .Document(userId)
                                        .Collection("amigos")
                                        .Document(amigoIdSeleccionado);

        DocumentReference suAmigoRef = db.Collection("users")
                                        .Document(amigoIdSeleccionado)
                                        .Collection("amigos")
                                        .Document(userId);

        // Ejecutar todas las eliminaciones en un batch
        WriteBatch batch = db.StartBatch();

        // 1. Eliminar solicitud de amistad
        batch.Delete(solicitudRef);

        // 2. Eliminar de mi lista de amigos
        batch.Delete(miAmigoRef);

        // 3. Eliminar de la lista de amigos del otro usuario
        batch.Delete(suAmigoRef);

        // Ejecutar el batch
        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            panelConfirmacionEliminar.SetActive(false);

            if (task.IsCompleted && !task.IsFaulted)
            {
                ShowMessage($"{amigoNombreSeleccionado} ha sido eliminado de tu lista de amigos");
                CargarAmigos(inputBuscar.text.Trim());
            }
            else
            {
                ShowMessage("Error al eliminar amigo", true);
                Debug.LogError($"Error al eliminar amigo: {task.Exception}");
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