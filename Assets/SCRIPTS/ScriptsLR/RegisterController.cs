using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegisterController : MonoBehaviour
{
    public TMP_InputField userNameInput;
    public Button completeProfileButton;
    public Dropdown roles;
    [SerializeField] private GameObject m_OcupacionUI = null;// Activar Lista ocupación 
    private string ocupacionSelecionada; // para actualizar 

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private Dictionary<string, int> rangos = new Dictionary<string, int>()
    {
        { "Novato de laboratorio", 0 },
        { "Arquitecto molecular", 3000},
        { "Visionario Cuántico", 9000 },
        { "Amo del caos químico", 25000 }
    };

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        List<string> opciones = new List<string>() { "Seleccionar una ocupación", "Estudiante", "Profesor" };
        roles.AddOptions(opciones);
        roles.value = 0;
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        CambiarColor();

        if (auth.CurrentUser != null)
        {
            completeProfileButton.onClick.AddListener(OnCompleteProfileButtonClick);
        }
        else
        {
            Debug.LogError("Firebase no está listo o no hay usuario autenticado.");
        }
        // verificamos que tenga ocupacion guardada y mostramos o no el panel
        string Tempocupacion = PlayerPrefs.GetString("TempOcupacion", "").Trim();

        if (Tempocupacion != "")
        {
            m_OcupacionUI.SetActive(false);
        }
        else
        {
            m_OcupacionUI.SetActive(true);
        }
    }

    void CambiarColor()
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    public void OnCompleteProfileButtonClick()
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogError("No se encontró un usuario autenticado.");
            return;
        }

        string userName = userNameInput.text;
        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.Save();

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

    private async void SaveUserData(FirebaseUser user)
    {
        string userId = user.UserId;
        DocumentReference docRef = db.Collection("users").Document(userId);

        string avatarUrl = "Avatares/defecto";  

        bool tieneUsuarioTemporal = PlayerPrefs.HasKey("TempUsername");
        bool encuestaCompletada = PlayerPrefs.GetInt("TempEncuestaCompletada", 0) == 1;
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);

        if (tieneUsuarioTemporal)
        {
            ocupacionSelecionada = PlayerPrefs.GetString("TempOcupacion", "");
        }
        else
        {
            ocupacionSelecionada = roles.options[roles.value].text;
        }

            Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "DisplayName", user.DisplayName },
        { "Email", user.Email },
        { "Ocupacion", ocupacionSelecionada },
        { "EncuestaCompletada", encuestaCompletada },
        { "xp", xpTemp },
        { "avatar", avatarUrl },
        { "Rango", "Novato de laboratorio" }
    };

        PlayerPrefs.SetString("Estadouser", "nube");
        PlayerPrefs.SetString("userId", userId);
        PlayerPrefs.Save();

        try
        {
            await docRef.SetAsync(userData, SetOptions.MergeAll);
            Debug.Log("✅ Datos de usuario guardados en Firestore.");

            if (tieneUsuarioTemporal)
            {
                PlayerPrefs.DeleteKey("TempUsername");
                PlayerPrefs.SetInt("TempXP", 0);
                PlayerPrefs.DeleteKey("TempOcupacion");
                PlayerPrefs.DeleteKey("TempAvatar");
                PlayerPrefs.DeleteKey("TempRango");
                PlayerPrefs.SetString("Estadouser", "nube");
                PlayerPrefs.Save();
            }

            CrearSubcoleccionGrupos(userId);
            VerificarYActualizarRango(userId);
            await SubirMisionesJSON(userId);

            SceneManager.LoadScene("Login");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar datos del usuario: {e.Message}");
        }
    }


    private void CrearSubcoleccionGrupos(string userId)
    {
        CollectionReference gruposRef = db.Collection("users").Document(userId).Collection("grupos");

        string[] nombresGrupos = {
            "Metales Alcalinos", "Metales Alcalinotérreos", "Metales del Grupo del Escandio", "Metales del Grupo del Titanio",
            "Metales del Grupo del Vanadio", "Metales del Grupo del Cromo", "Metales del Grupo del Manganeso", "Metales del Grupo del Hierro",
            "Metales del Grupo del Cobalto", "Metales del Grupo del Níquel", "Metales del Grupo del Cobre", "Metales del Grupo del Zinc",
            "Lantánidos", "Actínidos", "Metaloides", "No Metales", "Halógenos", "Gases Nobles"
        };

        foreach (string nombreGrupo in nombresGrupos)
        {
            Dictionary<string, object> grupoData = new Dictionary<string, object>
            {
                { "nivel", 1 },
                { "nivel_maximo", 15 },
                { "nombre", nombreGrupo },
                { "ruta_imagen", $"GruposImages/{nombreGrupo}" }
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

    private void VerificarYActualizarRango(string userId)
    {
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
                        if (xp >= rango.Value) nuevoRango = rango.Key;
                    }

                    if (snapshot.ContainsField("Rango") && snapshot.GetValue<string>("Rango") != nuevoRango)
                    {
                        docRef.UpdateAsync("Rango", nuevoRango);
                    }
                }
            }
        });
    }

    private async Task SubirMisionesJSON(string userId)
    {
        string jsonMisiones = PlayerPrefs.GetString("misionesJSON", "{}");

        if (jsonMisiones == "{}")
        {
            Debug.LogWarning("⚠️ No hay datos de misiones guardados.");
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        await db.Collection("users").Document(userId).SetAsync(data, SetOptions.MergeAll);
    }
}
