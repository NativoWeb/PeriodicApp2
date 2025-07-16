using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class UpdateData : MonoBehaviour
{
    // variables firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;
    private DatabaseReference dbReference;

    // Estado
    private bool hayInternet = false;
    private bool isSyncing = false;
    private bool firebaseInicializado = false;

    // UI
    [SerializeField] GameObject m_NotificacionRegistroUI = null;
    [SerializeField] GameObject m_NotificacionLogueoUI = null;

    // Definición de ruta local, aunque ReportePathManager es mejor práctica.
    public static string ReportesPath => Path.Combine(Application.persistentDataPath, "ReportesEncuestas");

    void Start()
    {
        // 1. Empezamos la inicialización de Firebase.
        //    TODA la lógica que depende de Firebase debe estar dentro de la lambda.
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {

            // 2. Verificamos el resultado de la inicialización.
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                Debug.Log("✅ Firebase se ha inicializado correctamente.");
                firebaseInicializado = true;

                // 3. Ahora que Firebase está listo, podemos inicializar sus servicios.
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                currentUser = auth.CurrentUser; // Puede ser null si no hay nadie logueado.

                // 4. Ejecutar toda la lógica que requiere Firebase.
                HandleOnlineMode(); // Contiene la lógica de usuario, XP, encuestas, etc.
                SincronizarReportesPendientes(); // Sincroniza reportes locales.
            }
            else
            {
                Debug.LogError($"❌ No se pudieron resolver las dependencias de Firebase: {task.Result}");
                // Si Firebase falla, tratamos la app como si estuviera offline para esta sesión.
                firebaseInicializado = false;
            }
        });

        // 5. Tareas que NO dependen de Firebase pueden ejecutarse fuera, de forma paralela.
        //    Por ejemplo, copiar archivos JSON locales que no vienen de la nube.
        StartCoroutine(CheckAndCopyJsons());
    }

    // 🔹 Modo online (Ahora solo se llama si Firebase está OK)
    private async void HandleOnlineMode()
    {
        if (currentUser == null)
        {
            string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");
            if (estadoUsuario == "local") m_NotificacionRegistroUI.SetActive(true);
            else m_NotificacionLogueoUI.SetActive(true);
            return;
        }

        string userId = currentUser.UserId;
        PlayerPrefs.SetString("UserID", userId); // ¡IMPORTANTE! Guardar el UserID en PlayerPrefs
        Debug.Log($"Usuario autenticado: {userId}. Guardando en PlayerPrefs.");

        // *** LLAMADA EN EL LUGAR CORRECTO ***
        // Primero, descargamos los datos del usuario para tener la información más reciente.
        await GetUserData(userId);

        // Ahora, con los datos actualizados, podemos sincronizar el resto.
        await SincronizarEncuestasAsignadas(userId);

        // La sincronización de estados y XP puede hacerse después, ya que lee de PlayerPrefs.
        bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        if (estadoAprendizaje) await ActualizarEstadoEncuestaEnFirebase(userId, "EstadoEncuestaAprendizaje", estadoAprendizaje);

        bool estadoConocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;
        if (estadoConocimiento) await ActualizarEstadoEncuestaEnFirebase(userId, "EstadoEncuestaConocimiento", estadoConocimiento);

        await ActualizarXPEnFirebase(userId);
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        // Solo intentamos sincronizar si la app tiene foco Y Firebase ya está listo.
        if (hasFocus && firebaseInicializado)
        {
            Debug.Log("La aplicación recuperó el foco. Intentando sincronizar reportes.");
            SincronizarReportesPendientes();
        }
    }

    public async void SincronizarReportesPendientes()
    {
        if (isSyncing) return;
        if (Application.internetReachability == NetworkReachability.NotReachable) return;
        if (db == null)
        {
            Debug.LogWarning("Sincronización omitida: Firestore no está listo.");
            return;
        }

        isSyncing = true;
        Debug.Log("[Sincronización Firestore] Proceso iniciado.");

        string reportesPath = Path.Combine(Application.persistentDataPath, "ReportesEncuestas");
        if (!Directory.Exists(reportesPath)) { isSyncing = false; return; }

        List<string> archivosReporte = new List<string>(Directory.GetFiles(reportesPath, "*.json"));
        if (archivosReporte.Count == 0) { isSyncing = false; return; }

        Debug.Log($"[Sincronización Firestore] Se encontraron {archivosReporte.Count} reportes pendientes.");

        int subidosConExito = 0;
        int yaExistian = 0;
        int fallidos = 0;

        foreach (var filePath in archivosReporte)
        {
            try
            {
                string jsonContenido = await File.ReadAllTextAsync(filePath);
                ReporteIntento reporte = JsonUtility.FromJson<ReporteIntento>(jsonContenido);

                if (reporte == null || string.IsNullOrEmpty(reporte.idReporte))
                {
                    Debug.LogWarning($"[Sincronización] Archivo corrupto: {Path.GetFileName(filePath)}");
                    fallidos++;
                    continue;
                }

                DocumentReference docRef = db.Collection("reportes").Document(reporte.idReporte);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    File.Delete(filePath);
                    yaExistian++;
                }
                else
                {
                    Dictionary<string, object> reporteData = new Dictionary<string, object>
                {
                    { "idReporte", reporte.idReporte },
                    { "idEncuesta", reporte.idEncuesta },
                    { "idUsuario", reporte.idUsuario },
                    { "idComunidad", reporte.idComunidad },
                    { "fechaIntento", reporte.fechaIntento },
                    { "respuestasCorrectas", reporte.respuestasCorrectas },
                    { "totalPreguntas", reporte.totalPreguntas },
                    { "minimoParaAprobar", reporte.minimoParaAprobar },
                    { "resultadoFinal", reporte.resultadoFinal },
                    // ¡No incluimos el campo 'timestamp' aquí!
                };

                    // Subimos el diccionario limpio.
                    await docRef.SetAsync(reporteData);
                    // Y luego añadimos el timestamp del servidor por separado.
                    await docRef.UpdateAsync("timestamp", FieldValue.ServerTimestamp);

                    File.Delete(filePath);
                    subidosConExito++;
                    Debug.Log($"[Sincronización] Subido: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Sincronización] Fallo al procesar {Path.GetFileName(filePath)}: {ex.Message} \nStackTrace: {ex.StackTrace}");
                fallidos++;
            }
        }

        isSyncing = false;
        Debug.Log($"[Sincronización Firestore] Proceso finalizado. Subidos: {subidosConExito}, Ya existían: {yaExistian}, Fallidos: {fallidos}");
    }

    private async Task ActualizarEstadoEncuestaEnFirebase(string userId, string encuesta, bool estadoencuesta)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync(encuesta, estadoencuesta);
        Debug.Log($"Actualizado '{encuesta}' a '{estadoencuesta}' en Firebase.");
    }

    private async Task ActualizarXPEnFirebase(string userId)
    {
        int tempXP = PlayerPrefs.GetInt("TempXP", 0);
        int RachaXp = PlayerPrefs.GetInt("RachaXP", 0);
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                int xpActual = snapshot.GetValue<int>("xp");
                int nuevoXP = xpActual + RachaXp; // Solo sumamos la racha, TempXP parece ser el total ya.
                await userRef.UpdateAsync("xp", nuevoXP);
                Debug.Log($"XP actualizado en Firebase a: {nuevoXP}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al actualizar XP en Firebase: " + ex.Message);
        }
    }

    // Cambiado a Task para poder esperarlo en HandleOnlineMode
    public async Task GetUserData(string userId)
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                Debug.Log($"Descargando datos para el usuario {userId}...");

                Dictionary<string, object> data = snapshot.ToDictionary();

                // Usamos TryGetValue para evitar errores si un campo no existe
                if (data.TryGetValue("xp", out object xpObj)) PlayerPrefs.SetInt("TempXP", Convert.ToInt32(xpObj));
                if (data.TryGetValue("DisplayName", out object nameObj)) PlayerPrefs.SetString("DisplayName", nameObj.ToString());
                if (data.TryGetValue("EstadoEncuestaAprendizaje", out object eaObj)) PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", Convert.ToBoolean(eaObj) ? 1 : 0);
                if (data.TryGetValue("EstadoEncuestaConocimiento", out object ecObj)) PlayerPrefs.SetInt("EstadoEncuestaConocimiento", Convert.ToBoolean(ecObj) ? 1 : 0);
                if (data.TryGetValue("Ocupacion", out object ocuObj)) PlayerPrefs.SetString("TempOcupacion", ocuObj.ToString());
                if (data.TryGetValue("Rango", out object rangoObj)) PlayerPrefs.SetString("Rango", rangoObj.ToString());

                // Campos opcionales
                if (data.TryGetValue("Edad", out object edadObj)) PlayerPrefs.SetInt("Edad", Convert.ToInt32(edadObj));
                if (data.TryGetValue("Departamento", out object depObj)) PlayerPrefs.SetString("Departamento", depObj.ToString());
                if (data.TryGetValue("Ciudad", out object ciuObj)) PlayerPrefs.SetString("Ciudad", ciuObj.ToString());

                PlayerPrefs.Save(); // Forzar guardado de PlayerPrefs
                Debug.Log("Datos del usuario descargados y guardados en PlayerPrefs.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al traer los datos desde firebase: {ex.Message}");
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

    private async Task SincronizarEncuestasAsignadas(string UserId)
    {
        if (currentUser == null || db == null)
        {
            Debug.LogWarning("Usuario no autenticado o DB no inicializada. No se pueden sincronizar encuestas.");
            return;
        }

        string userId = currentUser.UserId;

        try
        {
            // 1. Crear el directorio base si no existe (sin cambios)
            string encuestasAsignadasPath = Path.Combine(Application.persistentDataPath, "EncuestasAsignadas");
            Directory.CreateDirectory(encuestasAsignadasPath);

            // 2. Buscar todas las comunidades donde el usuario actual es miembro.
            //    La colección "comunidades" y el campo "miembros" están correctos en minúsculas.
            //
            // <<< ¡ESTA ES LA LÍNEA QUE CAMBIA! >>>
            // En lugar de WhereEqualTo, usamos WhereArrayContains para buscar el userId dentro del array "miembros".
            Firebase.Firestore.Query queryComunidades = db.Collection("comunidades").WhereArrayContains("miembros", userId);

            QuerySnapshot comunidadesSnapshot = await queryComunidades.GetSnapshotAsync();

            // Este log ahora debería mostrar el número correcto.
            Debug.Log($"Encontradas {comunidadesSnapshot.Count} comunidades para el usuario.");

            // El resto del código funciona perfectamente sin cambios.
            foreach (DocumentSnapshot docComunidad in comunidadesSnapshot.Documents)
            {
                string nombreComunidad = docComunidad.GetValue<string>("nombre");
                if (string.IsNullOrEmpty(nombreComunidad))
                {
                    Debug.LogWarning($"Comunidad con ID {docComunidad.Id} no tiene campo 'nombre'. Se omitirá.");
                    continue;
                }

                string nombreCarpetaComunidad = SanitizarNombreArchivo(nombreComunidad);
                string pathCarpetaComunidad = Path.Combine(encuestasAsignadasPath, nombreCarpetaComunidad);
                Directory.CreateDirectory(pathCarpetaComunidad);

                Debug.Log($"Procesando comunidad: '{nombreComunidad}'");

                if (docComunidad.ToDictionary().TryGetValue("encuestasAsignadas", out object encuestasObj) && encuestasObj is Dictionary<string, object> encuestasMap)
                {
                    foreach (var parEncuesta in encuestasMap)
                    {
                        if (parEncuesta.Value is bool esAsignada && esAsignada)
                        {
                            string idEncuesta = parEncuesta.Key;
                            await DescargarYGuardarEncuesta(idEncuesta, pathCarpetaComunidad);
                        }
                    }
                }
            }
            Debug.Log("✅ Sincronización de encuestas asignadas finalizada.");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error catastrófico durante la sincronización de encuestas: {e.Message}\n{e.StackTrace}");
        }
    }

    private async Task DescargarYGuardarEncuesta(string idEncuesta, string pathDestino)
    {
        try
        {
            DocumentReference encuestaRef = db.Collection("Encuestas").Document(idEncuesta);
            DocumentSnapshot encuestaSnapshot = await encuestaRef.GetSnapshotAsync();

            if (encuestaSnapshot.Exists)
            {
                // 5. Convertir los datos del documento a un diccionario y luego a JSON
                Dictionary<string, object> datosEncuesta = encuestaSnapshot.ToDictionary();
                string jsonContenido = JsonConvert.SerializeObject(datosEncuesta, Formatting.Indented);

                // 6. Guardar el archivo JSON
                string nombreArchivo = $"{idEncuesta}.json";
                string pathCompleto = Path.Combine(pathDestino, nombreArchivo);
                await File.WriteAllTextAsync(pathCompleto, jsonContenido);

                Debug.Log($"    -> Descargada y guardada encuesta '{idEncuesta}' en '{pathCompleto}'");
            }
            else
            {
                Debug.LogWarning($"    -> No se encontró la encuesta con ID '{idEncuesta}' en la colección 'Encuestas'.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"    -> ❌ Error al descargar o guardar la encuesta '{idEncuesta}': {e.Message}");
        }
    }

    private string SanitizarNombreArchivo(string nombre)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            nombre = nombre.Replace(c, '_');
        }
        return nombre;
    }
}
