using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Logout : MonoBehaviour
{

    private FirebaseAuth auth;
    public FirebaseFirestore db;// instanciamos db
    private string userId;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();
    }
    public void logout() // ################################################################ M�todo para cerrar sesi�n
    {
       // await SubirMisionesJSON(); ponerlo apenas se pueda URGENTE
       
        PlayerPrefs.DeleteAll(); // Elimina el ID del usuario guardado
        PlayerPrefs.Save(); // Guarda los cambios
        auth.SignOut(); // Cierra la sesi�n en Firebase
        Debug.Log("Sesi�n cerrada correctamente");
        // Opcional: Redirigir a la escena de login
        SceneManager.LoadScene("Start");
    }

}
