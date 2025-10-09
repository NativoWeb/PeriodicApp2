using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PeriodicApp.Core.Application.UseCases;
using PeriodicApp.Core.Domain.Interfaces;
using PeriodicApp.Infrastructure.Services;
using PeriodicApp.Presentation;

public class RegisterController : MonoBehaviour
{

    [Header("UI - Campos de Entrada")]
    public TMP_InputField nombresInput;
    public TMP_InputField userNameInput;
    public TMP_InputField edadInput;
    public Dropdown departamentoDropdown;
    public Dropdown ciudadDropdown;
    public Dropdown generoDropdown;
    public Dropdown gradoEscolarDropdown;
    public TMP_InputField direccionViviendaInput;
    public Dropdown estratoSocioeconomicoDropdown;
    public Dropdown lugarResidenciaDropdown;

    [Header("UI - Otros Elementos")]
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
    private string nombresUsuario;
    private int edadUsuario;
    private string departamentoUsuario;
    private string ciudadUsuario;
    private string generoUsuario;
    private string gradoEscolarUsuario;
    private string direccionViviendaUsuario;
    private string estratoSocioeconomicoUsuario;
    private string lugarResidenciaUsuario;


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
        subirDatosJSONUseCase = new SubirDatosJSON(firestore,
            localStorage,
            ServiceLocator.Persistence);
        actualizarRangoUseCase = new ActualizarRangoUsuario(firestore, localStorage);

        ButtonMessage.onClick.AddListener(ClosePanelMessage);

        roles.AddOptions(new System.Collections.Generic.List<string> { "Seleccionar una ocupación", "Estudiante", "Profesor" });
        roles.value = 0;
        roles.onValueChanged.AddListener(delegate { CambiarColor(); });
        CambiarColor();

        // Inicializar dropdowns de departamento y ciudad
        InicializarDropdownDepartamento();
        departamentoDropdown.onValueChanged.AddListener(OnDepartamentoChanged);
        ciudadDropdown.interactable = false;

        // Inicializar dropdown de género
        InicializarDropdownGenero();

        // Inicializar dropdowns adicionales
        InicializarDropdownGradoEscolar();
        InicializarDropdownEstratoSocioeconomico();
        InicializarDropdownLugarResidencia();

        m_OcupacionUI.SetActive(!PlayerPrefs.HasKey("TemOcupacion"));

