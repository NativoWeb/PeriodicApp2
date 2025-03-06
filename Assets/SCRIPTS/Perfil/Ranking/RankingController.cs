using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.UI; // PARA Poner todos loa elementos ui incluyendo imagenes
using System.Collections;

public class RankingController : MonoBehaviour
{

    private FirebaseFirestore db; // para acceder a la bd
    private string userId; // para tener el userid y acceder al documento desde la colección
    
    //inicializamos las variables a poner dentro de unity
    public TMP_Text posicionText;  // Texto donde se mostrará la 
    public TMP_Text Xptext; // donde vamos a mostrar el xp desde la bd
    public TMP_Text UserName; // mostramos el nombre del usuario
    public Image avatarimage; // mostrar la imagen
    public TMP_Text rangotext; // mostramos el rango
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        userId = PlayerPrefs.GetString("userId", "").Trim();  // Obtener el ID del usuario logueado

        if (!string.IsNullOrEmpty(userId))
        {
            ObtenerPosicionUsuario();// ###################################### FUNCION PARA ORGANIZAR LOS RANKING Y PONERLO EN EL posicionText
        }
        else
        {
            posicionText.text = "Posición: No disponible";
        }

        GetUserXp(userId);// ################################# FUNCION PARA TRAER EL XP DEL USUARIO AL Xptext
                          // Iniciar la carga de datos de usuario sin perder await
        StartCoroutine(LoadUserData(userId));
    }
    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId);
        yield return new WaitUntil(() => task.IsCompleted);
    }


    private string ObtenerAvatarPorRango(string rangos)
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

    async Task GetUserData(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log($"usuario encontrado en firebase : {userId}");
            string userName = snapshot.GetValue<string>("DisplayName");
            string rangos = snapshot.GetValue<string>("Rango");
            //int xp = snapshot.GetValue<int>("xp");
            string avatarUrl = snapshot.GetValue<string>("avatar");
            


            // obtener la url del avatar dependiendo el rango
            string avatarPath = ObtenerAvatarPorRango(rangos); // traigo la url de obteneravatar por rango
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath); // le pongo la dirección y hhago que vaya a la carpeta resources y lo busque

            if (avatarSprite != null)
            {
                avatarimage.sprite = avatarSprite;
            }
            else
            {
                Debug.LogError($"No se encontró el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
                avatarimage.sprite = Resources.Load<Sprite>("Avatares/default");  // Avatar por defecto si no se encuentra

            }

            UserName.text ="¡Hola " + userName +"!";
            rangotext.text = "!" + rangos + "!";

        }
    }
    
    async void ObtenerPosicionUsuario() // ############################################ FUNCION PARA ORGANIZAR LOS RANKING Y PONERLO EN EL posicionText
    {
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        int posicion = 1; // Empezamos en la posición 1

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Id == userId)  // Si encontramos al usuario logueado
            {
                posicionText.text = "Posición: #" + posicion;
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                return; // Salimos del bucle
            }
            posicion++; // Si no es el usuario, aumentamos la posición
        }

        // Si no lo encontró en la base de datos
        posicionText.text = "Posición: No encontrada";
        Debug.LogError("No se encontró al usuario en el ranking.");
    }



    void GetUserXp(string userId) // ############################################ FUNCION PARA TRAER EL XP DEL USUARIO AL Xptext
    {
        //hacer la referencia a la colleción users  y que vaya y busque el usuario por el id
        DocumentReference userRef = db.Collection("users").Document(userId);

        // userRef= se ubica dentro de el usuario dentro del collecion, getSnapshotAsync = pide la informacion, ContinueWithOnMainThread 
        //le dice al código que se siga ejecutando lo demás, el Task define la tarea
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(Task =>
        {
            // si la tarea es completada y el resultado existe entonces...
            if (Task.IsCompleted && Task.Result.Exists)
            {
                // guarde el resultado del resultado de la tarea(task) dentro de variable xp
                int xp = Task.Result.GetValue<int>("xp");

                //mostrar dentro de el txt en unity
                Xptext.text = xp.ToString();
            }
            else
            {
                Debug.LogWarning("Usuario no encontrado ");
            }

        });
    }

   
}
