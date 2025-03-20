﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class StartAppManager : MonoBehaviour
{
    public static bool IsReady = false; // 🔹 Bandera para indicar si terminó
    private bool yaVerificado = false; // 🔹 Evita ejecuciones repetidas

    void Start()
    {
        Debug.Log("⌛ Verificando conexión a Internet...");
        StartCoroutine(CheckInternetConnection());
    }

    // 🔹 Corrutina para verificar conexión
    IEnumerator CheckInternetConnection()
    {
        yield return new WaitForSeconds(1f); // Esperar un segundo antes de validar

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("❌ No hay conexión a internet. Verificando usuario temporal...");
            HandleOfflineMode();
        }
        else
        {
            Debug.Log("🌍 Conexión a internet detectada. Verificando datos guardados...");
            HandleOnlineMode();
        }
    }

    // 🔹 Modo offline
    void HandleOfflineMode()
    {
        if (yaVerificado) return; // 🔹 Si ya se ejecutó, salir

        yaVerificado = true; // 🔹 Marcar como ejecutado

        string estadoUsuario = PlayerPrefs.GetString("Estadouser", "");

        if (estadoUsuario == "nube")
        {
            Debug.Log("☁️ Usuario autenticado en la nube. Permitiendo acceso offline.");
            LoadSceneIfNotAlready("Login");
        }
        else if (IsTemporaryUserSaved())
        {
            Debug.Log("✅ Usuario temporal encontrado. Enviando a Inicio.");
            LoadSceneIfNotAlready("Inicio");
        }
        else
        {
            Debug.Log("🆕 No se encontró usuario temporal. Creando usuario provisional...");
            CreateTemporaryUser();
            LoadSceneIfNotAlready("InicioOffline");
        }

        IsReady = true; // 🔹 Marcamos como listo también en modo offline
    }


    // 🔹 Modo online
    void HandleOnlineMode()
    {
        if (yaVerificado) return;

        yaVerificado = true;

        if (IsTemporaryUserSaved())
        {
            Debug.Log("📝 Datos temporales encontrados. Enviando a Registro.");
            LoadSceneIfNotAlready("Email");
        }
        else
        {
            Debug.Log("🔑 No hay datos temporales. Enviando a Login.");
            LoadSceneIfNotAlready("Login");
        }

        IsReady = true; // ✅ Marcamos como listo
    }

    // 🔹 Evita recargar la misma escena si ya está activa
    void LoadSceneIfNotAlready(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // Verificar si hay datos de usuario temporal guardados
    bool IsTemporaryUserSaved()
    {
        return PlayerPrefs.HasKey("TempUsername") &&
               PlayerPrefs.HasKey("TempOcupacion") &&
               PlayerPrefs.HasKey("TempXP") &&
               PlayerPrefs.HasKey("TempAvatar") &&
               PlayerPrefs.HasKey("TempRango") &&
               PlayerPrefs.HasKey("TempEncuestaCompletada");
    }

    // Crear y guardar usuario temporal en PlayerPrefs
    void CreateTemporaryUser()
    {
        string username = "tempUser_" + Random.Range(1000, 9999).ToString();
        string ocupacionSeleccionada = "Otro"; // Por defecto
        string avatarUrl = "Avatares/defecto"; // Por defecto
        bool encuestaCompletada = false;

        // Guardar datos en PlayerPrefs
        PlayerPrefs.SetString("TempUsername", username);
        PlayerPrefs.SetString("TempOcupacion", ocupacionSeleccionada);
        PlayerPrefs.SetInt("TempXP", 0);
        PlayerPrefs.SetString("TempAvatar", avatarUrl);
        PlayerPrefs.SetString("TempRango", "Novato de laboratorio");
        PlayerPrefs.SetString("EstadoUser", "local");
        PlayerPrefs.SetInt("Nivel", 1);
        PlayerPrefs.SetInt("TempEncuestaCompletada", encuestaCompletada ? 1 : 0);


        PlayerPrefs.Save();

        Debug.Log("✅ Usuario provisional creado: " + username);
    }
}
