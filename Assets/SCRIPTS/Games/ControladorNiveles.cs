using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Auth;
using System.Threading.Tasks;

public class ControladorNiveles : MonoBehaviour
{
    public Button[] botonesNiveles; // Asigna los botones en el Inspector
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        CargarProgreso();
    }

    async void CargarProgreso()
    {
        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId)
                                      .Collection("grupos").Document("grupo 1");

        DocumentSnapshot snapshot = await docGrupo.GetSnapshotAsync();

        int nivelDesbloqueado = 1; // Nivel por defecto

        if (snapshot.Exists && snapshot.TryGetValue<int>("nivel", out int nivelGuardado))
        {
            nivelDesbloqueado = nivelGuardado;
        }

        Debug.Log($"🔹 Nivel desbloqueado en Firestore: {nivelDesbloqueado}");

        // Activar los botones según el nivel desbloqueado
        for (int i = 0; i < botonesNiveles.Length; i++)
        {
            botonesNiveles[i].interactable = (i < nivelDesbloqueado);
        }
    }
}
