using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Extensions;
using UnityEngine.EventSystems;
using System.Collections;

public class DatosPersonalesManager : MonoBehaviour
{
    [Header("panel y dropdowns")]
    [SerializeField] private GameObject m_dropdownsUI = null;
    [SerializeField] private TMP_Dropdown edadDropdown;
    [SerializeField] private TMP_Dropdown departamentoDropdown;
    [SerializeField] private TMP_Dropdown ciudadDropdown;

    //panel para mostrar la información básica
    [Header("Panel Información básica")]
    [SerializeField] private GameObject m_InfoBasicaUI = null;
    public TMP_Text edadtxt;
    public TMP_Text departamentotxt;
    public TMP_Text ciudadtxt;
    public TMP_Text Messagetxt;

    //Panel de entrada información básica
    [Header("Panel pop-up")]
    [SerializeField] private GameObject m_PanelentradaUI = null;

    [Header("btns información personal")]
    public Button btnGuardar;
    public Button btnActualizar;

    // instanciamos wifi 
    private bool hayInternet = false;

    // instanciamos firebase 
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;

    
    private Dictionary<string, List<string>> ciudadesPorDepartamento = new Dictionary<string, List<string>>();

    void Start()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        
        
        if (hayInternet)
        {
            // instanciamos firebase
            auth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;

            if (auth.CurrentUser != null)
            {
                btnGuardar.onClick.AddListener(GuardarDatos);

            }
            else
            {
                Debug.Log("no hay usuario autenticado");
            }
            // volvemos a activar los btn en caso de wifi
        
            VerificarCampos();
           
        }
        else
        {
           if (estadouser == "local")
            {
                ActivarPanelDropdowns();
                CargarTotalementeDropDowns();
            }
            else
            {
                MostrarDatosOffline();
            }
           

        }
        btnActualizar.onClick.AddListener(ActivarPanelDropdowns);
}

    async void VerificarCampos()
    {
        if (hayInternet)
        {
            DocumentReference docRef = db.Collection("users").Document(userId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Dictionary<string, object> datos = snapshot.ToDictionary();
                bool tieneEdad = datos.ContainsKey("Edad");
                bool tieneDepartamento = datos.ContainsKey("Departamento");
                bool tieneCiudad = datos.ContainsKey("Ciudad");

                if (tieneEdad && tieneDepartamento && tieneCiudad)
                {
                    // Cargar datos desde Firebase y mostrar en panel info
                    GetuserData();
                    ActivarPanelInfo();
                }
                else
                {
                    ActivasPanelEntradaDatos();
                    ActivarPanelDropdowns();
                }
            }
        }
        else
        {
            string estadouser = PlayerPrefs.GetString("Estadouser", "");
            if (estadouser == "local")
            {
                ActivarPanelDropdowns();
                CargarTotalementeDropDowns();
            }
            else
            {
                MostrarDatosOffline();
            }
        }
    }
    private async void GetuserData()
    {
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                int edad = snapshot.GetValue<int>("Edad");
                PlayerPrefs.SetInt("Edad", edad);
                string departamento = snapshot.GetValue<string>("Departamento");
                PlayerPrefs.SetString("Departamento", departamento);
                string ciudad = snapshot.GetValue<string>("Ciudad");
                PlayerPrefs.SetString("Ciudad", ciudad);
                PlayerPrefs.Save();

                // Actualizar UI inmediatamente
                edadtxt.text = edad.ToString();
                departamentotxt.text = departamento;
                ciudadtxt.text = ciudad;

                // Activar panel de información
                ActivarPanelInfo();
            }
            else
            {
                string mensaje = "Completa tus datos personales para continuar";
                StartCoroutine(MostrarMensajeTemporal(mensaje, 4f));
                ActivasPanelEntradaDatos();
            }
        }
        catch (Exception e)
        {
            string mensaje = "Error al cargar datos. Intenta nuevamente";
            StartCoroutine(MostrarMensajeTemporal(mensaje, 3f));
            Debug.LogError($"❌ no se pudo actualizar la informacion basica del usuario: {e.Message}");
        }
    }

    private void MostrarDatosOffline()
    {
        // Cargar datos desde PlayerPrefs
        edadtxt.text = PlayerPrefs.GetInt("Edad", 0).ToString();
        departamentotxt.text = PlayerPrefs.GetString("Departamento", "");
        ciudadtxt.text = PlayerPrefs.GetString("Ciudad", "");

        //desactivamos el panel de los dropdowns
        m_dropdownsUI.SetActive(false);
        btnGuardar.interactable = false;

        // activamos panel informacion básica 
        m_InfoBasicaUI.SetActive(true);
        btnActualizar.interactable = false;

        
        StartCoroutine(MostrarMensajeTemporal("Modo offline - Sin conexión a internet", 3f));
    }

    void LlenarDropdowns()
    {
        // Lista de edades (1 a 100 años)
        List<string> edades = new List<string>();
        for (int i = 10; i <= 100; i++)
        {
            edades.Add(i.ToString());
        }
        ActualizarDropdown(edadDropdown, edades);

        // Lista de departamentos de Colombia
        List<string> departamentos = new List<string>(ciudadesPorDepartamento.Keys);
        ActualizarDropdown(departamentoDropdown, departamentos);

        // Inicializar ciudades con el primer departamento
        ActualizarCiudades();
    }

    void LlenarCiudadesPorDepartamento()
    {
        ciudadesPorDepartamento["Seleccionar"] = new List<string> { "Seleccione un departamento" };
        ciudadesPorDepartamento["Amazonas"] = new List<string> { "Leticia", "Puerto Nariño" };
        ciudadesPorDepartamento["Antioquia"] = new List<string> { "Medellín", "Bello", "Envigado", "Itagüí", "Rionegro", "Apartadó", "Turbo", "Sabaneta" };
        ciudadesPorDepartamento["Arauca"] = new List<string> { "Arauca", "Saravena", "Tame", "Arauquita" };
        ciudadesPorDepartamento["Atlántico"] = new List<string> { "Barranquilla", "Soledad", "Malambo", "Sabanalarga", "Puerto Colombia" };
        ciudadesPorDepartamento["Bolívar"] = new List<string> { "Cartagena", "Magangué", "Turbaco", "Arjona", "El Carmen de Bolívar" };
        ciudadesPorDepartamento["Boyacá"] = new List<string> { "Tunja", "Duitama", "Sogamoso", "Chiquinquirá", "Paipa" };
        ciudadesPorDepartamento["Caldas"] = new List<string> { "Manizales", "La Dorada", "Chinchiná", "Villamaría", "Riosucio" };
        ciudadesPorDepartamento["Caquetá"] = new List<string> { "Florencia", "San Vicente del Caguán", "Puerto Rico", "Doncello" };
        ciudadesPorDepartamento["Casanare"] = new List<string> { "Yopal", "Aguazul", "Villanueva", "Tauramena" };
        ciudadesPorDepartamento["Cauca"] = new List<string> { "Popayán", "Santander de Quilichao", "Patía", "Puerto Tejada" };
        ciudadesPorDepartamento["Cesar"] = new List<string> { "Valledupar", "Aguachica", "Codazzi", "La Jagua de Ibirico" };
        ciudadesPorDepartamento["Chocó"] = new List<string> { "Quibdó", "Istmina", "Condoto", "Bahía Solano" };
        ciudadesPorDepartamento["Córdoba"] = new List<string> { "Montería", "Cereté", "Sahagún", "Lorica", "Montelíbano" };
        ciudadesPorDepartamento["Cundinamarca"] = new List<string> { "Bogotá", "Soacha", "Zipaquirá", "Girardot", "Facatativá", "Chía", "Fusagasugá" };
        ciudadesPorDepartamento["Guainía"] = new List<string> { "Inírida" };
        ciudadesPorDepartamento["Guaviare"] = new List<string> { "San José del Guaviare", "Calamar", "Miraflores" };
        ciudadesPorDepartamento["Huila"] = new List<string> { "Neiva", "Pitalito", "Garzón", "La Plata" };
        ciudadesPorDepartamento["La Guajira"] = new List<string> { "Riohacha", "Maicao", "Uribia", "Fonseca" };
        ciudadesPorDepartamento["Magdalena"] = new List<string> { "Santa Marta", "Ciénaga", "Fundación", "El Banco" };
        ciudadesPorDepartamento["Meta"] = new List<string> { "Villavicencio", "Acacías", "Granada", "Puerto Gaitán" };
        ciudadesPorDepartamento["Nariño"] = new List<string> { "Pasto", "Ipiales", "Tumaco", "Túquerres" };
        ciudadesPorDepartamento["Norte de Santander"] = new List<string> { "Cúcuta", "Ocaña", "Pamplona", "Villa del Rosario" };
        ciudadesPorDepartamento["Putumayo"] = new List<string> { "Mocoa", "Puerto Asís", "Orito", "Valle del Guamuez" };
        ciudadesPorDepartamento["Quindío"] = new List<string> { "Armenia", "Circasia", "Montenegro", "Calarcá" };
        ciudadesPorDepartamento["Risaralda"] = new List<string> { "Pereira", "Dosquebradas", "Santa Rosa de Cabal", "La Virginia" };
        ciudadesPorDepartamento["San Andrés y Providencia"] = new List<string> { "San Andrés", "Providencia" };
        ciudadesPorDepartamento["Santander"] = new List<string> { "Bucaramanga", "Floridablanca", "Girón", "Piedecuesta", "Barrancabermeja" };
        ciudadesPorDepartamento["Sucre"] = new List<string> { "Sincelejo", "Corozal", "Sampués", "San Marcos" };
        ciudadesPorDepartamento["Tolima"] = new List<string> { "Ibagué", "Espinal", "Melgar", "Honda" };
        ciudadesPorDepartamento["Valle del Cauca"] = new List<string> { "Cali", "Palmira", "Buenaventura", "Tuluá", "Cartago", "Buga" };
        ciudadesPorDepartamento["Vaupés"] = new List<string> { "Mitú" };
        ciudadesPorDepartamento["Vichada"] = new List<string> { "Puerto Carreño", "La Primavera" };
    }


    void ActualizarCiudades()
    {
        string departamentoSeleccionado = departamentoDropdown.options[departamentoDropdown.value].text;
        if (ciudadesPorDepartamento.ContainsKey(departamentoSeleccionado))
        {
            ActualizarDropdown(ciudadDropdown, ciudadesPorDepartamento[departamentoSeleccionado]);
        }
    }

    void ActualizarDropdown(TMP_Dropdown dropdown, List<string> opciones)
    {
        dropdown.ClearOptions(); // Limpiar opciones anteriores
        dropdown.AddOptions(opciones); // Agregar nuevas opciones
    }

    public void GuardarDatos()
    {
        // Limpiar mensaje previo
        Messagetxt.text = "";

        string userId = auth.CurrentUser.UserId;
        string edadtxt = edadDropdown.options[edadDropdown.value].text;
        string departamento = departamentoDropdown.options[departamentoDropdown.value].text;
        string ciudad = ciudadDropdown.options[ciudadDropdown.value].text;

        // Validar cada campo individualmente
        List<string> errores = new List<string>();

        // Si todo está bien, proceder con el guardado
        int.TryParse(edadtxt, out int edad);

        if (string.IsNullOrEmpty(edadtxt))
        {
            errores.Add("• Selecciona una edad válida");
        }

        if (departamento == "Seleccionar")
        {
            errores.Add("• Selecciona un departamento");
        }

        if (ciudad == "Seleccione un departamento" || string.IsNullOrEmpty(ciudad))
        {
            errores.Add("• Selecciona una ciudad");
        }

        // Si hay errores, mostrarlos y salir
        if (errores.Count > 0)
        {
            string mensajeError = "Por favor completa:\n" + string.Join("\n", errores);
            StartCoroutine(MostrarMensajeTemporal(mensajeError, 5f)); // 5 segundos para mensajes largos
            return;
        }

        

        // Guardar en PlayerPrefs
        PlayerPrefs.SetInt("Edad", edad);
        PlayerPrefs.SetString("Departamento", departamento);
        PlayerPrefs.SetString("Ciudad", ciudad);
        PlayerPrefs.Save();

        // Actualizar UI inmediatamente
        this.edadtxt.text = edad.ToString();
        departamentotxt.text = departamento;
        ciudadtxt.text = ciudad;

        if (auth.CurrentUser != null)
        {
            DocumentReference userRef = db.Collection("users").Document(userId);
            Dictionary<string, object> datosUsuario = new Dictionary<string, object>
        {
            { "Edad", edad },
            { "Departamento", departamento },
            { "Ciudad", ciudad }
        };

            userRef.SetAsync(datosUsuario, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("Datos guardados en Firestore");
                    edadDropdown.interactable = false;
                    departamentoDropdown.interactable = false;
                    ciudadDropdown.interactable = false;

                    // Mostrar mensaje de éxito
                    StartCoroutine(MostrarMensajeTemporal("✓ Datos guardados correctamente", 2f));

                    // Cambiar al panel de información y actualizar botones
                    ActivarPanelInfo();
                    
                }
                else
                {
                    Debug.LogError("Error al guardar los datos: " + task.Exception);
                    StartCoroutine(MostrarMensajeTemporal("✗ Error al guardar los datos", 3f));
                }
            });
        }
    }
    public void ActivarPanelDropdowns()
    {
        // Cargar los dropdowns
        CargarTotalementeDropDowns();

        // Cargar los datos actuales en los Dropdowns
        int edadActual = PlayerPrefs.GetInt("Edad", 0);
        string departamentoActual = PlayerPrefs.GetString("Departamento", "");
        string ciudadActual = PlayerPrefs.GetString("Ciudad", "");

        if (edadActual > 0 && !string.IsNullOrEmpty(departamentoActual) && !string.IsNullOrEmpty(ciudadActual))
        {
            // Esperar un frame para asegurar que los dropdowns están listos
            StartCoroutine(CargarDatosDespuesDeUnFrame(edadActual, departamentoActual, ciudadActual));
        }

        // Activar/desactivar elementos de UI
        m_dropdownsUI.SetActive(true);
        btnGuardar.interactable = true;
        m_InfoBasicaUI.SetActive(false);
        edadDropdown.interactable = true;
        departamentoDropdown.interactable = true;
        ciudadDropdown.interactable = true;
    }

    private System.Collections.IEnumerator CargarDatosDespuesDeUnFrame(int edad, string departamento, string ciudad)
    {
        yield return null; // Esperar un frame para asegurar que los dropdowns están listos
        CargarDatosEnDropdowns(edad, departamento, ciudad);
    }

    private void ActivarPanelInfo()
    {
        // Limpiar mensaje al mostrar la información
        Messagetxt.text = "";
        // Cargar datos actualizados desde PlayerPrefs
        edadtxt.text = PlayerPrefs.GetInt("Edad", 0).ToString();
        departamentotxt.text = PlayerPrefs.GetString("Departamento", "");
        ciudadtxt.text = PlayerPrefs.GetString("Ciudad", "");

        // activamos panel info y boton actualizar 
        m_InfoBasicaUI.SetActive(true);
        btnActualizar.interactable = true;

        // desactivo panel dropdowns y boton guardar
        m_dropdownsUI.SetActive(false);
        btnGuardar.interactable = false;

        // desactivo los dropdowns
        edadDropdown.interactable = false;
        departamentoDropdown.interactable = false;
        ciudadDropdown.interactable = false;

       
    }
    private void CargarTotalementeDropDowns()
    {
        LlenarCiudadesPorDepartamento();
        LlenarDropdowns();
        departamentoDropdown.onValueChanged.AddListener(delegate { ActualizarCiudades(); });
    }
    private void CargarDatosEnDropdowns(int edad, string departamento, string ciudad)
    {
        // Establecer edad
        for (int i = 0; i < edadDropdown.options.Count; i++)
        {
            if (edadDropdown.options[i].text == edad.ToString())
            {
                edadDropdown.value = i;
                break;
            }
        }

        // Establecer departamento
        for (int i = 0; i < departamentoDropdown.options.Count; i++)
        {
            if (departamentoDropdown.options[i].text == departamento)
            {
                departamentoDropdown.value = i;
                break;
            }
        }

        // Actualizar ciudades para el departamento seleccionado
        ActualizarCiudades();

        // Establecer ciudad (después de actualizar las ciudades)
        for (int i = 0; i < ciudadDropdown.options.Count; i++)
        {
            if (ciudadDropdown.options[i].text == ciudad)
            {
                ciudadDropdown.value = i;
                break;
            }
        }
    }
    private void ActivasPanelEntradaDatos()
    {

        m_PanelentradaUI.SetActive(true);
        // desactivamos los botones de amigos y comunidad
        
    }
    public void DesactivarPanelEntrada()
    {
        m_PanelentradaUI.SetActive(false);
        // Esperar un frame antes de enfocar el dropdown para evitar errores de UI
        StartCoroutine(SetDropdownFocus());
    }

    private System.Collections.IEnumerator SetDropdownFocus()
    {
        yield return null; // Esperar un frame

        EventSystem.current.SetSelectedGameObject(edadDropdown.gameObject);
    }
   
    private IEnumerator MostrarMensajeTemporal(string mensaje, float tiempo = 3f)
    {
        Messagetxt.text = mensaje;
        yield return new WaitForSeconds(tiempo);

        // Limpiar solo si el mensaje actual es el mismo que estamos mostrando
        if (Messagetxt.text == mensaje)
        {
            Messagetxt.text = "";
        }
    }

}