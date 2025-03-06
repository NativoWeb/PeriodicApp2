using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;  // Importante para Imagenes
using UnityEngine.SceneManagement;
public class ControllerPerfil : MonoBehaviour
{
    //instanciar la conexion a la bd
    private FirebaseFirestore db;

    //variables de UI unity
    public TMP_Text tmpUsername;
    public TMP_Text tmpCorreo;
    public Image avatarImage;  // Componente Image donde se mostrará el avatar
    // variables para cargas las misiones
    public Transform content;// donde va el scroll 
    public GameObject buttonPrefab; // boton que se crea cada vez que hay una mision
    public string userId;

    void Start()
    {
        Debug.Log("ControllerPerfil Start ejecutándose...");

        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        Debug.Log("UserID en PlayerPrefs: " + userId);

        if (!string.IsNullOrEmpty(userId))
        {
            ObtenerDatosUsuario(userId);//########################## llamado de funcion para datos
        }
        else
        {
            tmpUsername.text = "Usuario: No encontrado";
            tmpCorreo.text = "Correo: No encontrado";
        }

        CargarMisioness();// ##################################### llamado de funcion para cargar las misiones dentro del scroll view
    }

    private string ObtenerAvatarPorRango(string rangos) //############################################# funcion para obtener nivel del usuario en particular
    {
        string avatarPath = string.Empty;
        if (rangos == "Novato de laboratorio")
        {
            avatarPath = "Avatares/nivel1";
        }
        else if (rangos == "Arquitecto molecular")
        {
            avatarPath = "Avatares/nivel2";
        }
        else if (rangos == "Visionario Cuántico")
        {
            avatarPath = "Avatares/nivel3";
        }
        else if (rangos == "Amo del caos químico")
        {
            avatarPath = "Avatares/nivel4";
        }
        else
        {
            avatarPath = "Avatares/defecto";
        }

        Debug.Log($"Ruta de avatar por nivel: {avatarPath}");  // Verifica la ruta generada
        return avatarPath;
    }

    async void ObtenerDatosUsuario(string userId) //###################################################### traemos los datos para pasarlos a los txt
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log("Documento encontrado en Firestore");
            string username = snapshot.GetValue<string>("DisplayName");
            string correo = snapshot.GetValue<string>("Email");
            string rangos = snapshot.GetValue<string>("Rango");
            int xp = snapshot.GetValue<int>("xp");
            string avatarUrl = snapshot.GetValue<string>("avatar"); // Recuperar la ruta del avatar


            // Obtener la ruta del avatar según el nivel
            string avatarPath = ObtenerAvatarPorRango(rangos);
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath); // Cargar la imagen desde Resources
            
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;  // Asignar la imagen al Image
            }
            else
            {
                Debug.LogError($"No se encontró el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
                avatarImage.sprite = Resources.Load<Sprite>("Avatares/default");  // Avatar por defecto si no se encuentra
            }

            Debug.Log($"Nombre de usuario Firestore: {username}");
            Debug.Log($"Correo Firestore: {correo}");

            tmpUsername.text = "¡Hola, " + username + "!";
            tmpCorreo.text = "Correo: " + correo;
        }
        else
        {
            Debug.LogError("El documento del usuario no existe en Firestore.");
            tmpUsername.text = "Usuario: No disponible";
            tmpCorreo.text = "Correo: No disponible";
        }
    }

    // Método para obtener el nivel del usuario desde Firestore
    async Task<string> ObtenerRangoUsuario(string userId) //################################## obtenemos el rango para poder mostrar mision dependiendo el rango de cada usuario en especifico
    {
        // Referencia a la colección de usuarios en Firestore
        DocumentReference userRef = db.Collection("users").Document(userId);

        // Obtenemos el documento del usuario
        DocumentSnapshot userSnap = await userRef.GetSnapshotAsync();

        if (userSnap.Exists)
        {
            // Obtenemos el nivel del usuario
            string rango = userSnap.GetValue<string>("Rango");
            Debug.Log($"rango del usuario: {rango}");
            return rango;
        }
        else
        {
            Debug.LogError("No se encontró el usuario en Firestore.");
            return ""; // Devolvemos 0 si no se encuentra el usuario
        }
    }

    public async void CargarMisioness()//############################################# mostramos todo dentro del content del scrollview
    {
        // Obtener el nivel del usuario
        string rangoUsuario = await ObtenerRangoUsuario(userId);// acá llamamos la funcion para pasarle el rango y poder comparar

        // Filtrar misiones por nivel
        Query misionesQuery = db.Collection("misiones").WhereEqualTo("rangoRequerido", rangoUsuario);
        QuerySnapshot snapshot = await misionesQuery.GetSnapshotAsync();

        // Recorremos las misiones obtenidas
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            if (document.Exists)
            {
                string titulo = document.GetValue<string>("titulo");
                string descripcion = document.GetValue<string>("descripcion");
                int xp = document.GetValue<int>("xp");
                string rutaEscena = document.GetValue<string>("rutaEscena");
                string misionID = document.Id; // ID de la misión

                Debug.Log($"Misión: {titulo}, {descripcion}, XP: {xp}, Escena: {rutaEscena}");

                // Instanciamos el botón para mostrar la misión
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Obtener componentes del botón
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                // Asignamos la información al botón
                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asignar evento al botón para cambiar de escena
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));
                }
                else
                {
                    Debug.LogWarning("El botón no tiene componente Button.");
                }

                // Cargar el progreso del usuario en la misión
                await CargarProgreso(userId, misionID, barraProgreso);// acá modificamos el slider
            }
            else
            {
                Debug.LogWarning("Documento no encontrado.");
            }
        }
    }


    // Método para cargar el progreso del usuario
    async Task CargarProgreso(string userId, string missionId, Slider barraProgreso) // ################################## función para cargar el slider que esta dentro del botónd de prefabs
    {
        DocumentReference docRef = db.Collection("progreso_misiones").Document(userId).Collection("misiones").Document(missionId);
        DocumentSnapshot docSnap = await docRef.GetSnapshotAsync();

        if (docSnap.Exists)
        {
            int progreso = docSnap.GetValue<int>("progreso");
            barraProgreso.value = progreso / 100f; // Normalizar entre 0 y 1
            Debug.Log($"✅ Progreso de {missionId}: {progreso}%");
        }
        else
        {
            barraProgreso.value = 0f; // Si no existe, empieza en 0
            Debug.Log($"⚠️ No se encontró progreso para {missionId}, iniciando en 0%");
        }
    }

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
