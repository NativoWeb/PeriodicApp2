using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ConfigurarBrevo : MonoBehaviour
{
    [Header("Configuración de API Key de Brevo")]
    [Tooltip("Pega aquí tu nueva API key de Brevo")]
    [SerializeField] private string brevoApiKey = "";

    void Start()
    {
        ConfigurarApiKey();
    }

    [ContextMenu("Configurar API Key de Brevo")]
    public void ConfigurarApiKey()
    {
        if (string.IsNullOrEmpty(brevoApiKey))
        {
            Debug.LogError("ERROR: Debes ingresar la API key de Brevo en el Inspector antes de ejecutar este script.");
            return;
        }

        // Limpiar espacios en blanco al inicio y final
        brevoApiKey = brevoApiKey.Trim();

        // Guardar la API key en PlayerPrefs
        PlayerPrefs.SetString("BREVO_API_KEY", brevoApiKey);
        PlayerPrefs.Save();

        Debug.Log($"✓ API Key de Brevo configurada correctamente.");
        Debug.Log($"✓ Longitud de la clave: {brevoApiKey.Length} caracteres");
        Debug.Log($"✓ Primeros 20 caracteres: {brevoApiKey.Substring(0, Mathf.Min(20, brevoApiKey.Length))}...");
        Debug.Log("✓ Ahora ejecuta 'Probar Conexión con Brevo' para validar la clave.");
    }

    [ContextMenu("Probar Conexión con Brevo")]
    public void ProbarConexion()
    {
        StartCoroutine(ProbarConexionCoroutine());
    }

    private IEnumerator ProbarConexionCoroutine()
    {
        string apiKey = PlayerPrefs.GetString("BREVO_API_KEY", "");

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("No hay API key configurada. Ejecuta 'Configurar API Key de Brevo' primero.");
            yield break;
        }

        Debug.Log("Probando conexión con Brevo API...");
        Debug.Log($"API Key a usar (primeros 20 chars): {apiKey.Substring(0, Mathf.Min(20, apiKey.Length))}...");
        Debug.Log($"Longitud: {apiKey.Length} caracteres");

        // Probar con el endpoint de cuenta de Brevo
        UnityWebRequest request = UnityWebRequest.Get("https://api.brevo.com/v3/account");
        request.SetRequestHeader("api-key", apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✓ ✓ ✓ ÉXITO: La API key es válida y funciona correctamente.");
            Debug.Log($"Respuesta: {request.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"✗ ERROR: La API key NO es válida.");
            Debug.LogError($"Código de error: {request.responseCode}");
            Debug.LogError($"Mensaje: {request.error}");
            Debug.LogError($"Respuesta de Brevo: {request.downloadHandler.text}");

            if (request.responseCode == 401)
            {
                Debug.LogError("ERROR 401: La API key es incorrecta o no tiene permisos.");
                Debug.LogError("Verifica que:");
                Debug.LogError("1. Copiaste la clave completa desde Brevo");
                Debug.LogError("2. La clave no tiene espacios al inicio o final");
                Debug.LogError("3. Es una clave API v3 de Brevo (no SMTP)");
                Debug.LogError("4. La clave no ha sido revocada");
            }
        }
    }

    [ContextMenu("Verificar API Key Guardada")]
    public void VerificarApiKey()
    {
        string keyGuardada = PlayerPrefs.GetString("BREVO_API_KEY", "");

        if (string.IsNullOrEmpty(keyGuardada))
        {
            Debug.LogWarning("No hay ninguna API key guardada en PlayerPrefs.");
        }
        else
        {
            Debug.Log($"✓ API Key encontrada (longitud: {keyGuardada.Length} caracteres)");
            Debug.Log($"Primeros caracteres: {keyGuardada.Substring(0, Mathf.Min(10, keyGuardada.Length))}...");
        }
    }

    [ContextMenu("Eliminar API Key")]
    public void EliminarApiKey()
    {
        PlayerPrefs.DeleteKey("BREVO_API_KEY");
        PlayerPrefs.Save();
        Debug.Log("API Key eliminada de PlayerPrefs");
    }
}
