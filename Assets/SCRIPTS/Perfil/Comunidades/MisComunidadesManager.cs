using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class MisComunidadesManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;
    public TMP_InputField inputBusqueda;
    public Button botonBuscar;
    public TMP_Text textoEstado;
    public GameObject panelEstado;

    [Header("Configuración Live Search")]
    public float tiempoEsperaLiveSearch = 0.3f;
    private Coroutine liveSearchCoroutine;

    [Header("Configuración de Mensajes")]
    public string mensajeCargando = "Cargando comunidades...";
    public string mensajeNoResultados = "No se encontraron coincidencias";
    public string mensajeError = "Error al cargar los datos";
    public string mensajeListo = "{0} comunidades encontradas";

    [Header("Componentes de Tarjeta")]
    public string formatoMiembros = "{0} Miembros";

    [Header("Referencia Panel Detalle")]
    public GameObject panelDetalleGrupo;
    public ComunidadDetalleManager detalleManager;

    [Header("Referencia a panel SIN comunidades")]
    [SerializeField] private GameObject panelSinComunidades;

    private string usuarioActualId;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private List<Dictionary<string, object>> todasComunidades = new List<Dictionary<string, object>>();

    // MODIFICADO: Variable para el idioma
    private string appIdioma;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // MODIFICADO: Obtener idioma y configurar textos de la UI
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");
        InicializarTextosUI();

        if (panelEstado != null) panelEstado.SetActive(false);

        if (auth.CurrentUser != null)
        {
            usuarioActualId = auth.CurrentUser.UserId;
            MostrarMensajeEstado(mensajeCargando, true);
            CargarComunidadesDelUsuario();

            if (botonBuscar != null)
            {
                botonBuscar.onClick.AddListener(BuscarComunidades);
            }

            if (inputBusqueda != null)
            {
                inputBusqueda.onValueChanged.AddListener(IniciarLiveSearch);
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            // MODIFICADO: Texto de error traducido
            string errorAuth = (appIdioma == "ingles") ? "No authenticated user" : "No hay usuario autenticado";
            MostrarMensajeEstado(errorAuth, true);
            Debug.LogWarning(errorAuth);
        }

        if (panelDetalleGrupo != null && detalleManager == null)
        {
            detalleManager = panelDetalleGrupo.GetComponent<ComunidadDetalleManager>();
        }
    }

    // MODIFICADO: Nuevo método para centralizar la traducción de textos de la UI
    void InicializarTextosUI()
    {
        if (appIdioma == "ingles")
        {
            mensajeCargando = "Loading your communities...";
            mensajeNoResultados = "No matches found";
            mensajeError = "Error loading data";
            mensajeListo = "{0} communities found";
            formatoMiembros = "{0} Members";

            // Traducir el texto del panel "Sin Comunidades"
            if (panelSinComunidades != null)
            {
                TMP_Text textoPanel = panelSinComunidades.GetComponentInChildren<TMP_Text>();
                if (textoPanel != null)
                {
                    textoPanel.text = "You are not a member of any community yet. Go and explore!";
                }
            }
        }
        // Si no es "en", se usan los valores por defecto en español del inspector.
    }

    void IniciarLiveSearch(string texto)
    {
        if (liveSearchCoroutine != null)
        {
            StopCoroutine(liveSearchCoroutine);
        }
        liveSearchCoroutine = StartCoroutine(RealizarLiveSearch(texto));
    }

    IEnumerator RealizarLiveSearch(string texto)
    {
        yield return new WaitForSeconds(tiempoEsperaLiveSearch);
        BuscarComunidades();
    }

    void MostrarMensajeEstado(string mensaje, bool mostrar = true)
    {
        if (textoEstado != null)
        {
            textoEstado.text = mensaje;
        }

        if (panelEstado != null)
        {
            panelEstado.SetActive(mostrar);
        }

        // Ocultar mensajes de éxito automáticamente
        if (mostrar && (mensaje.Contains("encontradas") || mensaje.Contains("found")))
        {
            Invoke("OcultarPanelEstado", 3f);
        }
    }

    void OcultarPanelEstado()
    {
        if (panelEstado != null && textoEstado.text != mensajeError && textoEstado.text != mensajeCargando)
        {
            panelEstado.SetActive(false);
        }
    }

    public void CargarComunidadesDelUsuario()
    {
        Query query = db.Collection("comunidades")
                      .WhereArrayContains("miembros", usuarioActualId);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                MostrarMensajeEstado(mensajeError, true);
                Debug.LogError("Error al cargar comunidades del usuario: " + task.Exception);
                return;
            }

            todasComunidades.Clear();

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();
                data["documentId"] = doc.Id;
                todasComunidades.Add(data);
            }

            if (todasComunidades.Count == 0)
            {
                if (panelSinComunidades != null) panelSinComunidades.SetActive(true);
            }
            else
            {
                if (panelSinComunidades != null) panelSinComunidades.SetActive(false);
            }

            MostrarTodasComunidades();
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    void BuscarComunidades()
    {
        if (inputBusqueda == null)
        {
            MostrarTodasComunidades();
            return;
        }

        string terminoBusqueda = inputBusqueda.text.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            MostrarTodasComunidades();
            return;
        }

        int resultadosEncontrados = 0;

        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        foreach (var comunidad in todasComunidades)
        {
            string nombre = comunidad.GetValueOrDefault("nombre", "").ToString().ToLower();
            if (nombre.Contains(terminoBusqueda))
            {
                CrearTarjetaComunidad(comunidad);
                resultadosEncontrados++;
            }
        }

        if (resultadosEncontrados > 0)
        {
            // MODIFICADO: Mensaje de resultados traducido
            string msgResultados = (appIdioma == "ingles")
                ? $"{resultadosEncontrados} results found"
                : $"Se encontraron {resultadosEncontrados} resultados";
            MostrarMensajeEstado(msgResultados, true);
        }
        else
        {
            MostrarMensajeEstado(mensajeNoResultados, true);
        }
    }

    void MostrarTodasComunidades()
    {
        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        foreach (var comunidad in todasComunidades)
        {
            CrearTarjetaComunidad(comunidad);
        }

        if (todasComunidades.Count > 0)
        {
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        }
    }

    void CrearTarjetaComunidad(Dictionary<string, object> dataComunidad)
    {
        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);
        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        Image imageComunidad = FindChildByName(tarjeta, "ImageComunidad")?.GetComponent<Image>();

        // MODIFICADO: Texto por defecto traducido
        string nombre = dataComunidad.GetValueOrDefault("nombre", (appIdioma == "ingles") ? "Unnamed" : "Sin nombre").ToString();
        string ComunidadPath = dataComunidad.GetValueOrDefault("imagenRuta", "").ToString();

        Sprite imageSprite = Resources.Load<Sprite>(ComunidadPath) ?? Resources.Load<Sprite>("Comunidades/ImagenesComunidades/default");
        if (imageComunidad != null) imageComunidad.sprite = imageSprite;

        int cantidadMiembros = 0;
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
        {
            cantidadMiembros = miembros.Count;
        }

        foreach (TMP_Text texto in textos)
        {
            switch (texto.gameObject.name)
            {
                case "TextoNombre":
                    texto.text = nombre;
                    break;
                case "TextoMiembros":
                    texto.text = string.Format(formatoMiembros, cantidadMiembros);
                    break;
            }
        }

        Button botonDetalle = FindChildByName(tarjeta, "BotonVerDetalle")?.GetComponent<Button>();
        if (botonDetalle != null)
        {
            botonDetalle.onClick.AddListener(() => MostrarDetalleComunidad(dataComunidad));
        }
    }

    void MostrarDetalleComunidad(Dictionary<string, object> dataComunidad)
    {
        if (detalleManager != null)
        {
            detalleManager.MostrarDetalle(dataComunidad, usuarioActualId);
        }
    }

    GameObject FindChildByName(GameObject parent, string name)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.gameObject;
            GameObject found = FindChildByName(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }

    public bool HayConexion()
    {
        bool hayConexion = Application.internetReachability != NetworkReachability.NotReachable;
        if (!hayConexion)
        {
            // MODIFICADO: Mensaje de conexión traducido
            string msgNoConexion = (appIdioma == "ingles")
                ? "No internet connection. Some features may not be available."
                : "No hay conexión a internet. Algunas funciones pueden no estar disponibles.";
            MostrarMensajeEstado(msgNoConexion, true);
        }
        return hayConexion;
    }
}