        completeProfileButton.onClick.AddListener(OnCompleteProfileButtonClick);

    }

    private void CambiarColor()
    {
        Text label = roles.captionText;
        label.color = (roles.value == 0) ? Color.gray : Color.black;
    }

    private void InicializarDropdownDepartamento()
    {
        departamentoDropdown.ClearOptions();
        var departamentos = DepartamentoCiudadService.ObtenerDepartamentos();
        departamentos.Insert(0, "Seleccionar departamento");
        departamentoDropdown.AddOptions(departamentos);
        departamentoDropdown.value = 0;
    }

    private void OnDepartamentoChanged(int index)
    {
        ciudadDropdown.ClearOptions();

        if (index == 0) // "Seleccionar departamento"
        {
            ciudadDropdown.interactable = false;
            ciudadDropdown.AddOptions(new System.Collections.Generic.List<string> { "Seleccionar ciudad" });
            ciudadDropdown.value = 0;
            return;
        }

        string departamentoSeleccionado = departamentoDropdown.options[index].text;
        var ciudades = DepartamentoCiudadService.ObtenerCiudadesPorDepartamento(departamentoSeleccionado);

        ciudades.Insert(0, "Seleccionar ciudad");
        ciudadDropdown.AddOptions(ciudades);
        ciudadDropdown.value = 0;
        ciudadDropdown.interactable = true;
    }

    private void InicializarDropdownGenero()
    {
        generoDropdown.ClearOptions();
        var generos = new System.Collections.Generic.List<string>
        {
            "Seleccionar género",
            "Femenino",
            "Masculino",
            "Otro"
        };
        generoDropdown.AddOptions(generos);
        generoDropdown.value = 0;
    }

    private void InicializarDropdownGradoEscolar()
    {
        gradoEscolarDropdown.ClearOptions();
        var grados = new System.Collections.Generic.List<string>
        {
            "Seleccionar grado escolar",
            "Preescolar",
            "1° Primaria",
            "2° Primaria",
            "3° Primaria",
            "4° Primaria",
            "5° Primaria",
            "6° Bachillerato",
            "7° Bachillerato",
            "8° Bachillerato",
            "9° Bachillerato",
            "10° Bachillerato",
            "11° Bachillerato",
            "Técnico",
            "Tecnólogo",
            "Universitario",
            "Posgrado"
        };
        gradoEscolarDropdown.AddOptions(grados);
        gradoEscolarDropdown.value = 0;
    }

    private void InicializarDropdownEstratoSocioeconomico()
    {
        estratoSocioeconomicoDropdown.ClearOptions();
        var estratos = new System.Collections.Generic.List<string>
        {
            "Seleccionar estrato",
            "Estrato 1",
            "Estrato 2",
            "Estrato 3",
            "Estrato 4",
            "Estrato 5",
            "Estrato 6"
        };
        estratoSocioeconomicoDropdown.AddOptions(estratos);
        estratoSocioeconomicoDropdown.value = 0;
    }

    private void InicializarDropdownLugarResidencia()
    {
        lugarResidenciaDropdown.ClearOptions();
        var lugares = new System.Collections.Generic.List<string>
        {
            "Seleccionar lugar de residencia",
            "Urbana",
            "Rural"
        };
        lugarResidenciaDropdown.AddOptions(lugares);
        lugarResidenciaDropdown.value = 0;
    }

  
    private async void OnCompleteProfileButtonClick()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            m_SinInternetUI.SetActive(true);
        }

        string nombres = nombresInput != null ? nombresInput.text.Trim() : string.Empty;
        string edadTexto = edadInput != null ? edadInput.text.Trim() : string.Empty;
        string departamento = departamentoDropdown != null && departamentoDropdown.value > 0
            ? departamentoDropdown.options[departamentoDropdown.value].text
            : string.Empty;
        string ciudad = ciudadDropdown != null && ciudadDropdown.value > 0
            ? ciudadDropdown.options[ciudadDropdown.value].text
            : string.Empty;
        string genero = generoDropdown != null && generoDropdown.value > 0
            ? generoDropdown.options[generoDropdown.value].text
            : string.Empty;
        string gradoEscolar = gradoEscolarDropdown != null && gradoEscolarDropdown.value > 0
            ? gradoEscolarDropdown.options[gradoEscolarDropdown.value].text
            : string.Empty;
        string direccionVivienda = direccionViviendaInput != null ? direccionViviendaInput.text.Trim() : string.Empty;
        string estratoSocioeconomico = estratoSocioeconomicoDropdown != null && estratoSocioeconomicoDropdown.value > 0
            ? estratoSocioeconomicoDropdown.options[estratoSocioeconomicoDropdown.value].text
            : string.Empty;
        string lugarResidencia = lugarResidenciaDropdown != null && lugarResidenciaDropdown.value > 0
            ? lugarResidenciaDropdown.options[lugarResidenciaDropdown.value].text
            : string.Empty;
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

        if (string.IsNullOrEmpty(departamento))
        {
            MostrarMensaje("Debes seleccionar un departamento", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(ciudad))
        {
            MostrarMensaje("Debes seleccionar una ciudad", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(genero))
        {
            MostrarMensaje("Debes seleccionar tu género", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(gradoEscolar))
        {
            MostrarMensaje("Debes seleccionar tu grado escolar", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(direccionVivienda))
        {
            MostrarMensaje("Debes ingresar tu dirección de vivienda", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(estratoSocioeconomico))
        {
            MostrarMensaje("Debes seleccionar tu estrato socioeconómico", Color.red);
            return;
        }

        if (string.IsNullOrEmpty(lugarResidencia))
        {
            MostrarMensaje("Debes seleccionar tu lugar de residencia", Color.red);
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
        departamentoUsuario = departamento;
        ciudadUsuario = ciudad;
        generoUsuario = genero;
        gradoEscolarUsuario = gradoEscolar;
        direccionViviendaUsuario = direccionVivienda;
        estratoSocioeconomicoUsuario = estratoSocioeconomico;
        lugarResidenciaUsuario = lugarResidencia;
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
            {"Departamento", string.IsNullOrEmpty(departamentoUsuario) ? (departamentoDropdown != null && departamentoDropdown.value > 0 ? departamentoDropdown.options[departamentoDropdown.value].text : string.Empty) : departamentoUsuario },
            {"Ciudad", string.IsNullOrEmpty(ciudadUsuario) ? (ciudadDropdown != null && ciudadDropdown.value > 0 ? ciudadDropdown.options[ciudadDropdown.value].text : string.Empty) : ciudadUsuario },
            {"Genero", string.IsNullOrEmpty(generoUsuario) ? (generoDropdown != null && generoDropdown.value > 0 ? generoDropdown.options[generoDropdown.value].text : string.Empty) : generoUsuario },
            {"GradoEscolar", string.IsNullOrEmpty(gradoEscolarUsuario) ? (gradoEscolarDropdown != null && gradoEscolarDropdown.value > 0 ? gradoEscolarDropdown.options[gradoEscolarDropdown.value].text : string.Empty) : gradoEscolarUsuario },
            {"DireccionVivienda", string.IsNullOrEmpty(direccionViviendaUsuario) ? (direccionViviendaInput != null ? direccionViviendaInput.text.Trim() : string.Empty) : direccionViviendaUsuario },
            {"EstratoSocioeconomico", string.IsNullOrEmpty(estratoSocioeconomicoUsuario) ? (estratoSocioeconomicoDropdown != null && estratoSocioeconomicoDropdown.value > 0 ? estratoSocioeconomicoDropdown.options[estratoSocioeconomicoDropdown.value].text : string.Empty) : estratoSocioeconomicoUsuario },
            {"LugarResidencia", string.IsNullOrEmpty(lugarResidenciaUsuario) ? (lugarResidenciaDropdown != null && lugarResidenciaDropdown.value > 0 ? lugarResidenciaDropdown.options[lugarResidenciaDropdown.value].text : string.Empty) : lugarResidenciaUsuario }
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
