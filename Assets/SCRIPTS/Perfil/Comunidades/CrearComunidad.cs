using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class CrearComunidad : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nombreInput;
    public TMP_InputField descripcionInput;
    public Toggle publicaToggle;
    public Toggle privadaToggle;
    public Button crearButton;
    public TMP_Text mensajeTexto;

    [Header("Referencias para imagen de comunidad")]
    public GameObject panelSelectorImagenes; // Panel que muestra las imágenes
    public Image imagenSeleccionada; // Imagen que se muestra como seleccionada
    private string rutaImagenSeleccionada = "";
    public GameObject contenedorImagenes;
    public GameObject prefabBotonImagen;
    public Sprite spriteDefault;

    [Header("Contadores de caracteres")]
    public TMP_Text contadorNombre;
    public TMP_Text contadorDescripcion;

    private string currentUserId;
    private string currentUsername;

    private async void Start()
    {
        // Configurar listeners
        crearButton.onClick.AddListener(OnCrearComunidad);

        // Cargar información del usuario
        await CargarDatosUsuario();
        CargarImagenesDisponibles();

        // El botón siempre está habilitado
        crearButton.interactable = true;

        nombreInput.onValueChanged.AddListener(ActualizarContadorNombre);
        descripcionInput.onValueChanged.AddListener(ActualizarContadorDescripcion);

        // Mostrar los valores iniciales también
        ActualizarContadorNombre(nombreInput.text);
        ActualizarContadorDescripcion(descripcionInput.text);

    }

    private void ActualizarContadorNombre(string texto)
    {
        int cantidad = texto.Length;
        contadorNombre.text = $"{cantidad}/50";
    }

    private void ActualizarContadorDescripcion(string texto)
    {
        int cantidad = texto.Length;
        contadorDescripcion.text = $"{cantidad}/400";
    }

    public void AbrirSelectorImagenes()
    {
        CargarImagenesDisponibles();
        panelSelectorImagenes.SetActive(true);
    }
    void CargarImagenesDisponibles()
    {
        foreach (Transform child in contenedorImagenes.transform)
            Destroy(child.gameObject); // limpiar

        Sprite[] imagenes = Resources.LoadAll<Sprite>("Comunidades/ImagenesComunidades");

        foreach (var img in imagenes)
        {
            GameObject nuevoBoton = Instantiate(prefabBotonImagen, contenedorImagenes.transform);
            nuevoBoton.transform.Find("Imagen").GetComponent<Image>().sprite = img;

            string nombreImagen = img.name;

            nuevoBoton.GetComponent<Button>().onClick.AddListener(() =>
            {
                SeleccionarImagen(nombreImagen, img);
            });
        }
    }


    void SeleccionarImagen(string nombreImagen, Sprite sprite)
    {
        imagenSeleccionada.sprite = sprite;
        rutaImagenSeleccionada = "Comunidades/ImagenesComunidades/" + nombreImagen;
        panelSelectorImagenes.SetActive(false); // Cierra el panel
    }

    public void CerrarPanelSelectorImagen ()
    {
        if (panelSelectorImagenes != null)
        panelSelectorImagenes.SetActive(false);
    }
    private async Task CargarDatosUsuario()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            currentUserId = user.UserId;

            // Obtener el nombre de usuario desde Firestore
            DocumentReference docRef = FirebaseFirestore.DefaultInstance
                .Collection("users").Document(currentUserId);

            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                currentUsername = snapshot.GetValue<string>("DisplayName");
            }
            else
            {
                currentUsername = user.Email; // Fallback al email si no tiene username
            }
        }
    }

    private bool ValidarFormulario(out string mensajeError)
    {
        mensajeError = "";

        if (string.IsNullOrWhiteSpace(nombreInput.text))
        {
            mensajeError = "El nombre de la comunidad es requerido";
            return false;
        }

        if (string.IsNullOrWhiteSpace(descripcionInput.text))
        {
            mensajeError = "La descripción es requerida";
            return false;
        }

        if (!publicaToggle.isOn && !privadaToggle.isOn)
        {
            mensajeError = "Selecciona un tipo de comunidad";
            return false;
        }

        if (nombreInput.text.Length > 50)
        {
            mensajeError = "El nombre es demasiado largo (máx. 50 caracteres)";
            return false;
        }
        if( descripcionInput.text.Length > 400)
        {
            mensajeError = "La descripción es demasiada larga (máx. 400 caracteres)";
            return false;
        }

        if (string.IsNullOrEmpty(rutaImagenSeleccionada))
        {
            mensajeError = "Debes seleccionar una imagen para la comunidad.";
            return false;
        }
        return true;
    }

    private async void OnCrearComunidad()
    {
        bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        if (hayInternet)
        {


            // Validar el formulario
            if (!ValidarFormulario(out string mensajeError))
            {
                MostrarMensaje(mensajeError, true);
                return;
            }

            try
            {
                // Obtener datos del formulario
                string nombre = nombreInput.text.Trim();
                string descripcion = descripcionInput.text.Trim();
                string tipo = publicaToggle.isOn ? "publica" : "privada";

                // Crear documento en Firestore
                DocumentReference docRef = FirebaseFirestore.DefaultInstance.Collection("comunidades").Document();

                Dictionary<string, object> comunidad = new Dictionary<string, object>
            {
                { "nombre", nombre },
                { "imagenRuta", rutaImagenSeleccionada }, // aquí guardas la ruta
                { "descripcion", descripcion },
                { "tipo", tipo },
                { "fechaCreacion", Timestamp.GetCurrentTimestamp() },
                { "creadorId", currentUserId },
                { "creadorUsername", currentUsername }, // Guardamos el username
                { "miembros", new List<string> { currentUserId } } // Añadir creador como miembro
            };

                await docRef.SetAsync(comunidad);

                MostrarMensaje("Comunidad creada exitosamente!", false);
                LimpiarFormulario();
                Invoke("VolverAComunidad", 2f);
            }
            catch (Exception e)
            {
                MostrarMensaje($"Error al crear comunidad: {e.Message}", true);
                Debug.LogError(e);
            }
        }
        else
        {
            MostrarMensaje($"SIN CONEXION A INTERNET, esta operación no esta disponible por el momento, intente nuevamente más tarde", true);
            Invoke("VolverAComunidad", 4f);
        }
    }

    // función para volver al ranking si no tiene wifi
    void VolverAComunidad()
    {
        SceneManager.LoadScene("Comunidad");
    }
    private void MostrarMensaje(string mensaje, bool esError)
    {
        mensajeTexto.text = mensaje;
        CancelInvoke("OcultarMensaje");
        Invoke("OcultarMensaje", 4f);
    }

    private void OcultarMensaje()
    {
        mensajeTexto.text = "";
    }

    public void LimpiarFormulario()
    {
        nombreInput.text = "";
        descripcionInput.text = "";
        publicaToggle.isOn = false;
        privadaToggle.isOn = false;
        rutaImagenSeleccionada = "";
        imagenSeleccionada.sprite = null; // borra la imagen del botón
        imagenSeleccionada.sprite = spriteDefault;
        if (panelSelectorImagenes != null)
            panelSelectorImagenes.SetActive(false); 
               
    }
}