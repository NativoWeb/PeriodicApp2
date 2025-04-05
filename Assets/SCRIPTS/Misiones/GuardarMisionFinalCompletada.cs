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

        string claveFinal = $"MisionFinal_{categoria}";
        PlayerPrefs.SetInt(claveFinal, 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Misión final de {categoria} marcada como completada.");

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

        categorias[categoria]["misionFinalCompletada"] = true;
        PlayerPrefs.SetString("misionesCategoriasJSON", json.ToString());
        PlayerPrefs.Save();

        Debug.Log($"✅ JSON actualizado para misión final de {categoria}.");

        int xp = PlayerPrefs.GetInt("xp_mision_final", 200); // Puedes cambiar el valor por defecto

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await SubirMisionesJSON();
            SumarXPFirebase(xp);
        }
        else
        {
            SumarXPTemporario(xp);
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
