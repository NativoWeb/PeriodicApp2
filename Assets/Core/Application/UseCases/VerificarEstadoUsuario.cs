using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using PeriodicApp.Core.Application;
using PeriodicApp.Core.Application.Interfaces;
using PeriodicApp.Core.Domain.Interfaces;

public class VerificarEstadoUsuario
{
    private readonly IServicioFirestore firestoreService;
    private readonly IPlayerPrefsService _playerPrefs;
    private readonly INetworkService _networkService;
    private readonly ILoggingService _logger;
    private readonly ISceneService _sceneService;
    private readonly IPersistenceService _persistenceService;
    private readonly IResourceLoaderService _resourceLoader;

    public VerificarEstadoUsuario(
        IServicioFirestore firestoreService,
        IPlayerPrefsService playerPrefs,
        INetworkService networkService,
        ILoggingService logger,
        ISceneService sceneService,
        IPersistenceService persistenceService,
        IResourceLoaderService resourceLoader)
    {
        this.firestoreService = firestoreService;
        _playerPrefs = playerPrefs;
        _networkService = networkService;
        _logger = logger;
        _sceneService = sceneService;
        _persistenceService = persistenceService;
        _resourceLoader = resourceLoader;
    }

    public async Task Ejecutar(string userId)
    {
        bool hayInternet = _networkService.IsConnected();

        var docRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            _logger.LogError("No se encontraron datos para este usuario.");
            return;
        }

        string ocupacion = snapshot.GetValue<string>("Ocupacion");
        bool estadoAprendizaje = hayInternet
            ? snapshot.ContainsField("EstadoEncuestaAprendizaje") && snapshot.GetValue<bool>("EstadoEncuestaAprendizaje")
            : _playerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;

        bool estadoConocimiento = hayInternet
            ? snapshot.ContainsField("EstadoEncuestaConocimiento") && snapshot.GetValue<bool>("EstadoEncuestaConocimiento")
            : _playerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        _logger.Log($"Usuario: {ocupacion}, Aprendizaje: {estadoAprendizaje}, Conocimiento: {estadoConocimiento}");

        if (ocupacion == "Profesor")
        {
            _sceneService.LoadScene("InicioProfesor1");
        }
        else if (ocupacion == "Estudiante")
        {
            if (estadoAprendizaje && estadoConocimiento)
            {
                // Descargar progreso antes de redirigir
                await DescargarProgreso(userId);
                _sceneService.LoadScene("Inicio");
            }
            else
            {
                _sceneService.LoadScene("SeleccionarEncuesta");
            }
        }
    }
    private async Task DescargarProgreso(string userId)
    {
        await DescargarDocumentoYGuardar(userId, "categorias", "categorias_encuesta_firebase.json");
        await DescargarDocumentoYGuardar(userId, "misiones", "Json_Misiones.json");
        await DescargarDocumentoYGuardar(userId, "logros", "Json_Logros.json");

        // ✅ Verificar si Json_Informacion.json ya existe en persistentDataPath
        string nombreArchivo = "Json_Informacion.json";
        string rutaLocal = Path.Combine(_persistenceService.GetPersistentDataPath(), nombreArchivo);

        if (!File.Exists(rutaLocal))
        {
            string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
            string contenidoRecurso = _resourceLoader.LoadTextAsset($"Plantillas_Json/{nombreSinExtension}");


            if (contenidoRecurso != null)
            {
                File.WriteAllText(rutaLocal, contenidoRecurso);
                _logger.Log($"✅ Archivo auxiliar '{nombreArchivo}' copiado desde Resources.");
            }
            else
            {
                _logger.LogError($"❌ No se encontró '{nombreArchivo}' en Resources/Plantillas_Json.");
            }
        }
        else
        {
            _logger.Log($"📁 El archivo auxiliar '{nombreArchivo}' ya existe localmente.");
        }
    }

    private async Task DescargarDocumentoYGuardar(string userId, string nombreDocumento, string nombreArchivo)
    {
        var docRef = FirebaseFirestore.DefaultInstance
            .Collection("users").Document(userId)
            .Collection("datos").Document(nombreDocumento);

        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            _logger.LogWarning($"⚠️ No se encontró el documento '{nombreDocumento}'.");
            return;
        }

        // 1) Traemos todos los campos del doc en un diccionario
        var data = snapshot.ToDictionary();

        string contenidoAEscribir = null;

        // 2) Buscamos el primer campo que sea un string y parezca JSON
        foreach (var kv in data)
        {
            if (kv.Value is string s)
            {
                var t = s.TrimStart();
                if (t.StartsWith("{") || t.StartsWith("["))
                {
                    contenidoAEscribir = s;
                    _logger.Log($"📑 Extrayendo JSON desde el campo '{kv.Key}'.");
                    break;
                }
            }
        }

        // 3) Si no había campo JSON-texto, serializamos todo el diccionario
        if (contenidoAEscribir == null)
        {
            contenidoAEscribir = Newtonsoft.Json.JsonConvert
                .SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            _logger.Log("🗄️ Ningún campo era JSON-texto. Serializando el diccionario completo.");
        }

        // 4) Guardamos en disco
        string ruta = Path.Combine(_persistenceService.GetPersistentDataPath(), nombreArchivo);
        File.WriteAllText(ruta, contenidoAEscribir);
        _logger.Log($"✅ Documento '{nombreDocumento}' guardado en: {ruta}");
    }

    // Usamos un envoltorio para convertir objetos genéricos en JSON
    [System.Serializable]
    private class Wrapper
    {
        public object datos;

        public Wrapper(object datos)
        {
            this.datos = datos;
        }
    }
}
