using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Core.Domain.Interfaces;
using PeriodicApp.Infrastructure.Services;

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
    private TMP_InputField nombresInput;
    private TMP_InputField edadInput;
    private TMP_InputField ciudadInput;
    private TMP_InputField generoInput;
    private string nombresUsuario;
    private int edadUsuario;
    private string ciudadUsuario;
    private string generoUsuario;
    private bool camposAdicionalesConfigurados;


    private async void Start()
    {
        bool inicializado = await FirebaseServiceLocator.InicializarFirebase();
        if (!inicializado)
        {
            MostrarMensaje("Error al inicializar Firebase", Color.red);
            return;
        }

        //Iniciar Servicios
        localStorage = new LocalStorageService();
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

        ConfigurarCamposAdicionales();
    }

    private void CambiarColor()
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    private void ConfigurarCamposAdicionales()
    {
        if (camposAdicionalesConfigurados || userNameInput == null)
        {
            return;
        }

        RectTransform parent = userNameInput.transform.parent as RectTransform;
        RectTransform userNameRect = userNameInput.GetComponent<RectTransform>();
        if (parent == null || userNameRect == null)
        {
            return;
        }

        float spacing = 130f;
        Vector2 basePosition = userNameRect.anchoredPosition;
        int baseIndex = userNameRect.GetSiblingIndex();

        nombresInput = CrearCampoAdicional("inputNames", "Nombres completos", basePosition + new Vector2(0, spacing), parent);
        edadInput = CrearCampoAdicional("inputAge", "Edad", basePosition - new Vector2(0, spacing), parent, TMP_InputField.ContentType.IntegerNumber);
        ciudadInput = CrearCampoAdicional("inputCity", "Ciudad", basePosition - new Vector2(0, 2f * spacing), parent);
        generoInput = CrearCampoAdicional("inputGender", "Género", basePosition - new Vector2(0, 3f * spacing), parent);

        if (nombresInput != null)
        {
            nombresInput.transform.SetSiblingIndex(baseIndex);
        }

        userNameRect.SetSiblingIndex(baseIndex + 1);

        if (edadInput != null)
        {
            edadInput.transform.SetSiblingIndex(baseIndex + 2);
        }

        if (ciudadInput != null)
        {
            ciudadInput.transform.SetSiblingIndex(baseIndex + 3);
        }

        if (generoInput != null)
        {
            generoInput.transform.SetSiblingIndex(baseIndex + 4);
        }

        AjustarPosicionCampo(nombresInput, basePosition + new Vector2(0, spacing));
        AjustarPosicionCampo(userNameInput, basePosition);
        AjustarPosicionCampo(edadInput, basePosition - new Vector2(0, spacing));
        AjustarPosicionCampo(ciudadInput, basePosition - new Vector2(0, 2f * spacing));
        AjustarPosicionCampo(generoInput, basePosition - new Vector2(0, 3f * spacing));

        if (m_OcupacionUI != null)
        {
            RectTransform ocupacionRect = m_OcupacionUI.GetComponent<RectTransform>();
            if (ocupacionRect != null)
            {
                ocupacionRect.SetSiblingIndex(baseIndex + 5);
                ocupacionRect.anchoredPosition = basePosition - new Vector2(0, 4f * spacing);
            }
        }

        if (completeProfileButton != null)
        {
            RectTransform buttonRect = completeProfileButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.SetSiblingIndex(baseIndex + 6);
                buttonRect.anchoredPosition = basePosition - new Vector2(0, 5f * spacing);
            }
        }

        camposAdicionalesConfigurados = true;
    }

    private TMP_InputField CrearCampoAdicional(string nombreObjeto, string placeholder, Vector2 posicion, RectTransform parent, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
    {
        TMP_InputField campo = Instantiate(userNameInput, parent);
        campo.gameObject.name = nombreObjeto;

        TMP_Text placeholderText = campo.transform.Find("Text Area/Placeholder")?.GetComponent<TMP_Text>();
        if (placeholderText != null)
        {
            placeholderText.text = placeholder;
        }

        TMP_Text textComponent = campo.transform.Find("Text Area/Text")?.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = string.Empty;
        }

        campo.text = string.Empty;
        campo.contentType = contentType;
        campo.ForceLabelUpdate();

        AjustarPosicionCampo(campo, posicion);

        return campo;
    }

    private void AjustarPosicionCampo(TMP_InputField campo, Vector2 posicion)
    {
        if (campo == null)
        {
            return;
        }

        RectTransform rect = campo.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = posicion;
        }
    }

    private async void OnCompleteProfileButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            m_SinInternetUI.SetActive(true);
        }

        string nombres = nombresInput != null ? nombresInput.text.Trim() : string.Empty;
        string edadTexto = edadInput != null ? edadInput.text.Trim() : string.Empty;
        string ciudad = ciudadInput != null ? ciudadInput.text.Trim() : string.Empty;
        string genero = generoInput != null ? generoInput.text.Trim() : string.Empty;
        string userName = userNameInput.text.Trim();
        string temOcupacion = PlayerPrefs.GetString("TemOcupacion", "").Trim();
        bool ocupacionGuardada = !string.IsNullOrEmpty(temOcupacion);

        if (string.IsNullOrEmpty(nombres))
        {
            MostrarMensaje("Debes ingresar tus nombres", Color.red);
            return;
        }

        if (!int.TryParse(edadTexto, out int edad) || edad <= 0)
        {
            MostrarMensaje("Debes ingresar una edad válida", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(ciudad))
        {
            MostrarMensaje("Debes ingresar tu ciudad", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(genero))
        {
            MostrarMensaje("Debes ingresar tu género", Color.red);
            return;
        }

        if (roles.value == 0 && !ocupacionGuardada)
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

        nombresUsuario = nombres;
        edadUsuario = edad;
        ciudadUsuario = ciudad;
        generoUsuario = genero;
        PlayerPrefs.SetString("DisplayName", userName);
        PlayerPrefs.SetInt("EmailVerified", 1);
        PlayerPrefs.Save();

        bool perfilActualizado = await actualizarPerfilUsuarioUseCase.EjecutarAsync(userName);

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
        { { "DisplayName", FirebaseServiceLocator.Auth.CurrentUser.DisplayName },
            {"Email", FirebaseServiceLocator.Auth.CurrentUser.Email },
            {"Ocupacion", ocupacionSeleccionada },
            {"EstadoEncuestaAprendizaje", PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1 },
            {"EstadoEncuestaConocimiento", PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1},
            {"xp", PlayerPrefs.GetInt("TempXP", 0) },
            {"avatar", "Avatares/defecto" },
            {"Rango", "Novato de laboratorio" },
            {"Nombres", string.IsNullOrEmpty(nombresUsuario) ? (nombresInput != null ? nombresInput.text.Trim() : string.Empty) : nombresUsuario },
            {"Edad", edadUsuario > 0 ? edadUsuario : (edadInput != null && int.TryParse(edadInput.text.Trim(), out int edadLocal) ? edadLocal : 0) },
            {"Ciudad", string.IsNullOrEmpty(ciudadUsuario) ? (ciudadInput != null ? ciudadInput.text.Trim() : string.Empty) : ciudadUsuario },
            {"Genero", string.IsNullOrEmpty(generoUsuario) ? (generoInput != null ? generoInput.text.Trim() : string.Empty) : generoUsuario }
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
