using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Firebase.Firestore;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Runtime.CompilerServices;
using Firebase.Extensions;
using System.Threading.Tasks;


public class UpdateData : MonoBehaviour
{

    // variables firebase 
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;
    // Internet
    private bool hayInternet = false;

    //panel para registro 
    [SerializeField] GameObject m_NotificacionRegistroUI = null;
    [SerializeField] GameObject m_NotificacionLogueoUI = null;


    void Start()
    {
    
        // Verificar conexión a internet
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            // incializamos firebase
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
           

            Debug.Log("⌛ Verificando conexión a Internet...desde UpdateData");
            HandleOnlineMode();
         
        }
        else
        {
            Debug.Log("No es posible actualizar datos por el momento, el progreso se cargará cuando tengas conexión a internet... desde UpdateData");
        }
    }


    // 🔹 Modo online
    private async void HandleOnlineMode() // ----------------------------------------------------------------------------------
    {
    
        string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");

        if (estadoUsuario == "local")
        {
            m_NotificacionRegistroUI.SetActive(true); // si tiene wifi y el usuario no esta en la nube, lo mandamos a registrarse
        }
        else if ( estadoUsuario == "sinloguear")
        {
            m_NotificacionLogueoUI.SetActive(true);
        }
        else if (estadoUsuario == "nube")
        {
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

            if (estadoencuestaaprendizaje == true)
            {
                PlayerPrefs.SetInt("EstadoEncuestaAprendizaje", 1);
            }
            else if (estadoencuestaconocimiento == true)
            {
                PlayerPrefs.SetInt("EstadoEncuestaConocimiento", 1);

            }

            ActualizarEstadoEncuestaAprendizaje(userId, estadoencuestaaprendizaje);
            ActualizarEstadoEncuestaConocimiento(userId, estadoencuestaconocimiento);
            await SubirDatosJSON();
            ActualizarXPEnFirebase(userId);
        }
        
    }


    private async void ActualizarEstadoEncuestaAprendizaje(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaAprendizaje", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Aprendizaje... {userId}: {estadoencuesta} desde UpdateData");
    }

    private async void ActualizarEstadoEncuestaConocimiento(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaConocimiento", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Conocimiento... {userId}: {estadoencuesta} desde UpdateData");
    }


    private async void ActualizarXPEnFirebase(string userId)
    {
        int tempXP = PlayerPrefs.GetInt("TempXP", 0); // Obtener XP temporal
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            // Obtener el XP actual de Firebase
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                int xpActual = snapshot.GetValue<int>("xp"); // XP actual en Firebase

                int nuevoXP = xpActual + tempXP; // Sumar XP

                // Actualizar el XP en Firebase
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "xp", nuevoXP }
            };

                await userRef.UpdateAsync(updates);
                Debug.Log($"✅ XP actualizado en Firebase para {userId}: {nuevoXP}");

                PlayerPrefs.SetInt("TempXP", 0); // Reiniciar XP temporal
            }
            else
            {
                Debug.LogWarning("⚠️ No se encontró el usuario en Firebase.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }
    public async Task SubirDatosJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        // Obtener JSON de misiones y categorías desde PlayerPrefs
        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON", "{}");
        string jsonCategorias = PlayerPrefs.GetString("CategoriasOrdenadas", "{}");

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");
        DocumentReference categoriasDoc = db.Collection("users").Document(userId).Collection("datos").Document("categorias");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (jsonMisiones != "{}")
        {
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (jsonCategorias != "{}")
        {
            Dictionary<string, object> dataCategorias = new Dictionary<string, object>
        {
            { "categorias", jsonCategorias },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(categoriasDoc.SetAsync(dataCategorias, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay datos de misiones ni categorías para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);

        Debug.Log("✅ Datos de misiones y categorías subidos en documentos separados.");
    }

}
