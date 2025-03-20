﻿using Firebase.Auth;
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

    // -------------------------------------- RANGOS --------------------------------------
    private Dictionary<string, int> rangos = new Dictionary<string, int>()
    {
        { "Novato de laboratorio", 0 },
        { "Arquitecto molecular", 3000},
        { "Visionario Cuántico", 9000 },
        { "Amo del caos químico", 25000 }
    };
    // -----------------------------------------------------------------------------------

    void Start()
    {
        /*-----------------------------------------------Lista de ocupaciones-----------------------------------------------*/

        // Crear lista de opciones con "Ocupación" como la primera opción
        List<string> opciones = new List<string>() { "Seleccionar una ocupación", "Estudiante", "Profesor" };
        roles.AddOptions(opciones);
        roles.value = 0; // Asegurar que la opción por defecto sea "Seleccionar una ocupación"
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        CambiarColor(); // Aplicar color inicial

        // Usar la instancia de DbConnexion para obtener la autenticación
        if (DbConnexion.Instance.IsFirebaseReady())
        {
            completeProfileButton.onClick.AddListener(OnCompleteProfileButtonClick);
        }
        else
        {
            Debug.LogError("Firebase no está listo.");
        }
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
        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.Save();
        FirebaseUser currentUser = DbConnexion.Instance.Auth.CurrentUser; // Usar la instancia de DbConnexion para obtener el usuario

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
        FirebaseFirestore firestore = DbConnexion.Instance.Firestore; // Obtener instancia de Firestore
        DocumentReference docRef = firestore.Collection("users").Document(user.UserId);

        string avatarUrl = "Avatares/defecto";  // Ruta de avatar por defecto
        string ocupacionSeleccionada = roles.options[roles.value].text;

        // Verificar si existe un usuario temporal
        bool tieneUsuarioTemporal = PlayerPrefs.HasKey("TempUsername");
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0); // Obtener XP temporal, si existe

        // Crear datos de usuario
        Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "DisplayName", user.DisplayName },
        { "Email", user.Email },
        { "Ocupacion", ocupacionSeleccionada },
        { "EncuestaCompletada", false},
        { "xp", xpTemp },  // Si tenía XP temporal, lo subimos
        { "avatar", avatarUrl },
        { "Rango", "Novato de laboratorio" }
       
    };
        PlayerPrefs.SetString("Estadouser", "nube");
        PlayerPrefs.SetString("userId", user.UserId);
        PlayerPrefs.Save();

        docRef.SetAsync(userData, SetOptions.MergeAll).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Error al guardar los datos del usuario.");
                return;
            }

            Debug.Log("✅ Datos de usuario guardados en Firestore.");

            // 🔹 Si tenía usuario temporal, eliminarlo
            if (tieneUsuarioTemporal)
            {
                Debug.Log("♻️ Se detectó un usuario temporal. Eliminando datos temporales...");
                PlayerPrefs.DeleteKey("TempUsername");
                PlayerPrefs.SetInt("TempXP", 0);
                PlayerPrefs.DeleteKey("TempOcupacion");
                PlayerPrefs.DeleteKey("TempAvatar");
                PlayerPrefs.DeleteKey("TempRango");
                PlayerPrefs.SetString("Estadouser", "nube");
                PlayerPrefs.Save();
            }

            // 🔹 Crear la subcolección "grupos"
            CrearSubcoleccionGrupos(user.UserId);

            // 🔹 Verificar y actualizar rango con el nuevo XP
            VerificarYActualizarRango(user.UserId);

            // 🔹 Redirigir a la escena correcta según la ocupación
            if (ocupacionSeleccionada == "Estudiante")
            {
                SceneManager.LoadScene("EcnuestaScen1e");
            }
            else if (ocupacionSeleccionada == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor");
            }
        });
    }


    // ✅ FUNCION PARA CREAR LA SUBCOLECCIÓN "grupos"
    private void CrearSubcoleccionGrupos(string userId)
    {
        FirebaseFirestore firestore = DbConnexion.Instance.Firestore; // Usar la instancia de DbConnexion para Firestore
        CollectionReference gruposRef = firestore.Collection("users").Document(userId).Collection("grupos");

        // Lista de nombres de los 18 grupos (puedes personalizar los nombres)
        string[] nombresGrupos = new string[] {
            "Metales Alcalinos", "Metales Alcalinotérreos", "Metales del Grupo del Escandio", "Metales del Grupo del Titanio", "Metales del Grupo del Vanadio", "Metales del Grupo del Cromo",
            "Metales del Grupo del Manganeso", "Metales del Grupo del Hierro", "Metales del Grupo del Cobalto", "Metales del Grupo del Níquel", "Metales del Grupo del Cobre", "Metales del Grupo del Zinc",
            "Lantánidos", "Actínidos", "Metaloides", "No Metales", "Halógenos", "Gases Nobles"
        };

        // Iterar sobre cada grupo para crear el documento con los datos iniciales
        for (int i = 0; i < nombresGrupos.Length; i++)
        {
            string nombreGrupo = nombresGrupos[i];
            Dictionary<string, object> grupoData = new Dictionary<string, object>
            {
                { "nivel", 1 }, // Nivel inicial
                { "nivel_maximo", 15 }, // Nivel máximo, puedes cambiar este valor según necesidad
                { "nombre", nombreGrupo },
                { "ruta_imagen", $"GruposImages/Grupo{i + 1}" } // Ruta de la imagen, ajusta según tu carpeta Resources
            };

            gruposRef.Document(nombreGrupo).SetAsync(grupoData).ContinueWithOnMainThread(task => {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log($"Grupo '{nombreGrupo}' creado correctamente.");
                }
                else
                {
                    Debug.LogError($"Error al crear grupo '{nombreGrupo}': {task.Exception?.Message}");
                }
            });
        }
    }

    // ------------------------- FUNCIÓN PARA VERIFICAR Y ACTUALIZAR RANGO -------------------------
    private void VerificarYActualizarRango(string userId)
    {
        FirebaseFirestore db = DbConnexion.Instance.Firestore; // Usar la instancia de DbConnexion para Firestore
        DocumentReference docRef = db.Collection("users").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists && snapshot.ContainsField("xp"))
                {
                    int xp = snapshot.GetValue<int>("xp");
                    string nuevoRango = "Novato de laboratorio";

                    foreach (var rango in rangos)
                    {
                        if (xp >= rango.Value)
                        {
                            nuevoRango = rango.Key;
                        }
                    }

                    string rangoActual = snapshot.ContainsField("Rango") ? snapshot.GetValue<string>("Rango") : "Novato de laboratorio";

                    if (nuevoRango != rangoActual)
                    {
                        docRef.UpdateAsync("Rango", nuevoRango).ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.IsCompleted)
                            {
                                Debug.Log($"✅ Rango actualizado a: {nuevoRango}");
                            }
                        });
                    }
                }
            }
            else
            {
                Debug.LogError("Error al verificar el XP del usuario.");
            }
        });
    }
    // ---------------------------------------------------------------------------------------------

}
