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

public class MisComunidadesManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject tarjetaPrefab;
    public Transform contenedor;
    public TMP_InputField inputBusqueda;
    public Button botonBuscar;
    public TMP_Text textoEstado; // Nuevo: Texto para mostrar mensajes de estado
    public GameObject panelEstado; // Nuevo: Panel contenedor del texto de estado

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
    public GameObject panelDetalleGrupo= null;
    public TMP_Text detalleNombre;
    public TMP_Text detalleDescripcion;
    public TMP_Text detalleFecha;
    public TMP_Text detalleCreador;
    public TMP_Text detalleMiembros;

    [Header("Panel Detalle Miembros")]
    public GameObject panelMiembros; // Panel con el ScrollView
    public Transform contenedorMiembros; // Contenedor donde se instanciarán los miembros
    public GameObject prefabMiembro; // Prefab de un TextMeshProUGUI o un diseño para cada miembro
    public Button btnVerMiembros; // Botón en el panel detalle que activará el panel miembros

    [Header("Panel Solicitudes")]
    public GameObject panelSolicitudes;
    public Transform contenedorSolicitudes;
    public GameObject prefabSolicitud;
    public Button btnVerSolicitudes;  
    private string comunidadActualId; // Guardar el ID al abrir el detalle

    // 👇 NUEVO BLOQUE
  
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
                inputBusqueda.onSubmit.AddListener(delegate { BuscarComunidades(); });
            }
        }
        else
        {
            MostrarMensajeEstado("No hay usuario autenticado", true);
            Debug.LogWarning("No hay usuario autenticado");
        }
        
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
                todasComunidades.Add(data);
            }

            MostrarTodasComunidades();
            MostrarMensajeEstado(string.Format(mensajeListo, todasComunidades.Count), true);
        });
    }

    void BuscarComunidades()
    {
        if (inputBusqueda == null || string.IsNullOrWhiteSpace(inputBusqueda.text))
        {
            MostrarTodasComunidades();
            return;
        }

        string terminoBusqueda = inputBusqueda.text.Trim().ToLower();
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
            string descripcion = comunidad.GetValueOrDefault("descripcion", "").ToString().ToLower();

            if (nombre.Contains(terminoBusqueda) || descripcion.Contains(terminoBusqueda))
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
        if (panelDetalleGrupo == null) return;

        // Extraer datos de la comunidad
        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
        string tipo = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();
        string creador = dataComunidad.GetValueOrDefault("creadorUsername", "Sin creador").ToString();

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

        // Configurar el botón (por si el usuario quiere recargar)
        btnVerMiembros.onClick.RemoveAllListeners();
        btnVerMiembros.onClick.AddListener(() => MostrarMiembros(dataComunidad));

        // 👇 Llamar automáticamente a MostrarMiembros
        MostrarMiembros(dataComunidad);

        // Seleccionar el botón para navegación con teclado/controller
        EventSystem.current.SetSelectedGameObject(btnVerMiembros.gameObject);
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

    //void MostrarSolicitudes(Dictionary<string, object> dataComunidad)
    //{
    //    if (panelSolicitudes == null || contenedorSolicitudes == null) return;

    //    // Limpiar solicitudes anteriores
    //    foreach (Transform child in contenedorSolicitudes)
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    comunidadActualId = dataComunidad.GetValueOrDefault("id", "").ToString();
    //    if (string.IsNullOrEmpty(comunidadActualId))
    //    {
    //        Debug.LogError("ID de comunidad no encontrado");
    //        return;
    //    }

    //    // Consultar solicitudes pendientes
    //    db.Collection("comunidades").Document(comunidadActualId)
    //      .Collection("solicitudes")
    //      .WhereEqualTo("estado", "pendiente")
    //      .GetSnapshotAsync()
    //      .ContinueWithOnMainThread(task =>
    //      {
    //          if (task.IsFaulted) return;

    //          // 1. Recopilar todos los userIDs únicos
    //          var userIDs = new List<string>();
    //          foreach (DocumentSnapshot doc in task.Result.Documents)
    //          {
    //              var data = doc.ToDictionary();
    //              userIDs.Add(data.GetValueOrDefault("usuarioId", "").ToString());
    //          }

    //          // 2. Cargar todos los usuarios en una sola consulta
    //          db.Collection("users").WhereIn(FieldPath.DocumentId, userIDs).GetSnapshotAsync()
    //            .ContinueWithOnMainThread(usersTask =>
    //            {
    //                if (usersTask.IsFaulted) return;

    //                // 3. Mapear datos de usuarios por ID
    //                var userDataMap = new Dictionary<string, Dictionary<string, object>>();
    //                foreach (DocumentSnapshot userDoc in usersTask.Result.Documents)
    //                {
    //                    userDataMap[userDoc.Id] = userDoc.ToDictionary();
    //                }

    //                // 4. Crear las solicitudes con los datos combinados
    //                foreach (DocumentSnapshot doc in task.Result.Documents)
    //                {
    //                    var dataSolicitud = doc.ToDictionary();
    //                    string usuarioId = dataSolicitud.GetValueOrDefault("usuarioId", "").ToString();

    //                    if (userDataMap.TryGetValue(usuarioId, out var userData))
    //                    {
    //                        string displayName = userData.GetValueOrDefault("DisplayName", "Usuario").ToString();
    //                        string rango = userData.GetValueOrDefault("Rango", "Nuevo").ToString();

    //                        CrearSolicitudUI(doc.Id, dataSolicitud, displayName, rango);
    //                    }
    //                }
    //            });
    //      });
    //}
    //void CrearSolicitudUI(string solicitudId, Dictionary<string, object> dataSolicitud, string displayName, string rango)
    //{
    //    GameObject solicitudGO = Instantiate(prefabSolicitud, contenedorSolicitudes);
    //    TarjetaSolicitudUI ui = solicitudGO.GetComponent<TarjetaSolicitudUI>();

    //    string usuarioId = dataSolicitud.GetValueOrDefault("usuarioId", "").ToString();

    //    // Configurar datos temporales (mientras se carga la info del usuario)
    //    ui.Configurar(
    //        solicitudId,
    //        comunidadActualId,
    //        usuarioId,
    //        "Cargando...",  // Nombre provisional
    //        "...",          // Rango provisional
    //        "Hoy",         // Fecha (puedes usar dataSolicitud["fecha"] si existe)
    //        AceptarSolicitud,
    //        RechazarSolicitud
    //    );

    //    // 👇 Cargar datos del usuario desde la colección `users`
    //    db.Collection("users").Document(usuarioId).GetSnapshotAsync().ContinueWithOnMainThread(userTask =>
    //    {
    //        if (userTask.IsFaulted || !userTask.Result.Exists)
    //        {
    //            Debug.LogError("Error al cargar datos del usuario");
    //            return;
    //        }

    //        var userData = userTask.Result.ToDictionary();
    //        string displayName = userData.GetValueOrDefault("DisplayName", "Usuario").ToString();
    //        string rango = userData.GetValueOrDefault("Rango", "Novato de laboratorio").ToString();

    //        // Actualizar el UI con los datos reales
    //        ui.textoNombre.text = displayName;
    //        ui.textoRango.text = rango;
    //    });
    //}

    //void AceptarSolicitud(string solicitudId, string comunidadId)
    //{
    //    var solicitudRef = db.Collection("comunidades").Document(comunidadId)
    //                       .Collection("solicitudes").Document(solicitudId);

    //    solicitudRef.GetSnapshotAsync().ContinueWithOnMainThread(solicitudTask =>
    //    {
    //        if (solicitudTask.IsFaulted) return;

    //        var data = solicitudTask.Result.ToDictionary();
    //        string usuarioId = data.GetValueOrDefault("usuarioId", "").ToString();

    //        // 1. Añadir usuario a miembros
    //        var comunidadRef = db.Collection("comunidades").Document(comunidadId);
    //        comunidadRef.UpdateAsync("miembros", FieldValue.ArrayUnion(usuarioId))
    //            .ContinueWithOnMainThread(updateTask =>
    //            {
    //                if (updateTask.IsFaulted) return;

    //                // 2. Marcar solicitud como "aceptada"
    //                solicitudRef.UpdateAsync("estado", "aceptada")
    //                    .ContinueWithOnMainThread(_ => DestroySolicitudUI(solicitudId));
    //            });
    //    });
    //}
    //void RechazarSolicitud(string solicitudId, string comunidadId)
    //{
    //    db.Collection("comunidades").Document(comunidadId)
    //      .Collection("solicitudes").Document(solicitudId)
    //      .UpdateAsync("estado", "rechazada")
    //      .ContinueWithOnMainThread(task =>
    //      {
    //          if (!task.IsFaulted)
    //          {
    //              DestroySolicitudUI(solicitudId);
    //          }
    //      });
    //}
    //void DestroySolicitudUI(string solicitudId)
    //{
    //    foreach (Transform child in contenedorSolicitudes)
    //    {
    //        if (child.gameObject.name == solicitudId)
    //        {
    //            Destroy(child.gameObject);
    //            break;
    //        }
    //    }
    //}
    public void CerrarPanelDetalle()
    {
        if (panelDetalleGrupo != null)
            panelDetalleGrupo.SetActive(false);

        // Limpiar scroll de miembros
        foreach (Transform hijo in contenedorMiembros)
        {
            Destroy(hijo.gameObject);
        }
    }


}