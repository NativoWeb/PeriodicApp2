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
    public GameObject panelAbajo;
    public TMP_Text messageText;
    public TMP_InputField searchInput;
    public Button searchButton;

    public Transform ContentPartidaNueva;
    public GameObject userResultPrefab;
    public GameObject userPartidaNueva;
    public GameObject scrollActivos;
    public GameObject scrollNuevos;



    public Texture2D ImgSeleccionada;
    public Button BtnNuevaPartida;
    public Button BtnPartidaActiva;
    [SerializeField] private Transform contentMiTurno;
    [SerializeField] private Transform contentTurnoOponente;



    [Header("Search Settings")]
    public float searchDelay = 0.5f;
    public int minSearchChars = 2;

    public CrearPartidaManager crearPartidaManager; // Se lo asignaremos luego

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

        PlayerPrefs.SetInt("AbrirConPanel", 1);

        ShowPartidasMiTurno();
        scrollNuevos.SetActive(false);
        ShowPartidasTurnoOponente();
        BtnNuevaPartida.onClick.AddListener(() =>
        {
            BtnNuevaPartida.GetComponent<Image>().color = new Color32(81, 178, 124, 255);
            BtnPartidaActiva.GetComponent<Image>().color = new Color32(255, 251, 239, 255);

            BtnNuevaPartida.GetComponentInChildren<TMP_Text>().color = new Color32(255, 255, 255, 255);
            BtnPartidaActiva.GetComponentInChildren<TMP_Text>().color = new Color32(59, 53, 139, 255);

            panelAbajo.SetActive(false);
            searchInput.gameObject.SetActive(true);
            scrollNuevos.SetActive(true);
            scrollActivos.SetActive(false);
            ShowRandomUsers();
        });

        BtnPartidaActiva.onClick.AddListener(() =>
        {
            BtnNuevaPartida.GetComponent<Image>().color = new Color32(255, 251, 239, 255);
            BtnPartidaActiva.GetComponent<Image>().color = new Color32(81, 178, 124, 255);

            BtnNuevaPartida.GetComponentInChildren<TMP_Text>().color = new Color32(59, 53, 139, 255);
            BtnPartidaActiva.GetComponentInChildren<TMP_Text>().color = new Color32(255, 255, 255, 255);

            panelAbajo.SetActive(true);
            searchInput.gameObject.SetActive(false);
            scrollNuevos.SetActive(false);
            scrollActivos.SetActive(true);
            ShowPartidasMiTurno();
            ShowPartidasTurnoOponente();
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
        LimpiarResultadosNuevos();
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

        LimpiarResultadosNuevos();
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
    void ShowPartidasMiTurno()
    {
        LimpiarResultadosActivos();
        ShowMessage("Cargando partidas en tu turno...");

        string miUid = auth.CurrentUser.UserId;
        var partidasRef = db.Collection("partidasQuimicados");

        var qA = partidasRef.WhereEqualTo("estado", "jugando").WhereEqualTo("jugadorA", miUid).GetSnapshotAsync();
        var qB = partidasRef.WhereEqualTo("estado", "jugando").WhereEqualTo("jugadorB", miUid).GetSnapshotAsync();

        Task.WhenAll(qA, qB).ContinueWithOnMainThread(tasks =>
        {
            if (tasks.IsFaulted)
            {
                Debug.LogError("Error cargando partidas: " + tasks.Exception);
                ShowMessage("Error al cargar partidas.");
                return;
            }

            var docs = qA.Result.Documents
                .Concat(qB.Result.Documents)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .Where(d => d.GetValue<string>("turnoActual") == miUid) // 👈 Solo si es mi turno
                .ToList();

            if (docs.Count == 0)
            {
                ShowMessage("No tienes partidas donde sea tu turno.");
                return;
            }

            foreach (var partidaDoc in docs)
            {
                InstanciarPartidaEnLista(partidaDoc, contentMiTurno);
            }
        });
    }
    void ShowPartidasTurnoOponente()
    {
        LimpiarResultadosActivos();
        ShowMessage("Cargando partidas en turno del oponente...");

        string miUid = auth.CurrentUser.UserId;
        var partidasRef = db.Collection("partidasQuimicados");

        var qA = partidasRef.WhereEqualTo("estado", "jugando").WhereEqualTo("jugadorA", miUid).GetSnapshotAsync();
        var qB = partidasRef.WhereEqualTo("estado", "jugando").WhereEqualTo("jugadorB", miUid).GetSnapshotAsync();

        Task.WhenAll(qA, qB).ContinueWithOnMainThread(tasks =>
        {
            if (tasks.IsFaulted)
            {
                Debug.LogError("Error cargando partidas: " + tasks.Exception);
                ShowMessage("Error al cargar partidas.");
                return;
            }

            var docs = qA.Result.Documents
                .Concat(qB.Result.Documents)
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .Where(d => d.GetValue<string>("turnoActual") != miUid) // 👈 Solo si NO es mi turno
                .ToList();

            if (docs.Count == 0)
            {
                ShowMessage("No hay partidas esperando al oponente.");
                return;
            }

            foreach (var partidaDoc in docs)
            {
                InstanciarPartidaEnLista(partidaDoc, contentTurnoOponente);
            }
        });
    }

    void InstanciarPartidaEnLista(DocumentSnapshot partidaDoc, Transform contenedor)
    {
        string partidaId = partidaDoc.Id;
        string jugadorA = partidaDoc.GetValue<string>("jugadorA");
        string jugadorB = partidaDoc.GetValue<string>("jugadorB");
        string estado = partidaDoc.GetValue<string>("estado");
        int rondaActual = partidaDoc.GetValue<int>("rondaActual");

        string miUid = auth.CurrentUser.UserId;
        string oponenteUid = miUid == jugadorA ? jugadorB : jugadorA;
        string keyCoronasMi = miUid == jugadorA ? "CategoriasJugadorA" : "CategoriasJugadorB";
        string keyCoronasOponente = miUid == jugadorA ? "CategoriasJugadorB" : "CategoriasJugadorA";

        // Leer diccionarios de coronas
        Dictionary<string, object> coronasMi = partidaDoc.ContainsField(keyCoronasMi) ? partidaDoc.GetValue<Dictionary<string, object>>(keyCoronasMi) : new();
        Dictionary<string, object> coronasOp = partidaDoc.ContainsField(keyCoronasOponente) ? partidaDoc.GetValue<Dictionary<string, object>>(keyCoronasOponente) : new();

        int contadorMi = coronasMi.Count(kv => kv.Value is bool b && b);
        int contadorOp = coronasOp.Count(kv => kv.Value is bool b && b);

        db.Collection("users").Document(oponenteUid).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("Error cargando datos del oponente");
                return;
            }

            var userDoc = task.Result;
            string nombreOponente = userDoc.GetValue<string>("DisplayName");
            string rangoOponente = userDoc.GetValue<string>("Rango");

            GameObject userEntry = Instantiate(userResultPrefab, contenedor);

            var avatarImg = userEntry.transform.Find("ImgAvatar").GetComponent<Image>();
            var nameText = userEntry.transform.Find("TxtNombre").GetComponent<TMP_Text>();
            var marcadorText = userEntry.GetComponentsInChildren<TMP_Text>()
            .FirstOrDefault(t => t.name == "TxtMarcador");
            var selectButton = userEntry.transform.Find("BtnSeleccionInvisible").GetComponent<Button>();

            nameText.text = nombreOponente;
            marcadorText.text = $"{contadorMi} - {contadorOp}";

            Sprite avatar = Resources.Load<Sprite>(ObtenerRutaAvatar(rangoOponente));
            if (avatar != null) avatarImg.sprite = avatar;

            selectButton.onClick.AddListener(() =>
            {
                if (ultimoFeedbackActivo != null)
                    ultimoFeedbackActivo.gameObject.SetActive(false);

                uidSeleccionado = oponenteUid;
                PlayerPrefs.SetString("partidaIdQuimicados", partidaId);
                SceneManager.LoadScene("QuimicadosGame");
            });
        });
    }



    void InstanciarUsuarioEnLista(DocumentSnapshot doc)
    {
        string userId = doc.Id;
        string name = doc.GetValue<string>("DisplayName");
        string rank = doc.GetValue<string>("Rango");

        GameObject userEntry = Instantiate(userPartidaNueva, ContentPartidaNueva);

        var avatarImg = userEntry.transform.Find("ImgAvatar").GetComponent<Image>();
        var nameText = userEntry.transform.Find("TxtNombre").GetComponent<TMP_Text>();
        var feedbackImg = userEntry.transform.Find("ImgFeedBack").GetComponent<RawImage>();
        var selectButton = userEntry.transform.Find("BtnSeleccionInvisible").GetComponent<Button>();

        nameText.text = name;
        Sprite avatar = Resources.Load<Sprite>(ObtenerRutaAvatar(rank));
        if (avatar != null) avatarImg.sprite = avatar;

        selectButton.onClick.AddListener(() =>
        {
            if (ultimoFeedbackActivo != null)
                ultimoFeedbackActivo.gameObject.SetActive(false);

            feedbackImg.texture = ImgSeleccionada;
            feedbackImg.gameObject.SetActive(true);
            ultimoFeedbackActivo = feedbackImg;

            uidSeleccionado = userId;

            string miUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            PlayerPrefs.SetString("partidaIdQuimicados", doc.Id);

            if (crearPartidaManager != null)
            {
                crearPartidaManager.jugadorSeleccionadoUID = uidSeleccionado;
                crearPartidaManager.CrearPartida();
            }
        });

    }

    void LimpiarResultadosNuevos()
    {
        foreach (Transform child in ContentPartidaNueva)
            Destroy(child.gameObject);
    }
    void LimpiarResultadosActivos()
    {
        foreach (Transform child in contentMiTurno)
            Destroy(child.gameObject);
        foreach (Transform child in contentTurnoOponente)
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
