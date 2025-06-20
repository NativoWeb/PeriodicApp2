using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;

public class VerificarEstadoUsuario
{
    private readonly IServicioFirestore firestoreService;

    public VerificarEstadoUsuario(IServicioFirestore firestoreService)
    {
        this.firestoreService = firestoreService;
    }

    public async Task Ejecutar(string userId)
    {
        bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        var docRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No se encontraron datos para este usuario.");
            return;
        }

        string ocupacion = snapshot.GetValue<string>("Ocupacion");
        bool estadoAprendizaje = hayInternet
            ? snapshot.ContainsField("EstadoEncuestaAprendizaje") && snapshot.GetValue<bool>("EstadoEncuestaAprendizaje")
            : PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;

        bool estadoConocimiento = hayInternet
            ? snapshot.ContainsField("EstadoEncuestaConocimiento") && snapshot.GetValue<bool>("EstadoEncuestaConocimiento")
            : PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        Debug.Log($"Usuario: {ocupacion}, Aprendizaje: {estadoAprendizaje}, Conocimiento: {estadoConocimiento}");

        if (ocupacion == "Profesor")
        {
            SceneManager.LoadScene("InicioProfesor1");
        }
        else if (ocupacion == "Estudiante")
        {
            if (estadoAprendizaje && estadoConocimiento)
            {
                // Descargar progreso antes de redirigir
                await DescargarProgreso(userId);
                SceneManager.LoadScene("Inicio");
            }
            else
            {
                SceneManager.LoadScene("SeleccionarEncuesta");
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
        string rutaLocal = Path.Combine(Application.persistentDataPath, nombreArchivo);

        if (!File.Exists(rutaLocal))
        {
            string nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
            TextAsset archivoJson = Resources.Load<TextAsset>($"Plantillas_Json/{nombreSinExtension}");

            if (archivoJson != null)
            {
                File.WriteAllText(rutaLocal, archivoJson.text);
                Debug.Log($"✅ Archivo auxiliar '{nombreArchivo}' copiado desde Resources.");
            }
            else
            {
                Debug.LogError($"❌ No se encontró '{nombreArchivo}' en Resources/Plantillas_Json.");
            }
        }
        else
        {
            Debug.Log($"📁 El archivo auxiliar '{nombreArchivo}' ya existe localmente.");
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
            Debug.LogWarning($"⚠️ No se encontró el documento '{nombreDocumento}'.");
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
                    Debug.Log($"📑 Extrayendo JSON desde el campo '{kv.Key}'.");
                    break;
                }
            }
        }

        // 3) Si no había campo JSON-texto, serializamos todo el diccionario
        if (contenidoAEscribir == null)
        {
            contenidoAEscribir = Newtonsoft.Json.JsonConvert
                .SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            Debug.Log("🗄️ Ningún campo era JSON-texto. Serializando el diccionario completo.");
        }

        // 4) Guardamos en disco
        string ruta = Path.Combine(Application.persistentDataPath, nombreArchivo);
        File.WriteAllText(ruta, contenidoAEscribir);
        Debug.Log($"✅ Documento '{nombreDocumento}' guardado en: {ruta}");
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
