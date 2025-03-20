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

    private async void Awake()
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

        // Inicializar Firebase correctamente
        await FirebaseApp.CheckAndFixDependenciesAsync();
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    public async void GuardarProgresoFirestore(int nivelActualJugado, int correctas, FirebaseAuth auth)
    {
        // ⚠️ Verificar conexión a Internet
        if (ConnectionManager.Instance == null)
        {
            Debug.LogError("❌ ConnectionManager no ha sido inicializado.");
            return;
        }

        if (!ConnectionManager.Instance.IsConnectedToInternet())
        {
            Debug.LogWarning("⚠️ No hay conexión a Internet. Se otorgarán XP localmente.");

            return;
        }

        // ⚙️ Si hay conexión, continuar con lo que ya tenías
        if (auth == null || auth.CurrentUser == null)
        {
            Debug.LogError("❌ FirebaseAuth no está inicializado o el usuario no ha iniciado sesión.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docGrupo = db.Collection("users").Document(userId).Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();
            int nivelAlmacenado = snapshotGrupo.Exists && snapshotGrupo.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;
            int xpActual = snapshotUsuario.Exists && snapshotUsuario.TryGetValue<int>("xp", out int xp) ? xp : 0;

            int xpGanado = correctas * 100;

            if (nivelActualJugado < nivelAlmacenado)
            {
                xpGanado /= 2;
                Debug.Log("🔻 Jugaste un nivel menor, XP reducida a la mitad.");
            }

            bool subirNivel = nivelActualJugado >= nivelAlmacenado;
            int nuevoNivel = subirNivel ? nivelActualJugado : nivelAlmacenado;
            int nuevoXp = xpActual + xpGanado;

            await docUsuario.SetAsync(new Dictionary<string, object> { { "xp", nuevoXp } }, SetOptions.MergeAll);

            if (subirNivel)
            {
                await docGrupo.SetAsync(new Dictionary<string, object> { { "nivel", nuevoNivel } }, SetOptions.MergeAll);
            }

            Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");

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
