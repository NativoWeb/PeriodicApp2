using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase;
using Unity.Properties;

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

        VerificarPrimeraEjecucion();
        StartCoroutine(CheckInternetConnection());
        StartCoroutine(DeleteAccount());
    }

    private void VerificarPrimeraEjecucion()
    {
        bool esPrimeraVez = PlayerPrefs.GetInt("isFirstRun", 1) == 1;

        if (esPrimeraVez)
        {
            Debug.Log("[StartApp] Primera ejecución detectada: limpiando datos");

            PlayerPrefs.DeleteAll();

            try
            {
                if (auth != null) auth.SignOut();
                if (FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    FirebaseAuth.DefaultInstance.SignOut();
                    Debug.Log("[StartApp] Sesión de Firebase cerrada");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[StartApp] Error cerrando sesión Firebase: " + e.Message);
            }

            PlayerPrefs.SetInt("isFirstRun", 0);
            PlayerPrefs.Save();
        }
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
            // Offline: usar datos guardados localmente, NO intentar Firebase auth (requiere internet y borra sesión si falla)
            NavegueConDatosLocales();
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


    // Navega usando datos guardados en PlayerPrefs (para usuarios "nube" sin internet)
    void NavegueConDatosLocales()
    {
        string ocupacion = PlayerPrefs.GetString("TempOcupacion", "").Trim();
        bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoConocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        Debug.Log($"[StartApp] Offline con sesión guardada. Ocupacion={ocupacion}, Aprendizaje={estadoAprendizaje}, Conocimiento={estadoConocimiento}");

        if (ocupacion == "Profesor")
        {
            SceneManager.LoadScene("InicioProfesor1");
        }
        else if (ocupacion == "Estudiante")
        {
            if (estadoAprendizaje && estadoConocimiento)
            {
                SceneManager.LoadScene("Inicio");
            }
            else
            {
                SceneManager.LoadScene("SeleccionarEncuesta");
            }
        }
        else
        {
            // Sin datos de ocupación guardados localmente (nunca se logueó con internet antes)
            LoadSceneIfNotAlready("Login");
        }
    }

    // 🔹 Modo online
    void HandleOnlineMode()
    {
        if (yaVerificado) return;

        yaVerificado = true;

        string EstadoUsuario = PlayerPrefs.GetString("Estadouser","");
        // estos debugs no son error pero los pongo pa verlo en el cel
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

        if (PlayerPrefs.GetInt("rememberMe", 0) != 1)
        {
            Debug.LogWarning("🛑 rememberMe no está activo, cancelando AutoLogin");
            return;
        }

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
                    Debug.LogError("[StartApp] Falló el login automático: " + task.Exception?.Message);
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("Start");
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

        // Si ya existe un archivo local (puede tener completaciones offline),
        // subir el local a Firebase y usarlo directamente. No sobreescribir con Firebase.
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, "Json_Misiones.json");
        if (System.IO.File.Exists(filePath))
        {
            Debug.Log("[StartApp] Archivo local Json_Misiones.json encontrado. Subiendo a Firebase y usando datos locales.");
            _ = SubirMisionesLocalesAFirebase(userId, filePath);
            CheckUserStatus(userId);
            return;
        }

        // No hay archivo local — descargar desde Firebase
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");

        misionesDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[StartApp] Fallo al obtener snapshot de misiones: " + task.Exception?.Message);
                CheckUserStatus(userId);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists || !snapshot.ContainsField("misiones"))
            {
                Debug.Log("[StartApp] No hay misiones guardadas en Firebase, usando datos locales.");
                CheckUserStatus(userId);
                return;
            }

            string misionesJson = snapshot.GetValue<string>("misiones");
            Debug.Log("[StartApp] Misiones descargadas de Firebase.");

            if (!string.IsNullOrEmpty(misionesJson))
            {
                // Guardar en el archivo que usa GestorMisiones y GuardarMisionCompletada
                try
                {
                    System.IO.File.WriteAllText(filePath, misionesJson);
                    Debug.Log("[StartApp] Json_Misiones.json restaurado desde Firebase.");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[StartApp] No se pudo escribir Json_Misiones.json: " + e.Message);
                }
                // Clave correcta usada por todo el sistema
                PlayerPrefs.SetString("misionesCategoriasJSON", misionesJson);
                PlayerPrefs.Save();
            }

            CheckUserStatus(userId);
        });
    }

    private async System.Threading.Tasks.Task SubirMisionesLocalesAFirebase(string userId, string filePath)
    {
        try
        {
            string jsonMisiones = System.IO.File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(jsonMisiones)) return;

            DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "misiones", jsonMisiones },
                { "timestamp", FieldValue.ServerTimestamp }
            };
            await misionesDoc.SetAsync(data, SetOptions.MergeAll);
            Debug.Log("[StartApp] Misiones locales subidas a Firebase correctamente.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[StartApp] No se pudieron subir misiones locales a Firebase: " + e.Message);
        }
    }


    private void CheckUserStatus(string userId)
    {
        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("[StartApp] Error obteniendo datos de usuario: " + task.Exception?.Message);
                auth.SignOut();
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SceneManager.LoadScene("Start");
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                Debug.LogWarning("[StartApp] Documento de usuario no existe en Firestore. Limpiando sesión.");
                auth.SignOut();
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                SceneManager.LoadScene("Start");
                return;
            }

            string ocupacion = snapshot.GetValue<string>("Ocupacion");

            bool estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;

            bool estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;

            // Guardar en PlayerPrefs para poder navegar offline en sesiones futuras sin internet
            PlayerPrefs.SetString("TempOcupacion", ocupacion);
            PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", estadoencuestaaprendizaje ? 1 : 0);
            PlayerPrefs.SetInt("EstadoEncuestaConocimiento", estadoencuestaconocimiento ? 1 : 0);
            PlayerPrefs.Save();

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