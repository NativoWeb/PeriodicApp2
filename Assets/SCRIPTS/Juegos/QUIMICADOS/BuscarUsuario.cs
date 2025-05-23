using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using System.Linq;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;


public class BuscarUsuario : MonoBehaviour
{
    [Header("Panel de Error")]
    public GameObject PanelSinInternet;

    [Header("UI References")]
    public TMP_Text messageText;
    public TMP_InputField searchInput;
    public Button searchButton;
    public Button btnJugar;
    public Transform resultsContainer;
    public GameObject userResultPrefab;
    public Texture2D ImgSeleccionada;
    public Button BtnNuevaPartida;
    public Button BtnPartidaActiva;
    public Button BtnContinuar;


    [Header("Search Settings")]
    public float searchDelay = 0.5f;
    public int minSearchChars = 2;

    public CrearPartidaManager crearPartidaManager; // Se lo asignaremos luego
    public ContadorNotificacion notificacionController; // Asignalo desde el inspector

    private string uidSeleccionado = null;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string currentUserId;

    private RawImage ultimoFeedbackActivo = null;

    private string lastSearchText = "";
    private float lastSearchTime;
    private bool searchScheduled = false;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        currentUserId = currentUser?.UserId;
        db = FirebaseFirestore.DefaultInstance;

        StartCoroutine(VerificarConexionPeriodicamente());

        searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        searchButton.onClick.AddListener(() => SearchUser(searchInput.text));

        ShowRandomUsers();
        BtnNuevaPartida.onClick.AddListener(() =>
        {
            BtnNuevaPartida.GetComponent<Image>().color = new Color32(151, 177, 224, 255);
            BtnPartidaActiva.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
            btnJugar.gameObject.SetActive(true);
            BtnContinuar.gameObject.SetActive(false);
            ShowRandomUsers();
        });

