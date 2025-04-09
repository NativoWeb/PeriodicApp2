using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Security.Permissions;

public class AmigosController : MonoBehaviour
{
    public GameObject amigoPrefab; // Prefab del amigo
    public Transform contentPanel; // Panel dentro del Scroll View
    public TMP_InputField inputBuscar; // Campo de texto para buscar amigos
    public Button botonBuscar; // Botón de búsqueda

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;


    // instanciamos panel agregar amigos 
    [SerializeField] public GameObject m_AgregarAmigosUI = null;
    [SerializeField] public GameObject m_SolicitudesUI = null;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            userId = auth.CurrentUser.UserId; // Obtener ID del usuario autenticado
            Debug.Log($"Usuario autenticado: {userId}");
            CargarAmigos("");

            // Agregar listener al botón de búsqueda
            botonBuscar.onClick.AddListener(() => {
                string nombreBuscar = inputBuscar.text.Trim();
                Debug.Log($"Buscando amigos con el nombre: {nombreBuscar}");
                CargarAmigos(nombreBuscar);
            });
        }
        else
        {
            Debug.LogError("No hay usuario autenticado.");
        }
    }

    void CargarAmigos(string filtroNombre)
    {
        Debug.Log("Cargando amigos y solicitudes pendientes...");

        // Limpiar la lista de amigos antes de cargar nuevos
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("El userId es nulo o vacío.");
            return;
        }

        // Consultar solicitudes aceptadas y pendientes donde el usuario es remitente o destinatario
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", userId)
          .WhereIn("estado", new List<object> { "aceptada", "pendiente" }) // Buscar aceptadas y pendientes
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  foreach (DocumentSnapshot document in task.Result.Documents)
                  {
                      string amigoId = document.GetValue<string>("idDestinatario");
                      string nombreAmigo = document.GetValue<string>("nombreDestinatario");
                      string status = document.GetValue<string>("estado");

                      MostrarAmigo(amigoId, nombreAmigo, status, filtroNombre);
                  }
              }
              else
              {
                  Debug.LogError("Error al obtener amigos remitentes: " + task.Exception);
              }
          });

        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", userId)
          .WhereIn("estado", new List<object> { "aceptada", "pendiente" }) // Buscar aceptadas y pendientes
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  foreach (DocumentSnapshot document in task.Result.Documents)
                  {
                      string amigoId = document.GetValue<string>("idRemitente");
                      string nombreAmigo = document.GetValue<string>("nombreRemitente");
                      string status = document.GetValue<string>("estado");

                      MostrarAmigo(amigoId, nombreAmigo, status, filtroNombre);
                  }
              }
              else
              {
                  Debug.LogError("Error al obtener amigos destinatarios: " + task.Exception);
              }
          });
    }


    void MostrarAmigo(string amigoId, string nombreAmigo, string status, string filtroNombre)
    {
        Debug.Log($"Mostrando amigo/solicitud: {nombreAmigo} ({status})");

        // Si hay un filtro y el nombre no coincide, lo omitimos
        if (!string.IsNullOrEmpty(filtroNombre) && !nombreAmigo.ToLower().Contains(filtroNombre.ToLower()))
        {
            Debug.Log($"El amigo {nombreAmigo} no coincide con la búsqueda.");
            return;
        }

        GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);

        // Asignar el nombre
        nuevoAmigo.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = nombreAmigo;

        // Obtener el panel de estado y cambiar color según estado
        GameObject panelEstado = nuevoAmigo.transform.Find("EstadoPanel").gameObject;
        TMP_Text estadoTxt = nuevoAmigo.transform.Find("Estadotxt").GetComponent<TMP_Text>();

        if (status == "aceptada")
        {
            panelEstado.GetComponent<Image>().color = new Color32(0x52, 0xD9, 0x99, 0xFF); // Verde personalizado
            estadoTxt.text = "Amigos";
        }
        else if (status == "pendiente")
        {
            panelEstado.GetComponent<Image>().color = new Color32(0x37, 0xBD, 0xF7, 0xFF); // Azul personalizado
            estadoTxt.text = "Pendiente";
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
