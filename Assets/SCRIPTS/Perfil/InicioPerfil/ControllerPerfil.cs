using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControllerPerfil : MonoBehaviour
{
    // Firebase
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    // Interfaz
    public TMP_Text tmpUsername;
    public TMP_Text tmpCorreo;
    public Image avatarImage;
    public Button GameButton;

    // Misiones
    public Transform content;
    public GameObject buttonPrefab;
    private string userId;
    private string rangoActual;

    // Internet
    private bool hayInternet = false;

    async void Start()
    {

        Debug.Log("ControllerPerfil Start ejecutándose...");

        // Inicializar Firebase
        await FirebaseApp.CheckAndFixDependenciesAsync();
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Verificar conexión a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            Debug.Log("✅ Conexión a internet detectada");
            currentUser = auth.CurrentUser;
            userId = PlayerPrefs.GetString("userId", "").Trim();

            if (!string.IsNullOrEmpty(userId))
            {
                Debug.Log("✅ Usuario autenticado: " + userId);
                EscucharCambiosUsuario(userId);
            }
            else
            {
                Debug.LogError("❌ No hay usuario autenticado");
                MostrarDatosOffline();
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No hay conexión a internet. Cargando datos offline.");
            MostrarDatosOffline();
            ImprimirDatosPlayerPrefs();
        }

        
    }

  
    private void MostrarDatosOffline()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        string correo = PlayerPrefs.GetString("Email", "Correo no disponible");
        string rangos = PlayerPrefs.GetString("TempRango", "");
        int xp = PlayerPrefs.GetInt("xp", 0);
        rangoActual = rangos;

        tmpUsername.text = "¡Hola, " + username + "!";
        tmpCorreo.text = "Correo: " + correo;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");
        avatarImage.sprite = avatarSprite;
    }

    private void EscucharCambiosUsuario(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                string username = snapshot.GetValue<string>("DisplayName");
                string correo = snapshot.GetValue<string>("Email");
                string rangos = snapshot.GetValue<string>("Rango");
                int xpFirebase = snapshot.GetValue<int>("xp");

                int xpTemp = PlayerPrefs.GetInt("TempXP", 0); // XP guardado localmente

                // ✅ Comparar XP de Firebase con TempXP y actualizar si es necesario
                if (xpTemp > 0) // Solo actualizamos si TempXP es mayor a 0
                {
                    int nuevoXP = xpFirebase + xpTemp; // Sumar TempXP al XP de Firebase
                    ActualizarXPEnFirebase(userId, nuevoXP);
                    PlayerPrefs.SetInt("TempXP", 0); // Resetear TempXP después de la actualización
                    PlayerPrefs.Save();
                    Debug.Log($"🔄 XP actualizado en Firebase: {xpFirebase} ➡ {nuevoXP}");
                }

                rangoActual = rangos;
                ActualizarRangoSegunXP(xpFirebase);

                // Guardar en PlayerPrefs
                PlayerPrefs.SetString("DisplayName", username);
                PlayerPrefs.SetString("Email", correo);
                PlayerPrefs.SetString("Rango", rangos);
                PlayerPrefs.SetInt("xp", xpFirebase);
                PlayerPrefs.Save();

                // Cargar avatar y datos
                string avatarPath = ObtenerAvatarPorRango(rangos);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/default");
                avatarImage.sprite = avatarSprite;

                tmpUsername.text = "¡Hola, " + username + "!";
                tmpCorreo.text = "Correo: " + correo;

                LimpiarMisiones();
                CargarMisiones(); // Recargar misiones según rango
            }
            else
            {
                Debug.LogError("❌ El documento no existe");
                MostrarDatosOffline();
            }
        });
    }
    private async void ActualizarXPEnFirebase(string userId, int nuevoXP)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("xp", nuevoXP);
        Debug.Log($"✅ XP actualizado en Firebase para el usuario {userId}: {nuevoXP}");
    }
    public async void CargarMisiones()
    {
        Debug.Log($"Cargando misiones para el rango: {rangoActual}");
        Query misionesQuery = db.Collection("misiones").WhereEqualTo("rangoRequerido", rangoActual);
        QuerySnapshot snapshot = await misionesQuery.GetSnapshotAsync();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (document.Exists)
            {
                string titulo = document.GetValue<string>("titulo");
                string descripcion = document.GetValue<string>("descripcion");
                int xp = document.GetValue<int>("xp");
                string rutaEscena = document.GetValue<string>("rutaEscena");
                string misionID = document.Id;

                GameObject newButton = Instantiate(buttonPrefab, content);
                TextMeshProUGUI[] texts = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                texts[0].text = titulo;
                texts[1].text = descripcion;
                texts[2].text = $"XP: {xp}";

                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));

                await CargarProgreso(userId, misionID, barraProgreso);
            }
        }
    }

    async Task CargarProgreso(string userId, string missionId, Slider barraProgreso)
    {
        DocumentReference docRef = db.Collection("progreso_misiones").Document(userId).Collection("misiones").Document(missionId);
        DocumentSnapshot docSnap = await docRef.GetSnapshotAsync();

        if (docSnap.Exists)
        {
            int progreso = docSnap.GetValue<int>("progreso");
            barraProgreso.value = progreso / 100f;
        }
        else
        {
            barraProgreso.value = 0f;
        }
    }

    public async void ActualizarRangoSegunXP(int xp)
    {
        string nuevoRango = ObtenerRangoSegunXP(xp);
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("Rango", nuevoRango);
        rangoActual = nuevoRango;
    }

    private void LimpiarMisiones()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    void CambiarEscena(string rutaEscena)
    {
        if (Application.CanStreamedLevelBeLoaded(rutaEscena))
            SceneManager.LoadScene(rutaEscena);
    }
    // ✅ Rango según XP
    private string ObtenerRangoSegunXP(int xp)
    {
        if (xp >= 3000) return "Amo del caos químico";
        if (xp >= 2000) return "Visionario Cuántico";
        if (xp >= 1000) return "Arquitecto molecular";
        return "Novato de laboratorio";
    }

    // ✅ Avatar según rango
    private string ObtenerAvatarPorRango(string rangos)
    {
        switch (rangos)
        {
            case "Novato de laboratorio": return "Avatares/nivel1";
            case "Arquitecto molecular": return "Avatares/nivel2";
            case "Visionario Cuántico": return "Avatares/nivel3";
            case "Amo del caos químico": return "Avatares/nivel4";
            default: return "Avatares/default";
        }
    }
    public void ImprimirDatosPlayerPrefs()
    {
        string username = PlayerPrefs.GetString("TempUsername", "");
        string ocupacion = PlayerPrefs.GetString("TempOcupacion", "");
        string rango = PlayerPrefs.GetString("TempRango", "");
        int encuestaCompletada = PlayerPrefs.GetInt("TempEncuestaCompletada", 0);
        int xp = PlayerPrefs.GetInt("TempXP", 0);

        Debug.Log("========== DATOS GUARDADOS EN PLAYERPREFS ==========");
        Debug.Log("Nombre de usuario: " + username);
        Debug.Log("Ocupación: " + ocupacion);
        Debug.Log("Rango: " + rango);
        Debug.Log("XP: " + xp);
        Debug.Log("estado encuesta: " + encuestaCompletada);
        Debug.Log("====================================================");
    }

}


