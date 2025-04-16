using UnityEngine;
using Firebase.Extensions;
using Firebase.Database;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class InvitacionManager : MonoBehaviour
{
    public static InvitacionManager instancia; 
    
    public GameObject panelInvitacionGO; // Asigna desde el Inspector
    private PanelInvitacionController panelInvitacion;

    FirebaseFirestore db;
    private DatabaseReference realtime;
    private string miUID;
    string invitacionIdSeleccionada;
    string remitente; 
    private HashSet<string> invitacionesProcesadas = new HashSet<string>();


    private bool debeCambiarEscena = false;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        realtime = FirebaseDatabase.DefaultInstance.RootReference;
        //miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        miUID = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

        if (string.IsNullOrEmpty(miUID))
        {
            Debug.LogError("❌ FirebaseAuth aún no tiene un usuario válido.");
        }


        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            Debug.LogWarning("Usuario no autenticado aún.");
            return;
        }

        EscucharInvitaciones();
    }
    void EscucharInvitaciones()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("invitaciones")
            .Child(miUID)
            .ValueChanged += OnInvitacionRecibida;
    }

    void OnInvitacionRecibida(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error al escuchar invitaciones: " + args.DatabaseError.Message);
            return;
        }

        if (!args.Snapshot.Exists) return;

        // args.Snapshot = nodo de todas las invitaciones de miUID
        foreach (var invitacion in args.Snapshot.Children)
        {
            string invitacionId = invitacion.Key;

            string estado = invitacion.Child("estado").Value.ToString();
            string from = invitacion.Child("from").Value.ToString();
            string juego = invitacion.Child("juego").Value.ToString();
            string partidaId = invitacion.Child("partidaId").Value.ToString();

            db.Collection("users").Document(from).GetSnapshotAsync().ContinueWith(task => {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string nombre1 = task.Result.GetValue<string>("DisplayName");
                    remitente = nombre1;
                }
                else
                {
                    Debug.LogWarning("⚠️ No se encontró el usuario en Firestore.");
                }
            });

            string receptor = args.Snapshot.Key; // debería seguir siendo miUID

            if (receptor != miUID)
            {
                Debug.LogWarning("🔒 Invitación no destinada a este usuario.");
                return;
            }

            if (estado == "pendiente")
            {
                if (remitente != null)
                {
                    MostrarPanelInvitacion(remitente, juego, partidaId, invitacionId);
                }
                else
                {
                    return;
                }
                break; // Para que solo se procese una vez
            }
        }
    }

    public void MostrarPanelInvitacion(string from, string juego, string partidaId, string invitacionId)
    {
        invitacionIdSeleccionada = invitacionId;

        if (panelInvitacionGO != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            panelInvitacionGO = Instantiate(panelInvitacionGO, canvas.transform);
            panelInvitacion = panelInvitacionGO.GetComponent<PanelInvitacionController>();
        }
        
        panelInvitacion.Mostrar(from, juego, partidaId);
    }
    public void AceptarInvitacion()
    {
        if (string.IsNullOrEmpty(invitacionIdSeleccionada))
        {
            Debug.LogError("❌ No se ha seleccionado ninguna invitación.");
            return;
        }

        realtime.Child("invitaciones").Child(miUID).Child(invitacionIdSeleccionada)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("⚠️ Error al obtener la invitación: " + task.Exception);
                    return;
                }

                if (task.IsCompleted && task.Result.Exists)
                {
                    var snap = task.Result;

                    if (!snap.HasChild("partidaId"))
                    {
                        Debug.LogError("❌ La invitación no tiene 'partidaId'.");
                        return;
                    }

                    string partidaId = snap.Child("partidaId").Value.ToString();
                    Debug.Log("🎯 Partida encontrada: " + partidaId);

                    // Guardar partidaId y cambiar escena
                    PlayerPrefs.SetString("PartidaId", partidaId);
                    PlayerPrefs.Save();

                    // Cambiar el estado de la invitación
                    realtime.Child("invitaciones").Child(miUID).Child(invitacionIdSeleccionada).Child("estado")
                    .SetValueAsync("aceptado").ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            Debug.Log("✅ Estado de invitación actualizado. Cargando escena...");
                            SceneManager.LoadScene("CombateQuimico");
                        }
                        else
                        {
                            Debug.LogError("❌ No se pudo actualizar el estado de la invitación.");
                        }
                    });
                }
                else
                {
                    Debug.LogError("❌ No se encontró la invitación.");
                }
            });
    }

    public void RechazarInvitacion()
    {
        realtime.Child("invitaciones").Child(miUID).Child("estado").SetValueAsync("rechazada");
    }
}
