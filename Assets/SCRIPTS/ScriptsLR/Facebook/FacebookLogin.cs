using UnityEngine;
using UnityEngine.SceneManagement;
using Facebook.Unity;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FacebookLogin : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("✅ Firebase Inicializado correctamente");
            }
            else
            {
                Debug.LogError("❌ No se pudo inicializar Firebase: " + task.Result);
            }
        });
        InitializeFacebook();
    }


    void InitializeFacebook()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(() =>
            {
                if (FB.IsInitialized)
                {
                    FB.ActivateApp();
                    Debug.Log("✅ Facebook SDK inicializado");
                }
                else
                {
                    Debug.LogError("❌ Falló la inicialización del Facebook SDK");
                }
            });
        }
        else
        {
            FB.ActivateApp();
        }
    }

    public void LoginWithFacebook()
    {
        if (!FB.IsInitialized) return;

        var permissions = new List<string> { "public_profile", "email" };
        FB.LogInWithReadPermissions(permissions, async (result) =>
        {
            if (result == null || result.Error != null || !FB.IsLoggedIn)
            {
                Debug.LogError("Error en login Facebook: " + (result?.Error ?? "Resultado nulo"));
                return;
            }

            try
            {
                // 1. Autenticar en Firebase
                var credential = FacebookAuthProvider.GetCredential(AccessToken.CurrentAccessToken.TokenString);
                var authResult = await auth.SignInWithCredentialAsync(credential);

                // 2. Preparar datos para el registro
                PlayerPrefs.SetString("AuthProvider", "facebook");
                PlayerPrefs.SetString("ProviderEmail", authResult.Email);
                PlayerPrefs.SetString("UserID", authResult.UserId);

                // 3. Verificar si ya completó el registro
                var userDoc = await db.Collection("users").Document(authResult.UserId).GetSnapshotAsync();

                if (userDoc.Exists && userDoc.ContainsField("Ocupacion") && userDoc.ContainsField("DisplayName"))
                {
                    // Redirigir según ocupación
                    string ocupacion = userDoc.GetValue<string>("Ocupacion");
                    RedirectByOccupation(ocupacion);
                }
                else
                {
                    // Nuevo usuario - Ir a completar registro
                    SceneManager.LoadScene("Registrar");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error en el proceso de login: " + e.Message);
            }
        });
    }

    private void RedirectByOccupation(string ocupacion)
    {
        switch (ocupacion.ToLower())
        {
            case "profesor":
                SceneManager.LoadScene("inicioProfesor");
                break;
            case "estudiante":
            default:
                SceneManager.LoadScene("Categorias");
                break;
        }
    }
}