using UnityEngine;
using Firebase.Extensions;
using Firebase.Database;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class InvitacionManager : MonoBehaviour
{
    public static InvitacionManager instancia;

    public GameObject panelInvitacionGO; // Asignar prefab desde el Inspector
    private PanelInvitacionController panelInvitacion;

    private FirebaseFirestore db;
    private DatabaseReference realtime;
    private string miUID;
    private string invitacionIdSeleccionada;
    private string juegoSeleccionado;
    private string rutaSeleccionada;

    private HashSet<string> invitacionesProcesadas = new HashSet<string>();

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
        miUID = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

        EscucharInvitacionesCombate();
        EscucharInvitacionesQuimicados();
    }

    void EscucharInvitacionesCombate()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("invitaciones")
            .Child(miUID)
            .ChildAdded += OnInvitacionCombateRecibida;
    }

    void EscucharInvitacionesQuimicados()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("QuimicadosInvitaciones")
            .Child(miUID)
            .ChildAdded += OnInvitacionQuimicadosRecibida;
    }

    void OnInvitacionCombateRecibida(object sender, ChildChangedEventArgs args)
    {
        ProcesarInvitacion(args, "CombateQuimico", "invitaciones");
    }

    void OnInvitacionQuimicadosRecibida(object sender, ChildChangedEventArgs args)
    {
        ProcesarInvitacion(args, "Quimicados", "QuimicadosInvitaciones");
    }

    void ProcesarInvitacion(ChildChangedEventArgs args, string escenaDestino, string ruta)
    {
        if (args.DatabaseError != null || !args.Snapshot.Exists) return;

        string invitacionId = args.Snapshot.Key;
        string estado = args.Snapshot.Child("estado").Value?.ToString();
        string from = args.Snapshot.Child("from").Value?.ToString();
        string juego = args.Snapshot.Child("juego").Value?.ToString();
        string partidaId = args.Snapshot.Child("partidaId").Value?.ToString();

        if (string.IsNullOrEmpty(from) || estado != "pendiente") return;
        if (invitacionesProcesadas.Contains(invitacionId)) return;

        db.Collection("users").Document(from).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string remitente = task.Result.GetValue<string>("DisplayName");
                invitacionesProcesadas.Add(invitacionId);
                MostrarPanelInvitacion(remitente, juego, partidaId, invitacionId, escenaDestino, ruta);
            }
        });
    }

    void MostrarPanelInvitacion(string remitente, string juego, string partidaId, string invitacionId, string escenaDestino, string ruta)
    {
        invitacionIdSeleccionada = invitacionId;
        juegoSeleccionado = juego;
        rutaSeleccionada = ruta;

        if (panelInvitacionGO != null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            var instanciaGO = Instantiate(panelInvitacionGO, canvas.transform);
            panelInvitacion = instanciaGO.GetComponent<PanelInvitacionController>();
            panelInvitacion.Mostrar(remitente, juego, partidaId);
        }
    }

    public void AceptarInvitacion()
    {
        if (string.IsNullOrEmpty(invitacionIdSeleccionada)) return;

        string ruta = rutaSeleccionada;
        string escena = (ruta == "invitaciones") ? "CombateQuimico" : "Quimicados";

        realtime.Child(ruta).Child(miUID).Child(invitacionIdSeleccionada)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists) return;

                string partidaId = task.Result.Child("partidaId").Value?.ToString();
                if (string.IsNullOrEmpty(partidaId)) return;

                PlayerPrefs.SetString("PartidaId", partidaId);
                PlayerPrefs.SetString("modoJuego", "online");
                PlayerPrefs.Save();

                realtime.Child(ruta).Child(miUID).Child(invitacionIdSeleccionada).Child("estado")
                    .SetValueAsync("aceptado").ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsCompleted)
                        {
                            Debug.Log("✅ Estado actualizado. Cargando escena...");
                            SceneManager.LoadScene(escena);
                        }
                        else
                        {
                            Debug.LogError("❌ Error al actualizar el estado.");
                        }
                    });
            });
    }

    public void RechazarInvitacion()
    {
        if (string.IsNullOrEmpty(invitacionIdSeleccionada)) return;

        realtime.Child(rutaSeleccionada).Child(miUID).Child(invitacionIdSeleccionada).Child("estado")
            .SetValueAsync("rechazada");
    }
}
