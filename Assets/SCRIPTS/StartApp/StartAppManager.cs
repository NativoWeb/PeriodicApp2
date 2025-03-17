using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class StartAppManager : MonoBehaviour
{
    void Start()
    {
        // Suscribirse al evento de conexión
        ConnectionManager.OnConnectionChanged += HandleConnectionChange;
        Debug.Log("Esperando verificar conexión...");
    }

    // Método que se dispara cuando cambia la conexión
    void HandleConnectionChange()
    {
        Debug.Log("Conexión verificada. Online: " + ConnectionManager.isOnline + ", Offline: " + ConnectionManager.isOffline);
        CheckConnectionAndProceed();
    }

    // Verificar conexión y decidir a dónde ir
    void CheckConnectionAndProceed()
    {
        if (ConnectionManager.isOffline) // No hay internet
        {
            Debug.Log("No hay conexión a internet. Verificando usuario temporal...");
            if (IsTemporaryUserSaved())
            {
                Debug.Log("Usuario temporal encontrado. Enviando a Inicio.");
                SceneManager.LoadScene("Inicio"); // ⚠️ Asegúrate que esta escena esté bien escrita en Build Settings
            }
            else
            {
                Debug.Log("No se encontró usuario temporal. Creando usuario provisional...");
                CreateTemporaryUser();
                SceneManager.LoadScene("InicioOffline"); // ⚠️ Asegúrate que esta escena esté bien escrita en Build Settings
            }
        }
        else if (ConnectionManager.isOnline) // Hay internet
        {
            Debug.Log("Hay conexión a internet, enviando al Login...");
            SceneManager.LoadScene("Login"); // Ir a Login
        }
        else
        {
            Debug.LogWarning("Estado de conexión no definido.");
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

        var userData = new Dictionary<string, object>
        {
            { "DisplayName", username },
            { "Ocupacion", ocupacionSeleccionada },
            { "EncuestaCompletada", encuestaCompletada },
            { "xp", 0 },
            { "avatar", avatarUrl },
            { "Rango", "Novato de laboratorio" }
        };

        // Guardar datos en PlayerPrefs
        PlayerPrefs.SetString("TempUsername", username);
        PlayerPrefs.SetString("TempOcupacion", ocupacionSeleccionada);
        PlayerPrefs.SetInt("TempXP", 0);
        PlayerPrefs.SetString("TempAvatar", avatarUrl);
        PlayerPrefs.SetString("TempRango", "Novato de laboratorio");
        PlayerPrefs.SetInt("TempEncuestaCompletada", encuestaCompletada ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Usuario provisional creado: " + username);
    }

    // Muy importante: cuando salgas de la escena, quitas la suscripción
    private void OnDestroy()
    {
        ConnectionManager.OnConnectionChanged -= HandleConnectionChange;
    }
}
