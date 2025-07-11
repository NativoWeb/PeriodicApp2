using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

public class UpdateData : MonoBehaviour
{
    // variables firebase 
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;

    // Internet
    private bool hayInternet = false;

    //panel para registro 
    [SerializeField] GameObject m_NotificacionRegistroUI = null;
    [SerializeField] GameObject m_NotificacionLogueoUI = null;


    void Start()
    {

        // Verificar conexión a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            // incializamos firebase
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;


            Debug.Log("⌛ Verificando conexión a Internet...desde UpdateData");
            HandleOnlineMode();
            StartCoroutine(CheckAndCopyJsons());
        }
        else
        {
            Debug.Log("No es posible actualizar datos por el momento, el progreso se cargará cuando tengas conexión a internet... desde UpdateData");
        }
        GetuserData();
    }


    // 🔹 Modo online
    private  void HandleOnlineMode() // ----------------------------------------------------------------------------------
    {

        string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");

        if (estadoUsuario == "local")
        {
            m_NotificacionRegistroUI.SetActive(true); // si tiene wifi y el usuario no esta en la nube, lo mandamos a registrarse
        }
        else if (estadoUsuario == "sinloguear")
        {
            m_NotificacionLogueoUI.SetActive(true);
        }
        else if (estadoUsuario == "nube")
        {
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            if (estadoencuestaaprendizaje)
            {
                ActualizarEstadoEncuestaEnFirebase(userId,"EstadoEncuestaAprendizaje", estadoencuestaaprendizaje);
            }
            if (estadoencuestaconocimiento)
            {
                ActualizarEstadoEncuestaEnFirebase(userId, "EstadoEncuestaConocimiento", estadoencuestaconocimiento);
            }
            
            ActualizarXPEnFirebase(userId);
        }

    }

    public string[] jsonFileNames = { "Json_Misiones.json", "Json_logros.json", "Json_Informacion.json", "categorias_encuesta_firebase.json", "Json_Informacion_en.json" };

    public IEnumerator CheckAndCopyJsons()
    {
        string persistentDataPath = Application.persistentDataPath;

        foreach (string fileName in jsonFileNames)
        {
            string filePath = Path.Combine(persistentDataPath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.Log($"El archivo {fileName} no existe en {persistentDataPath}.");

                if (fileName == "categorias_encuesta_firebase.json")
                {
                    // Intentar cargar desde PlayerPrefs
                    string jsonFromPlayerPrefs = PlayerPrefs.GetString("categorias_encuesta_firebase_json", "");

                    if (!string.IsNullOrEmpty(jsonFromPlayerPrefs))
                    {
                        try
                        {
                            File.WriteAllText(filePath, jsonFromPlayerPrefs);
                            Debug.Log($"✅ {fileName} copiado desde PlayerPrefs a {filePath}");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"❌ Error al copiar {fileName} desde PlayerPrefs a {filePath}: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No se encontró {fileName} en PlayerPrefs.");

                        // Definir la ruta completa en StreamingAssets
                        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, fileName);

                        UnityWebRequest request = UnityWebRequest.Get(streamingAssetsPath);
                        yield return request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                        {
                            Debug.LogError($"❌ Error al leer {fileName} desde StreamingAssets: {request.error}");
                            continue; // Saltar al siguiente archivo
                        }

                        string fileContent = request.downloadHandler.text;

                        if (!string.IsNullOrEmpty(fileContent))
                        {
                            try
                            {
                                File.WriteAllText(filePath, fileContent);
                                Debug.Log($"✅ {fileName} copiado desde StreamingAssets a {filePath}");
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError($"❌ Error al copiar {fileName} desde StreamingAssets a {filePath}: {e.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ No se encontró contenido en {fileName} dentro de StreamingAssets.");
                        }
                    }
                }
                else
                {
                    Debug.Log($"✅ El archivo {fileName} ya existe en {persistentDataPath}.");
                }
            }
        }
    }

private async void ActualizarEstadoEncuestaEnFirebase(string userId,string encuesta, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync(encuesta, estadoencuesta);
    }


    private async void ActualizarXPEnFirebase(string userId)
    {
        int tempXP = PlayerPrefs.GetInt("TempXP", 0); // Obtener XP temporal
        int RachaXp = PlayerPrefs.GetInt("RachaXP", 0); // Obtener XP temporal
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            // Obtener el XP actual de Firebase
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                int xpActual = snapshot.GetValue<int>("xp"); // XP actual en Firebase
                tempXP = xpActual + RachaXp;
                int nuevoXP = tempXP; // Actualiza XP
                // Actualizar el XP en Firebase
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "xp", nuevoXP }
            };

                await userRef.UpdateAsync(updates);
                Debug.Log($"nuevoo xpppppppppDiccionario:{updates}");
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
            Debug.Log("Error al obtener xp desde updatedata Firebase:" + ex.Message);
        }
    }

    private async void GetuserData()
    {
        // verificar la conexion a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            DocumentReference userRef = db.Collection("users").Document(userId);

            try
            {
                // Obtener el XP actual de Firebase
                DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    int xpActual = snapshot.GetValue<int>("xp"); // XP actual en Firebase
                    PlayerPrefs.SetInt("TempXP", xpActual);
                    Debug.Log($"el xp traido desde UpdateData es de: {xpActual}");
                    string username = snapshot.GetValue<string>("DisplayName");
                    PlayerPrefs.SetString("DisplayName", username);
                    bool estadoencuestaaprendizaje = snapshot.GetValue<bool>("EstadoEncuestaAprendizaje");
                    PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", estadoencuestaaprendizaje ? 1 : 0);
                    bool estadoencuestaconocimiento = snapshot.GetValue<bool>("EstadoEncuestaConocimiento");
                    PlayerPrefs.SetInt("EstadoEncuestaConocimiento", estadoencuestaconocimiento ? 1 : 0);
                    string ocupacion = snapshot.GetValue<string>("Ocupacion");
                    PlayerPrefs.SetString("TempOcupacion", ocupacion);
                    string rango = snapshot.GetValue<string>("Rango");
                    PlayerPrefs.SetString("Rango", rango);

                    // Verificamos si los campos existen ----------------------------- para guardarlos en playerprefs

                    Dictionary<string, object> datos = snapshot.ToDictionary();
                    bool tieneEdad = datos.ContainsKey("Edad");
                    bool tieneDepartamento = datos.ContainsKey("Departamento");
                    bool tieneCiudad = datos.ContainsKey("Ciudad");

                    if (tieneEdad && tieneDepartamento && tieneCiudad)
                    {
                        int edad = snapshot.GetValue<int>("Edad");
                        PlayerPrefs.SetInt("Edad", edad);
                        string departamento = snapshot.GetValue<string>("Departamento");
                        PlayerPrefs.SetString("Departamento", departamento);
                        string ciudad = snapshot.GetValue<string>("Ciudad");
                        PlayerPrefs.SetString("Ciudad", ciudad);

                        
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error al traer los datos desde firebase: {ex.Message}");
            }
        }
        else
        {
        }
    }
}
