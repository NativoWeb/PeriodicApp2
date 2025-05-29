﻿//using UnityEngine;
//using TMPro;
//using UnityEngine.UI;
//using Firebase.Auth;
//using Firebase.Firestore;
//using System.Collections.Generic;
//using Firebase.Extensions;
//using System.Linq;
//using System.Threading.Tasks;
//using System;
//using Firebase.Database;
//using System.Collections;
//using UnityEngine.SceneManagement;

//public class QuimicadosModo : MonoBehaviour
//{
//    FirebaseFirestore db;
//    private DatabaseReference realtime;
//    private FirebaseAuth auth;

//    [Header("Paneles")]
//    public GameObject panelSeleccionJuego;
//    public GameObject panelSeleccionModo;
//    public GameObject PanelAmigos;

//    public GameObject amigoPrefab;
//    public Transform contentPanel;

//    public Button Cpu;
//    public Button Amigos;

//    GameObject nuevoAmigo;
//    private string juegoActual;
//    private int amigosCargados = 0;

//    void Start()
//    {
//        db = FirebaseFirestore.DefaultInstance;
//        auth = FirebaseAuth.DefaultInstance;
//        realtime = FirebaseDatabase.DefaultInstance.RootReference;
//    }

//    public void cerrarPanel()
//    {
//        panelSeleccionModo.SetActive(false);
//        PanelAmigos.SetActive(false);
//    }

//    public void SeleccionarJuego()
//    {
//        // Mostrar panel de selección de modo
//        panelSeleccionModo.SetActive(true);

//        Cpu.onClick.AddListener(JugarConCPU);
//        Amigos.onClick.AddListener(JugarConAmigos);
//    }

//    public void JugarConCPU()
//    {
//        PlayerPrefs.SetString("modoJuego", "cpu");
//        SceneManager.LoadScene("Quimicados");
//    }

//    public void JugarConAmigos()
//    {
//        PlayerPrefs.SetString("modoJuego", "online");
//        PanelAmigos.SetActive(true);
//        panelSeleccionModo.SetActive(false);
//        ClearFriendList();

//        CargarAmigos("");
//    }
//    //----------------------------------------------------------Modo Online---------------------------------------------------------------------//
//    void CargarAmigos(string filtroNombre)
//    {
//        amigosCargados = 0;
//        ClearFriendList();

//        if (string.IsNullOrEmpty(auth.CurrentUser.UserId))
//        {
//            return;
//        }

//        HashSet<string> amigosMostrados = new HashSet<string>();

//        // Consulta amigos donde el usuario es remitente
//        db.Collection("SolicitudesAmistad")
//          .WhereEqualTo("idRemitente", auth.CurrentUser.UserId)
//          .WhereIn("estado", new List<object> { "aceptada" })
//          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
//          {
//              if (task.IsCompleted)
//              {
//                  Debug.Log("amigos1");
//                  ProcessFriends(task.Result.Documents, true, filtroNombre, amigosMostrados);
//              }
//              else
//              {
//                  return;
//              }
//          });

//        // Consulta amigos donde el usuario es destinatario
//        db.Collection("SolicitudesAmistad")
//          .WhereEqualTo("idDestinatario", auth.CurrentUser.UserId)
//          .WhereIn("estado", new List<object> { "aceptada" })
//          .GetSnapshotAsync().ContinueWithOnMainThread(task =>
//          {
//              if (task.IsCompleted)
//              {
//                  ProcessFriends(task.Result.Documents, false, filtroNombre, amigosMostrados);
//              }
//          });
//    }
//    void ProcessFriends(IEnumerable<DocumentSnapshot> documents, bool isSender, string filtroNombre, HashSet<string> amigosMostrados)
//    {
//        foreach (DocumentSnapshot document in documents)
//        {
//            string amigoId = isSender ? document.GetValue<string>("idDestinatario") : document.GetValue<string>("idRemitente");
//            string nombreAmigo = isSender ? document.GetValue<string>("nombreDestinatario") : document.GetValue<string>("nombreRemitente");

