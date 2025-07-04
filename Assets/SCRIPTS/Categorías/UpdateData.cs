using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


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
        StartCoroutine(CheckAndCopyJsons());

        // Verificar conexión a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            // incializamos firebase
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;

            Debug.Log("⌛ Verificando conexión a Internet...desde UpdateData");
            HandleOnlineMode();
        }
        else
        {
            Debug.Log("No es posible actualizar datos por el momento, el progreso se cargará cuando tengas conexión a internet... desde UpdateData");
        }
        LLamarVerificarCategorias();
        GetuserData();
    }

    private async Task LLamarVerificarCategorias()
    {
        currentUser = auth.CurrentUser;
        userId = currentUser.UserId;

        await VerificarOCrearCategoriasEnFirestore(userId);
    }

    // 🔹 Modo online
    private async void HandleOnlineMode() // ----------------------------------------------------------------------------------
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

    private IEnumerator CheckAndCopyJsons()
    {
        string persistentDataPath = Application.persistentDataPath;

        foreach (string fileName in jsonFileNames)
        {
            string filePath = Path.Combine(persistentDataPath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.Log($"🔍 El archivo {fileName} no existe en {persistentDataPath}.");

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
                            Debug.LogError($"❌ Error al copiar {fileName} desde PlayerPrefs: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No se encontró {fileName} en PlayerPrefs.");
                    }
                }
                else
                {
                    // Quitar extensión para Resources.Load
                    string resourceFileName = Path.GetFileNameWithoutExtension(fileName);
                    TextAsset resourceJson = Resources.Load<TextAsset>($"Plantillas_Json/{ resourceFileName}");

                    if (resourceJson != null)
                    {
                        try
                        {
                            File.WriteAllText(filePath, resourceJson.text);
                            Debug.Log($"✅ {fileName} copiado desde Resources a {filePath}");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"❌ Error al escribir {fileName} desde Resources: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No se encontró {fileName} en la carpeta Resources.");
                    }
                }
            }
            else
            {
                Debug.Log($"✔️ El archivo {fileName} ya existe en {persistentDataPath}");
            }
        }

        yield return null;
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
                int tempXp = xpActual + RachaXp;
                int nuevoXP = tempXP; // Actualiza XP

                // Actualizar el XP en Firebase
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "xp", nuevoXP }
            };

                await userRef.UpdateAsync(updates);
            }
            else
            {
            }
        }
        catch (Exception e)
        {
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
            catch (Exception )
            {
            }
        }
    }

    public async Task VerificarOCrearCategoriasEnFirestore(string userId)
    {
        Debug.Log("📁 Iniciando verificación/creación de categorías en Firestore...");

        // — 1) Cargar JSON (PlayerPrefs o archivo) —
        string json = PlayerPrefs.GetString("categorias_encuesta_firebase_json", null);
        if (string.IsNullOrEmpty(json))
        {
            string path = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
                Debug.Log($"📂 JSON cargado desde archivo: {path}");
            }
            else
            {
                Debug.LogError("❌ No hay JSON de categorías en PlayerPrefs ni en archivo.");
                return;
            }
        }
        else
        {
            Debug.Log("🗄️ JSON cargado desde PlayerPrefs.");
        }

        // — 2) Parsear con JObject y extraer el JArray —
        JToken rootToken;
        try
        {
            rootToken = JToken.Parse(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ JSON inválido: " + ex.Message);
            return;
        }

        var categoriasToken = rootToken["categorias"] as JArray;
        if (categoriasToken == null)
        {
            Debug.LogError("❌ No se encontró la propiedad 'categorias' en el JSON.");
            return;
        }

        // — 3) Convertir cada item en Dictionary<string, object> —
        var listaCategorias = new List<object>();
        foreach (var item in categoriasToken)
        {
            // esto convierte {"Titulo": "...", "Descripcion": "...", "Porcentaje": 10.39}
            // en un Dictionary<string, object>
            var dictItem = item.ToObject<Dictionary<string, object>>();
            listaCategorias.Add(dictItem);
        }

        // — 4) Preparar el payload para Firestore —
        var payload = new Dictionary<string, object>
    {
        { "categorias", listaCategorias }
    };

        // — 5) Subir a Firestore si no existe —
        var docRef = FirebaseFirestore.DefaultInstance
            .Collection("users").Document(userId)
            .Collection("datos").Document("categorias");

        try
        {
            var snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                Debug.Log("✅ El documento de categorías ya existe en Firestore.");
            }
            else
            {
                await docRef.SetAsync(payload);
                Debug.Log("🆕 Categorías subidas a Firestore con la estructura JSON original.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Error al subir categorías a Firestore: " + e.Message);
        }
    }
}
