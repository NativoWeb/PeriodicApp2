using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GuardarMisionFinalCompletada : MonoBehaviour
{
    public Button botonCompletarFinal; // Botón de la misión final

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        var user = auth.CurrentUser;
        if (user != null)
        {
            userId = user.UserId;
        }
        else
        {
            Debug.LogError("❌ No hay usuario autenticado.");
        }

        if (botonCompletarFinal != null)
        {
            botonCompletarFinal.onClick.AddListener(MarcarFinalComoCompletada);
        }

    }
    public void MarcarFinalComoCompletada()
    {
        string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (string.IsNullOrEmpty(categoria))
        {
            Debug.LogError("❌ No se encontró la categoría seleccionada.");
            return;
        }

        // Marcar localmente como completada
        string claveFinal = $"MisionFinal_{categoria}";
        PlayerPrefs.SetInt(claveFinal, 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Misión final de {categoria} marcada como completada.");

        // Actualizar JSON y otorgar XP
        ActualizarJSONFinal(categoria);
    }

    private async void ActualizarJSONFinal(string categoria)
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ JSON vacío.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Misiones_Categorias") || !json["Misiones_Categorias"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura JSON inválida.");
            return;
        }

        var categorias = json["Misiones_Categorias"]["Categorias"];

        if (!categorias.HasKey(categoria))
        {
            Debug.LogError($"❌ Categoría '{categoria}' no encontrada.");
            return;
        }

        // Verificar si ya está completada antes de hacer cualquier cosa
        bool yaCompletada = false;

        if (categorias[categoria].HasKey("Mision Final") &&
            categorias[categoria]["Mision Final"].HasKey("MisionFinal") &&
            categorias[categoria]["Mision Final"]["MisionFinal"].HasKey("completada"))
        {
            yaCompletada = categorias[categoria]["Mision Final"]["MisionFinal"]["completada"].AsBool;
        }

        if (yaCompletada)
        {
            Debug.Log($"⚠️ La misión final de la categoría '{categoria}' ya estaba completada. No se otorga XP ni se actualiza logro.");
            return;
        }

        // Marcar como completada
        if (!categorias[categoria].HasKey("Mision Final"))
            categorias[categoria]["Mision Final"] = new JSONObject();
        if (!categorias[categoria]["Mision Final"].HasKey("MisionFinal"))
            categorias[categoria]["Mision Final"]["MisionFinal"] = new JSONObject();

        categorias[categoria]["Mision Final"]["MisionFinal"]["completada"] = true;

        // Guardar el JSON actualizado
        PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
        PlayerPrefs.Save();

        Debug.Log($"✅ Misión final de la categoría '{categoria}' marcada como completada en el JSON.");

        // XP normales + XP por logro desbloqueado
        int xpMisionFinal = 30;
        int xpLogroCategoria = 20;
        int xpTotal = xpMisionFinal + xpLogroCategoria;

        string claveLogroCategoria = $"LogroCategoria_{categoria}";
        Debug.Log($"🎉 ¡Logro de la categoría '{categoria}' desbloqueado! +{xpLogroCategoria} XP extra");

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await SubirMisionesJSON();
            SumarXPFirebase(xpTotal);
        }
        else
        {
            SumarXPTemporario(xpTotal);
        }
    }


    void SumarXPTemporario(int xp)
    {
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);
        xpTemp += xp;
        PlayerPrefs.SetInt("TempXP", xpTemp);
        PlayerPrefs.Save();
        Debug.Log($"🔄 XP {xp} sumado temporalmente. Total TempXP: {xpTemp}");
    }

    async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = snapshot.Exists && snapshot.TryGetValue("xp", out int valor) ? valor : 0;
            int nuevoXP = xpActual + xp;
            await userRef.UpdateAsync("xp", nuevoXP);
            Debug.Log($"✅ XP actualizado: {nuevoXP}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al subir XP: {e.Message}");
        }
    }

    public async Task SubirMisionesJSON()
    {
        if (string.IsNullOrEmpty(userId)) return;

        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON", "{}");

        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        await misionesDoc.SetAsync(data, SetOptions.MergeAll);
        Debug.Log("✅ JSON de misiones final subido.");
    }
}