//            if (!amigosMostrados.Contains(amigoId))
//            {
//                amigosMostrados.Add(amigoId);
//                if (ShouldShowFriend(nombreAmigo, filtroNombre))
//                {
//                    CreateFriendCard(amigoId, nombreAmigo);
//                    amigosCargados++;
//                }
//            }
//        }
//    }
//    bool ShouldShowFriend(string nombreAmigo, string filtroNombre)
//    {
//        return string.IsNullOrEmpty(filtroNombre) ||
//               nombreAmigo.ToLower().Contains(filtroNombre.ToLower());
//    }
//    void CreateFriendCard(string amigoId, string nombreAmigo)
//    {
//        nuevoAmigo = Instantiate(amigoPrefab, contentPanel);
//        nuevoAmigo.transform.Find("TxtNombre").GetComponent<TMP_Text>().text = nombreAmigo;
//        nuevoAmigo.transform.Find("BtnInvitar").GetComponent<Button>().onClick.AddListener(() => InvitarAmigo(amigoId, "Quimicados"));
//        // Cargar rango
//        LoadFriendRank(amigoId, nuevoAmigo);
//    }
//    void LoadFriendRank(string amigoId, GameObject amigoUI)
//    {
//        db.Collection("users").Document(amigoId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
//        {
//            if (task.IsCompleted && task.Result.Exists)
//            {
//                string rango = task.Result.GetValue<string>("Rango") ?? "Novato de laboratorio";
//                var rangoText = amigoUI.transform.Find("TxtRango")?.GetComponent<TMP_Text>();
//                if (rangoText != null) rangoText.text = rango;
//            }
//        });
//    }
//    void ClearFriendList()
//    {
//        foreach (Transform child in contentPanel)
//        {
//            Destroy(child.gameObject);
//        }
//    }
//    private void InvitarAmigo(string amigoUID, string juego)
//    {
//        PanelAmigos.SetActive(false);
//        string miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
//        string partidaId = realtime.Child("PartidasQuimicados").Push().Key;
//        PlayerPrefs.SetString("PartidaId", partidaId);
//        PlayerPrefs.Save();
//        string invitacionId = realtime.Child("QuimicadosInvitaciones").Child(amigoUID).Push().Key; // ID único

//        Dictionary<string, object> datosPartida = new Dictionary<string, object>
//    {
//        { "jugadorA", miUID },
//        { "jugadorB", amigoUID },
//        { "juego", juego },
//        { "estado", "esperando" },
//        { "ronda", 1 } // ✅ Asegúrate de agregar esto
//    };

//        Dictionary<string, object> datosInvitacion = new Dictionary<string, object>
//    {
//        { "partidaId", partidaId },
//        { "from", miUID },
//        { "juego", juego },
//        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
//        { "estado", "pendiente" }
//    };

//        var updates = new Dictionary<string, object>
//        {
//            [$"QuimicadosInvitaciones/{amigoUID}/{invitacionId}"] = datosInvitacion,
//            [$"PartidasQuimicados/{partidaId}"] = datosPartida
//        };

//        realtime.UpdateChildrenAsync(updates).ContinueWith(async task =>
//        {
//            if (task.IsCompleted)
//            {
//                await Task.Delay(5000); // Espera de 5 segundos

//                // Consultamos si sigue pendiente
//                var invitacionSnap = await realtime.Child("QuimicadosInvitaciones").Child(amigoUID).Child(invitacionId).GetValueAsync();

//                if ((invitacionSnap.Exists && invitacionSnap.Child("estado").Value.ToString() == "pendiente")
//                || (invitacionSnap.Exists && invitacionSnap.Child("estado").Value.ToString() == "rechazada"))
//                {
//                    var deleteUpdates = new Dictionary<string, object>
//                    {
//                        [$"QuimicadosInvitaciones/{amigoUID}/{invitacionId}"] = null,
//                        [$"PartidasQuimicados/{partidaId}"] = null
//                    };

//                    await realtime.UpdateChildrenAsync(deleteUpdates);
//                }
//                else
//                {
//                    SceneManager.LoadScene("Quimicados");
//                }
//            }
//        });
//    }
//}
