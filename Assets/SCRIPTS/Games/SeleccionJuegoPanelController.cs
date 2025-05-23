using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using Firebase.Extensions;
using System.Linq;
using System.Threading.Tasks;
using System;
using Firebase.Database;
using System.Collections;
using UnityEngine.SceneManagement;
using Google.Protobuf.WellKnownTypes;
using UnityEngine.Networking;

public class SeleccionJuegoPanelController : MonoBehaviour
{
    FirebaseFirestore db;
    private DatabaseReference realtime;


    private DatabaseReference presenciaJugadorRef;

    private FirebaseAuth auth;

    [Header("Paneles")]
    public GameObject panelSeleccionJuego;
    public GameObject panelSeleccionModo;
    public GameObject PanelAmigos;

    public GameObject amigoPrefab;
    public Transform contentPanel;

    public Button btnAmigos;

    GameObject nuevoAmigo;
    private string juegoActual;
    private int amigosCargados = 0;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        realtime = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(VerificarConexionPeriodicamente());
    }

    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return VerificarConexionReal();
            yield return new WaitForSeconds(2f);
        }
    }
    private IEnumerator VerificarConexionReal()
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.timeout = 3;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            btnAmigos.interactable = false;
        }
        else
        {
            btnAmigos.interactable = true;
        }
    }
    public void cerrarPanel()
    {
        panelSeleccionModo.SetActive(false);
        PanelAmigos.SetActive(false);
    }

    public void SeleccionarJuego()
    {
        // Mostrar panel de selección de modo
        panelSeleccionModo.SetActive(true);
    }

    public void JugarConCPU()
    {
        PlayerPrefs.SetString("modoJuego", "cpu");
        SceneManager.LoadScene("CombateQuimico");
    }

    public void JugarConAmigos()
    {
        PlayerPrefs.SetString("modoJuego", "online");
        PanelAmigos.SetActive(true);
        panelSeleccionModo.SetActive(false);

        CargarAmigos("");
    }
    //----------------------------------------------------------Modo Online---------------------------------------------------------------------//
    void CargarAmigos(string filtroNombre)
    {
        amigosCargados = 0;
        ClearFriendList();

        if (string.IsNullOrEmpty(auth.CurrentUser.UserId))
        {
            return;
        }

        HashSet<string> amigosMostrados = new HashSet<string>();

        // Consulta amigos donde el usuario es remitente
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idRemitente", auth.CurrentUser.UserId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  Debug.Log("amigos1");
                  ProcessFriends(task.Result.Documents, true, filtroNombre, amigosMostrados);
              }
              else
              {
                  return;
              }
          });

        // Consulta amigos donde el usuario es destinatario
        db.Collection("SolicitudesAmistad")
          .WhereEqualTo("idDestinatario", auth.CurrentUser.UserId)
          .WhereIn("estado", new List<object> { "aceptada" })
          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  ProcessFriends(task.Result.Documents, false, filtroNombre, amigosMostrados);
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
        nuevoAmigo = Instantiate(amigoPrefab, contentPanel);
        nuevoAmigo.transform.Find("TxtNombre").GetComponent<TMP_Text>().text = nombreAmigo;
        nuevoAmigo.transform.Find("BtnInvitar").GetComponent<Button>().onClick.AddListener(() => InvitarAmigo(amigoId, "Combate Quimico"));
        // Cargar rango
        LoadFriendRank(amigoId, nuevoAmigo);
    }
    void LoadFriendRank(string amigoId, GameObject amigoUI)
    {
        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                string rango = task.Result.GetValue<string>("Rango") ?? "Novato de laboratorio";
                var rangoText = amigoUI.transform.Find("TxtRango")?.GetComponent<TMP_Text>();
                if (rangoText != null) rangoText.text = rango;
            }
        });
    }
    void ClearFriendList()
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }
    private void InvitarAmigo(string amigoUID, string juego)
    {
        PanelAmigos.SetActive(false);
        string miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        string partidaId = realtime.Child("partidas").Push().Key;
        PlayerPrefs.SetString("PartidaId", partidaId);
        PlayerPrefs.Save();
        string invitacionId = realtime.Child("invitaciones").Child(amigoUID).Push().Key; // ID único

        Dictionary<string, object> datosPartida = new Dictionary<string, object>
    {
        { "jugadorA", miUID },
        { "jugadorB", amigoUID },
        { "juego", juego },
        { "estado", "esperando" },
        { "vidaA", 100 },
        { "vidaB", 100 },
        { "ronda", 1 } // ✅ Asegúrate de agregar esto
    };


        Dictionary<string, object> datosInvitacion = new Dictionary<string, object>
    {
        { "partidaId", partidaId },
        { "from", miUID },
        { "juego", juego },
        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
        { "estado", "pendiente" }
    };

        var updates = new Dictionary<string, object>
        {
            [$"invitaciones/{amigoUID}/{invitacionId}"] = datosInvitacion,
            [$"partidas/{partidaId}"] = datosPartida
        };

        DatabaseReference estadoRef = realtime
        .Child("invitaciones")
        .Child(amigoUID)
        .Child(invitacionId)
        .Child("estado");

        bool aceptado = false;

        // ESCUCHA EN TIEMPO REAL
        estadoRef.ValueChanged += (sender, args) =>
        {
            if (args.Snapshot.Exists && args.Snapshot.Value.ToString() == "aceptado")
            {
                aceptado = true;
                Debug.Log("✅ Invitación aceptada, entrando a la partida...");
                SceneManager.LoadScene("CombateQuimico");
            }
        };

        // ACTUALIZAMOS LOS DATOS Y ESPERAMOS 5 SEGUNDOS COMO BACKUP
        realtime.UpdateChildrenAsync(updates).ContinueWith(async task =>
        {
            if (task.IsCompleted)
            {
                RegistrarPresencia();

                await Task.Delay(5000);

                // Si luego de 5 seg NO fue aceptado, revisamos su estado actual y decidimos
                if (!aceptado)
                {
                    var invitacionSnap = await realtime
                        .Child("invitaciones")
                        .Child(amigoUID)
                        .Child(invitacionId)
                        .GetValueAsync();

                    if ((invitacionSnap.Exists && invitacionSnap.Child("estado").Value.ToString() == "pendiente")
                    || (invitacionSnap.Exists && invitacionSnap.Child("estado").Value.ToString() == "rechazada"))
                    {
                        var deleteUpdates = new Dictionary<string, object>
                        {
                            [$"invitaciones/{amigoUID}/{invitacionId}"] = null,
                            [$"partidas/{partidaId}"] = null
                        };

                        await realtime.UpdateChildrenAsync(deleteUpdates);
                    }
                }
            }
        });

    }

    void RegistrarPresencia()
    {
        string partidaId = PlayerPrefs.GetString("PartidaId");

        presenciaJugadorRef = FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .Child("presencia")
            .Child(auth.CurrentUser.UserId);

        Dictionary<string, object> datosPresencia = new Dictionary<string, object>
        {
            { "conectado", true },
            { "timestamp", ServerValue.Timestamp }
        };

        presenciaJugadorRef.SetValueAsync(datosPresencia);
        presenciaJugadorRef.OnDisconnect().RemoveValue();
    }

}
