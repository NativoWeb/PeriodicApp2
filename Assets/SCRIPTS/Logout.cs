using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Logout : MonoBehaviour
{

    private FirebaseAuth auth;
    public FirebaseFirestore db;// instanciamos db
    public string userId;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();
    }
    public void logout() // ################################################################ Método para cerrar sesión
    {
       // await SubirMisionesJSON(); ponerlo apenas se pueda URGENTE
        auth.SignOut(); // Cierra la sesión en Firebase
        PlayerPrefs.DeleteAll(); // Elimina el ID del usuario guardado
        PlayerPrefs.Save(); // Guarda los cambios

        Debug.Log("Sesión cerrada correctamente");

        // Opcional: Redirigir a la escena de login
        SceneManager.LoadScene("Start");
    }

}
