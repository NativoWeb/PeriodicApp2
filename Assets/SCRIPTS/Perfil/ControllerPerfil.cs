using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.UI;  // Importante para Image

public class ControllerPerfil : MonoBehaviour
{
    public TMP_Text tmpUsername;
    public TMP_Text tmpCorreo;
    public Image avatarImage;  // Componente Image donde se mostrará el avatar

    private FirebaseFirestore db;

    void Start()
    {
        Debug.Log("ControllerPerfil Start ejecutándose...");

        db = FirebaseFirestore.DefaultInstance;
        string userId = PlayerPrefs.GetString("userId", "");

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
    }

    private string ObtenerAvatarPorNivel(int nivel)
    {
        string avatarPath = string.Empty;
        if (nivel == 1)
        {
            avatarPath = "Avatares/nivel1";
        }
        else if (nivel == 2)
        {
            avatarPath = "Avatares/nivel2";
        }
        else if (nivel == 3)
        {
            avatarPath = "Avatares/nivel3";
        }
        else if (nivel == 4)
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
            int nivel = snapshot.GetValue<int>("nivel");
            int xp = snapshot.GetValue<int>("xp");
            string avatarUrl = snapshot.GetValue<string>("avatar"); // Recuperar la ruta del avatar


            // Obtener la ruta del avatar según el nivel
            string avatarPath = ObtenerAvatarPorNivel(nivel);
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

  
}
