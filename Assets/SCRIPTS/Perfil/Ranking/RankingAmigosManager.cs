using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;
using System.Linq;

public class RankingAmigosManager : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    

    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    // Referencia al botón de Amigos
    public Button btnAmigos;
    [SerializeField] private GameObject RankingAmigosPanel = null;

    // instancia btn general
    public Button btnGeneral;
    public GameObject PanelRankingGeneral;

    private string usuarioActualID;
    private string usuarioActualNombre;
    private int usuarioActualXP;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        usuarioActualID = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        usuarioActualNombre = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.DisplayName;

        // Asignamos el listener al botón de amigos
        if (btnAmigos != null)
        {
            btnAmigos.onClick.AddListener(ActivarRankingAmigos);
        }

        // desactivamos el panel 
        if(btnAmigos == null)
        {
            RankingAmigosPanel.SetActive(false);
        }

        // Obtenemos el XP del usuario actual
        ObtenerXPUsuarioActual();

        if(btnGeneral != null)
        {
            RankingAmigosPanel.SetActive(false);
        }
    }

    private void ObtenerXPUsuarioActual()
    {
        db.Collection("users").Document(usuarioActualID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                if (task.Result.TryGetValue<int>("xp", out int xp))
                {
                    usuarioActualXP = xp;
                }
                else
                {
                    usuarioActualXP = 0;
                }
            }
        });
    }

    public void ActivarRankingAmigos()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            RankingAmigosPanel.SetActive(true);
            ObtenerRankingAmigos();
        }
    }

   

    public void ObtenerRankingAmigos()
    {

        // desactivamos primero el panel de ranking general 
        PanelRankingGeneral.SetActive(false);
        // Limpiar lista anterior
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Resetear textos del podio
        primeroNombre.text = "---";
        primeroXP.text = "0 xp";
        segundoNombre.text = "---";
        segundoXP.text = "0 xp";
        terceroNombre.text = "---";
        terceroXP.text = "0 xp";

        // Primero obtenemos las solicitudes de amistad aceptadas
        db.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idRemitente", usuarioActualID)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    List<string> idsAmigos = new List<string>();

                    // Agregamos los IDs de los destinatarios (amigos)
                    foreach (DocumentSnapshot document in task.Result.Documents)
                    {
                        string idAmigo = document.GetValue<string>("idDestinatario");
                        idsAmigos.Add(idAmigo);
                    }

                    // También buscamos solicitudes donde somos el destinatario
                    ObtenerSolicitudesComoDestinatario(idsAmigos);
                }
            });
    }

    private void ObtenerSolicitudesComoDestinatario(List<string> idsAmigos)
    {
        db.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idDestinatario", usuarioActualID)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    // Agregamos los IDs de los remitentes (también amigos)
                    foreach (DocumentSnapshot document in task.Result.Documents)
                    {
                        string idAmigo = document.GetValue<string>("idRemitente");
                        idsAmigos.Add(idAmigo);
                    }

                    // Una vez tenemos todos los IDs de amigos, obtenemos sus datos
                    ObtenerDatosAmigos(idsAmigos);
                }
            });
    }

    private void ObtenerDatosAmigos(List<string> idsAmigos)
    {
        // Agregamos al usuario actual a la lista para comparación
        List<(string id, string nombre, int xp)> listaJugadores = new List<(string, string, int)>();
        listaJugadores.Add((usuarioActualID, usuarioActualNombre, usuarioActualXP));

        // Si no hay amigos, mostramos solo al usuario
        if (idsAmigos.Count == 0)
        {
            MostrarRankingFinal(listaJugadores);
            return;
        }

        // Contador para saber cuándo hemos procesado a todos los amigos
        int contadorAmigos = 0;

        foreach (string idAmigo in idsAmigos)
        {
            db.Collection("users").Document(idAmigo).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                contadorAmigos++;

                if (task.IsCompleted && task.Result.Exists)
                {
                    string nombre = task.Result.GetValue<string>("DisplayName");
                    int xp = 0;

                    if (task.Result.TryGetValue<int>("xp", out int xpValue))
                    {
                        xp = xpValue;
                    }

                    listaJugadores.Add((idAmigo, nombre, xp));
                }

                // Si ya procesamos a todos los amigos, mostramos el ranking
                if (contadorAmigos >= idsAmigos.Count)
                {
                    MostrarRankingFinal(listaJugadores);
                }
            });
        }
    }

    private void MostrarRankingFinal(List<(string id, string nombre, int xp)> listaJugadores)
    {
        // Ordenar por XP de mayor a menor
        var listaOrdenada = listaJugadores.OrderByDescending(j => j.xp).ToList();

        // Asignar valores al podio
        if (listaOrdenada.Count > 0)
        {
            primeroNombre.text = listaOrdenada[0].nombre;
            primeroXP.text = listaOrdenada[0].xp + " xp";
        }
        if (listaOrdenada.Count > 1)
        {
            segundoNombre.text = listaOrdenada[1].nombre;
            segundoXP.text = listaOrdenada[1].xp + " xp";
        }
        if (listaOrdenada.Count > 2)
        {
            terceroNombre.text = listaOrdenada[2].nombre;
            terceroXP.text = listaOrdenada[2].xp + " xp";
        }

        // Agregar jugadores a la lista desde la posición 4 en adelante
        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            GameObject jugadorUI = CrearElementoRanking(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp);

            // Resaltar al usuario actual
            if (listaOrdenada[i].id == usuarioActualID)
            {
                ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                jugadorUI.GetComponent<Image>().color = customColor;
            }
        }

        // Si el usuario no está entre los primeros 3, buscamos su posición
        int posicionUsuario = listaOrdenada.FindIndex(j => j.id == usuarioActualID) + 1;

        // Si el usuario está entre los primeros 3, resaltamos su posición en el podio
        if (posicionUsuario <= 3 && posicionUsuario > 0)
        {
            
        }
    }

    GameObject CrearElementoRanking(int posicion, string nombre, int xp)
    {
        GameObject jugadorUI = Instantiate(prefabJugador, content);
        TMP_Text nombreTMP = jugadorUI.transform.Find("Nombre").GetComponent<TMP_Text>();
        TMP_Text xpTMP = jugadorUI.transform.Find("XP").GetComponent<TMP_Text>();
        TMP_Text posicionTMP = jugadorUI.transform.Find("Posicion").GetComponent<TMP_Text>();

        nombreTMP.text = nombre;
        xpTMP.text = "EXP \n" + xp;
        posicionTMP.text = "#" + posicion.ToString();

        return jugadorUI;
    }
}