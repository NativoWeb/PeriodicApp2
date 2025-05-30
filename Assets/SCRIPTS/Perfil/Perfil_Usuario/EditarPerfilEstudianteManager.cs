//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;
//using Firebase.Auth;
//using Firebase.Firestore;
//using System;
//using Firebase.Extensions;
//using System.Security.Cryptography;
//using System.Net;


//public class EditarPerfilEstudianteManager : MonoBehaviour
//{
//    // instancias firebase 
//    private FirebaseAuth auth;
//    private FirebaseFirestore db;
//    private FirebaseUser currentUser;
//    private string userId;


//    [Header("Panel Editar Perfil y componentes")]
//    [SerializeField] public GameObject panelEditar;
//    [SerializeField] private TMP_Dropdown edadDropdown;
//    [SerializeField] private TMP_Dropdown departamentoDropdown;
//    [SerializeField] private TMP_Dropdown ciudadDropdown;
//    [SerializeField] private TMP_Text messageTxt; // Referencia al texto para mensajes
//    public Button GuardarCambios;


//    private Dictionary<string, List<string>> ciudadesPorDepartamento = new Dictionary<string, List<string>>();

//    void Start()
//    {
//        // inicializamos firebase
//        auth = FirebaseAuth.DefaultInstance;
//        db = FirebaseFirestore.DefaultInstance;
//        currentUser = auth.CurrentUser;
//        userId = currentUser.UserId;

//        if (string.IsNullOrEmpty(userId))
//        {
//            Debug.Log(" Sin usuario autenticado, desde editarPerfilEstudiante");
//            return;
//        }
//        CargarTotalementeDropDowns();
//        verificarCampos();
//        GuardarCambios.onClick.AddListener(ActualizarDatos);
//    }

//    private async void verificarCampos()
//    {
//        if (!HayInternet())
//        {
//            messageTxt.text = ("no hay CONEXION A INTERNET");
//            messageTxt.color = Color.red;
//            return;
//        }
//        DocumentReference userRef = db.Collection("users").Document(userId);
//        DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
//        if (snapshot.Exists)
//        {
//            Dictionary<string, object> datos = snapshot.ToDictionary();

//            bool tieneedad = datos.ContainsKey("Edad");
//            bool tienedepartamento = datos.ContainsKey("Departamento");
//            bool tieneciudad = datos.ContainsKey("Ciudad");

//            if (tieneedad && tieneciudad && tienedepartamento)
//            {
//                GetuserData();
//            }
//            else
//            {
//                CargarTotalementeDropDowns();
//            }
//        }
//    }

//    private async void GetuserData()
//    {
//        DocumentReference UserRef = db.Collection("users").Document(userId);

//        try
//        {
//            DocumentSnapshot snapshot = await UserRef.GetSnapshotAsync();
//            if (snapshot.Exists)
//            {
//                int edad = snapshot.GetValue<int>("Edad");
//                string departamento = snapshot.GetValue<string>("Departamento");
//                string ciudad = snapshot.GetValue<string>("Ciudad");

//                // llenamos los dropdowns con la informaci�n ya del usuario 
//                for (int i = 0; i < edadDropdown.options.Count; i++)
//                {
//                    if (edadDropdown.options[i].text == edad.ToString())
//                    {
//                        edadDropdown.value = i;
//                        break;
//                    }
//                }

//                // Establecer departamento
//                for (int i = 0; i < departamentoDropdown.options.Count; i++)
//                {
//                    if (departamentoDropdown.options[i].text == departamento)
//                    {
//                        departamentoDropdown.value = i;
//                        break;
//                    }
//                }

//                // Actualizar ciudades para el departamento seleccionado
//                ActualizarCiudades();

//                // Establecer ciudad (despu�s de actualizar las ciudades)
//                for (int i = 0; i < ciudadDropdown.options.Count; i++)
//                {
//                    if (ciudadDropdown.options[i].text == ciudad)
//                    {
//                        ciudadDropdown.value = i;
//                        break;
//                    }
//                }
//            }

//        }
//        catch (Exception e)
//        {
//            Debug.Log($"Error al conseguir los datos del usuario : {e.Message}");
//        }
//    }

//    void LlenarDropdowns()
//    {
//        // Lista de edades con opci�n inicial "Seleccione edad"
//        List<string> edades = new List<string> { "Seleccione edad" };
//        for (int i = 10; i <= 100; i++)
//        {
//            edades.Add(i.ToString());
//        }
//        ActualizarDropdown(edadDropdown, edades);

//        // Lista de departamentos de Colombia con opci�n inicial
//        List<string> departamentos = new List<string> { "Seleccione departamento" };
//        departamentos.AddRange(ciudadesPorDepartamento.Keys);
//        ActualizarDropdown(departamentoDropdown, departamentos);

