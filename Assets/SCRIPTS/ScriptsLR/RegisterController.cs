using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegisterController : MonoBehaviour
{
    public TMP_Text txtMensaje;
    public GameObject panelMessage;
    public Button ButtonMesage;

    public TMP_InputField userNameInput;
    public Button completeProfileButton;
    public Dropdown roles;
    [SerializeField] private GameObject m_OcupacionUI = null;// Activar Lista ocupación 

    private string ocupacionSelecionada; // para actualizar 

    //instanciar firebase
    private FirebaseAuth auth;
    private FirebaseFirestore db;


    // verificar wifi
    private bool hayInternet = false;
    

    //pop up sin internet
    [SerializeField] private GameObject m_SinInternetUI = null;

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

        ButtonMesage.onClick.AddListener(ClosePanelMessage);

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

    void CambiarColor()// -------------------------------------------------------------------------
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    public void OnCompleteProfileButtonClick()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {
            FirebaseUser currentUser = auth.CurrentUser;

            if (currentUser == null)
            {
                Debug.LogError("No se encontró un usuario autenticado.");
                return;
            }

            bool ocupacion = PlayerPrefs.HasKey("TempOcupacion");

            if (roles.value == 0 && !ocupacion)
            {
                panelMessage.SetActive(true);
                txtMensaje.text = "Debes seleccionar una ocupación antes de continuar.";
                txtMensaje.color = Color.red;
                return;
            }

            string userName = userNameInput.text.Trim();

            if (string.IsNullOrEmpty(userName))
            {
                panelMessage.SetActive(true);
                txtMensaje.text = "Debes ingresar un nombre de usuario antes de continuar.";
                txtMensaje.color = Color.red;
                return;
            }

            if (userName.Length < 8 || userName.Length > 10)
            {
                panelMessage.SetActive(true);
                txtMensaje.text = "El nombre de usuario debe tener entre 8 y 10 caracteres.";
                txtMensaje.color = Color.red;
                return;
            }

            // Verificar si el nombre de usuario ya existe en Firestore
            CheckUsernameAvailability(userName, currentUser);
        }
        else
        { 
            m_SinInternetUI.SetActive(true);
        }
    }

    private async void CheckUsernameAvailability(string userName, FirebaseUser currentUser)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        Query query = db.Collection("users").WhereEqualTo("DisplayName", userName);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        
            if (snapshot.Count > 0)
            {
            // Si ya existe un usuario con ese nombre, mostrar error
            panelMessage.SetActive(true);
            txtMensaje.text = "El nombre de usuario ya está en uso. Elige otro.";
            txtMensaje.color = Color.red;
            return;
        }

        // Guardar el nombre en PlayerPrefs
        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.Save();

        if (PlayerPrefs.GetInt("EmailVerified", 0) == 1)
        {
            Debug.Log("✅ Correo verificado. Continuando con el registro...");
            UpdateUserProfile(currentUser, userName);
            return;
        }

        PlayerPrefs.SetInt("EmailVerified", 1);
        PlayerPrefs.Save();
        UpdateUserProfile(currentUser, userName);
    }

    private void UpdateUserProfile(FirebaseUser user, string userName)// -------------------------------------------------------------------------
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

    private async void SaveUserData(FirebaseUser user) // -------------------------------------------------------------------------
    {
        string userId = user.UserId;
        DocumentReference docRef = db.Collection("users").Document(userId);

        string avatarUrl = "Avatares/defecto";

        bool tieneUsuarioTemporal = PlayerPrefs.HasKey("TempOcupacion");

        // Verificar Estado de las Encuestas-----------------
        bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;
        //-------------------------------------
        int xpTemp = PlayerPrefs.GetInt("TempXP", 0);

        if (tieneUsuarioTemporal)
        {
            ocupacionSelecionada = PlayerPrefs.GetString("TempOcupacion", "");
            Debug.Log($"la ocupacion seleccionada antes de guardar en firebase es : {ocupacionSelecionada}");
        }
        else
        {
            ocupacionSelecionada = roles.options[roles.value].text;
            Debug.Log($"la ocupacion seleccionada antes de guardar en firebase es : {ocupacionSelecionada}");
        }


        Dictionary<string, object> userData = new Dictionary<string, object>
    {
        { "DisplayName", user.DisplayName },
        { "Email", user.Email },
        { "Ocupacion", ocupacionSelecionada },
        { "EstadoEncuestaAprendizaje", estadoencuestaaprendizaje },
        { "EstadoEncuestaConocimiento", estadoencuestaconocimiento },
        { "xp", xpTemp },
        { "avatar", avatarUrl },
        { "Rango", "Novato de laboratorio" }
    };


        PlayerPrefs.SetString("Estadouser", "sinloguear");
        PlayerPrefs.SetString("userId", userId);
        PlayerPrefs.SetString("TempOcupacion", ocupacionSelecionada); // guardamos la ocupación para poder hacer el tryofflinelogin si se entra la primera vez con wifi
        PlayerPrefs.DeleteKey("UsuarioEliminar");
        PlayerPrefs.Save();

        try
        {
            await docRef.SetAsync(userData, SetOptions.MergeAll);
            Debug.Log("✅ Datos de usuario guardados en Firestore.");

          // acá pongo el autologin para que en offline pueda entrar normal

           
            VerificarYActualizarRango(userId);
            await SubirDatosJSON(userId);

            // mandeme a el login después de registrarme
            SceneManager.LoadScene("Login");
        }
        catch (System.Exception e)
        {
            PlayerPrefs.DeleteKey("userEmail");
            PlayerPrefs.DeleteKey("userPassword");
            Debug.LogError($"Error al guardar datos del usuario: {e.Message}");

        }
    }


    private void VerificarYActualizarRango(string userId)// -------------------------------------------------------------------------
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

    public async Task SubirDatosJSON(string UserId)
    {
        if (string.IsNullOrEmpty(UserId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        // Obtener JSON de misiones y categorías desde PlayerPrefs
        string jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON");
        string jsonCategorias = PlayerPrefs.GetString("CategoriasOrdenadas", "");

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(UserId).Collection("datos").Document("misiones");
        DocumentReference categoriasDoc = db.Collection("users").Document(UserId).Collection("datos").Document("categorias");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (jsonMisiones != "{}")
        {
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (jsonCategorias != "{}")
        {
            Dictionary<string, object> dataCategorias = new Dictionary<string, object>
        {
            { "categorias", jsonCategorias },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(categoriasDoc.SetAsync(dataCategorias, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay datos de misiones ni categorías para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);

        Debug.Log("✅ Datos de misiones y categorías subidos en documentos separados.");
    }

    private void ClosePanelMessage()
    {
        panelMessage.SetActive(false);
    }
}