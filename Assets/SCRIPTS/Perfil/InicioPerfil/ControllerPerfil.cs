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
        Debug.Log("ControllerPerfil ejecutándose...");

        try
        {
            await FirebaseApp.CheckAndFixDependenciesAsync();
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ControllerPerfil] Error inicializando Firebase: {e.Message}");
            MostrarDatosOffline();
            return;
        }

        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            currentUser = auth.CurrentUser;
            userId = PlayerPrefs.GetString("userId", "").Trim();

            if (currentUser == null || string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[ControllerPerfil] No hay usuario autenticado. Redirigiendo a Start.");
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SceneManager.LoadScene("Start");
                return;
            }

            Debug.Log("[ControllerPerfil] Usuario autenticado: " + userId);
            EscucharCambiosUsuario(userId);
        }
        else
        {
            Debug.LogWarning("[ControllerPerfil] Sin conexión. Cargando datos offline.");
            MostrarDatosOffline();
            ImprimirDatosPlayerPrefs();
        }
    }
  
    private void MostrarDatosOffline()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        int xp = PlayerPrefs.GetInt("xp", 0);

        // ✅ Calcular el rango correcto según el XP (igual que en PerfilManager)
        string rangoCalculado = ObtenerRangoSegunXP(xp);
        rangoActual = rangoCalculado;

        Debug.Log($"🔍 [INICIO OFFLINE] XP guardado: {xp}, Rango calculado: {rangoCalculado}");

        // mostrar datos del usuario en la interfaz
        tmpUsername.text = "¡Hola, " + username + "!";


        string avatarPath = ObtenerAvatarPorRango(rangoCalculado);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

        avatarImage.sprite = avatarSprite;

        // Guardar el rango actualizado
        PlayerPrefs.SetString("Rango", rangoCalculado);
        PlayerPrefs.Save();
    }

    private void EscucharCambiosUsuario(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        docRef.Listen(snapshot =>
        {
            if (snapshot.Exists)
            {
                string username = snapshot.GetValue<string>("DisplayName");
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

                // ✅ CALCULAR EL RANGO SEGÚN EL XP ACTUAL (igual que en PerfilManager)
                string rangoCalculado = ObtenerRangoSegunXP(xpFirebase);
                rangoActual = rangoCalculado;

                Debug.Log($"🔍 [INICIO] XP Firebase: {xpFirebase}, XP Temp: {xpTemp}, Rango guardado: {rangos}, Rango calculado: {rangoCalculado}");

                // ✅ Actualizar rango en Firebase solo si cambió
                if (rangoCalculado != rangos)
                {
                    ActualizarRangoSegunXP(xpFirebase);
                    Debug.Log($"✅ Rango actualizado en Inicio: {rangoCalculado}");
                }

                // ✅ Cargar avatar usando el rango CALCULADO (no el guardado en Firebase)
                string avatarPath = ObtenerAvatarPorRango(rangoCalculado);
                Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/Rango1");

                avatarImage.sprite = avatarSprite;// información que se muestra del usuario en la interfaz ----------------------------------------------------


                // Guardar en PlayerPrefs
                PlayerPrefs.SetString("DisplayName", username);
                PlayerPrefs.SetString("Rango", rangoCalculado); // ✅ Guardar el rango calculado
                PlayerPrefs.SetInt("xp", xpFirebase);
                PlayerPrefs.SetString("Avatar", avatarPath);
                PlayerPrefs.Save();

                tmpUsername.text = "¡Hola, " + username + "!"; // información que se muestra del usuario en la interfaz ----------------------------------------------------
              

                LimpiarMisiones();
                CargarMisiones(); // Recargar misiones según rango
            }
            else
            {
                Debug.LogWarning("[ControllerPerfil] El documento del usuario no existe en Firestore. Redirigiendo a Start.");
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SceneManager.LoadScene("Start");
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
        if (xp >= 10000) return "Leyenda química";
        if (xp >= 6000) return "Sabio de la tabla";
        if (xp >= 3500) return "Maestro de Laboratorio";
        if (xp >= 2300) return "Experto Molecular";
        if (xp >= 1200) return "Cientifico en Formacion";
        if (xp >= 600) return "Promesa quimica";
        if (xp >= 200) return "Aprendiz Atomico";
        return "Novato de laboratorio";
    }

    // ✅ Avatar según rango
    private string ObtenerAvatarPorRango(string rangos)
    {
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
    public void ImprimirDatosPlayerPrefs()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        string ocupacion = PlayerPrefs.GetString("TempOcupacion", "");
        string rango = PlayerPrefs.GetString("Rango", "");
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


