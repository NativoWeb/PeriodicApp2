using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegisterController : MonoBehaviour
{
    public TMP_InputField userNameInput;
    public Button completeProfileButton;
    public Dropdown roles;
    private FirebaseAuth auth;

    void Start()
    {
        /*-----------------------------------------------Lista de ocupaciones-----------------------------------------------*/

        // Crear lista de opciones con "Ocupación" como la primera opción
        List<string> opciones = new List<string>() { "Seleccionar una ocupación", "Estudiante", "Profesor" };
        roles.AddOptions(opciones);
        roles.value = 0; // Asegurar que la opción por defecto sea "Seleccionar una ocupación"
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        CambiarColor(); // Aplicar color inicial

        /*------------------------------------------------------------------------------------------------------------------*/

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (app != null)
            {
                auth = FirebaseAuth.DefaultInstance;
                completeProfileButton.onClick.AddListener(OnCompleteProfileButtonClick);
            }
            else
            {
                Debug.LogError("Firebase no se ha podido inicializar.");
            }
        });
    }

    /*******************************************Función para cambiar el color del Dropdown*******************************************/
    void CambiarColor()
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    /**************************************************************************************/

    public void OnCompleteProfileButtonClick()
    {
        string userName = userNameInput.text;
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser != null)
        {
            if (PlayerPrefs.GetInt("EmailVerified", 0) == 1)
            {
                Debug.Log("✅ Correo verificado. Continuando con el registro...");
                UpdateUserProfile(currentUser, userName);
                return;
            }

            currentUser.ReloadAsync().ContinueWithOnMainThread(task => {
                if (task.IsCompleted && currentUser.IsEmailVerified)
                {
                    Debug.Log("✅ Correo verificado después de recarga.");
                    PlayerPrefs.SetInt("EmailVerified", 1);
                    PlayerPrefs.Save();
                    UpdateUserProfile(currentUser, userName);
                }
                else
                {
                    Debug.LogError("⚠️ El correo aún no está verificado.");
                }
            });
        }
        else
        {
            Debug.LogError("No se ha encontrado un usuario autenticado.");
        }
    }

    private void UpdateUserProfile(FirebaseUser user, string userName)
    {
        UserProfile profile = new UserProfile { DisplayName = userName };
        user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                SaveUserData(user);
                Debug.Log("Perfil actualizado con éxito.");
            }
            else
            {
                Debug.LogError("Error al actualizar el perfil.");
            }
        });
    }

    private void SaveUserData(FirebaseUser user)
    {
        FirebaseFirestore firestore = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = firestore.Collection("users").Document(user.UserId);

        // Obtener la ocupación seleccionada
        string ocupacionSeleccionada = roles.options[roles.value].text;

        Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "DisplayName", user.DisplayName },
        { "Email", user.Email },
        { "Ocupacion", ocupacionSeleccionada },
        { "EncuestaCompletada", false } // 🔹 Marcamos la encuesta como no completada inicialmente
    };

        PlayerPrefs.SetString("userId", user.UserId);
        PlayerPrefs.Save();

        docRef.SetAsync(userData, SetOptions.MergeAll).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Error al guardar los datos del usuario.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error al guardar los datos: " + task.Exception?.Message);
                return;
            }
            Debug.Log("Datos de usuario guardados en Firestore.");

            // 🔹 Redirigir a la escena correcta según la ocupación
            if (ocupacionSeleccionada == "Estudiante")
            {
                SceneManager.LoadScene("EcnuestaScen1e"); // Enviar a la encuesta
            }
            else if (ocupacionSeleccionada == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor"); // Enviar a la vista de profesor
            }
        });
    }
}
