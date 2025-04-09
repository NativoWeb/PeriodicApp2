using Firebase.Firestore; // Importa la librer�a para interactuar con Firebase Firestore
using System.Collections.Generic; // Importa las colecciones gen�ricas 
using UnityEngine; // Importa las funcionalidades de Unity
using TMPro; // Importa TextMesh Pro para trabajar con texto
using Firebase.Extensions; // Importa extensiones espec�ficas de Firebase
using System.Threading.Tasks; // Importa para trabajar con tareas as�ncronas
using UnityEngine.UI; // Importa las funcionalidades de UI (interfaz de usuario) de Unity
using System.Collections; // Importa para trabajar con corutinas (funciones as�ncronas dentro de Unity)

public class RankingController : MonoBehaviour
{
    private FirebaseFirestore db; // Base de datos de Firebase
    private string userId; // ID del usuario actual (almacenado en PlayerPrefs)

    // Referencias a los elementos de la interfaz
    public TMP_Text posicionText; // Texto que muestra la posici�n en el ranking
    public TMP_Text Xptext; // Texto que muestra los puntos de experiencia (XP) del usuario
    public TMP_Text UserName; // Texto que muestra el nombre del usuario
    public Image avatarimage; // Imagen que muestra el avatar del usuario
    public TMP_Text rangotext; // Texto que muestra el rango del usuario

    // internet
    private bool hayInternet = false;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Conecta con la base de datos de Firebase
        userId = PlayerPrefs.GetString("userId", "").Trim(); // Obtiene el ID del usuario guardado en PlayerPrefs

        // Verificar conexi�n a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {
            Debug.Log("Conexion a internet exitosa... Desde rankingcontroller");
            // Si el ID no est� vac�o, obtenemos la posici�n y los datos del usuario
            if (!string.IsNullOrEmpty(userId))
            {
                ObtenerPosicionUsuario(); // Llama a la funci�n para obtener la posici�n del usuario en el ranking
                StartCoroutine(LoadUserData(userId)); // Llama a la funci�n para cargar los datos del usuario (en corutina para esperar la respuesta de Firebase)
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
        UserName.text = "�Hola, " + username + "!";
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
        var task = GetUserData(userId); // Llama a la funci�n GetUserData para obtener los datos del usuario
        yield return new WaitUntil(() => task.IsCompleted); // Espera hasta que la tarea (obtener los datos) est� completada
    }

    // Funci�n que devuelve la ruta del avatar seg�n el rango del usuario
    private string ObtenerAvatarPorRango(string rangos)
    {
        string avatarPath = rangos switch
        {
            "Novato de laboratorio" => "Avatares/nivel1", // Ruta del avatar para el rango "Novato de laboratorio"
            "Arquitecto molecular" => "Avatares/nivel2", // Ruta del avatar para el rango "Arquitecto molecular"
            "Visionario Cu�ntico" => "Avatares/nivel3", // Ruta del avatar para el rango "Visionario Cu�ntico"
            "Amo del caos qu�mico" => "Avatares/nivel4", // Ruta del avatar para el rango "Amo del caos qu�mico"
            _ => "Avatares/defecto" // Si no hay un rango definido, se asigna el avatar por defecto
        };

        Debug.Log($"Ruta de avatar por nivel: {avatarPath}"); // Muestra en consola la ruta del avatar
        return avatarPath; // Devuelve la ruta del avatar correspondiente
    }

    // Funci�n para obtener los datos del usuario desde Firebase
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
            UserName.text = "�Usuario no encontrado!";
            rangotext.text = "Sin rango";
            Xptext.text = "0";
            return; // Sale de la funci�n si el usuario no existe
        }

        Debug.Log($"Usuario encontrado en Firebase: {userId}"); // Muestra el ID del usuario encontrado

        // Obtiene los valores del usuario (nombre, rango, XP) de Firestore, si existen
        string userName = snapshot.ContainsField("DisplayName") ? snapshot.GetValue<string>("DisplayName") : "Sin nombre";
        string rangos = snapshot.ContainsField("Rango") ? snapshot.GetValue<string>("Rango") : "Sin rango";
        int xp = snapshot.ContainsField("xp") ? snapshot.GetValue<int>("xp") : 0;

        // Muestra los datos en la interfaz
        Xptext.text = xp.ToString(); // Muestra los puntos de experiencia
        UserName.text = "�Hola " + userName + "!"; // Muestra el nombre del usuario
        rangotext.text = "�" + rangos + "!"; // Muestra el rango del usuario

        // Obtiene y asigna el avatar correspondiente seg�n el rango
        string avatarPath = ObtenerAvatarPorRango(rangos); // Obtiene la ruta del avatar seg�n el rango
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath); // Carga la imagen del avatar

        // Si encontr� la imagen del avatar, la asigna a la interfaz
        if (avatarSprite != null)
        {
            avatarimage.sprite = avatarSprite;
        }
        else
        {
            // Si no se encuentra el avatar, muestra un avatar por defecto
            Debug.LogError($"No se encontr� el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
            avatarimage.sprite = Resources.Load<Sprite>("Avatares/default");
        }
    }

    // Funci�n para obtener la posici�n del usuario en el ranking
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
            posicionText.text = "Posici�n: No disponible"; // Muestra mensaje indicando que no hay usuarios
            return; // Sale de la funci�n si no hay usuarios
        }

        int posicion = 1; // Comienza desde la posici�n 1 en el ranking
        bool encontrado = false; // Variable para indicar si se encuentra al usuario

        // Recorre todos los usuarios del ranking
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Si el ID del documento coincide con el ID del usuario actual
            if (doc.Id == userId)
            {
                encontrado = true; // Marca que se encontr� al usuario-
                posicionText.text = "Posici�n: #" + posicion; // Muestra la posici�n en el ranking
                PlayerPrefs.SetInt("posicion", posicion); // guardo posici�n para mostrarla offline --------------------------------
                Debug.Log($"El usuario {userId} est� en la posici�n {posicion} del ranking.");
                break; // Sale del ciclo ya que se encontr� al usuario
            }
            posicion++; // Incrementa la posici�n para el siguiente usuario
        }

        // Si no se encontr� al usuario
        if (!encontrado)
        {
            Debug.LogError("No se encontr� al usuario en el ranking.");
            posicionText.text = "Posici�n: No encontrada"; // Muestra un mensaje de error
        }


    }
}
