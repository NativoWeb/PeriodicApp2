using Firebase.Firestore; // Importa la librería para interactuar con Firebase Firestore
using System.Collections.Generic; // Importa las colecciones genéricas 
using UnityEngine; // Importa las funcionalidades de Unity
using TMPro; // Importa TextMesh Pro para trabajar con texto
using Firebase.Extensions; // Importa extensiones específicas de Firebase
using System.Threading.Tasks; // Importa para trabajar con tareas asíncronas
using UnityEngine.UI; // Importa las funcionalidades de UI (interfaz de usuario) de Unity
using System.Collections; // Importa para trabajar con corutinas (funciones asíncronas dentro de Unity)
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Unity;
public class PerfilManager : MonoBehaviour
{
    private FirebaseFirestore db; // Base de datos de Firebase
    private FirebaseAuth auth;
    private string userId; // ID del usuario actual (almacenado en PlayerPrefs)

    // Referencias a los elementos de la interfaz
    public TMP_Text posicionText; // Texto que muestra la posición en el ranking
    public TMP_Text Xptext; // Texto que muestra los puntos de experiencia (XP) del usuario
    public TMP_Text UserName; // Texto que muestra el nombre del usuario
    public Image avatarimage; // Imagen que muestra el avatar del usuario
    public TMP_Text rangotext; // Texto que muestra el rango del usuario

  

    // instanciamos panel 
    [SerializeField] public GameObject m_logoutUI = null;
    // internet
    private bool hayInternet = false;


    void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Conecta con la base de datos de Firebase
        auth = FirebaseAuth.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim(); // Obtiene el ID del usuario guardado en PlayerPrefs

