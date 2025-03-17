using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class GuardarProgreso : MonoBehaviour
{
    public static GuardarProgreso Instance { get; private set; }

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        auth = FirebaseAuth.DefaultInstance ?? FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance ?? FirebaseFirestore.DefaultInstance;
    }


    public async void GuardarProgresoFirestore(int nivelActualJugado, int correctas, FirebaseAuth auth) //Agregar parametro de grupo
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId).Collection("grupos").Document("Metales Alcalinos"); //implementar el grupo del parametro
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        try
        {
            // Obtener datos actuales
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();
            int nivelAlmacenado = snapshotGrupo.Exists && snapshotGrupo.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;
            int xpActual = snapshotUsuario.Exists && snapshotUsuario.TryGetValue<int>("xp", out int xp) ? xp : 0;

            int xpGanado = correctas * 100;

            // 🔹 Si el usuario juega un nivel menor al suyo, gana la mitad de XP y NO sube de nivel
            if (nivelActualJugado < nivelAlmacenado)
            {
                xpGanado /= 2;
                Debug.Log("🔻 Jugaste un nivel menor, XP reducida a la mitad.");
            }

            bool subirNivel = nivelActualJugado >= nivelAlmacenado;
            int nuevoNivel = subirNivel ? nivelActualJugado : nivelAlmacenado;
            int nuevoXp = xpActual + xpGanado;

            // Guardar XP
            await docUsuario.SetAsync(new Dictionary<string, object> { { "xp", nuevoXp } }, SetOptions.MergeAll);

            // Guardar Nivel si sube
            if (subirNivel)
            {
                await docGrupo.SetAsync(new Dictionary<string, object> { { "nivel", nuevoNivel } }, SetOptions.MergeAll);
            }

            Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");

            // Guardar localmente en PlayerPrefs
            PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
            PlayerPrefs.SetInt("xp", nuevoXp);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
        }
    }
}
