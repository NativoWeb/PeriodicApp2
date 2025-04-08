using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Extensions;
using Unity.Android.Types;
using UnityEngine.EventSystems;

public class DatosPersonales : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown edadDropdown;
    [SerializeField] private TMP_Dropdown departamentoDropdown;
    [SerializeField] private TMP_Dropdown ciudadDropdown;

    //panel dropdowns
    [SerializeField] private GameObject m_dropdownsUI = null;

    //panel para mostrar la información básica
    [SerializeField] private GameObject m_InfoBasicaUI = null;
    public TMP_Text edadtxt;
    public TMP_Text departamentotxt;
    public TMP_Text ciudadtxt;

    //Panel de entrada información básica
    [SerializeField] private GameObject m_PanelentradaUI = null;

    //
    public Button btnGuardar;
    public Button btnActualizar;
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
        DocumentReference docRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Dictionary<string, object> datos = snapshot.ToDictionary();

            // Verificamos si los campos existen
            bool tieneEdad = datos.ContainsKey("Edad");
            bool tieneDepartamento = datos.ContainsKey("Departamento");
            bool tieneCiudad = datos.ContainsKey("Ciudad");

            if (tieneEdad && tieneDepartamento && tieneCiudad)
            {

                ActivarPanelInfo();
                GetuserData();

            }
            else
            {
                ActivasPanelEntradaDatos();
                ActivarPanelDropdowns();
                CargarTotalementeDropDowns();


            }
        }
    }
    private async void GetuserData()
    {
 
        DocumentReference userRef = db.Collection("users").Document(userId);

        try
        {
            // Obtener el XP actual de Firebase
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

                Debug.Log("Get-user-Data desde DatosPersonales puso bien los player prefs");

                // asignamos los datos a los txt 
                edadtxt.text = edad.ToString();
                departamentotxt.text = departamento;
                ciudadtxt.text = ciudad;

            }else
            {
                Debug.Log("no ha completado datos básicos");
            }
         

        }
        catch (Exception e)
        {
            Debug.LogError($"❌ no se pudo actualizar la informacion basica del usuario: {e.Message}");
        }

    }

    private void MostrarDatosOffline()
    {

        //desactivamos el panel de los dropdowns
        m_dropdownsUI.SetActive(false);
        btnGuardar.interactable = false;
        // activamos panel informacion básica 
        m_InfoBasicaUI.SetActive(true);
        btnActualizar.interactable =false;

        // cargamos los datos al panel info usuario
        edadtxt.text = PlayerPrefs.GetInt("Edad", 0).ToString();
        departamentotxt.text = PlayerPrefs.GetString("Departamento", "");
        ciudadtxt.text = PlayerPrefs.GetString("Ciudad", "");
    }

    void LlenarDropdowns()
    {
        // Lista de edades (1 a 100 años)
        List<string> edades = new List<string>();
        for (int i = 0; i <= 100; i++)
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
            string estadouser = PlayerPrefs.GetString("Estadouser", "");

            string userId = auth.CurrentUser.UserId; // Obtener el ID del usuario 
            string edadtxt = edadDropdown.options[edadDropdown.value].text;
            string departamento = departamentoDropdown.options[departamentoDropdown.value].text;
            string ciudad = ciudadDropdown.options[ciudadDropdown.value].text;
            
            if (edadtxt != "0" && departamento != "Seleccionar" && ciudad != "Seleccione un departamento")
            {

                // pasamos edad a int antes de guardarlo en la bd
                int.TryParse(edadtxt, out int edad);

                // guardamos en player prefs por si no tiene wifi y es la primera vez que entra
                PlayerPrefs.SetInt("Edad", edad);
                PlayerPrefs.GetString("Departamento",departamento);
                PlayerPrefs.GetString("Ciudad", ciudad);

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
                        // Deshabilitar los Dropdowns y el botón
                        edadDropdown.interactable = false;
                        departamentoDropdown.interactable = false;
                        ciudadDropdown.interactable = false;

                    }
                    else
                    {
                        Debug.LogError("Error al guardar los datos: " + task.Exception);
                        
                    }
                });
            }
            else
            {
                Debug.Log("datos invalidos, no se pueden guardar datos a firebase");
            }

        }
        else
        {
            Debug.LogError("No hay usuario autenticado.");
        }
    }
    public void ActivarPanelDropdowns()
    {
        CargarTotalementeDropDowns();

        // Cargar los datos actuales en los Dropdowns
        int edadActual = PlayerPrefs.GetInt("Edad", 0);
        string departamentoActual = PlayerPrefs.GetString("Departamento", "");
        string ciudadActual = PlayerPrefs.GetString("Ciudad", "");

        if (edadActual > 0 && !string.IsNullOrEmpty(departamentoActual) && !string.IsNullOrEmpty(ciudadActual))
        {
            CargarDatosEnDropdowns(edadActual, departamentoActual, ciudadActual);
        }

        // activamos panel dropdwon y boton guardar
        m_dropdownsUI.SetActive(true);
        btnGuardar.interactable = true;

        // desactivo panel info y boton actualizar
        m_InfoBasicaUI.SetActive(false);

        // activo dropdowns
        edadDropdown.interactable = true;
        departamentoDropdown.interactable = true;
        ciudadDropdown.interactable = true;
    }

    private void ActivarPanelInfo()
    {
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

        // Establecer ciudad
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

}