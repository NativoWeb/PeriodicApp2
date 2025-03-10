using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using TMPro;

public class GestorProgresoFirebase : MonoBehaviour
{
    public static GestorProgresoFirebase Instance; // Singleton para acceder desde otros scripts

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
        }

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    public async void GuardarProgreso(int nivelActualJugado, int correctas)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        DocumentReference docGrupo = db.Collection("users").Document(userId).Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        try
        {
            // Obtener datos actuales
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();

            int nivelAlmacenado = snapshotGrupo.Exists && snapshotGrupo.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;
            int xpActual = snapshotUsuario.Exists && snapshotUsuario.TryGetValue<int>("xp", out int xp) ? xp : 0;

            int xpGanado = correctas * 200;
            bool subirNivel = nivelActualJugado > nivelAlmacenado;

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
