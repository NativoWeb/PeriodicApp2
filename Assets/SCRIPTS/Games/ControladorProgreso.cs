using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

public class ControladorProgreso : MonoBehaviour
{
    public Slider progresoSlider;  // Asigna el Slider en el Inspector
    public int maxNiveles = 30;    // Total de niveles
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private Color colorProgreso = new Color(81f / 255f, 178f / 255f, 124f / 255f); // #51B27C

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        CargarProgreso();
    }

    async void CargarProgreso()
    {
        string userId = auth.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }

        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("Grupo 1");

        DocumentSnapshot snapshot = await docGrupo.GetSnapshotAsync();

        int nivelDesbloqueado = 1; // Nivel por defecto

        if (snapshot.Exists && snapshot.TryGetValue<int>("nivel", out int nivelGuardado))
        {
            nivelDesbloqueado = nivelGuardado;
        }

        Debug.Log($"🔹 Nivel desbloqueado: {nivelDesbloqueado}");

        // Actualizar el slider con el progreso
        ActualizarProgreso(nivelDesbloqueado);
    }

    void ActualizarProgreso(int nivelActual)
    {
        float progreso = (float)nivelActual / maxNiveles;
        progresoSlider.value = progreso;

        // Cambia el color del relleno directamente en el componente Slider
        progresoSlider.fillRect.GetComponent<Image>().color = new Color(81f / 255f, 178f / 255f, 124f / 255f); // #51B27C

        Debug.Log($"📊 Progreso actualizado: {progreso * 100}%");
    }

}
