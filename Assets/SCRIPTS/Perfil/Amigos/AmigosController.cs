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
        Debug.Log("Buscando amigos...");

        // Limpiar la lista de amigos antes de cargar nuevos
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        db.Collection("Solicitudes_Amistad").Document(userId).Collection("Usuarios")
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  Debug.Log($"Consulta completada. Documentos encontrados: {task.Result.Count}");

                  if (task.Result.Count > 0)
                  {
                      foreach (DocumentSnapshot document in task.Result.Documents)
                      {
                          string amigoId = document.Id; // ID del usuario amigo
                          string status = document.ContainsField("status") ? document.GetValue<string>("status") : "Pendiente";

                          Debug.Log($"Amigo encontrado: {amigoId}, Estado: {status}");
                          MostrarAmigo(amigoId, status, filtroNombre);
                      }
                  }
                  else
                  {
                      Debug.Log("No tienes solicitudes de amistad.");
                  }
              }
              else
              {
                  Debug.LogError("Error al obtener las solicitudes de amistad: " + task.Exception);
              }
          });
    }

    void MostrarAmigo(string amigoId, string status, string filtroNombre)
    {
        Debug.Log($"Buscando información del amigo: {amigoId}");

        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    string nombre = data.ContainsKey("DisplayName") ? data["DisplayName"].ToString() : "Sin nombre";
                    string rango = data.ContainsKey("Rank") ? data["Rank"].ToString() : "Sin rango";

                    Debug.Log($"Amigo encontrado en 'users': {nombre}, Rango: {rango}, Estado: {status}");

                    // Si hay un filtro de búsqueda, solo muestra los amigos cuyo nombre coincida
                    if (!string.IsNullOrEmpty(filtroNombre) && !nombre.ToLower().Contains(filtroNombre.ToLower()))
                    {
                        Debug.Log($"Amigo {nombre} no coincide con la búsqueda.");
                        return;
                    }

                    GameObject nuevoAmigo = Instantiate(amigoPrefab, contentPanel);

                    // Asignar nombre y rango
                    nuevoAmigo.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = nombre;
                    nuevoAmigo.transform.Find("Rangotxt").GetComponent<TMP_Text>().text = rango;

                    // Obtener panel de estado y cambiar color
                    GameObject panelEstado = nuevoAmigo.transform.Find("EstadoPanel").gameObject;
                    TMP_Text estadoTxt = nuevoAmigo.transform.Find("Estadotxt").GetComponent<TMP_Text>();

                    if (status == "Aceptada")
                    {
                        panelEstado.GetComponent<Image>().color = new Color32(0x52, 0xD9, 0x99, 0xFF); // Verde personalizado
                        estadoTxt.text = "Amigos";
                    }
                    else if (status == "Pendiente")
                    {
                        panelEstado.GetComponent<Image>().color = new Color32(0x37, 0xBD, 0xF7, 0xFF); // Azul personalizado
                        estadoTxt.text = "Pendiente";
                    }
                }
                else
                {
                    Debug.LogWarning($"No se encontró el documento del amigo en 'users': {amigoId}");
                }
            }
            else
            {
                Debug.LogError($"Error al obtener la información del amigo {amigoId}: {task.Exception}");
            }
        });
    }

    public void ActivarPanelAgregarAmigos()
    {

        m_AgregarAmigosUI.SetActive(true);
        
    }

}
