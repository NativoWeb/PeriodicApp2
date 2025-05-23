using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;

public class FirebaseUsuarioRepositorio :IUsuarioRepositorio
{
    private readonly FirebaseFirestore firestore;

    public FirebaseUsuarioRepositorio()
    {
        firestore = FirebaseFirestore.DefaultInstance;
    }

    public async Task ActualizarEstadoEncuestaAprendizajeAsync(string userId, bool estado)
    {
        var userRef = firestore.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaAprendizaje", estado);
    }

    public async Task<(bool, bool)> ObtenerEstadosEncuestasAsync(string userId)
    {
        var snapshot = await firestore.Collection("users").Document(userId).GetSnapshotAsync();
        bool aprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") && snapshot.GetValue<bool>("EstadoEncuestaAprendizaje");
        bool conocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") && snapshot.GetValue<bool>("EstadoEncuestaConocimiento");

        return (aprendizaje, conocimiento);
    }
}
