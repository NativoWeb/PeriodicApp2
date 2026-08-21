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

        // ✅ OPTIMIZACIÓN: Mostrar datos guardados INMEDIATAMENTE para carga rápida
        MostrarDatosGuardados();

        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                // ✅ Actualizar datos en segundo plano sin bloquear la UI
                StartCoroutine(LoadUserData(userId));
                ObtenerPosicionUsuario(); // Se ejecuta en paralelo, actualiza cuando termine
            }
            else
            {
                Debug.Log(localizedTexts["offlineData"]);
            }
        }
        else
        {
            Debug.Log(localizedTexts["offlineData"]);
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

    // ✅ OPTIMIZACIÓN: Mostrar datos guardados inmediatamente para carga rápida
    private void MostrarDatosGuardados()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");

        // ✅ Leer XP guardado (no TempXP) - TempXP es solo temporal
        int xpGuardado = PlayerPrefs.GetInt("xp", 0);
        int posicion = PlayerPrefs.GetInt("posicion", 0);

        Debug.Log($"⚡ [PERFIL CARGA RÁPIDA] Mostrando datos guardados - XP: {xpGuardado}, Posición: {posicion}");

        // ✅ Calcular el rango correcto según el XP
        string rangos = ObtenerRangoSegunXP(xpGuardado);

        // Mostrar datos inmediatamente
        UserName.text = string.Format(localizedTexts["greeting"], username);
        posicionText.text = posicion > 0 ? string.Format(localizedTexts["position"], posicion) : localizedTexts["positionUnavailable"];
        Xptext.text = xpGuardado.ToString();
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
            case "Alquimista Supremo": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }

    // ✅ Calcular rango según XP (copiado de ControllerPerfil.cs)
    private string ObtenerRangoSegunXP(int xp)
    {
        if (xp >= 25000) return "Alquimista Supremo";
        if (xp >= 10000) return "Leyenda química";
        if (xp >= 6000) return "Sabio de la tabla";
        if (xp >= 3500) return "Maestro de Laboratorio";
        if (xp >= 2300) return "Experto Molecular";
        if (xp >= 1200) return "Cientifico en Formacion";
        if (xp >= 600) return "Promesa quimica";
        if (xp >= 200) return "Aprendiz Atomico";
        return "Novato de laboratorio";
    }

    // ✅ Actualizar rango en Firebase
    private async Task ActualizarRangoEnFirebase(string userId, string nuevoRango)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("Rango", nuevoRango);
        Debug.Log($"✅ Rango actualizado en Firebase: {nuevoRango}");
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

        Debug.Log($"🔍 [PERFIL] XP Firebase: {xp}, Rango guardado: {rangos}");

        // ✅ ACTUALIZAR EL RANGO SEGÚN EL XP ACTUAL (igual que en ControllerPerfil)
        string nuevoRango = ObtenerRangoSegunXP(xp);
        Debug.Log($"🔍 [PERFIL] Rango calculado según XP: {nuevoRango}");
        if (nuevoRango != rangos)
        {
            await ActualizarRangoEnFirebase(userId, nuevoRango);
            rangos = nuevoRango;
            Debug.Log($"✅ Rango actualizado en Perfil: {rangos}");
        }

        // ✅ Actualizar UI con datos de Firebase
        Xptext.text = xp.ToString();
        UserName.text = string.Format(localizedTexts["greeting"], userName);
        rangotext.text = rangos;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/Rango1");
        avatarimage.sprite = avatarSprite;

        // Guardar en PlayerPrefs
        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.SetInt("xp", xp);
        PlayerPrefs.SetString("Rango", rangos);
        PlayerPrefs.SetString("Avatar", avatarPath);
        PlayerPrefs.Save();

        Debug.Log($"✅ [PERFIL] Datos actualizados desde Firebase");
    }

    public async void ObtenerPosicionUsuario()
    {
        Debug.Log($"🔄 [PERFIL] Obteniendo posición en ranking...");

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
                PlayerPrefs.Save();
                Debug.Log($"✅ [PERFIL] Posición actualizada: #{posicion}");
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