using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO; // Necesario para manejar archivos

public class Logout : MonoBehaviour
{
    private FirebaseAuth auth;
    public FirebaseFirestore db;
    private string userId;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();
    }

    public void logout()
    {
        // await SubirMisionesJSON(); // URGENTE, pendiente de activar cuando se pueda

        // Eliminar archivos locales
        EliminarArchivosLocales();

        // Cerrar sesión
        auth.SignOut();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("Sesión cerrada correctamente");

        // Redirigir a la escena de login
        SceneManager.LoadScene("Start");
    }

    private void EliminarArchivosLocales()
    {
        string rutaMisiones = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");
        string rutaLogros = Path.Combine(Application.persistentDataPath, "Json_Logros.json");
        string rutaCategorias = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");

        if (File.Exists(rutaMisiones))
        {
            File.Delete(rutaMisiones);
            Debug.Log("Json_misiones.json eliminado");
        }
        else
        {
            Debug.Log("Json_misiones.json no existe");
        }

        if (File.Exists(rutaLogros))
        {
            File.Delete(rutaLogros);
            Debug.Log("Json_logros.json eliminado");
        }
        else
        {
            Debug.Log("Json_logros.json no existe");
        }

        if (File.Exists(rutaCategorias))
        {
            File.Delete(rutaCategorias);
            Debug.Log("categorias_encuesta_firebase.json eliminado");
        }
        else
        {
            Debug.Log("categorias_encuesta_firebase.json no existe");
        }
    }
}
