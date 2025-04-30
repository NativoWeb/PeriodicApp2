using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class RegisterController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField userNameInput;
    public TMP_Text txtMensaje;
    public Button completeProfileButton;
    public Dropdown roles;
    public GameObject m_OcupacionUI;
    public GameObject m_SinInternetUI;
    public GameObject panelMessage;
    public Button ButtonMessage;

    private ValidarNombreUsuario validarNombreUsuarioUseCase;
    private ActualizarPerfilUsuario actualizarPerfilUsuarioUseCase;
    private GuardarDatosUsuario guardarDatosUseCase;
    private SubirDatosJSON subirDatosJSONUseCase;
    private ActualizarRangoUsuario actualizarRangoUseCase;
    private IServicioLocalStorage localStorage;

    private string ocupacionSeleccionada;


    private void Start()
    {
        //Iniciar Servicios
        var firestore = new FirestoreService(FirebaseServiceLocator.Firestore);
        var auth = new FirebaseAuthService(FirebaseServiceLocator.Auth);

        //Casos de Uso
        validarNombreUsuarioUseCase = new ValidarNombreUsuario(firestore);
        actualizarPerfilUsuarioUseCase = new ActualizarPerfilUsuario(auth);
        guardarDatosUseCase = new GuardarDatosUsuario(firestore, localStorage);
        subirDatosJSONUseCase = new SubirDatosJSON(firestore, localStorage);
        actualizarRangoUseCase = new ActualizarRangoUsuario(firestore, localStorage);

        ButtonMessage.onClick.AddListener(ClosePanelMessage);

        roles.AddOptions(new System.Collections.Generic.List<string> { "Seleccionar una ocupación", "Estudiante", "Profesor" });
        roles.value = 0;
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        CambiarColor();

        m_OcupacionUI.SetActive(!PlayerPrefs.HasKey("TemOcupacion"));

        completeProfileButton.onClick.AddListener(OnCompleteProfileButtonClick);
    }

    private void CambiarColor()
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    private async void OnCompleteProfileButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            m_SinInternetUI.SetActive(true);
        }

        string userName = userNameInput.text.Trim();
        string temOcupacion = PlayerPrefs.GetString("TemOcupacion", "").Trim();
        bool ocupacionGuardada = !string.IsNullOrEmpty(temOcupacion);

        if (roles.value != 0 && !ocupacionGuardada)
        {
            MostrarMensaje("Debes seleccionar una ocupación antes de continuar", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(userName))
        {
            MostrarMensaje("Debes ingresar un nombre de usuario", Color.red);
            return;
        }

        if (userName.Length < 8 || userName.Length > 10)
        {
            MostrarMensaje("El nombre debe tener entre 8 y 10 caracteres", Color.red);
            return;
        }

        bool disponible = await validarNombreUsuarioUseCase.EstaDisponible(userName);
        if (!disponible)
        {
            MostrarMensaje("El nombre ya esta en uso. Elige otro", Color.red);
            return;
        }

        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.SetInt("EmailVerified", 1);
        PlayerPrefs.Save();

        bool perfilActualizado = await actualizarPerfilUsuarioUseCase.Ejecutar(userName);

        if (perfilActualizado)
        {
            await GuardarYSubirDatos();
        }
        else
        {
            MostrarMensaje("Error al actualizar el perfil.", Color.red);
        }
    }

    public async Task GuardarYSubirDatos()
    {
        string userdId = FirebaseServiceLocator.Auth.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(userdId))
        {
            MostrarMensaje("No se encontró usuario autenticado", Color.red);
            return;
        }

        ocupacionSeleccionada = PlayerPrefs.HasKey("TemOcupacion") ? PlayerPrefs.GetString("TemOcupacion", "") : roles.options[roles.value].text;

        var userData = new System.Collections.Generic.Dictionary<string, object>
        { { "DisplarName", FirebaseServiceLocator.Auth.CurrentUser.DisplayName },
            {"Email", FirebaseServiceLocator.Auth.CurrentUser.Email },
            {"Ocupacion", ocupacionSeleccionada },
            {"EstadoEncuestaAprendizaje", PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1 },
            {"EstadoEncuestaConocimiento", PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1},
            {"xp", PlayerPrefs.GetInt("TempXP", 0) },
            {"avatar", "Avatares/defecto" },
            {"Rango", "Novato de laboratorio" }
        };

        localStorage.Guardar("EstadoUser", "sinloguear");
        localStorage.Guardar("userId", userdId);
        localStorage.Guardar("TempOcupacion", ocupacionSeleccionada);
        localStorage.Eliminar("UsuarioEliminar");


        await guardarDatosUseCase.Ejecutar(userData);
        await actualizarRangoUseCase.Ejecutar();
        await subirDatosJSONUseCase.Ejecutar();

        SceneManager.LoadScene("Login");
    }

    private void MostrarMensaje(string mensaje, Color color)
    {
        panelMessage.SetActive(true);
        txtMensaje.text = mensaje;
        txtMensaje.color = color;
    }

    public void ClosePanelMessage()
    {
        panelMessage.SetActive(false);
    }
}
