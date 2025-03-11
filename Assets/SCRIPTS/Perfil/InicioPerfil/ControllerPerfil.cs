using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class ControllerPerfil : MonoBehaviour
{
    // Conexión a la base de datos Firestore
    private FirebaseFirestore db;

    // Variables de la interfaz de usuario
    public TMP_Text tmpUsername;
    public TMP_Text tmpCorreo;
    public Image avatarImage;

    // Variables para las misiones
    public Transform content; // Contenedor del scroll de misiones
    public GameObject buttonPrefab; // Prefab del botón de misión
    public string userId;
    private string rangoActual; // Rango del usuario

    void Start()
    {
        Debug.Log("ControllerPerfil Start ejecutándose...");
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();
        Debug.Log("UserID en PlayerPrefs: " + userId);

        if (!string.IsNullOrEmpty(userId))
        {
            ObtenerDatosUsuario(userId);
        }
        else
        {
            tmpUsername.text = "Usuario: No encontrado";
            tmpCorreo.text = "Correo: No encontrado";
        }

        CargarMisioness();
        ActualizarDatosUsuario();
    }

    // Función para actualizar los datos del usuario
    public void ActualizarDatosUsuario()
    {
        StartCoroutine(ActualizarDatosUsuarioCoroutine());
    }

    private IEnumerator ActualizarDatosUsuarioCoroutine()
    {
        ObtenerDatosUsuario(userId);
        yield return null; // Permite hacerlo asíncrono
    }

    // Devuelve la ruta del avatar según el rango del usuario
    private string ObtenerAvatarPorRango(string rangos)
    {
        string avatarPath = "Avatares/defecto"; // Avatar por defecto

        if (rangos == "Novato de laboratorio")
            avatarPath = "Avatares/nivel1";
        else if (rangos == "Arquitecto molecular")
            avatarPath = "Avatares/nivel2";
        else if (rangos == "Visionario Cuántico")
            avatarPath = "Avatares/nivel3";
        else if (rangos == "Amo del caos químico")
            avatarPath = "Avatares/nivel4";

        Debug.Log($"Ruta de avatar por nivel: {avatarPath}");
        return avatarPath;
    }

    // Obtiene los datos del usuario desde Firestore y actualiza la interfaz
    async void ObtenerDatosUsuario(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log("Documento encontrado en Firestore");

            string username = snapshot.GetValue<string>("DisplayName");
            string correo = snapshot.GetValue<string>("Email");
            string rangos = snapshot.GetValue<string>("Rango");
            rangoActual = rangos; // Guardamos el rango globalmente

            // Cargar y asignar avatar
            string avatarPath = ObtenerAvatarPorRango(rangos);
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/default");
            avatarImage.sprite = avatarSprite;

            // Actualizar textos de usuario
            tmpUsername.text = "¡Hola, " + username + "!";
            tmpCorreo.text = "Correo: " + correo;

            // Recargar las misiones con el nuevo rango
            LimpiarMisiones();
            CargarMisioness();
        }
        else
        {
            Debug.LogError("El documento del usuario no existe en Firestore.");
            tmpUsername.text = "Usuario: No disponible";
            tmpCorreo.text = "Correo: No disponible";
        }
    }
    // Función para actualizar el rango y avatar
    public async void ActualizarRangoSegunXP(int xp)
    {
        string nuevoRango = ObtenerRangoSegunXP(xp); // Obtenemos el rango según el XP
        DocumentReference userRef = db.Collection("users").Document(userId);

        // Actualizamos el rango del usuario
        await userRef.UpdateAsync("Rango", nuevoRango);
        rangoActual = nuevoRango;

        // Actualizamos el avatar y las misiones
        string avatarPath = ObtenerAvatarPorRango(nuevoRango);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/default");
        avatarImage.sprite = avatarSprite;

        LimpiarMisiones();
        CargarMisioness();
    }

    private string ObtenerRangoSegunXP(int xp)
    {
        if (xp >= 3000) return "Amo del caos químico";
        if (xp >= 2000) return "Visionario Cuántico";
        if (xp >= 1000) return "Arquitecto molecular";
        return "Novato de laboratorio";
    }


    // Carga las misiones filtradas por rango del usuario
    public async void CargarMisioness()
    {
        string rangoUsuario = rangoActual;

        Query misionesQuery = db.Collection("misiones").WhereEqualTo("rangoRequerido", rangoUsuario);
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

                // Crear botón de misión
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Asignar textos al botón
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asignar acción al botón
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));
                }

                // Cargar progreso de la misión
                await CargarProgreso(userId, misionID, barraProgreso);
            }
        }
    }

    // Carga el progreso de una misión específica
    async Task CargarProgreso(string userId, string missionId, Slider barraProgreso)
    {
        DocumentReference docRef = db.Collection("progreso_misiones").Document(userId).Collection("misiones").Document(missionId);
        DocumentSnapshot docSnap = await docRef.GetSnapshotAsync();

        if (docSnap.Exists)
        {
            int progreso = docSnap.GetValue<int>("progreso");
            barraProgreso.value = progreso / 100f; // Valor entre 0 y 1
            Debug.Log($"✅ Progreso de {missionId}: {progreso}%");
        }
        else
        {
            barraProgreso.value = 0f;
            Debug.Log($"⚠️ No se encontró progreso para {missionId}, iniciando en 0%");
        }
    }

    // Limpia los botones de misiones anteriores
    private void LimpiarMisiones()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    // Cambia de escena según la ruta proporcionada
    void CambiarEscena(string rutaEscena)
    {
        Debug.Log("Intentando cargar la escena: " + rutaEscena);
        if (Application.CanStreamedLevelBeLoaded(rutaEscena))
        {
            Debug.Log("Cambiando a la escena: " + rutaEscena);
            SceneManager.LoadScene(rutaEscena);
        }
        else
        {
            Debug.LogError("❌ ERROR: La escena '" + rutaEscena + "' no está en Build Settings o tiene un nombre incorrecto.");
        }
    }
}
