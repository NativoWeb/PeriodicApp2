using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class PerfilManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string userId;

    [Header("UI References")]
    public TMP_Text posicionText;
    public TMP_Text Xptext;
    public TMP_Text UserName;
    public Image avatarimage;
    public TMP_Text rangotext;

    [Header("Panel References")]
    [SerializeField] public GameObject m_logoutUI = null;

    private bool hayInternet = false;

    // MODIFICADO: Variables de localización
    private string appIdioma;
    private Dictionary<string, string> localizedTexts = new Dictionary<string, string>();

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        // MODIFICADO: Inicializar idioma y textos
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InitializeLocalizedTexts();

        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                ObtenerPosicionUsuario();
                StartCoroutine(LoadUserData(userId));
            }
            else
            {
                Debug.Log(localizedTexts["offlineData"]);
                MostrarDatosOffline();
            }
        }
        else
        {
            Debug.Log(localizedTexts["offlineData"]);
            MostrarDatosOffline();
        }
    }

    // MODIFICADO: Nuevo método para centralizar las traducciones
    void InitializeLocalizedTexts()
    {
        if (appIdioma == "ingles")
        {
            localizedTexts["greeting"] = "Hello, {0}!";
            localizedTexts["position"] = "Rank: #{0}";
            localizedTexts["positionUnavailable"] = "Position: N/A";
            localizedTexts["positionNotFound"] = "Position: Not Found";
            localizedTexts["noRank"] = "No Rank";
            localizedTexts["noXp"] = "No XP";
            localizedTexts["userNotFound"] = "User not found!";
            localizedTexts["defaultRank"] = "Lab Newbie";
            localizedTexts["offlineData"] = "No internet connection, showing offline data.";
            localizedTexts["logoutPanelError"] = "Logout panel is not assigned.";
            localizedTexts["logoutSuccess"] = "Logged out successfully.";
            localizedTexts["noUserToUpload"] = "No authenticated user to upload data.";
            localizedTexts["noDataToUpload"] = "No mission or category data to upload.";
            localizedTexts["uploadSuccess"] = "Mission and category data uploaded.";
        }
        else // Español por defecto
        {
            localizedTexts["greeting"] = "¡Hola, {0}!";
            localizedTexts["position"] = "Posición: #{0}";
            localizedTexts["positionUnavailable"] = "Posición: No disponible";
            localizedTexts["positionNotFound"] = "Posición: No encontrada";
            localizedTexts["noRank"] = "Sin rango";
            localizedTexts["noXp"] = "Sin XP";
            localizedTexts["userNotFound"] = "¡Usuario no encontrado!";
            localizedTexts["defaultRank"] = "Novato de laboratorio";
            localizedTexts["offlineData"] = "Sin conexión a internet, mostrando datos offline.";
            localizedTexts["logoutPanelError"] = "El panel de logout no está asignado.";
            localizedTexts["logoutSuccess"] = "✅ Sesión cerrada correctamente.";
            localizedTexts["noUserToUpload"] = "❌ No hay usuario autenticado.";
            localizedTexts["noDataToUpload"] = "⚠️ No hay datos de misiones ni categorías para subir.";
            localizedTexts["uploadSuccess"] = "✅ Datos de misiones y categorías subidos.";
        }
    }

    private void MostrarDatosOffline()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        string rangos = PlayerPrefs.GetString("Rango", localizedTexts["defaultRank"]);
        int xp = PlayerPrefs.GetInt("TempXP", 0);
        int posicion = PlayerPrefs.GetInt("posicion", 0);

        UserName.text = string.Format(localizedTexts["greeting"], username);
        posicionText.text = string.Format(localizedTexts["position"], posicion);
        Xptext.text = xp.ToString();
        rangotext.text = rangos;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");
        avatarimage.sprite = avatarSprite;
    }

    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId);
        yield return new WaitUntil(() => task.IsCompleted);
    }

    private string ObtenerAvatarPorRango(string rangos)
    {
        // Esta lógica depende de los nombres en español de la DB, no se traduce.
        switch (rangos)
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

    async Task GetUserData(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("Usuario no encontrado en la base de datos.");
            UserName.text = localizedTexts["userNotFound"];
            rangotext.text = localizedTexts["noRank"];
            Xptext.text = localizedTexts["noXp"];
            return;
        }

        string userName = snapshot.GetValue<string>("DisplayName") ?? "Sin nombre";
        string rangos = snapshot.GetValue<string>("Rango") ?? localizedTexts["defaultRank"];
        int xp = snapshot.GetValue<int>("xp");

        Xptext.text = xp.ToString();
        UserName.text = string.Format(localizedTexts["greeting"], userName);
        rangotext.text = rangos;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/Rango1");
        avatarimage.sprite = avatarSprite;
    }

    public async void ObtenerPosicionUsuario()
    {
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            Debug.LogWarning("No hay usuarios en la base de datos.");
            posicionText.text = localizedTexts["positionUnavailable"];
            return;
        }

        int posicion = 1;
        bool encontrado = false;

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Id == userId)
            {
                encontrado = true;
                posicionText.text = $"# {posicion}";
                PlayerPrefs.SetInt("posicion", posicion);
                break;
            }
            posicion++;
        }

        if (!encontrado)
        {
            Debug.LogError("No se encontró al usuario en el ranking.");
            posicionText.text = localizedTexts["positionNotFound"];
        }
    }

    public async void Logout()
    {
        await SubirDatosJSON();
        auth.SignOut();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log(localizedTexts["logoutSuccess"]);
        SceneManager.LoadScene("Start");
    }

    public async Task SubirDatosJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError(localizedTexts["noUserToUpload"]);
            return;
        }

        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON", "{}");
        string jsonCategorias = PlayerPrefs.GetString("CategoriasOrdenadas", "{}");

        List<Task> tareasSubida = new List<Task>();

        if (jsonMisiones != "{}")
        {
            DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
            {
                { "misiones", jsonMisiones },
                { "timestamp", FieldValue.ServerTimestamp }
            };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (jsonCategorias != "{}")
        {
            DocumentReference categoriasDoc = db.Collection("users").Document(userId).Collection("datos").Document("categorias");
            Dictionary<string, object> dataCategorias = new Dictionary<string, object>
            {
                { "categorias", jsonCategorias },
                { "timestamp", FieldValue.ServerTimestamp }
            };
            tareasSubida.Add(categoriasDoc.SetAsync(dataCategorias, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning(localizedTexts["noDataToUpload"]);
            return;
        }

        await Task.WhenAll(tareasSubida);
        Debug.Log(localizedTexts["uploadSuccess"]);
    }

    public void showlogout()
    {
        if (m_logoutUI != null)
            m_logoutUI.SetActive(true);
        else
            Debug.LogError(localizedTexts["logoutPanelError"]);
    }

    public void quitarlogout()
    {
        if (m_logoutUI != null)
            m_logoutUI.SetActive(false);
    }

    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            SceneManager.LoadScene("Ranking1");
        }
    }
}