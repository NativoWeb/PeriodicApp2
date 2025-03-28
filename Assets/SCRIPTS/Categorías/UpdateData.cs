﻿using UnityEngine;
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
    void HandleOnlineMode() // ----------------------------------------------------------------------------------
    {
    
        string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");

        if (estadoUsuario == "local")
        {
            m_NotificacionRegistroUI.SetActive(true); // si tiene wifi y el usuario no esta en la nube, lo mandamos a registrarse
        }

        else if (estadoUsuario == "nube")
        {
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            bool encuestacompletada = PlayerPrefs.GetInt("TempEncuestaCompletada", 0) == 1;

            if (encuestacompletada == false || encuestacompletada == true)
            {

                Debug.Log("Actualizando encuesta completada... Desde UpdateData");
                ActualizarEstadoEncuesta(userId, encuestacompletada);

            }

                ActualizarXPEnFirebase(userId);
        }
        
    }


    private async void ActualizarEstadoEncuesta(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = db.Collection("users").Document(userId);
        await userRef.UpdateAsync("EncuestaCompletada", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta... {userId}: {estadoencuesta} desde UpdateData");
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

}