//        // Inicializar ciudades con opci�n inicial
//        List<string> ciudadesInicial = new List<string> { "Seleccione ciudad" };
//        ActualizarDropdown(ciudadDropdown, ciudadesInicial);
//    }

//    void LlenarCiudadesPorDepartamento()
//    {
//        ciudadesPorDepartamento["Amazonas"] = new List<string> { "Leticia", "Puerto Nari�o" };
//        ciudadesPorDepartamento["Antioquia"] = new List<string> { "Medell�n", "Bello", "Envigado", "Itag��", "Rionegro", "Apartad�", "Turbo", "Sabaneta" };
//        ciudadesPorDepartamento["Arauca"] = new List<string> { "Arauca", "Saravena", "Tame", "Arauquita" };
//        ciudadesPorDepartamento["Atl�ntico"] = new List<string> { "Barranquilla", "Soledad", "Malambo", "Sabanalarga", "Puerto Colombia" };
//        ciudadesPorDepartamento["Bol�var"] = new List<string> { "Cartagena", "Magangu�", "Turbaco", "Arjona", "El Carmen de Bol�var" };
//        ciudadesPorDepartamento["Boyac�"] = new List<string> { "Tunja", "Duitama", "Sogamoso", "Chiquinquir�", "Paipa" };
//        ciudadesPorDepartamento["Caldas"] = new List<string> { "Manizales", "La Dorada", "Chinchin�", "Villamar�a", "Riosucio" };
//        ciudadesPorDepartamento["Caquet�"] = new List<string> { "Florencia", "San Vicente del Cagu�n", "Puerto Rico", "Doncello" };
//        ciudadesPorDepartamento["Casanare"] = new List<string> { "Yopal", "Aguazul", "Villanueva", "Tauramena" };
//        ciudadesPorDepartamento["Cauca"] = new List<string> { "Popay�n", "Santander de Quilichao", "Pat�a", "Puerto Tejada" };
//        ciudadesPorDepartamento["Cesar"] = new List<string> { "Valledupar", "Aguachica", "Codazzi", "La Jagua de Ibirico" };
//        ciudadesPorDepartamento["Choc�"] = new List<string> { "Quibd�", "Istmina", "Condoto", "Bah�a Solano" };
//        ciudadesPorDepartamento["C�rdoba"] = new List<string> { "Monter�a", "Ceret�", "Sahag�n", "Lorica", "Montel�bano" };
//        ciudadesPorDepartamento["Cundinamarca"] = new List<string> { "Bogot�", "Soacha", "Zipaquir�", "Girardot", "Facatativ�", "Ch�a", "Fusagasug�" };
//        ciudadesPorDepartamento["Guain�a"] = new List<string> { "In�rida" };
//        ciudadesPorDepartamento["Guaviare"] = new List<string> { "San Jos� del Guaviare", "Calamar", "Miraflores" };
//        ciudadesPorDepartamento["Huila"] = new List<string> { "Neiva", "Pitalito", "Garz�n", "La Plata" };
//        ciudadesPorDepartamento["La Guajira"] = new List<string> { "Riohacha", "Maicao", "Uribia", "Fonseca" };
//        ciudadesPorDepartamento["Magdalena"] = new List<string> { "Santa Marta", "Ci�naga", "Fundaci�n", "El Banco" };
//        ciudadesPorDepartamento["Meta"] = new List<string> { "Villavicencio", "Acac�as", "Granada", "Puerto Gait�n" };
//        ciudadesPorDepartamento["Nari�o"] = new List<string> { "Pasto", "Ipiales", "Tumaco", "T�querres" };
//        ciudadesPorDepartamento["Norte de Santander"] = new List<string> { "C�cuta", "Oca�a", "Pamplona", "Villa del Rosario" };
//        ciudadesPorDepartamento["Putumayo"] = new List<string> { "Mocoa", "Puerto As�s", "Orito", "Valle del Guamuez" };
//        ciudadesPorDepartamento["Quind�o"] = new List<string> { "Armenia", "Circasia", "Montenegro", "Calarc�" };
//        ciudadesPorDepartamento["Risaralda"] = new List<string> { "Pereira", "Dosquebradas", "Santa Rosa de Cabal", "La Virginia" };
//        ciudadesPorDepartamento["San Andr�s y Providencia"] = new List<string> { "San Andr�s", "Providencia" };
//        ciudadesPorDepartamento["Santander"] = new List<string> { "Bucaramanga", "Floridablanca", "Gir�n", "Piedecuesta", "Barrancabermeja" };
//        ciudadesPorDepartamento["Sucre"] = new List<string> { "Sincelejo", "Corozal", "Sampu�s", "San Marcos" };
//        ciudadesPorDepartamento["Tolima"] = new List<string> { "Ibagu�", "Espinal", "Melgar", "Honda" };
//        ciudadesPorDepartamento["Valle del Cauca"] = new List<string> { "Cali", "Palmira", "Buenaventura", "Tulu�", "Cartago", "Buga" };
//        ciudadesPorDepartamento["Vaup�s"] = new List<string> { "Mit�" };
//        ciudadesPorDepartamento["Vichada"] = new List<string> { "Puerto Carre�o", "La Primavera" };
//    }

