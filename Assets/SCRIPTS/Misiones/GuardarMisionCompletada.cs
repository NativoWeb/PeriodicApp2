﻿using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
//using System.Drawing.Text;

public class GuardarMisionCompletada : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el botón desde el Inspector
    //public Transform contenedorMisiones; // Asigna el contenedor de misiones en el Inspector

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
                Debug.LogError("❌ No hay usuario autenticado en Start.");
            }

            if (botonCompletarMision != null)
            {
                botonCompletarMision.onClick.AddListener(MarcarMisionComoCompletada);
            }
            else
            {
                Debug.LogError("❌ botonCompletarMision no está asignado en el Inspector.");
            }
    }

    public void MarcarMisionComoCompletada()
    {
        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);

        if (string.IsNullOrEmpty(elemento) || idMision == -1)
        {
            Debug.LogError("❌ No se encontraron datos válidos en PlayerPrefs.");
            return;
        }

        string claveMision = $"Mision_{elemento}_{idMision}";
        PlayerPrefs.SetInt(claveMision, 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Misión {idMision} del elemento {elemento} marcada como completada.");

        ActualizarMisionEnJSON(elemento, idMision);
    }

    private async void ActualizarMisionEnJSON(string elemento, int idMision)
    {
        string jsonString = PlayerPrefs.GetString("misionesJSON", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("misiones") || !json["misiones"].HasKey(elemento))
        {
            Debug.LogError($"❌ No se encontró el elemento '{elemento}' en el JSON.");
            return;
        }

        var niveles = json["misiones"][elemento]["niveles"].AsArray;
        bool cambioRealizado = false;
        int xpGanado = 0;

        for (int i = 0; i < niveles.Count; i++)
        {
            var nivel = niveles[i];

            if (nivel["id"].AsInt == idMision)
            {
                nivel["completada"] = true;
                xpGanado = nivel["xp"].AsInt; // Obtener el XP de la misión
                cambioRealizado = true;
                break;
            }
        }

        if (cambioRealizado)
        {
            PlayerPrefs.SetString("misionesJSON", json.ToString());
            PlayerPrefs.Save();
            Debug.Log($"✅ JSON actualizado para la misión {idMision} del elemento {elemento}: {json}");

            // Verificar conexión a Internet
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                await SubirMisionesJSON();
                SumarXPFirebase(xpGanado);
            }
            else
            {
                SumarXPTemporario(xpGanado);
            }
        }
        else
        {
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
        }
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }

    async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;

            await userRef.UpdateAsync("xp", xpNuevo);
            Debug.Log($"✅ XP actualizado en Firebase: {xpNuevo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }

    public async Task SubirMisionesJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string jsonMisiones = PlayerPrefs.GetString("misionesJSON", "{}"); // Obtener el JSON de PlayerPrefs

        if (jsonMisiones == "{}")
        {
            Debug.LogWarning("⚠️ No hay datos de misiones guardados.");
            return;
        }

        // Convertir JSON a Dictionary para Firestore
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        // Subir a Firestore dentro del documento del usuario
        DocumentReference userDoc = db.Collection("users").Document(userId);

        await userDoc.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ Misiones JSON guardadas en Firestore.");
            }
            else
            {
                Debug.LogError("❌ Error al guardar el JSON en Firestore: " + task.Exception);
            }
        });
    }
}
