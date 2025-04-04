using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.UI;
using Firebase.Extensions;
using System;

[RequireComponent(typeof(NetworkConnectionChecker))]
public class DatosPersonales : MonoBehaviour
{
    // Constantes para evitar strings mágicos
    private const string AGE_KEY = "Edad";
    private const string DEPARTMENT_KEY = "Departamento";
    private const string CITY_KEY = "Ciudad";
    private const string SELECT_DEPARTMENT = "Seleccionar";
    private const string SELECT_CITY = "Seleccione un departamento";
    private const string USERS_COLLECTION = "users";

    [Header("UI References")]
    [SerializeField] private TMP_Dropdown edadDropdown;
    [SerializeField] private TMP_Dropdown departamentoDropdown;
    [SerializeField] private TMP_Dropdown ciudadDropdown;
    [SerializeField] private Button btnGuardar;
    [SerializeField] private Button btnActualizar;
    [SerializeField] private GameObject connectionWarningPanel;

    private NetworkConnectionChecker connectionChecker;
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;

    private Dictionary<string, List<string>> ciudadesPorDepartamento = new Dictionary<string, List<string>>();

    private void Awake()
    {
        connectionChecker = GetComponent<NetworkConnectionChecker>();
        InitializeFirebase();
        InitializeDropdowns();
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        if (auth.CurrentUser != null)
        {
            currentUser = auth.CurrentUser;
            userId = currentUser.UserId;
        }
    }

    private void InitializeDropdowns()
    {
        btnGuardar.onClick.AddListener(GuardarDatos);
        btnActualizar.onClick.AddListener(ActivarDropdowns);

        departamentoDropdown.onValueChanged.AddListener(delegate { ActualizarCiudades(); });

        LlenarCiudadesPorDepartamento();
        LlenarDropdowns();
    }

    private void Start()
    {
        connectionChecker.OnConnectionChanged += HandleConnectionChange;
        CheckInitialConnection();
    }

    private void OnDestroy()
    {
        connectionChecker.OnConnectionChanged -= HandleConnectionChange;
    }

    private void CheckInitialConnection()
    {
        if (connectionChecker.HasInternetConnection)
        {
            VerificarCampos();
        }
        else
        {
            ShowConnectionWarning();
            LoadOfflineData();
        }
    }

    private void HandleConnectionChange(bool isConnected)
    {
        if (isConnected)
        {
            connectionWarningPanel.SetActive(false);
            VerificarCampos();
        }
        else
        {
            ShowConnectionWarning();
        }
    }

    private void ShowConnectionWarning()
    {
        connectionWarningPanel.SetActive(true);
        Debug.LogWarning("No hay conexión a internet. Algunas funciones pueden estar limitadas.");
    }

    private void LoadOfflineData()
    {
        // Cargar datos desde PlayerPrefs si existen
        if (PlayerPrefs.HasKey(AGE_KEY) &&
            PlayerPrefs.HasKey(DEPARTMENT_KEY) &&
            PlayerPrefs.HasKey(CITY_KEY))
        {
            SetDropdownValues(
                PlayerPrefs.GetInt(AGE_KEY),
                PlayerPrefs.GetString(DEPARTMENT_KEY),
                PlayerPrefs.GetString(CITY_KEY)
            );
        }
    }

