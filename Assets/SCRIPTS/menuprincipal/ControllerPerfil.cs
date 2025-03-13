//using UnityEngine;
//using TMPro;
//using Firebase.Firestore;
//using System.Threading.Tasks;

//public class ControllerPerfil : MonoBehaviour
//{
//    public TMP_Text tmpUsername;
//    public TMP_Text tmpCorreo;
//    private FirebaseFirestore db;

//    void Start()
//    {
//        Debug.Log("ControllerPerfil Start ejecutándose...");

//        db = FirebaseFirestore.DefaultInstance;
//        string userId = PlayerPrefs.GetString("userId", "");

//        Debug.Log("UserID en PlayerPrefs: " + userId);

//        if (!string.IsNullOrEmpty(userId))
//        {
//            ObtenerDatosUsuario(userId);
//        }
//        else
//        {
//            tmpUsername.text = "Usuario: No encontrado";
//            tmpCorreo.text = "Correo: No encontrado";
//        }
//    }

//    async void ObtenerDatosUsuario(string userId)
//    {
//        DocumentReference docRef = db.Collection("users").Document(userId);
//        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

//        if (snapshot.Exists)
//        {
//            Debug.Log("Documento encontrado en Firestore");
//            string username = snapshot.GetValue<string>("DisplayName");
//            string correo = snapshot.GetValue<string>("Email");

//            Debug.Log($"Nombre de usuario Firestore: {username}");
//            Debug.Log($"Correo Firestore: {correo}");

//            tmpUsername.text = "Usuario: " + username;
//            tmpCorreo.text = "Correo: " + correo;
//        }
//        else
//        {
//            Debug.LogError("El documento del usuario no existe en Firestore.");
//            tmpUsername.text = "Usuario: No disponible";
//            tmpCorreo.text = "Correo: No disponible";
//        }
//    }

//}
