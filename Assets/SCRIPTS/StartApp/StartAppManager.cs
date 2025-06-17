using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase;

public class StartAppManager : MonoBehaviour
{
    // acá si sirveeeeeeee
    public static bool IsReady = false; // 🔹 Bandera para indicar si terminó
    private bool yaVerificado = false; // 🔹 Evita ejecuciones repetidas

    //variables FIREBASE
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private async void Start()
    {
        Debug.Log("StartAppManager START ejecutado");

        bool listo = await FirebaseServiceLocator.InicializarFirebase();
        Debug.Log("Firebase inicializado: " + listo);
        if (!listo)
        {
            Debug.LogError("Firebase no se inicializó correctamente.");
            // Aquí podrías mostrar UI de error o reintentar
            return;
        }

        auth = FirebaseServiceLocator.Auth;
        db = FirebaseServiceLocator.Firestore;

        StartCoroutine(CheckInternetConnection());
        StartCoroutine(DeleteAccount());
    }


    // 🔹 Corrutina para verificar conexión
    IEnumerator CheckInternetConnection()

    {
        yield return new WaitForSeconds(0); // Esperar un segundo antes de validar


        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            HandleOfflineMode();
        }
        else
        {
            HandleOnlineMode();
        }
    }

   
    // 🔹 Modo offline
    void HandleOfflineMode()
    {
        if (yaVerificado) return; // 🔹 Si ya se ejecutó, salir

        yaVerificado = true; // 🔹 Marcar como ejecutado

        string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");
        


        // ---------------------------------------------- VALIDACIONES --------------------------------------------------------------------------

        if (estadoUsuario == "nube") 
        {
            AutoLogin();

        }
        else if (estadoUsuario == "local")
        {
            // Validar el estado de ambas encuestas para pasar a scena 

            bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            string ocupacion = PlayerPrefs.GetString("TempOcupacion", "").Trim();

            if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor1");
            }   
            else if (ocupacion == "Estudiante")
            {
                if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
                {
                    Debug.Log("Cargando escena: Inicio");
                    SceneManager.LoadScene("Inicio");
                }
                else
                {
                    SceneManager.LoadScene("SeleccionarEncuesta");
                }

            }

        }
        else if (string.IsNullOrEmpty(estadoUsuario))
        {

            CreateTemporaryUser();
            LoadSceneIfNotAlready("InicioOffline");

        }
        else if (estadoUsuario == "sinloguear") // funcion para cuando se registra con wifi y no se loguea, no le vuelva a crear otro usuario temporal -----------------------------
        {
            AutoLoginOnlyRegister();
        }

            IsReady = true; // 🔹 Marcamos como listo también en modo offline
    }


    // 🔹 Modo online
    void HandleOnlineMode()
    {
        if (yaVerificado) return;

        yaVerificado = true;

        string EstadoUsuario = PlayerPrefs.GetString("Estadouser","");
        Debug.Log("📍 EstadoUsuario = " + EstadoUsuario);

        // ---------------------------------------------- VALIDACIONES --------------------------------------------------------------------------
        if (EstadoUsuario == "local") 
        {

            SceneManager.LoadScene("Email");


        }
        else if (EstadoUsuario == "nube")
        {
            Debug.Log("Modo nube: AutoLogin");
            AutoLogin();

        }
        else if (EstadoUsuario == "sinloguear")
        {
            Debug.Log("Registrado pero nunca logueado");
            LoadSceneIfNotAlready("Login");

        }
        else if (string.IsNullOrEmpty(EstadoUsuario))
        {
            Debug.Log("Usuario Nuevo Ingresando...");
            LoadSceneIfNotAlready("Login");
        }

            IsReady = true; // ✅ Marcamos como listo
    }

    // 🔹 Evita recargar la misma escena si ya está activa
    void LoadSceneIfNotAlready(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }


    // Crear y guardar usuario temporal en PlayerPrefs
    void CreateTemporaryUser()
    {
        string username = "User_" + Random.Range(0, 999).ToString();
        string ocupacionSeleccionada = "Otro"; // Por defecto
        string avatarUrl = "Avatares/nivel1"; // Por defecto
        bool encuestaCompletada = false;

        // Guardar datos en PlayerPrefs
        PlayerPrefs.SetString("DisplayName", username);
        PlayerPrefs.SetString("TempOcupacion", ocupacionSeleccionada);
        PlayerPrefs.SetInt("TempXP", 0);
        PlayerPrefs.SetString("TempAvatar", avatarUrl);
        PlayerPrefs.SetString("Rango", "Novato de laboratorio");
        PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", encuestaCompletada ? 1 : 0);
        PlayerPrefs.SetInt("EstadoEncuestaConocimiento", encuestaCompletada ? 1 : 0);
        PlayerPrefs.SetInt("posicion", 0);
        PlayerPrefs.SetInt("Nivel", 1);
        PlayerPrefs.SetString("Estadouser", "local");
        PlayerPrefs.Save();
    }

    void AutoLogin()
    {
        Debug.Log("🚀 Entrando a AutoLogin()");

        if (PlayerPrefs.GetInt("rememberMe", 0) == 1)
        {
            string savedEmail = PlayerPrefs.GetString("userEmail", "");
            string savedPassword = PlayerPrefs.GetString("userPassword", "");

            Debug.Log($"📧 Email: {savedEmail}, ✅ rememberMe: 1");

            auth.SignInWithEmailAndPasswordAsync(savedEmail, savedPassword).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && task.Result != null)
                {
                    FirebaseUser user = task.Result.User;
                    Debug.Log($"✅ Login exitoso. UID: {user.UserId}");

                    PlayerPrefs.SetString("userId", user.UserId);
                    PlayerPrefs.SetString("Estadouser", "nube");
                    PlayerPrefs.Save();

                    CheckAndDownloadMisiones(user.UserId);  // debería cargar la escena
                }
                else
                {
                    Debug.LogError("❌ Falló el login automático.");
                    Debug.LogError(task.Exception?.Message);
                    TryOfflineLogin(savedEmail, savedPassword);
                }
            });
        }
        else
        {
            Debug.LogWarning("⚠️ rememberMe no está activo, no se hace AutoLogin.");
        }
    }



    void AutoLoginOnlyRegister() // funcion para cuando se registra con wifi y no se loguea, no le vuelva a crear otro usuario temporal -----------------------------
    {
        
            string savedEmail = PlayerPrefs.GetString("userEmail");
            string savedPassword = PlayerPrefs.GetString("userPassword");
            Debug.Log("entrando a tryofflinelogin, el usuario solo se registro, no se logueo");
            TryOfflineLogin(savedEmail, savedPassword);
          
    }


    /* ------------------------ 🔥 NUEVA FUNCIÓN PARA DESCARGAR MISIONES 🔥 ------------------------ */
    private void CheckAndDownloadMisiones(string userId)
    {
        Debug.Log("Verificando misiones...");

        DocumentReference userDoc = db.Collection("users").Document(userId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("❌ Fallo al obtener snapshot de misiones.");
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists || !snapshot.ContainsField("misiones"))
            {
                Debug.Log("No hay campo 'misiones', saltando a CheckUserStatus");
                CheckUserStatus(userId);
                return;
            }

            string misionesJson = snapshot.GetValue<string>("misiones");
            Debug.Log("Misiones obtenidas");

            if (!string.IsNullOrEmpty(misionesJson))
            {
                PlayerPrefs.SetString("misionesJSON", misionesJson);
                PlayerPrefs.Save();
            }

            CheckUserStatus(userId);
        });
    }


    private void CheckUserStatus(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                return;
            }

            string ocupacion = snapshot.GetValue<string>("Ocupacion");


            bool estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;

            bool estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;  // Valor por defecto si el campo no existe

            if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor1");
            }
            else if (ocupacion == "Estudiante")
            {
                if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
                {
                    SceneManager.LoadScene("Inicio");
                }
                else
                {
                    SceneManager.LoadScene("SeleccionarEncuesta");
                }
            }

        });
    }

    private void TryOfflineLogin(string email, string password)
    {
        if (PlayerPrefs.HasKey("userEmail") && PlayerPrefs.HasKey("userPassword") && PlayerPrefs.HasKey("userId"))
        {
            string savedEmail = PlayerPrefs.GetString("userEmail");
            string savedPassword = PlayerPrefs.GetString("userPassword");
            string savedUserId = PlayerPrefs.GetString("userId");

            if (email == savedEmail && password == savedPassword)
            {

                bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
                bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

                Debug.Log($"aprendizaje: {estadoencuestaaprendizaje}, Conocimiento: {estadoencuestaconocimiento}, desde try offline login");

                string ocupacion = PlayerPrefs.GetString("TempOcupacion", "");

                Debug.Log($"ocupacion: {ocupacion}, desde tryOfflineLogin - StartApp");

                if (ocupacion == "Profesor")
                {
                    SceneManager.LoadScene("InicioProfesor1");
                }
                else if (ocupacion == "Estudiante")
                {
                    if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
                    {
                        SceneManager.LoadScene("Inicio");
                    }
                    else
                    {
                        SceneManager.LoadScene("SeleccionarEncuesta");
                    }

                }

            }
            else if (email == savedEmail && password != savedPassword)
            {
            }
        }
        else
        {
        }
    }
    private IEnumerator DeleteAccount()
    {

        string UserEliminarId = PlayerPrefs.GetString("UsuarioEliminar", "");

        if (!string.IsNullOrEmpty(UserEliminarId))
        {
            FirebaseUser user = auth.CurrentUser;
            if (user != null && user.UserId == UserEliminarId)
            {
                var deleteTask = user.DeleteAsync();
                yield return new WaitUntil(() => deleteTask.IsCompleted);

                if (deleteTask.IsCompletedSuccessfully)
                {
                    PlayerPrefs.DeleteKey("UsuarioEliminar");
                }
                else
                {
                    
                }
            }
        }
        else
        {
        }
    }
}