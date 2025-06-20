using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using Firebase.Extensions;
using System.Security.Cryptography;
using System.Net;
using System.Linq;
using System.Threading.Tasks;

public class EditarPerfilEstudianteManager : MonoBehaviour
{
    // Instancias Firebase 
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;
    private string userId;

    [Header("Panel Editar Perfil y componentes")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_Dropdown edadDropdown;
    [SerializeField] private TMP_Dropdown departamentoDropdown;
    [SerializeField] private TMP_Dropdown ciudadDropdown;
    [SerializeField] private TMP_Text messageTxt;
    public Button GuardarCambios;

    [Header("Configuración de mensajes")]
    [SerializeField] private float messageDuration = 3f;
    private Coroutine currentMessageCoroutine;

    private Dictionary<string, List<string>> ciudadesPorDepartamento = new Dictionary<string, List<string>>();

    void Start()
    {
        // Inicializamos Firebase
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        currentUser = auth.CurrentUser;
        userId = currentUser.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.Log("Sin usuario autenticado, desde editarPerfilEstudiante");
            return;
        }

        CargarTotalementeDropDowns();
        verificarCampos();
        GuardarCambios.onClick.AddListener(ActualizarDatos);
    }

    private void ShowMessage(string message, bool isError = false)
    {
        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
        }

        messageTxt.text = message;
        messageTxt.color = isError ? Color.red : Color.green;
        messageTxt.gameObject.SetActive(true);

        currentMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        messageTxt.text = "";
        messageTxt.gameObject.SetActive(false);
        currentMessageCoroutine = null;
    }

    private async void verificarCampos()
    {
        if (!HayInternet())
        {
            ShowMessage("No hay CONEXIÓN A INTERNET", true);
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(userId);
        DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Dictionary<string, object> datos = snapshot.ToDictionary();

            bool tieneUsername = datos.ContainsKey("DisplayName");
            bool tieneEdad = datos.ContainsKey("Edad");
            bool tieneDepartamento = datos.ContainsKey("Departamento");
            bool tieneCiudad = datos.ContainsKey("Ciudad");

            if (tieneUsername && tieneEdad && tieneCiudad && tieneDepartamento)
            {
                GetuserData();
            }
            else
            {
                CargarTotalementeDropDowns();
            }
        }
    }

    private async void GetuserData()
    {
        DocumentReference UserRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await UserRef.GetSnapshotAsync();
            if (snapshot.Exists)
            {
                if (snapshot.ContainsField("DisplayName"))
                {
                    usernameInput.text = snapshot.GetValue<string>("DisplayName");
                }

                if (snapshot.ContainsField("Edad"))
                {
                    int edad = snapshot.GetValue<int>("Edad");
                    for (int i = 0; i < edadDropdown.options.Count; i++)
                    {
                        if (edadDropdown.options[i].text == edad.ToString())
                        {
                            edadDropdown.value = i;
                            break;
                        }
                    }
                }

                if (snapshot.ContainsField("Departamento"))
                {
                    string departamento = snapshot.GetValue<string>("Departamento");
                    for (int i = 0; i < departamentoDropdown.options.Count; i++)
                    {
                        if (departamentoDropdown.options[i].text == departamento)
                        {
                            departamentoDropdown.value = i;
                            break;
                        }
                    }
                }

                ActualizarCiudades();

                if (snapshot.ContainsField("Ciudad"))
                {
                    string ciudad = snapshot.GetValue<string>("Ciudad");
                    for (int i = 0; i < ciudadDropdown.options.Count; i++)
                    {
                        if (ciudadDropdown.options[i].text == ciudad)
                        {
                            ciudadDropdown.value = i;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error al conseguir los datos del usuario: {e.Message}");
        }
    }

    void LlenarDropdowns()
    {
        List<string> edades = new List<string> { "Seleccione edad" };
        for (int i = 10; i <= 100; i++) edades.Add(i.ToString());
        ActualizarDropdown(edadDropdown, edades);

        List<string> departamentos = new List<string> { "Seleccione departamento" };
        departamentos.AddRange(ciudadesPorDepartamento.Keys);
        ActualizarDropdown(departamentoDropdown, departamentos);

        List<string> ciudadesInicial = new List<string> { "Seleccione ciudad" };
        ActualizarDropdown(ciudadDropdown, ciudadesInicial);
    }

    void LlenarCiudadesPorDepartamento()
    {
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

        if (departamentoSeleccionado == "Seleccione departamento")
        {
            ActualizarDropdown(ciudadDropdown, new List<string> { "Seleccione ciudad" });
            return;
        }

        if (ciudadesPorDepartamento.ContainsKey(departamentoSeleccionado))
        {
            List<string> ciudades = new List<string> { "Seleccione ciudad" };
            ciudades.AddRange(ciudadesPorDepartamento[departamentoSeleccionado]);
            ActualizarDropdown(ciudadDropdown, ciudades);
        }
    }

    void ActualizarDropdown(TMP_Dropdown dropdown, List<string> opciones)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(opciones);
    }

    private void CargarTotalementeDropDowns()
    {
        LlenarCiudadesPorDepartamento();
        LlenarDropdowns();
        departamentoDropdown.onValueChanged.AddListener(delegate { ActualizarCiudades(); });
    }

    public void desactivarPanelEditar()
    {
        if (HayInternet())
        {
            if (string.IsNullOrEmpty(usernameInput.text) ||
                edadDropdown.options[edadDropdown.value].text == "Seleccione edad" ||
                departamentoDropdown.options[departamentoDropdown.value].text == "Seleccione departamento" ||
                ciudadDropdown.options[ciudadDropdown.value].text == "Seleccione ciudad")
            {
                ShowMessage("Por favor complete todos los campos", true);
                return;
            }
        }

        if (currentMessageCoroutine != null)
        {
            StopCoroutine(currentMessageCoroutine);
            currentMessageCoroutine = null;
        }
        messageTxt.text = "";
        messageTxt.gameObject.SetActive(false);
    }

    private async void ActualizarDatos()
    {
        if (!HayInternet())
        {
            ShowMessage("No hay CONEXIÓN A INTERNET", true);
            return;
        }

        if (string.IsNullOrEmpty(usernameInput.text) ||
            edadDropdown.options[edadDropdown.value].text == "Seleccione edad" ||
            departamentoDropdown.options[departamentoDropdown.value].text == "Seleccione departamento" ||
            ciudadDropdown.options[ciudadDropdown.value].text == "Seleccione ciudad")
        {
            ShowMessage("Por favor complete todos los campos", true);
            return;
        }

        string username = usernameInput.text.Trim();
        if (username.Length < 8 || username.Length > 10)
        {
            ShowMessage("El nombre de usuario debe tener entre 8 y 10 caracteres", true);
            return;
        }

        try
        {
            DocumentSnapshot userSnapshot = await db.Collection("users").Document(userId).GetSnapshotAsync();
            string currentUsername = userSnapshot.GetValue<string>("DisplayName");

            if (username != currentUsername)
            {
                bool usernameDisponible = await VerificarUsernameDisponible(username);
                if (!usernameDisponible)
                {
                    ShowMessage("El nombre de usuario ya está en uso", true);
                    return;
                }
            }

            if (username != currentUser.DisplayName)
            {
                UserProfile profile = new UserProfile { DisplayName = username };
                await currentUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(task => {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        Debug.LogError("Error al actualizar el perfil en Auth: " + task.Exception);
                        return;
                    }
                    currentUser = auth.CurrentUser;
                });
            }

            int edad = int.Parse(edadDropdown.options[edadDropdown.value].text);
            string departamento = departamentoDropdown.options[departamentoDropdown.value].text;
            string ciudad = ciudadDropdown.options[ciudadDropdown.value].text;

            DocumentReference userRef = db.Collection("users").Document(userId);
            Dictionary<string, object> datosUsuario = new Dictionary<string, object>
            {
                { "DisplayName", username },
                { "Edad", edad },
                { "Departamento", departamento },
                { "Ciudad", ciudad }
            };

            await userRef.SetAsync(datosUsuario, SetOptions.MergeAll);
            ShowMessage("Perfil actualizado correctamente");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al guardar los datos: " + ex.Message);
            ShowMessage("Error al actualizar los datos", true);
        }
    }

    private async Task<bool> VerificarUsernameDisponible(string username)
    {
        try
        {
            Query query = db.Collection("users").WhereEqualTo("DisplayName", username);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                if (document.Id != userId)
                {
                    return false;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al verificar nombre de usuario: {e.Message}");
            return false;
        }
    }

    public bool HayInternet()
    {
        try
        {
            using (var client = new WebClient())
            using (var stream = client.OpenRead("http://www.google.com"))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}