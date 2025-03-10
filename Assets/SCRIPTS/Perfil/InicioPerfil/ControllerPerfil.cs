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
    private string rangoActual; // Rango actualizado del usuario

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

    /*-------------------------------------------------funcion para actualizar dependiendo el xp-----------------------------------------------------*/
    public void ActualizarDatosUsuario()
    {
        StartCoroutine(ActualizarDatosUsuarioCoroutine());
    }

    private IEnumerator ActualizarDatosUsuarioCoroutine()
    {
        ObtenerDatosUsuario(userId);
        yield return null; // Para que pueda correrse de manera asíncrona si quieres añadir efectos visuales
    }

    /*--------------------------------------------------------------------------------------------------------------------------------------------*/
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

    async void ObtenerDatosUsuario(string userId)
    {
        

    DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log("Documento encontrado en Firestore");
            string username = snapshot.GetValue<string>("DisplayName");
            string correo = snapshot.GetValue<string>("Email");
            string rangos = snapshot.GetValue<string>("Rango"); // El rango actualizado
            rangoActual = rangos; // Guardar rango globalmente
            int xp = snapshot.GetValue<int>("xp");

            // Obtener la ruta del avatar según el rango
            string avatarPath = ObtenerAvatarPorRango(rangos);
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath); // Cargar imagen desde Resources

            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite; // Asignar imagen
            }
            else
            {
                Debug.LogError($"No se encontró el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
                avatarImage.sprite = Resources.Load<Sprite>("Avatares/default"); // Avatar por defecto
            }

            // Actualizar textos
            tmpUsername.text = "¡Hola, " + username + "!";
            tmpCorreo.text = "Correo: " + correo;

            // 🔑 Si quieres también mostrar el rango textual, podrías añadir otro TMP_Text para esto (opcional):
            // tmpRango.text = "Rango: " + rangos;

            // 🔄 Recargar misiones según nuevo rango
            LimpiarMisiones(); // Elimina misiones anteriores para recargar
            CargarMisioness(); // Recarga las misiones según nuevo rango

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

    public async void CargarMisioness()
    {
        // ✅ Usar el rango que ya tenemos almacenado
        string rangoUsuario = rangoActual;

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
                string misionID = document.Id;

                // Instanciar botón
                GameObject newButton = Instantiate(buttonPrefab, content);

                // Asignar info al botón
                TextMeshProUGUI[] textComponents = newButton.GetComponentsInChildren<TextMeshProUGUI>();
                Slider barraProgreso = newButton.GetComponentInChildren<Slider>();

                textComponents[0].text = titulo;
                textComponents[1].text = descripcion;
                textComponents[2].text = $"XP: {xp}";

                // Asignar evento al botón
                Button btn = newButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => CambiarEscena(rutaEscena));
                }

                // Cargar progreso
                await CargarProgreso(userId, misionID, barraProgreso);
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

    private void LimpiarMisiones()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
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
