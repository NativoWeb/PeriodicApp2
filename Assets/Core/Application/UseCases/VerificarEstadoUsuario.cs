using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        var docRef = Firebase.Firestore.FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
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
            SceneManager.LoadScene("InicioProfesor");
        }
        else if (ocupacion == "Estudiante")
        {
            if (estadoAprendizaje && estadoConocimiento)
                SceneManager.LoadScene("Inicio");
            else
                SceneManager.LoadScene("SeleccionarEncuesta");
        }
    }
}
