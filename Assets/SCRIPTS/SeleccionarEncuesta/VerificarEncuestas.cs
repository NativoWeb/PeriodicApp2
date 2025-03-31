using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Importante para trabajar con botones
public class VerificarEncuestas : MonoBehaviour
{

    // elementos interfaz
    public Button botonAprendizaje; 
    public Button botonConocimiento;

    // instanciar bd
    private FirebaseAuth auth;
    private FirebaseFirestore db;


    // verifica conexion wifi
    private bool hayInternet = false;
    

    //estados encuestas
    bool estadoencuestaaprendizaje;
    bool estadoencuestaconocimiento;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        actualizarencuestasfirebase();
        VerificarEncuestass();

    }

    private void actualizarencuestasfirebase()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayInternet)
        {


            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            FirebaseUser currentUser = auth.CurrentUser;

            string userId = currentUser.UserId;

            estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            if (estadoencuestaaprendizaje)
            {
                ActualizarEstadoEncuestaAprendizaje(userId, estadoencuestaaprendizaje);
            }
            if (estadoencuestaconocimiento)
            {
                ActualizarEstadoEncuestaConocimiento(userId, estadoencuestaconocimiento);
            }
        }
        else
        {
            Debug.Log("Sin conexión a internet no se puede actualizar encuestas desde firebase");
        }

    }
    private async void ActualizarEstadoEncuestaAprendizaje(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaAprendizaje", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Aprendizaje... {userId}: {estadoencuesta} desde verificar encuestas / actualizarestadoencuestaaprendizaje");
    }

    private async void ActualizarEstadoEncuestaConocimiento(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaConocimiento", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Conocimiento... {userId}: {estadoencuesta} desde verificar encuestas / actualizarestadoencuestaconocimiento");
    }

    private void VerificarEncuestass()
    {
        // Verificar conexión a Internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // Declarar variables antes del if para que sean accesibles en todo el método
        bool estadoencuestaaprendizaje;
        bool estadoencuestaconocimiento;

        if (hayInternet)
        {
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            FirebaseUser currentUser = auth.CurrentUser;

            if (currentUser == null) // Evitar errores si el usuario no está autenticado
            {
                Debug.LogError("❌ No hay un usuario autenticado.");
                return;
            }

            string userId = currentUser.UserId; // Obtener el UID del usuario

            DocumentReference docRef = db.Collection("users").Document(userId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("❌ Error al obtener los datos del usuario.");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    Debug.LogError("❌ No se encontraron datos para este usuario.");
                    return;
                }

                // Obtener valores de Firestore
                estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;
                estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;

                // Verificar si se deben cargar las categorías
                if (estadoencuestaaprendizaje && estadoencuestaconocimiento)
                {
                    SceneManager.LoadScene("Categorías");
                }

                Debug.Log($"encuesta aprendizaje : {estadoencuestaaprendizaje}, encuesta conocimiento, {estadoencuestaconocimiento}");
                // Actualizar UI de los botones en el hilo principal
                ActualizarUIBotones(estadoencuestaaprendizaje, estadoencuestaconocimiento);
            });
        }
        else
        {
            // Obtener valores desde PlayerPrefs si no hay conexión
            estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            // Verificar si se deben cargar las categorías (sin conexión)
            if (estadoencuestaaprendizaje && estadoencuestaconocimiento)
            {
                SceneManager.LoadScene("Categorías");
            }

            // Actualizar UI de los botones sin conexión
            ActualizarUIBotones(estadoencuestaaprendizaje, estadoencuestaconocimiento);
        }
    }

    // Método separado para actualizar la UI de los botones
    private void ActualizarUIBotones(bool estadoAprendizaje, bool estadoConocimiento)
    {
        botonAprendizaje.interactable = !estadoAprendizaje;
        botonConocimiento.interactable = !estadoConocimiento;

        if (!botonAprendizaje.interactable)
        {
            botonAprendizaje.GetComponentInChildren<TMP_Text>().color = Color.white;
        }

        if (!botonConocimiento.interactable)
        {
            botonConocimiento.GetComponentInChildren<TMP_Text>().color = Color.white;
        }
    }

}
