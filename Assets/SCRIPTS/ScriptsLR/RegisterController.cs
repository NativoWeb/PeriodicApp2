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
        // Agregar opciones al Dropdown
        roles.AddOptions(opciones);
        // Asegurar que la opción por defecto sea "Ocupación"
        roles.value = 0;
        // Cambiar el color del texto cuando sea "Ocupación"
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        // Aplicar el color gris al inicio
        CambiarColor();

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

    /*******************************************Funcion para ocupaciones*******************************************/

    void CambiarColor()
    {
        Text label = roles.captionText; // Obtener el texto actual del dropdown

        if (roles.value == 0) // Si está en "Ocupación"
        {
            label.color = Color.gray; // Cambiar el color a gris
        }
        else
        {
            label.color = Color.black; // Restaurar color normal
        }
    }

    /**************************************************************************************/

    public void OnCompleteProfileButtonClick()
    {
        string userName = userNameInput.text;
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser != null)
        {
            // 🔹 Primero verificamos si PlayerPrefs indica que el usuario ya verificó su correo
            if (PlayerPrefs.GetInt("EmailVerified", 0) == 1)
            {
                Debug.Log("✅ Correo verificado (según PlayerPrefs). Continuando con el registro...");
                UpdateUserProfile(currentUser, userName);
                return;
            }

            // 🔹 Si PlayerPrefs no está actualizado, recargamos el usuario desde Firebase
            currentUser.ReloadAsync().ContinueWithOnMainThread(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("Error al recargar la información del usuario.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al recargar el usuario: " + task.Exception?.Message);
                    return;
                }

                // 🔹 Verificamos si el usuario confirmó el correo después de la recarga
                if (currentUser.IsEmailVerified)
                {
                    Debug.Log("✅ Correo verificado después de recargar Firebase. Registrando usuario...");
                    PlayerPrefs.SetInt("EmailVerified", 1); // Guardamos la verificación
                    PlayerPrefs.Save();
                    UpdateUserProfile(currentUser, userName);
                }
                else
                {
                    Debug.LogError("⚠️ El correo aún no está verificado. Inténtalo nuevamente.");
                }
            });
        }
        else
        {
            Debug.LogError("No se ha encontrado un usuario autenticado.");
        }
    }


    //if (currentUser != null)
    //{
    //    if (currentUser.IsEmailVerified)
    //    {
    //        // Actualizar el nombre de usuario
    //        UpdateUserProfile(currentUser, userName);
    //    }
    //    else
    //    {
    //        Debug.Log("Por favor, verifica tu correo antes de continuar.");
    //    }
    //}
    //else
    //{
    //    Debug.LogError("No se ha encontrado un usuario autenticado.");
    //}


    private void UpdateUserProfile(FirebaseUser user, string userName)
    {
        // Actualizar el nombre de usuario en Firebase
        UserProfile profile = new UserProfile { DisplayName = userName };
        user.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
            if (task.IsCanceled)
            {
                Debug.LogError("Error al actualizar el perfil.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error al actualizar el perfil: " + task.Exception?.Message);
                return;
            }

            // Guardar información adicional si es necesario (por ejemplo, en Firestore)
            SaveUserData(user);

            // Cambiar a la siguiente escena después de completar el perfil
            Debug.Log("Perfil actualizado con éxito.");
            // Aquí podrías cambiar a la escena principal
            // SceneManager.LoadScene("MainScene");
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
        { "Ocupacion", ocupacionSeleccionada } // 🔹 Agregar ocupación
    };

    // Usa SetOptions.MergeAll para evitar sobreescribir otros datos existentes
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

        // Cambiar a la siguiente escena después de guardar los datos
        SceneManager.LoadScene("EcnuestaScen1e");
    });
}




}
