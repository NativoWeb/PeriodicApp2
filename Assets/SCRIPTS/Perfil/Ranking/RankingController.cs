using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Extensions;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;

public class RankingController: MonoBehaviour
{
    private FirebaseFirestore db; // Base de datos Firebase
    private string userId; // ID del usuario actual

    // Referencias a los elementos de la interfaz
    public TMP_Text posicionText;
    public TMP_Text Xptext;
    public TMP_Text UserName;
    public Image avatarimage;
    public TMP_Text rangotext;

    public ControllerPerfil controllerPerfil; // Referencia al ControllerPerfil


    // Función que se ejecuta al iniciar la escena
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance; // Conexión a Firebase
        userId = PlayerPrefs.GetString("userId", "").Trim(); // Obtenemos el ID del usuario guardado

        // Si el ID no está vacío, obtenemos la posición y los datos del usuario
        if (!string.IsNullOrEmpty(userId))
        {
            ObtenerPosicionUsuario(); // Llama la función para saber en qué puesto está el usuario
            StartCoroutine(LoadUserData(userId)); // Llama la función para cargar los datos del usuario
        }
        else
        {
            // Si no hay usuario guardado, muestra mensaje de error
            posicionText.text = "Posición: No disponible";
            Debug.LogError("No se encontró el ID del usuario.");
        }
        
    }

    
    // Corrutina que espera a que se carguen los datos del usuario
    IEnumerator LoadUserData(string userId)
    {
        var task = GetUserData(userId); // Llama a la función de obtener datos
        yield return new WaitUntil(() => task.IsCompleted); // Espera hasta que termine
    }

    // Función para devolver la ruta del avatar según el rango del usuario
    private string ObtenerAvatarPorRango(string rangos)
    {
        string avatarPath = rangos switch
        {
            "Novato de laboratorio" => "Avatares/nivel1",
            "Arquitecto molecular" => "Avatares/nivel2",
            "Visionario Cuántico" => "Avatares/nivel3",
            "Amo del caos químico" => "Avatares/nivel4",
            _ => "Avatares/defecto" // Si no encuentra el rango, pone avatar por defecto
        };

        Debug.Log($"Ruta de avatar por nivel: {avatarPath}");
        return avatarPath;
    }

    // Función para obtener los datos del usuario desde Firebase
    async Task GetUserData(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId); // Referencia al usuario
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync(); // Obtenemos los datos

        // Verificamos si el usuario existe
        if (!snapshot.Exists)
        {
            Debug.LogError("Usuario no encontrado en la base de datos.");
            UserName.text = "¡Usuario no encontrado!";
            rangotext.text = "Sin rango";
            Xptext.text = "0";
            return; // Salir de la función si no existe
        }

        Debug.Log($"Usuario encontrado en Firebase: {userId}");

        // Obtenemos cada dato del usuario (si existe)
        string userName = snapshot.ContainsField("DisplayName") ? snapshot.GetValue<string>("DisplayName") : "Sin nombre";
        string rangos = snapshot.ContainsField("Rango") ? snapshot.GetValue<string>("Rango") : "Sin rango";
        int xp = snapshot.ContainsField("xp") ? snapshot.GetValue<int>("xp") : 0;

        // Mostramos los datos en la pantalla
        Xptext.text = xp.ToString();
        UserName.text = "¡Hola " + userName + "!";
        rangotext.text = "¡" + rangos + "!";

        // Obtener y asignar la imagen del avatar según el rango
        string avatarPath = ObtenerAvatarPorRango(rangos);
        Sprite avatarSprite = Resources.Load<Sprite>(avatarPath);

        // Si encontró la imagen, la muestra. Si no, muestra imagen por defecto
        if (avatarSprite != null)
        {
            avatarimage.sprite = avatarSprite;
        }
        else
        {
            Debug.LogError($"No se encontró el avatar para la ruta: {avatarPath}. Asignando avatar por defecto.");
            avatarimage.sprite = Resources.Load<Sprite>("Avatares/default");
        }

        // Llamar a la función para actualizar el rango según el XP obtenido
        if (controllerPerfil != null)
        {
            controllerPerfil.ActualizarRangoSegunXP(xp); // Llama la función con el XP
        }
        else
        {
            Debug.LogError("ControllerPerfil no está asignado.");
        }
    }

    // Función para actualizar el rango


    // Función para obtener la posición del usuario en el ranking
    async void ObtenerPosicionUsuario()
    {
        // Consulta para obtener los usuarios ordenados por XP (de mayor a menor)
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync(); // Obtenemos los datos

        // Si no hay usuarios en la base de datos
        if (snapshot.Count == 0)
        {
            Debug.LogWarning("No hay usuarios registrados en la base de datos.");
            posicionText.text = "Posición: No disponible";
            return;
        }

        int posicion = 1; // Empezamos desde la posición 1
        bool encontrado = false; // Variable para saber si encontramos al usuario

        // Recorremos todos los usuarios
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Si el ID coincide con el del usuario actual
            if (doc.Id == userId)
            {
                encontrado = true; // Marcamos que lo encontramos
                posicionText.text = "Posición: #" + posicion; // Mostramos la posición
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                break; // Salimos del ciclo porque ya encontramos al usuario
            }
            posicion++; // Sumamos 1 a la posición para el siguiente usuario
        }

        // Si no encontramos al usuario
        if (!encontrado)
        {
            Debug.LogError("No se encontró al usuario en el ranking.");
            posicionText.text = "Posición: No encontrada";
        }
    }
}