        // Verificar conexión a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {
            Debug.Log("Conexion a internet exitosa... Desde rankingcontroller");
            // Si el ID no está vacío, obtenemos la posición y los datos del usuario
            if (!string.IsNullOrEmpty(userId))
            {
                ObtenerPosicionUsuario(); // Llama a la función para obtener la posición del usuario en el ranking
                StartCoroutine(LoadUserData(userId)); // Llama a la función para cargar los datos del usuario (en corutina para esperar la respuesta de Firebase)
            }
            else
            {
                Debug.Log("Usuario no autenticado, mostrando datos offline");
                MostrarDatosOffline();
            }

        }
        else
        {
            Debug.Log("Sin conexion a internet, mostrando datos offline");
            MostrarDatosOffline();
        }
    }
    private void MostrarDatosOffline()
    {
        string username = PlayerPrefs.GetString("DisplayName", "");
        string rangos = PlayerPrefs.GetString("Rango", "Novato de laboratorio");
        int xp = PlayerPrefs.GetInt("TempXP", 0);
        int posicion = PlayerPrefs.GetInt("posicion", 0);
       

        // mostrar datos del usuario en la interfaz 
        UserName.text = "¡Hola, " + username + "!";
        posicionText.text = $" # {posicion}";
        Xptext.text = xp.ToString();
        rangotext.text = rangos;

        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

        avatarimage.sprite = avatarSprite;
    }
    // Corutina que espera a que se carguen los datos del usuario
    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId); // Llama a la función GetUserData para obtener los datos del usuario
        yield return new WaitUntil(() => task.IsCompleted); // Espera hasta que la tarea (obtener los datos) esté completada
    }

    // Función que devuelve la ruta del avatar según el rango del usuario
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

    // Función para obtener los datos del usuario desde Firebase
    async Task GetUserData(string userId)
    {
        // Obtiene la referencia al documento del usuario desde Firestore usando su ID
        DocumentReference docRef = db.Collection("users").Document(userId);
        // Intenta obtener los datos del usuario
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        // Verifica si el usuario existe en la base de datos
        if (!snapshot.Exists)
        {
            // Si el usuario no existe, muestra un mensaje de error y asigna valores predeterminados
            Debug.LogError("Usuario no encontrado en la base de datos.");
            UserName.text = "¡Usuario no encontrado!";
            rangotext.text = "Sin rango";
            Xptext.text = "0";
            return; // Sale de la función si el usuario no existe
        }

        Debug.Log($"Usuario encontrado en Firebase: {userId}"); // Muestra el ID del usuario encontrado

        // Obtiene los valores del usuario (nombre, rango, XP) de Firestore, si existen
        string userName = snapshot.ContainsField("DisplayName") ? snapshot.GetValue<string>("DisplayName") : "Sin nombre";
        string rangos = snapshot.ContainsField("Rango") ? snapshot.GetValue<string>("Rango") : "Sin rango";
        int xp = snapshot.ContainsField("xp") ? snapshot.GetValue<int>("xp") : 0;

        // Muestra los datos en la interfaz
        Xptext.text = xp.ToString(); // Muestra los puntos de experiencia
        UserName.text = "¡Hola " + userName + "!"; // Muestra el nombre del usuario
        rangotext.text = "¡" + rangos + "!"; // Muestra el rango del usuario

        // Obtiene y asigna el avatar correspondiente según el rango
        string avatarPath = ObtenerAvatarPorRango(rangos); // Obtiene la ruta del avatar según el rango
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath); // Carga la imagen del avatar

        // Si encontró la imagen del avatar, la asigna a la interfaz
        if (avatarSprite != null)
        {
            avatarimage.sprite = avatarSprite;
        }
        else
        {
            // Si no se encuentra el avatar, muestra un avatar por defecto
            Debug.LogError($"No se encontró el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
            avatarimage.sprite = Resources.Load<Sprite>("Avatares/Rango1");
        }
    }

    // Función para obtener la posición del usuario en el ranking
    async void ObtenerPosicionUsuario()
    {
        // Realiza una consulta para obtener los usuarios ordenados por XP en orden descendente (de mayor a menor)
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        // Ejecuta la consulta y obtiene los datos
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        // Si no hay usuarios en la base de datos
        if (snapshot.Count == 0)
        {
            Debug.LogWarning("No hay usuarios registrados en la base de datos.");
            posicionText.text = "Posición: No disponible"; // Muestra mensaje indicando que no hay usuarios
            return; // Sale de la función si no hay usuarios
        }

        int posicion = 1; // Comienza desde la posición 1 en el ranking
        bool encontrado = false; // Variable para indicar si se encuentra al usuario

        // Recorre todos los usuarios del ranking
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Si el ID del documento coincide con el ID del usuario actual
            if (doc.Id == userId)
            {
                encontrado = true; // Marca que se encontró al usuario-
                posicionText.text = "Posición: #" + posicion; // Muestra la posición en el ranking
                PlayerPrefs.SetInt("posicion", posicion); // guardo posición para mostrarla offline --------------------------------
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                break; // Sale del ciclo ya que se encontró al usuario
            }
            posicion++; // Incrementa la posición para el siguiente usuario
        }

        // Si no se encontró al usuario
        if (!encontrado)
        {
            Debug.LogError("No se encontró al usuario en el ranking.");
            posicionText.text = "Posición: No encontrada"; // Muestra un mensaje de error
        }
    }
    public async void Logout()
    {
        await SubirDatosJSON(); // Guardar el JSON antes de cerrar sesión

        auth.SignOut();
        PlayerPrefs.DeleteAll();

        //PlayerPrefs.SetString("Estadouser", estadouser);
        PlayerPrefs.Save();

        Debug.Log("✅ Sesión cerrada correctamente.");
        SceneManager.LoadScene("Start");
    }
    public async Task SubirDatosJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        // Obtener JSON de misiones y categorías desde PlayerPrefs
        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON", "{}");
        string jsonCategorias = PlayerPrefs.GetString("CategoriasOrdenadas", "{}");

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
        DocumentReference categoriasDoc = db.Collection("users").Document(userId).Collection("datos").Document("categorias");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (jsonMisiones != "{}")
        {
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (jsonCategorias != "{}")
        {
            Dictionary<string, object> dataCategorias = new Dictionary<string, object>
        {
            { "categorias", jsonCategorias },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(categoriasDoc.SetAsync(dataCategorias, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay datos de misiones ni categorías para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);

        Debug.Log("✅ Datos de misiones y categorías subidos en documentos separados.");
    }



    public void showlogout()
    {
        if (m_logoutUI != null)
        {
            m_logoutUI.SetActive(true);
        }
        else
        {
            Debug.LogError("El panel de logout no está asignado.");
        }
    }

    public void quitarlogout()
    {
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