//    void ActualizarCiudades()
//    {
//        string departamentoSeleccionado = departamentoDropdown.options[departamentoDropdown.value].text;

//        if (departamentoSeleccionado == "Seleccione departamento")
//        {
//            List<string> ciudadesInicial = new List<string> { "Seleccione ciudad" };
//            ActualizarDropdown(ciudadDropdown, ciudadesInicial);
//            return;
//        }

//        if (ciudadesPorDepartamento.ContainsKey(departamentoSeleccionado))
//        {
//            List<string> ciudades = new List<string> { "Seleccione ciudad" };
//            ciudades.AddRange(ciudadesPorDepartamento[departamentoSeleccionado]);
//            ActualizarDropdown(ciudadDropdown, ciudades);
//        }
//    }

//    void ActualizarDropdown(TMP_Dropdown dropdown, List<string> opciones)
//    {
//        dropdown.ClearOptions();
//        dropdown.AddOptions(opciones);
//    }

//    private void CargarTotalementeDropDowns()
//    {
//        LlenarCiudadesPorDepartamento();
//        LlenarDropdowns();
//        departamentoDropdown.onValueChanged.AddListener(delegate { ActualizarCiudades(); });
//    }

//    public void activarPanelEditar()
//    {
//        panelEditar.SetActive(true);
//    }

//    public void desactivarPanelEditar()
//    {
//        if (HayInternet())
//        {
//            // Validar que todos los campos est�n seleccionados
//            if (edadDropdown.options[edadDropdown.value].text == "Seleccione edad" ||
//                departamentoDropdown.options[departamentoDropdown.value].text == "Seleccione departamento" ||
//                ciudadDropdown.options[ciudadDropdown.value].text == "Seleccione ciudad")
//            {
//                messageTxt.text = "Por favor complete todos los campos";
//                messageTxt.color = Color.red;
//                return;
//            }
//        }
//        // limpiamos el messageText al salir del panel 

//        if (panelEditar != null)
//            panelEditar.SetActive(false);

//        messageTxt.text = ("");

//    }

//    // M�todo para guardar los cambios en el perfil
//    private void ActualizarDatos()
//    {
//        if (!HayInternet())
//        {
//            messageTxt.text = ("no hay CONEXION A INTERNET");
//            messageTxt.color = Color.red;
//            return;
//        }
//        // Validar que todos los campos est�n seleccionados
//        if (edadDropdown.options[edadDropdown.value].text == "Seleccione edad" ||
//            departamentoDropdown.options[departamentoDropdown.value].text == "Seleccione departamento" ||
//            ciudadDropdown.options[ciudadDropdown.value].text == "Seleccione ciudad")
//        {
//            messageTxt.text = "Por favor complete todos los campos";
//            messageTxt.color = Color.red;
//            return;
//        }

//        // Todos los campos son v�lidos, proceder a actualizar
//        int edad = int.Parse(edadDropdown.options[edadDropdown.value].text);
//        string departamento = departamentoDropdown.options[departamentoDropdown.value].text;
//        string ciudad = ciudadDropdown.options[ciudadDropdown.value].text;


//        // Si todo est� bien, proceder con el guardado

//        // si todo es v�lido entramos a actualizar el perfil 
//        DocumentReference userRef = db.Collection("users").Document(userId);
//        Dictionary<string, object> datosUsuario = new Dictionary<string, object>
//        {
//            { "Edad", edad },
//            { "Departamento", departamento },
//            { "Ciudad", ciudad }
//        };

//        userRef.SetAsync(datosUsuario, SetOptions.MergeAll).ContinueWithOnMainThread(Task =>
//        {
//            if (Task.IsCompletedSuccessfully)
//            {
//                Debug.Log("Datos actualizados Correctamente");
//                Invoke("cerrarPanelEditar", 2f);
//            }
//            else
//            {
//                Debug.LogError("Error al guardar los datos: " + Task.Exception);
//                messageTxt.text = "Error al actualizar los datos";
//                messageTxt.color = Color.red;
//            }
//        });

//        messageTxt.text = "Perfil actualizado correctamente";
//        messageTxt.color = Color.green;


//    }

//    public void cerrarPanelEditar()
//    {
//        if (panelEditar != null)
//        {
//            panelEditar.SetActive(false);
//            messageTxt.text = ("");
//        }
//    }
//    public bool HayInternet()
//    {
//        try
//        {
//            using (var client = new WebClient())
//            using (var stream = client.OpenRead("http://www.google.com"))
//            {
//                return true;
//            }
//        }
//        catch
//        {
//            return false;
//        }
//    }
//}