        BtnPartidaActiva.onClick.AddListener(() =>
        {
            BtnNuevaPartida.GetComponent<Image>().color = new Color32(255, 255, 255, 255);
            BtnPartidaActiva.GetComponent<Image>().color = new Color32(151, 177, 224, 255);
            BtnContinuar.gameObject.SetActive(true);
            btnJugar.gameObject.SetActive(false);
            notificacionController.OcultarNotificacionYReiniciarContador();
            ShowActiveGames();
        });
    }
    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return VerificarConexionReal();
            yield return new WaitForSeconds(5f);
        }
    }
    private IEnumerator VerificarConexionReal()
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.timeout = 3;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            PanelSinInternet.SetActive(true);
        }
        else
        {
            PanelSinInternet.SetActive(false);
        }
    }
    void Update()
    {
        if (searchScheduled && Time.time >= lastSearchTime + searchDelay)
        {
            searchScheduled = false;
            SearchUser(lastSearchText);
        }
    }

    void OnSearchInputChanged(string input)
    {
        lastSearchText = input;

        if (string.IsNullOrEmpty(input))
        {
            ShowRandomUsers();
            return;
        }

        if (input.Length < minSearchChars)
        {
            ShowMessage($"Escribe al menos {minSearchChars} caracteres para buscar");
            return;
        }

        lastSearchTime = Time.time;
        searchScheduled = true;
        ShowMessage("Escribiendo...");
    }

    void ShowRandomUsers()
    {
        LimpiarResultados();
        ShowMessage("Cargando usuarios...");

        db.Collection("users").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error cargando usuarios: " + task.Exception);
                ShowMessage("Hubo un error al cargar usuarios.");
                return;
            }

            var users = task.Result.Documents.Where(doc => doc.Id != currentUserId).ToList();

            if (users.Count == 0)
            {
                ShowMessage("No hay otros usuarios registrados.");
                return;
            }

            foreach (var doc in users)
                InstanciarUsuarioEnLista(doc);
        });
    }

    void SearchUser(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            ShowRandomUsers();
            return;
        }

        if (username.Length < minSearchChars)
        {
            ShowMessage($"Escribe al menos {minSearchChars} caracteres para buscar");
            return;
        }

        LimpiarResultados();
        ShowMessage("Buscando...");

        db.Collection("users")
            .WhereGreaterThanOrEqualTo("DisplayName", username)
            .WhereLessThanOrEqualTo("DisplayName", username + "\uf8ff")
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (username != lastSearchText) return;

                if (task.IsFaulted || task.IsCanceled)
                {
                    ShowMessage("Hubo un error al buscar.");
                    return;
                }

                var found = false;

                foreach (var doc in task.Result.Documents)
                {
                    if (doc.Id == currentUserId) continue;

                    InstanciarUsuarioEnLista(doc);
                    found = true;
                }

                if (!found) ShowMessage("No se encontraron usuarios.");
            });
    }
    void ShowActiveGames()
    {
        LimpiarResultados();
        ShowMessage("Cargando partidas activas...");

        string miUid = auth.CurrentUser.UserId;
        var partidasRef = db.Collection("partidasQuimicados");

        // Consulta A
        var qA = partidasRef
            .WhereEqualTo("estado", "jugando")
            .WhereEqualTo("jugadorA", miUid)
            .GetSnapshotAsync();

        // Consulta B
        var qB = partidasRef
            .WhereEqualTo("estado", "jugando")
            .WhereEqualTo("jugadorB", miUid)
            .GetSnapshotAsync();

        // Esperar a ambas
        Task.WhenAll(qA, qB).ContinueWithOnMainThread(tasks =>
        {
            if (tasks.IsFaulted)
            {
                Debug.LogError("Error cargando partidas: " + tasks.Exception);
                ShowMessage("Error al cargar partidas.");
                return;
            }

            // resultados de cada query
            var snapA = qA.Result;
            var snapB = qB.Result;

            // combinar documentos (evitamos duplicados por ID)
            var docs = snapA.Documents
                .Concat(snapB.Documents)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();

            if (docs.Count == 0)
            {
                ShowMessage("No tienes partidas activas.");
                return;
            }

            // por cada partida encontrada, instanciar UI
            foreach (var partidaDoc in docs)
                InstanciarPartidaEnLista(partidaDoc);
        });
    }
    void InstanciarPartidaEnLista(DocumentSnapshot partidaDoc)
    {
        // Leer campos de la partida
        string partidaId = partidaDoc.Id;
        string jugadorA = partidaDoc.GetValue<string>("jugadorA");
        string jugadorB = partidaDoc.GetValue<string>("jugadorB");
        string estado = partidaDoc.GetValue<string>("estado");
        int rondaActual = partidaDoc.GetValue<int>("rondaActual");

        string miUid = auth.CurrentUser.UserId;
        // Determinar UID del oponente
        string oponenteUid = miUid == jugadorA ? jugadorB
                            : miUid == jugadorB ? jugadorA
                            : null;

        if (oponenteUid == null)
        {
            Debug.LogWarning("Este usuario no participa en la partida " + partidaId);
            return;
        }

        // Traer datos del oponente
        db.Collection("users")
          .Document(oponenteUid)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("Error al cargar user oponente: " + task.Exception);
                  return;
              }

              var userDoc = task.Result;
              if (!userDoc.Exists)
              {
                  Debug.LogError("No existe el documento de user " + oponenteUid);
                  return;
              }

              string nombreOponente = userDoc.GetValue<string>("DisplayName");
              string rangoOponente = userDoc.GetValue<string>("Rango");

              // Instanciar UI
              GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

              var avatarImg = userEntry.transform.Find("ImgAvatar").GetComponent<Image>();
              var nameText = userEntry.transform.Find("TxtNombre").GetComponent<TMP_Text>();
              var feedbackImg = userEntry.transform.Find("ImgFeedBack").GetComponent<RawImage>();
              var selectButton = userEntry.transform.Find("BtnSeleccionInvisible").GetComponent<Button>();

              nameText.text = nombreOponente;
              Sprite avatar = Resources.Load<Sprite>(ObtenerRutaAvatar(rangoOponente));
              if (avatar != null) avatarImg.sprite = avatar;

              selectButton.onClick.AddListener(() =>
              {
                  if (ultimoFeedbackActivo != null)
                      ultimoFeedbackActivo.gameObject.SetActive(false);

                  feedbackImg.texture = ImgSeleccionada;
                  feedbackImg.gameObject.SetActive(true);
                  ultimoFeedbackActivo = feedbackImg;

                  uidSeleccionado = oponenteUid;
                  PlayerPrefs.SetString("partidaIdQuimicados", partidaId);
                  BtnContinuar.onClick.AddListener(() =>
                  {
                      SceneManager.LoadScene("QuimicadosGame");
                  });
              });
              
          });
    }
    void InstanciarUsuarioEnLista(DocumentSnapshot doc)
    {
        string userId = doc.Id;
        string name = doc.GetValue<string>("DisplayName");
        string rank = doc.GetValue<string>("Rango");

        GameObject userEntry = Instantiate(userResultPrefab, resultsContainer);

        var avatarImg = userEntry.transform.Find("ImgAvatar").GetComponent<Image>();
        var nameText = userEntry.transform.Find("TxtNombre").GetComponent<TMP_Text>();
        var feedbackImg = userEntry.transform.Find("ImgFeedBack").GetComponent<RawImage>();
        var selectButton = userEntry.transform.Find("BtnSeleccionInvisible").GetComponent<Button>();

        nameText.text = name;
        Sprite avatar = Resources.Load<Sprite>(ObtenerRutaAvatar(rank));
        if (avatar != null) avatarImg.sprite = avatar;

        selectButton.onClick.AddListener(() =>
        {
            btnJugar.gameObject.SetActive(true);
            BtnContinuar.gameObject.SetActive(false);
            if (ultimoFeedbackActivo != null)
                ultimoFeedbackActivo.gameObject.SetActive(false);

            feedbackImg.texture = ImgSeleccionada;
            feedbackImg.gameObject.SetActive(true);
            ultimoFeedbackActivo = feedbackImg;

            uidSeleccionado = userId;

            string miUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            // Verificar si ya existe una partida con ese jugador
            db.Collection("partidasQuimicados")
              .WhereIn("jugadorA", new List<string> { miUid, userId })
              .WhereIn("jugadorB", new List<string> { miUid, userId })
              .GetSnapshotAsync()
              .ContinueWithOnMainThread(task =>
              {
                  if (task.IsFaulted)
                  {
                      Debug.LogError("❌ Error buscando partidas existentes: " + task.Exception);
                      return;
                  }

                  var docs = task.Result.Documents;

                  foreach (var doc in docs)
                  {
                      string a = doc.GetValue<string>("jugadorA");
                      string b = doc.GetValue<string>("jugadorB");
                      string estado = doc.GetValue<string>("estado");

                      bool jugadoresCoinciden = (a == miUid && b == userId) || (a == userId && b == miUid);

                      if (jugadoresCoinciden && (estado == "jugando" || estado == "pendiente"))
                      {
                          Debug.Log("⚠️ Ya existe una partida activa con este usuario.");

                          PlayerPrefs.SetString("partidaIdQuimicados", doc.Id);
                          btnJugar.gameObject.SetActive(false);
                          BtnContinuar.gameObject.SetActive(true);
                          BtnContinuar.onClick.AddListener(() =>
                          {
                              // Cargar directamente la escena de partida
                              SceneManager.LoadScene("QuimicadosGame");
                          });
                          return;
                      }
                  }

                  // Si no se encontró ninguna partida existente, proceder a crear una nueva
                  if (crearPartidaManager != null)
                      crearPartidaManager.jugadorSeleccionadoUID = uidSeleccionado;
              });
        });

    }

    void LimpiarResultados()
    {
        foreach (Transform child in resultsContainer)
            Destroy(child.gameObject);
    }

    void ShowMessage(string message)
    {
        messageText.text = message;
    }

    string ObtenerRutaAvatar(string rango)
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
}
