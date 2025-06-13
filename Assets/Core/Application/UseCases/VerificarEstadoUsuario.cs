using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using Firebase.Firestore;
using System.Collections.Generic;

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

        Dictionary<string, object> data = snapshot.ToDictionary();

        // Serializamos con Newtonsoft.Json para manejar estructuras anidadas correctamente
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
        string ruta = Path.Combine(Application.persistentDataPath, nombreArchivo);
        File.WriteAllText(ruta, json);
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
