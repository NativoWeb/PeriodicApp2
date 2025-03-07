using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Collections;
using Firebase.Extensions;
//using UnityEditor.TerrainTools;
using Firebase.Auth;
//using UnityEditor.Search;

public class EstadisticasController : MonoBehaviour
{
    private FirebaseAuth auth;// iniciamos el auth para cerrarlo con el boton de cerrar
    public FirebaseFirestore db;// instanciamos db
    public string userId;
    
    //instanciamos variables para poner dentro de unity
    public Image avatarImage;
    public TMP_Text rangotxt;

    //panel para cerrar sesion
    [SerializeField] private GameObject m_logoutUI = null;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();

        // Iniciar la carga de datos de usuario sin perder await
        StartCoroutine(LoadUserData(userId));
    }

    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId);
        yield return new WaitUntil(() => task.IsCompleted);
    }

    //funcion para mostrar el panel y cerrar sesion
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
    async Task GetUserData(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.LogAssertion($"Usuario encontrado en firebase : {userId}");
            string username = snapshot.GetValue<string>("DisplayName");
            string avatarUrl = snapshot.GetValue<string>("avatar");
            string rango = snapshot.GetValue<string>("Rango");

            string avatarPath = ObtenerAvatarPorRango(rango);// llamamos función para poder tener una ruta que pasar a la imagen 
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogError($"no se encontro una ruta valida para : {avatarPath}");
                avatarImage.sprite = Resources.Load<Sprite>(avatarPath);
            }
            rangotxt.text = "Su rango es :" + rango + "!";

        }

    }

    public string ObtenerAvatarPorRango(string rango)//############# funcion para obtener la ruta de la imagen que se va a poner dentro de la imagen 
    {
     string avatarPath = string.Empty;
    if (rango == "Novato de laboratorio")
        {
            avatarPath = "Avatares/nivel1";
        }
   else if (rango == "Arquitecto molecular ")
        {
            avatarPath = "Avatares/nivel2";
        }
   else if(rango == "Visionario Cuántico")
        {
            avatarPath = "Avatares/nivel3";
        }
   else if (rango == "Amo del caos químico")
        {
            avatarPath = "Avatares/nivel4";
        }
   else
        {
            avatarPath = "Avatares/defecto";
        }

        Debug.LogAssertion($"la ruta del avatar es: {avatarPath}");

        return avatarPath;
    }

   
    public void Logout() // ################################################################ Método para cerrar sesión
    {
        auth.SignOut(); // Cierra la sesión en Firebase
        PlayerPrefs.DeleteKey("userId"); // Elimina el ID del usuario guardado
        PlayerPrefs.Save(); // Guarda los cambios

        Debug.Log("Sesión cerrada correctamente");

        // Opcional: Redirigir a la escena de login
        SceneManager.LoadScene("Login"); 
    }
    
    
}
