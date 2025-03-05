//registercontroller
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

    private FirebaseAuth auth;

    void Start()
    {
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

        

        // Asignar avatar según el nivel
        string avatarUrl = "Avatares/defecto";  // Ruta de la imagen dentro de Resources

        // Asegúrate de que el DisplayName no esté vacío (si lo deseas)
        string displayName = string.IsNullOrEmpty(user.DisplayName) ? "Usuario Sin Nombre" : user.DisplayName;

        Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "DisplayName", user.DisplayName },
        { "Email", user.Email },
        { "xp", 0 },
        { "nivel",0 },
        { "avatar", avatarUrl } // Avatar inicial

    };

        // Usa SetOptions.MergeAll correctamente
        docRef.SetAsync(userData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
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

            PlayerPrefs.SetString("userId", user.UserId);
            PlayerPrefs.SetString("username", user.DisplayName);
            PlayerPrefs.SetString("correo", user.Email);
            PlayerPrefs.Save();

            Debug.Log("Guardando en PlayerPrefs:");
            Debug.Log("userId: " + user.UserId);
            Debug.Log("username: " + user.DisplayName);
            Debug.Log("correo: " + user.Email);
            // Cambiar a la siguiente escena después de guardar los datos
            SceneManager.LoadScene("EcnuestaScen1e");
        });
    }



}
