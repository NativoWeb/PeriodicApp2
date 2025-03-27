using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class StartAppManager : MonoBehaviour
{
    public static bool IsReady = false; // 🔹 Bandera para indicar si terminó
    private bool yaVerificado = false; // 🔹 Evita ejecuciones repetidas
   


    void Start()
    {
        //Verifica su hay conexion a internet
        StartCoroutine(CheckInternetConnection());
   
    }

    // 🔹 Corrutina para verificar conexión
    IEnumerator CheckInternetConnection()
    {
        yield return new WaitForSeconds(0); // Esperar un segundo antes de validar


        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            // si no hay internet, verifica si tiene un usuario temporal creado previamente
            HandleOfflineMode();
        }
        else
        {
            //si hay internet verifica si tiene datos guardados previamente del offline
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
            LoadSceneIfNotAlready("Login");
        }
        else if (IsTemporaryUserSaved())
        {
            string ocupacion = PlayerPrefs.GetString("TempOcupacion");


            if (ocupacion == "Profesor")
            {
                LoadSceneIfNotAlready("InicioProfesor");
            }
            else if (ocupacion == "Estudiante")
            {
                LoadSceneIfNotAlready("Categorías");
            }

        }
        else
        {
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

        if (IsTemporaryUserSaved()) // si hay datos encontrados del offline, se envia a registro para subirlo a la BD
        {             
            SceneManager.LoadScene("Email");

        }
        else //Si no tiene datos temporales lo manda a login
        {
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
        return PlayerPrefs.HasKey("DisplayName") &&
               PlayerPrefs.HasKey("TempOcupacion") &&
               PlayerPrefs.HasKey("TempXP") &&
               PlayerPrefs.HasKey("TempAvatar") &&
               PlayerPrefs.HasKey("Rango") &&
               PlayerPrefs.HasKey("TempEncuestaCompletada");
    }

    // Crear y guardar usuario temporal en PlayerPrefs
    void CreateTemporaryUser()
    {
        string username = "tempUser_" + Random.Range(1000, 9999).ToString();
        string ocupacionSeleccionada = "Otro"; // Por defecto
        string avatarUrl = "Avatares/nivel1"; // Por defecto
        bool encuestaCompletada = false;

        // Guardar datos en PlayerPrefs
        PlayerPrefs.SetString("DisplayName", username);
        PlayerPrefs.SetString("TempAvatar", avatarUrl);
        PlayerPrefs.SetString("Rango", "Novato de laboratorio");
        PlayerPrefs.SetInt("TempXP", 0);
        PlayerPrefs.SetInt("posicion", 0);
        PlayerPrefs.SetString("TempOcupacion", ocupacionSeleccionada);
        PlayerPrefs.SetInt("Nivel", 1);
        PlayerPrefs.SetInt("TempEncuestaCompletada", encuestaCompletada ? 1 : 0);

        PlayerPrefs.SetString("Estadouser", "local");
        PlayerPrefs.Save();
    }

}