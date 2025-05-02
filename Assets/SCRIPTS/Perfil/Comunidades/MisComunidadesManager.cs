using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.SceneManagement;

public class MisComunidadesManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;
    public TMP_InputField inputBusqueda;
    public Button botonBuscar;
    public TMP_Text textoEstado; // Nuevo: Texto para mostrar mensajes de estado
    public GameObject panelEstado; // Nuevo: Panel contenedor del texto de estado

    [Header("Configuración Live Search")]
    public float tiempoEsperaLiveSearch = 0.3f; // Tiempo en segundos para esperar entre búsquedas
    private Coroutine liveSearchCoroutine; // Referencia a la corrutina de búsqueda

    [Header("Configuración de Mensajes")]
    public string mensajeCargando = "Cargando comunidades...";
    public string mensajeNoResultados = "No se encontraron coincidencias";
    public string mensajeError = "Error al cargar los datos";
    public string mensajeListo = "{0} comunidades encontradas";

    [Header("Componentes de Tarjeta")]
    public string formatoMiembros = "{0} Miembros";

    private string usuarioActualId;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private List<Dictionary<string, object>> todasComunidades = new List<Dictionary<string, object>>();


    // instanciamos nuevo panel 
    [Header("Elementos panel detalle")]
    public GameObject panelDetalleGrupo = null;
    public TMP_Text detalleNombre;
    public TMP_Text detalleDescripcion;
    public TMP_Text detalleFecha;
    public TMP_Text detalleCreador;
    public TMP_Text detalleMiembros;

    [Header("Panel Detalle Miembros")]
    public GameObject panelMiembros = null; // Panel con el ScrollView
    public Transform contenedorMiembros; // Contenedor donde se instanciarán los miembros
    public GameObject prefabMiembro; // Prefab de un TextMeshProUGUI o un diseño para cada miembro
    public Button btnVerMiembros; // Botón en el panel detalle que activará el panel miembros

    [Header("Panel Detalle Solicitudes")]
    public GameObject panelSolicitudes = null;
    public Transform contenedorSolicitudes;
    public GameObject prefabSolicitud;
    public Button btnVerSolicitudes;

    [Header("Botón Abandonar Comunidad")]
    public GameObject panelConfirmacionAbandonar = null;
    public Button btnAbandonarComunidad;
    public TMP_Text textoConfirmacionAbandonar;
    public Button btnCancelarAbandonar;
    public Button btnConfirmarAbandonar;
    private string comunidadActualId;


    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

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
                // Configurar el evento onValueChanged para el live search
                inputBusqueda.onValueChanged.AddListener(IniciarLiveSearch);

                // Mantener también el evento onSubmit para compatibilidad
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            MostrarMensajeEstado("No hay usuario autenticado", true);
            Debug.LogWarning("No hay usuario autenticado");
        }
        if (panelConfirmacionAbandonar != null)
        {
            panelConfirmacionAbandonar.SetActive(false);

            if (btnConfirmarAbandonar != null)
            {
                btnConfirmarAbandonar.onClick.AddListener(ConfirmarAbandonarComunidad);
            }

            if (btnCancelarAbandonar != null)
            {
                btnCancelarAbandonar.onClick.AddListener(() => panelConfirmacionAbandonar.SetActive(false));
            }
        }
    }

    // Método para iniciar la búsqueda en tiempo real con un pequeño retraso
    void IniciarLiveSearch(string texto)
    {
        // Cancelar cualquier búsqueda en progreso
        if (liveSearchCoroutine != null)
        {
            StopCoroutine(liveSearchCoroutine);
        }

        // Iniciar una nueva búsqueda después de un pequeño retraso
        liveSearchCoroutine = StartCoroutine(RealizarLiveSearch(texto));
    }

    // Corrutina para realizar la búsqueda después de un tiempo de espera
    IEnumerator RealizarLiveSearch(string texto)
    {
        // Esperar un momento para evitar múltiples búsquedas mientras el usuario sigue escribiendo
        yield return new WaitForSeconds(tiempoEsperaLiveSearch);

        // Realizar la búsqueda con el texto actual
        BuscarComunidades();
    }

    // Nuevo método: Muestra mensajes de estado al usuario
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

        // Oculta automáticamente mensajes de éxito después de 3 segundos
        if (mostrar && mensaje == string.Format(mensajeListo, todasComunidades.Count))
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
                Debug.LogError("Error al cargar comunidades");
                return;
            }

            todasComunidades.Clear();

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();
                data["documentId"] = doc.Id;

                // Asegurarse de que el campo creadorId esté presente
                if (!data.ContainsKey("creadorId") && data.ContainsKey("creador"))
                {
                    data["creadorId"] = data["creador"]; // Asumimos que 'creador' contiene el ID
                }

                todasComunidades.Add(data);

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

        // Si el campo de búsqueda está vacío, mostrar todas las comunidades
        if (string.IsNullOrWhiteSpace(terminoBusqueda))
        {
            MostrarTodasComunidades();
            return;
        }

        int resultadosEncontrados = 0;

        // Limpiar contenedor
        foreach (Transform child in contenedor)
        {
            Destroy(child.gameObject);
        }

        // Filtrar comunidades
        foreach (var comunidad in todasComunidades)
        {
            string nombre = comunidad.GetValueOrDefault("nombre", "").ToString().ToLower();

            if (nombre.Contains(terminoBusqueda))
            {
                CrearTarjetaComunidad(comunidad);
                resultadosEncontrados++;
            }
        }

        // Mostrar mensaje de resultados
        if (resultadosEncontrados > 0)
        {
            MostrarMensajeEstado($"Se encontraron {resultadosEncontrados} resultados", true);
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

        MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
    }
    void CrearTarjetaComunidad(Dictionary<string, object> dataComunidad)
    {
        // Instanciar la tarjeta
        GameObject tarjeta = Instantiate(tarjetaPrefab, contenedor);

        // Obtener referencias a los componentes de UI
        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
        GameObject iconoPrivado = FindChildByName(tarjeta, "IconoPrivado");
        GameObject iconoPublico = FindChildByName(tarjeta, "IconoPublico");

        // Extraer datos (con valores por defecto)
        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();


        // Manejo de la fecha
        string fechaFormateada = "Fecha desconocida";
        if (dataComunidad.TryGetValue("fechaCreacion", out object fechaObj))
        {
            if (fechaObj is Timestamp timestamp)
            {
                DateTime fecha = timestamp.ToDateTime();
                // Formatear la fecha en español (ejemplo: "15 enero 2023")
                fechaFormateada = fecha.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
            }
            else if (fechaObj is string fechaString)
            {
                fechaFormateada = fechaString; // Si ya es un string, lo usamos tal cual
            }
        }

        int cantidadMiembros = 0;
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
        {
            cantidadMiembros = miembros.Count;
        }

        // Configurar UI
        foreach (TMP_Text texto in textos)
        {
            switch (texto.gameObject.name)
            {
                case "TextoNombre":
                    texto.text = nombre;
                    break;
                case "TextoDescripcion":
                    texto.text = descripcion;
                    break;
                case "TextoFecha":
                    texto.text = fechaFormateada;
                    break;
                case "TextoMiembros":
                    texto.text = string.Format(formatoMiembros, cantidadMiembros);
                    break;
                case "TextoTipo":
                    texto.text = tipo == "privada" ? "Privada" : "Pública";
                    break;
            }
        }

        // Configurar iconos
        if (iconoPrivado != null && iconoPublico != null)
        {
            iconoPrivado.SetActive(tipo == "privada");
            iconoPublico.SetActive(tipo != "privada");
        }

        // Configurar botón de detalle
        Button botonDetalle = FindChildByName(tarjeta, "BotonVerDetalle")?.GetComponent<Button>();
        if (botonDetalle != null)
        {
            botonDetalle.onClick.AddListener(() => MostrarDetalleComunidad(dataComunidad));
        }

    }


    // Método auxiliar para encontrar hijos por nombre
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
    void MostrarDetalleComunidad(Dictionary<string, object> dataComunidad)
    {
        LimpiarYCerrarPaneles();

        if (panelDetalleGrupo == null) return;

        // Extraer datos de la comunidad
        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();
        string creador = dataComunidad.GetValueOrDefault("creadorUsername", "Sin creador").ToString();
        string creadorId = dataComunidad.GetValueOrDefault("creadorId", "").ToString();

        // Manejo de la fecha
        string fechaFormateada = "Fecha desconocida";
        if (dataComunidad.TryGetValue("fechaCreacion", out object fechaObj))
        {
            if (fechaObj is Timestamp timestamp)
            {
                DateTime fecha = timestamp.ToDateTime();
                fechaFormateada = fecha.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("es-ES"));
            }
            else if (fechaObj is string fechaString)
            {
                fechaFormateada = fechaString;
            }
        }

        // Obtener cantidad de miembros
        int cantidadMiembros = 0;
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
        {
            cantidadMiembros = miembros.Count;
        }

        // Llenar los textos de los detalles
        detalleNombre.text = nombre;
        detalleDescripcion.text = descripcion;
        detalleFecha.text = $"Creada el {fechaFormateada}";
        detalleCreador.text = $"Creada por {creador}";
        detalleMiembros.text = $"{cantidadMiembros} miembros";

        // Mostrar el panel de detalles
        panelDetalleGrupo.SetActive(true);

        // Configurar botones
        btnVerMiembros.onClick.RemoveAllListeners();
        btnVerMiembros.onClick.AddListener(() => {
            LimpiarYCerrarPaneles();
            MostrarMiembros(dataComunidad);
            ActualizarEstadoBotones(true, false); // Activar botón miembros, desactivar solicitudes
        });

        // Configurar botón de solicitudes solo si el usuario actual es el creador
        if (btnVerSolicitudes != null)
        {
            bool esCreador = usuarioActualId == creadorId;
            btnVerSolicitudes.interactable = esCreador;
            btnVerSolicitudes.gameObject.SetActive(esCreador);

            if (esCreador)
            {
                btnVerSolicitudes.onClick.RemoveAllListeners();
                btnVerSolicitudes.onClick.AddListener(() => {
                    LimpiarYCerrarPaneles();
                    MostrarSolicitudes(dataComunidad);
                    ActualizarEstadoBotones(false, true); // Desactivar botón miembros, activar solicitudes
                });
            }
        }

        // Llamar automáticamente a MostrarMiembros
        MostrarMiembros(dataComunidad);
        ActualizarEstadoBotones(true, false); // Inicialmente activar botón miembros

        // Seleccionar el botón para navegación con teclado/controller
        if (btnVerMiembros != null && btnVerMiembros.gameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(btnVerMiembros.gameObject);
        }

        // configurar el btn de abandonarComunidad
        if (btnAbandonarComunidad != null)
        {
            btnAbandonarComunidad.onClick.RemoveAllListeners();
            btnAbandonarComunidad.onClick.AddListener(() => MostrarConfirmacionAbandonar(dataComunidad));

            // Ocultar el botón si el usuario es el creador
            bool esCreador = usuarioActualId == creadorId;
            btnAbandonarComunidad.gameObject.SetActive(!esCreador);
        }
    }
    // Nuevo método para actualizar el estado visual de los botones
    void ActualizarEstadoBotones(bool miembrosSeleccionado, bool solicitudesSeleccionado)
    {
        // Consigue los componentes de la imagen de los botones
        Image imgBtnMiembros = btnVerMiembros.GetComponent<Image>();
        Image imgBtnSolicitudes = btnVerSolicitudes ? btnVerSolicitudes.GetComponent<Image>() : null;

        // Configura color cuando está seleccionado (usa los colores que prefieras)
        Color colorSeleccionado = new Color(55, 189, 247, 255); // Azul - ajusta según tu UI
        Color colorNormal = Color.white; // Color normal - ajusta según tu UI

        // Actualiza los colores según el estado de selección
        if (imgBtnMiembros != null)
        {
            imgBtnMiembros.color = miembrosSeleccionado ? colorSeleccionado : colorNormal;
        }

        if (imgBtnSolicitudes != null)
        {
            imgBtnSolicitudes.color = solicitudesSeleccionado ? colorSeleccionado : colorNormal;
        }


        // Opcional: desactivar interacción con el botón seleccionado
        btnVerMiembros.interactable = !miembrosSeleccionado;
        if (btnVerSolicitudes) btnVerSolicitudes.interactable = !solicitudesSeleccionado;
    }

    void MostrarConfirmacionAbandonar(Dictionary<string, object> dataComunidad)
    {
        if (panelConfirmacionAbandonar == null) return;

        comunidadActualId = dataComunidad["documentId"].ToString();
        string nombreComunidad = dataComunidad.GetValueOrDefault("nombre", "esta comunidad").ToString();

        textoConfirmacionAbandonar.text = $"¿Estás seguro que deseas abandonar {nombreComunidad}?";
        panelConfirmacionAbandonar.SetActive(true);

        // Seleccionar el botón de cancelar por defecto para mejor UX
        EventSystem.current.SetSelectedGameObject(btnCancelarAbandonar.gameObject);
    }

    void ConfirmarAbandonarComunidad()
    {
        if (string.IsNullOrEmpty(comunidadActualId) || string.IsNullOrEmpty(usuarioActualId))
        {
            Debug.LogError("Falta información para abandonar la comunidad");
            return;
        }

        MostrarMensajeEstado("Abandonando comunidad...", true);
        panelConfirmacionAbandonar.SetActive(false);

        DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadActualId);

        // Eliminar al usuario de la lista de miembros
        comunidadRef.UpdateAsync("miembros", FieldValue.ArrayRemove(usuarioActualId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    MostrarMensajeEstado("Error al abandonar la comunidad", true);
                    Debug.LogError("Error al abandonar comunidad: " + task.Exception);
                    return;
                }

                MostrarMensajeEstado("Has abandonado la comunidad", true);

                // Volver a cargar las comunidades del usuario después de un breve retraso
                Invoke("VolverAScenaComunidades", 0.5f);
            });
    }

    void VolverAScenaComunidades()
    {
        SceneManager.LoadScene("Comunidad");
        
    }

    void MostrarMiembros(Dictionary<string, object> dataComunidad)
    {
        // Limpiar miembros anteriores
        foreach (Transform child in contenedorMiembros)
        {
            Destroy(child.gameObject);
        }
        if (panelMiembros == null || contenedorMiembros == null || prefabMiembro == null) return;

        // Limpiar TODOS los miembros anteriores (incluyendo mensajes de carga)
        foreach (Transform hijo in contenedorMiembros)
        {
            Destroy(hijo.gameObject);
        }

        // Limpiar cualquier corrutina en progreso
        StopAllCoroutines();

        // Mostrar mensaje de carga
        GameObject loadingItem = Instantiate(prefabMiembro, contenedorMiembros);
        TMP_Text loadingText = loadingItem.GetComponentInChildren<TMP_Text>();
        if (loadingText != null) loadingText.text = "Cargando miembros...";

        // Obtener la lista de miembros
        if (dataComunidad.TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
        {
            if (miembros.Count == 0)
            {
                Destroy(loadingItem);
                GameObject emptyItem = Instantiate(prefabMiembro, contenedorMiembros);
                TMP_Text emptyText = emptyItem.GetComponentInChildren<TMP_Text>();
                if (emptyText != null) emptyText.text = "No hay miembros en esta comunidad";
                panelMiembros.SetActive(true);
                return;
            }

            StartCoroutine(CargarMiembrosConInfo(miembros));
        }
        else
        {
            Destroy(loadingItem);
            GameObject errorItem = Instantiate(prefabMiembro, contenedorMiembros);
            TMP_Text errorText = errorItem.GetComponentInChildren<TMP_Text>();
            if (errorText != null) errorText.text = "Error al obtener la lista de miembros";
            panelMiembros.SetActive(true);
        }
    }

    IEnumerator CargarMiembrosConInfo(List<object> miembros)
    {
        // Limpiar mensaje de carga
        if (contenedorMiembros.childCount > 0)
            Destroy(contenedorMiembros.GetChild(0).gameObject);

        foreach (object miembro in miembros)
        {
            string idMiembro = miembro.ToString();

            // 1. Crear el item inmediatamente
            GameObject item = Instantiate(prefabMiembro, contenedorMiembros);

            // Obtener referencias a los componentes TMP_Text
            TMP_Text[] textos = item.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text nombreText = textos.Length > 0 ? textos[0] : null;
            TMP_Text rangoText = textos.Length > 1 ? textos[1] : null;

            if (nombreText == null || rangoText == null)
            {
                Debug.LogError("No se encontraron los componentes TMP_Text necesarios en el prefab");
                continue;
            }

            // Texto temporal mientras carga
            nombreText.text = "Cargando...";
            rangoText.text = "";

            // 2. Obtener datos del usuario
            var userRef = db.Collection("users").Document(idMiembro);
            var task = userRef.GetSnapshotAsync();

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                nombreText.text = $"{idMiembro} (error)";
                rangoText.text = "";
                Debug.LogError($"Error al cargar: {task.Exception}");
                continue;
            }

            var userDoc = task.Result;

            if (!userDoc.Exists)
            {
                nombreText.text = $"{idMiembro} (no existe)";
                rangoText.text = "";
                continue;
            }

            // 3. Actualizar con datos reales
            var userData = userDoc.ToDictionary();
            string displayName = userData.ContainsKey("DisplayName") ? userData["DisplayName"].ToString() : "Sin nombre";
            string rango = userData.ContainsKey("Rango") ? userData["Rango"].ToString() : "Sin rango";

            nombreText.text = displayName;
            rangoText.text = rango;
        }

        panelMiembros.SetActive(true);

    }

    public void CerrarPanelDetalle()
    {
        if (panelDetalleGrupo != null)
            panelDetalleGrupo.SetActive(false);

    }

    void LimpiarYCerrarPaneles()
    {
        // Limpiar panel de miembros
        foreach (Transform child in contenedorMiembros)
        {
            Destroy(child.gameObject);
        }
        panelMiembros.SetActive(false);

        // Limpiar panel de solicitudes
        foreach (Transform child in contenedorSolicitudes)
        {
            Destroy(child.gameObject);
        }
        panelSolicitudes.SetActive(false);

        // Restaurar el estado visual de los botones si ningún panel está activo
        if (btnVerMiembros != null && btnVerSolicitudes != null)
        {
            ActualizarEstadoBotones(false, false);
        }
    }
    void MostrarSolicitudes(Dictionary<string, object> dataComunidad)
    {
        // Limpiar solicitudes anteriores
        foreach (Transform child in contenedorSolicitudes)
        {
            Destroy(child.gameObject);
        }

        if (!dataComunidad.ContainsKey("documentId"))
        {
            Debug.LogError("La comunidad no tiene documentId");
            return;
        }

        string comunidadId = dataComunidad["documentId"].ToString();

        GameObject loadingItem = Instantiate(prefabSolicitud, contenedorSolicitudes);
        loadingItem.GetComponentInChildren<TMP_Text>().text = "Cargando solicitudes...";

        // Desactivar todos los botones en el mensaje de carga
        Button[] botones = loadingItem.GetComponentsInChildren<Button>(true);
        foreach (Button btn in botones)
        {
            btn.gameObject.SetActive(false);
        }

        db.Collection("solicitudes_comunidad")
          .WhereEqualTo("idComunidad", comunidadId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              Destroy(loadingItem);

              if (task.IsFaulted)
              {
                  GameObject errorItem = InstantiateErrorText("Error al cargar solicitudes");
                  // Desactivar botones en mensaje de error
                  Button[] errorButtons = errorItem.GetComponentsInChildren<Button>(true);
                  foreach (Button btn in errorButtons)
                  {
                      btn.gameObject.SetActive(false);
                  }
                  Debug.LogError(task.Exception);
                  return;
              }

              if (task.Result.Count == 0)
              {
                  GameObject noItems = InstantiateErrorText("No hay solicitudes pendientes");
                  // Desactivar botones cuando no hay solicitudes
                  Button[] noItemsButtons = noItems.GetComponentsInChildren<Button>(true);
                  foreach (Button btn in noItemsButtons)
                  {
                      btn.gameObject.SetActive(false);
                  }
                  return;
              }

              foreach (DocumentSnapshot solicitudDoc in task.Result.Documents)
              {
                  var data = solicitudDoc.ToDictionary();
                  CrearItemSolicitud(data, comunidadId, solicitudDoc.Id);
              }

              panelSolicitudes.SetActive(true);
          });
    }
    void CrearItemSolicitud(Dictionary<string, object> dataSolicitud, string comunidadId, string solicitudId)
    {
        GameObject item = Instantiate(prefabSolicitud, contenedorSolicitudes);

        // Guardar referencia al item en un componente temporal
        var itemController = item.AddComponent<SolicitudItemController>();
        itemController.Initialize(this, item, comunidadId, solicitudId);

        // Configurar textos
        if (dataSolicitud.ContainsKey("nombreUsuario"))
        {
            item.transform.Find("Nombretxt").GetComponent<TMP_Text>().text = dataSolicitud["nombreUsuario"].ToString();
        }

        if (dataSolicitud.ContainsKey("fechaSolicitud"))
        {
            Timestamp timestamp = (Timestamp)dataSolicitud["fechaSolicitud"];
            DateTime fecha = timestamp.ToDateTime().ToLocalTime();
            item.transform.Find("Fechatxt").GetComponent<TMP_Text>().text = fecha.ToString("dd/MM/yyyy HH:mm");
        }

        // Configurar botones
        Button btnAceptar = item.transform.Find("AceptarBtn").GetComponent<Button>();
        Button btnRechazar = item.transform.Find("RechazarBtn").GetComponent<Button>();

        btnAceptar.onClick.AddListener(() => ProcesarSolicitud(item, comunidadId, solicitudId, dataSolicitud["idUsuario"].ToString(), true));
        btnRechazar.onClick.AddListener(() => ProcesarSolicitud(item, comunidadId, solicitudId, dataSolicitud["idUsuario"].ToString(), false));
    }
    void ProcesarSolicitud(GameObject itemSolicitud, string comunidadId, string solicitudId, string usuarioId, bool aceptar)
    {
        // Eliminar el item inmediatamente
        Destroy(itemSolicitud);

        DocumentReference solicitudRef = db.Collection("solicitudes_comunidad").Document(solicitudId);
        DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadId);

        if (aceptar)
        {
            var batch = db.StartBatch();
            batch.Update(solicitudRef, "estado", "aceptada");
            batch.Update(comunidadRef, "miembros", FieldValue.ArrayUnion(usuarioId));

            batch.CommitAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully)
                {
                    Debug.LogError("Error al procesar solicitud: " + task.Exception);
                    // Opcional: Podrías reinstanciar el item si falla
                }
                else
                {
                    // Actualizar comunidad actual en memoria
                    ActualizarDatosComunidadLocal(comunidadId, usuarioId);
                }
            });
        }
        else
        {
            solicitudRef.UpdateAsync("estado", "rechazada")
                       .ContinueWithOnMainThread(task =>
                       {
                           if (!task.IsCompletedSuccessfully)
                           {
                               Debug.LogError("Error al rechazar solicitud: " + task.Exception);
                           }
                       });
        }
    }

    // Nuevo método para actualizar los datos de la comunidad en memoria
    void ActualizarDatosComunidadLocal(string comunidadId, string nuevoMiembroId)
    {
        // Buscar la comunidad en la lista local
        for (int i = 0; i < todasComunidades.Count; i++)
        {
            if (todasComunidades[i]["documentId"].ToString() == comunidadId)
            {
                // Actualizar la lista de miembros
                if (todasComunidades[i].TryGetValue("miembros", out object miembrosObj) && miembrosObj is List<object> miembros)
                {
                    miembros.Add(nuevoMiembroId);

                    // Si estamos viendo el detalle de esta comunidad, actualizar la UI
                    if (panelDetalleGrupo != null && panelDetalleGrupo.activeSelf)
                    {
                        // Actualizar la cantidad de miembros mostrada
                        detalleMiembros.text = $"{miembros.Count} miembros";

                        // Actualizar lista de miembros si está visible
                        if (panelMiembros != null && panelMiembros.activeSelf)
                        {
                            // Cargar la información del nuevo miembro
                            Dictionary<string, object> comunidadActual = todasComunidades[i];
                            MostrarMiembros(comunidadActual);
                        }
                    }
                }
                break;
            }
        }
    }
    GameObject InstantiateErrorText(string message)
    {
        GameObject errorItem = Instantiate(prefabSolicitud, contenedorSolicitudes);
        TMP_Text textComponent = errorItem.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = message;
        }

        // Desactivar todos los botones
        Button[] buttons = errorItem.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(false);
        }

        panelSolicitudes.SetActive(true);
        return errorItem;
    }
}