    private async void VerificarCampos()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("User ID is not available");
            return;
        }

        DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(userId);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Dictionary<string, object> datos = snapshot.ToDictionary();

                bool hasAllFields = datos.ContainsKey(AGE_KEY.ToLower()) &&
                                  datos.ContainsKey(DEPARTMENT_KEY.ToLower()) &&
                                  datos.ContainsKey(CITY_KEY.ToLower());

                if (hasAllFields)
                {
                    Debug.Log("Todos los campos existen. Cargando datos...");
                    GetUserData();
                }
                else
                {
                    Debug.Log("Faltan uno o más campos. Permitiendo edición...");
                    EnableEditing();
                }
            }
            else
            {
                Debug.Log("El documento no existe. Permitiendo edición...");
                EnableEditing();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al verificar campos: {e.Message}");
            LoadOfflineData();
        }
    }

    private async void GetUserData()
    {
        try
        {
            DocumentReference userRef = db.Collection(USERS_COLLECTION).Document(userId);
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                int edad = snapshot.GetValue<int>(AGE_KEY.ToLower());
                string departamento = snapshot.GetValue<string>(DEPARTMENT_KEY.ToLower());
                string ciudad = snapshot.GetValue<string>(CITY_KEY.ToLower());

                // Guardar en PlayerPrefs para uso offline
                PlayerPrefsManager.SaveUserData(edad, departamento, ciudad);

                // Actualizar UI
                SetDropdownValues(edad, departamento, ciudad);
                DisableDropdowns();

                Debug.Log("Datos personales cargados correctamente");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al obtener datos del usuario: {e.Message}");
            LoadOfflineData();
        }
    }

    private void SetDropdownValues(int edad, string departamento, string ciudad)
    {
        // Asignar edad
        int edadIndex = edadDropdown.options.FindIndex(option => option.text == edad.ToString());
        if (edadIndex != -1) edadDropdown.value = edadIndex;

        // Asignar departamento
        int departamentoIndex = departamentoDropdown.options.FindIndex(option => option.text == departamento);
        if (departamentoIndex != -1) departamentoDropdown.value = departamentoIndex;

        // Asignar ciudad
        int ciudadIndex = ciudadDropdown.options.FindIndex(option => option.text == ciudad);
        if (ciudadIndex != -1) ciudadDropdown.value = ciudadIndex;
    }

    private void LlenarDropdowns()
    {
        // Lista de edades (1 a 100 años)
        List<string> edades = new List<string>();
        for (int i = 1; i <= 100; i++)
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

    private void LlenarCiudadesPorDepartamento()
    {
        // Limpiar diccionario primero
        ciudadesPorDepartamento.Clear();

        // Agregar opciones por defecto
        ciudadesPorDepartamento[SELECT_DEPARTMENT] = new List<string> { SELECT_CITY };

        // Datos reales de departamentos y ciudades
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

    private void ActualizarCiudades()
    {
        string departamentoSeleccionado = departamentoDropdown.options[departamentoDropdown.value].text;

        if (ciudadesPorDepartamento.TryGetValue(departamentoSeleccionado, out List<string> ciudades))
        {
            ActualizarDropdown(ciudadDropdown, ciudades);
        }
    }

    private void ActualizarDropdown(TMP_Dropdown dropdown, List<string> opciones)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(opciones);
    }

    public void GuardarDatos()
    {
        if (!connectionChecker.HasInternetConnection)
        {
            Debug.LogWarning("No se puede guardar sin conexión a internet");
            ShowConnectionWarning();
            return;
        }

        if (auth.CurrentUser == null)
        {
            Debug.LogError("No hay usuario autenticado.");
            return;
        }

        string edadtxt = edadDropdown.options[edadDropdown.value].text;
        string departamento = departamentoDropdown.options[departamentoDropdown.value].text;
        string ciudad = ciudadDropdown.options[ciudadDropdown.value].text;

        if (!int.TryParse(edadtxt, out int edad) ||
            departamento == SELECT_DEPARTMENT ||
            ciudad == SELECT_CITY)
        {
            Debug.LogWarning("Datos inválidos. Por favor complete todos los campos correctamente.");
            return;
        }

        DocumentReference userRef = db.Collection(USERS_COLLECTION).Document(userId);

        Dictionary<string, object> datosUsuario = new Dictionary<string, object>
        {
            { AGE_KEY.ToLower(), edad },
            { DEPARTMENT_KEY.ToLower(), departamento },
            { CITY_KEY.ToLower(), ciudad }
        };

        userRef.SetAsync(datosUsuario, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Datos guardados en Firestore");

                // Guardar también en PlayerPrefs para uso offline
                PlayerPrefsManager.SaveUserData(edad, departamento, ciudad);

                DisableDropdowns();
            }
            else
            {
                Debug.LogError("Error al guardar los datos: " + task.Exception);
            }
        });
    }

    public void ActivarDropdowns()
    {
        if (!connectionChecker.HasInternetConnection)
        {
            ShowConnectionWarning();
            return;
        }

        EnableEditing();
        Debug.Log("Dropdowns activados para edición.");
    }

    private void EnableEditing()
    {
        edadDropdown.interactable = true;
        departamentoDropdown.interactable = true;
        ciudadDropdown.interactable = true;
    }

    private void DisableDropdowns()
    {
        edadDropdown.interactable = false;
        departamentoDropdown.interactable = false;
        ciudadDropdown.interactable = false;
    }
}

// Clase auxiliar para manejar PlayerPrefs
public static class PlayerPrefsManager
{
    public static void SaveUserData(int age, string department, string city)
    {
        PlayerPrefs.SetInt("Edad", age);
        PlayerPrefs.SetString("Departamento", department);
        PlayerPrefs.SetString("Ciudad", city);
        PlayerPrefs.Save();
    }

    public static (int age, string department, string city) LoadUserData()
    {
        return (
            PlayerPrefs.GetInt("Edad", 0),
            PlayerPrefs.GetString("Departamento", ""),
            PlayerPrefs.GetString("Ciudad", "")
        );
    }
}

// Clase para verificar conexión a internet
public class NetworkConnectionChecker : MonoBehaviour
{
    public event Action<bool> OnConnectionChanged;

    private bool lastConnectionStatus;

    private void Start()
    {
        lastConnectionStatus = Application.internetReachability != NetworkReachability.NotReachable;
        InvokeRepeating(nameof(CheckConnection), 1f, 1f);
    }

    private void CheckConnection()
    {
        bool currentStatus = Application.internetReachability != NetworkReachability.NotReachable;

        if (currentStatus != lastConnectionStatus)
        {
            lastConnectionStatus = currentStatus;
            OnConnectionChanged?.Invoke(currentStatus);
        }
    }

    public bool HasInternetConnection => lastConnectionStatus;
}