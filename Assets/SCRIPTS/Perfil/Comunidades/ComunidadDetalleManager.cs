using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class ComunidadDetalleManager : MonoBehaviour
{
    [Header("Elementos panel detalle")]
    public TMP_Text detalleNombre;
    public TMP_Text detalleDescripcion;
    public TMP_Text detalleFecha;
    public TMP_Text detalleCreador;
    public TMP_Text detalleMiembros;
    public GameObject PanelDetalleGrupo;
    public Button btnCerrarPanelDetalle;

    [Header("Panel Detalle Miembros")]
    public GameObject panelMiembros;
    public GameObject panelSeñalarMiembros;
    public Transform contenedorMiembros;
    public GameObject prefabMiembro;
    public Button btnVerMiembros;

    [Header("Panel Detalle Solicitudes")]
    public GameObject panelSolicitudes;
    public GameObject panelSeñalarSolicitudes;
    public Transform contenedorSolicitudes;
    public GameObject prefabSolicitud;
    public Button btnVerSolicitudes;

    [Header("Confirmar Abandonar Comunidad")]
    public GameObject panelConfirmacionAbandonar;
    public Button btnAbandonarComunidad;
    public TMP_Text textoConfirmacionAbandonar;
    public Button btnCancelarAbandonar;
    public Button btnConfirmarAbandonar;

    [Header("Confirmar Eliminar Comunidad")]
    public GameObject panelConfirmarEliminarComunidad;
    public Button BtnEliminarComunidad;
    public TMP_Text txtConfirmarEliminar;
    public Button btnCancelarEliminar;
    public Button btnConfirmarEliminar;

    [Header("Notificación Solicitudes a Comunidad")]
    public GameObject notificationPanel; // Panel que contendrá el número
    public TMP_Text notificationCountText; // Texto para mostrar el número
    private ListenerRegistration listener; // 🔄 Referencia al listener

    [Header("Referencia a Mis comunidades")]
    public MisComunidadesManager miscomunidadesManager;
    private string comunidadActualId;
    private string usuarioActualId;
    private FirebaseFirestore db;
    private Dictionary<string, object> datosComunidadActual;

    void Awake()
    {
        db = FirebaseFirestore.DefaultInstance;

        if (panelConfirmacionAbandonar != null)
        {
            panelConfirmacionAbandonar.SetActive(false);
            btnConfirmarAbandonar.onClick.AddListener(ConfirmarAbandonarComunidad);
            btnCancelarAbandonar.onClick.AddListener(() => panelConfirmacionAbandonar.SetActive(false));
        }
        if (panelConfirmarEliminarComunidad != null)
        {
            panelConfirmarEliminarComunidad.SetActive(false);
            btnConfirmarEliminar.onClick.AddListener(ConfirmarEliminarComunidad);
            btnCancelarEliminar.onClick.AddListener(() => panelConfirmarEliminarComunidad.SetActive(false));
        }


        if (btnCerrarPanelDetalle != null)
        {
            btnCerrarPanelDetalle.onClick.AddListener(CerrarPanelDetalle);
        }
        
    }

    public void MostrarDetalle(Dictionary<string, object> dataComunidad, string usuarioId)
    {
        usuarioActualId = usuarioId;
        datosComunidadActual = new Dictionary<string, object>(dataComunidad);
        LimpiarYCerrarPaneles();
        
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        comunidadActualId = dataComunidad["documentId"].ToString();

        ActualizarInterfazConDatos(dataComunidad);

        // notificaciones
        CheckPendingRequests(comunidadActualId);
        SetupRealTimeListener(comunidadActualId);

        PanelDetalleGrupo.SetActive(true);

        ConfigurarBotones(dataComunidad);
        MostrarMiembros();
        ActualizarEstadoBotones(true, false);

        if (btnVerMiembros != null)
        {
            EventSystem.current.SetSelectedGameObject(btnVerMiembros.gameObject);
        }

    }
    // ----------------------------------NOTIFICACIONES DE SOLICITUDES A COMUNIDAD-----------------------------------
    public void CheckPendingRequests(string comunidadActualId)
    {


        db.Collection("solicitudes_comunidad")
          .WhereEqualTo("idComunidad", comunidadActualId)
          .WhereEqualTo("estado", "pendiente")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("Error al obtener solicitudes: " + task.Exception);
                  return;
              }

              int pendingCount = task.Result.Count;
              UpdateNotificationUI(pendingCount);
          });
    }

    void UpdateNotificationUI(int count)
    {
        if (notificationPanel == null || notificationCountText == null) return; // ❗ Seguridad

        if (count > 0)
        {
            notificationPanel.SetActive(true);
            notificationCountText.text = count.ToString();
        }
        else
        {
            notificationPanel.SetActive(false);
        }
    }

    void SetupRealTimeListener(string comunidadActualId)
    {

        listener = db.Collection("solicitudes_comunidad")
          .WhereEqualTo("idComunidad", comunidadActualId)
          .WhereEqualTo("estado", "pendiente")
          .Listen(snapshot =>
          {
              if (this == null || gameObject == null) return; // 🛡️ Protege de destrucción
              UpdateNotificationUI(snapshot.Count);
          });
    }

    void OnDestroy()
    {
        listener?.Stop(); // ❌ Cancela el listener si el objeto es destruido
    }

    // ----------------------------------NOTIFICACIONES DE SOLICITUDES A COMUNIDAD-----------------------------------

   

    private void ActualizarInterfazConDatos(Dictionary<string, object> dataComunidad)
    {
        string nombre = dataComunidad.GetValueOrDefault("nombre", "Sin nombre").ToString();
        string descripcion = dataComunidad.GetValueOrDefault("descripcion", "Sin descripción").ToString();
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

        // Actualizar UI
        detalleNombre.text = nombre;
        detalleDescripcion.text = descripcion;
        detalleFecha.text = fechaFormateada;
        detalleCreador.text = $"Creada por {creador}";
        detalleMiembros.text = $"{cantidadMiembros} miembros";
    }

    private void ConfigurarBotones(Dictionary<string, object> dataComunidad)
    {
        string creadorId = dataComunidad.GetValueOrDefault("creadorId", "").ToString();
        string tipocomunidad = dataComunidad.GetValueOrDefault("tipo", "publica").ToString().ToLower();

        btnVerMiembros.onClick.RemoveAllListeners();
        btnVerMiembros.onClick.AddListener(() => {
            LimpiarYCerrarPaneles();
            MostrarMiembros();
            ActualizarEstadoBotones(true, false);
        });

        if (btnVerSolicitudes != null)
        {
            bool esCreador = usuarioActualId == creadorId;
            btnVerSolicitudes.interactable = esCreador;
            btnVerSolicitudes.gameObject.SetActive(esCreador);
            if (tipocomunidad != "privada")
            {
                btnVerSolicitudes.gameObject.SetActive(false);
            }

            if (esCreador)
            {
                btnVerSolicitudes.onClick.RemoveAllListeners();
                btnVerSolicitudes.onClick.AddListener(() => {
                    LimpiarYCerrarPaneles();
                    MostrarSolicitudes(dataComunidad);
                    ActualizarEstadoBotones(false, true);
                });
            }
        }


        if (btnAbandonarComunidad != null)
        {
            btnAbandonarComunidad.onClick.RemoveAllListeners();
            btnAbandonarComunidad.onClick.AddListener(() => MostrarConfirmacionAbandonar(dataComunidad));
            btnAbandonarComunidad.gameObject.SetActive(usuarioActualId != creadorId);
        }
        // acá ponemos el activar el btn si es creador para que elimine la comunidad y mostrar el panel de confirmación y hacer la función para eliminar la comunidad ----------------------------------------------------------
        if (BtnEliminarComunidad != null)
        {
            BtnEliminarComunidad.onClick.RemoveAllListeners();
            BtnEliminarComunidad.onClick.AddListener(() => MostrarConfirmacionEliminar(dataComunidad));
            BtnEliminarComunidad.gameObject.SetActive(usuarioActualId == creadorId);
        }
    }

    void ActualizarEstadoBotones(bool miembrosSeleccionado, bool solicitudesSeleccionado)
    {
        Image imgBtnMiembros = btnVerMiembros.GetComponent<Image>();
        Image imgBtnSolicitudes = btnVerSolicitudes ? btnVerSolicitudes.GetComponent<Image>() : null;

        Color colorSeleccionado = new Color(55, 189, 247, 255);
        Color colorNormal = Color.white;

        if (imgBtnMiembros != null)
        {
            imgBtnMiembros.color = miembrosSeleccionado ? colorSeleccionado : colorNormal;
        }

        if (imgBtnSolicitudes != null)
        {
            imgBtnSolicitudes.color = solicitudesSeleccionado ? colorSeleccionado : colorNormal;
        }

        btnVerMiembros.interactable = !miembrosSeleccionado;
        if (btnVerSolicitudes) btnVerSolicitudes.interactable = !solicitudesSeleccionado;
    }

    void MostrarConfirmacionAbandonar(Dictionary<string, object> dataComunidad)
    {
        if (panelConfirmacionAbandonar == null) return;

        string nombreComunidad = dataComunidad.GetValueOrDefault("nombre", "esta comunidad").ToString();
        textoConfirmacionAbandonar.text = $"¿Estás seguro que deseas abandonar {nombreComunidad}?";
        panelConfirmacionAbandonar.SetActive(true);
        EventSystem.current.SetSelectedGameObject(btnCancelarAbandonar.gameObject);
    }
    void MostrarConfirmacionEliminar(Dictionary<string, object> dataComunidad)
    {
        if (panelConfirmacionAbandonar == null) return;

        string nombreComunidad = dataComunidad.GetValueOrDefault("nombre", "esta comunidad").ToString();
        txtConfirmarEliminar.text = $"¿Estás seguro que deseas Eliminar {nombreComunidad}?";
        panelConfirmarEliminarComunidad.SetActive(true);
        EventSystem.current.SetSelectedGameObject(btnCancelarEliminar.gameObject);
    }

    void ConfirmarAbandonarComunidad()
    {
        bool hayConexion = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayConexion)
        {


            if (string.IsNullOrEmpty(comunidadActualId) || string.IsNullOrEmpty(usuarioActualId))
            {
                Debug.LogError("Falta información para abandonar la comunidad");
                return;
            }

            panelConfirmacionAbandonar.SetActive(false);
            DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadActualId);

            comunidadRef.UpdateAsync("miembros", FieldValue.ArrayRemove(usuarioActualId))
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogError("Error al abandonar comunidad: " + task.Exception);
                        return;
                    }

                    SceneManager.LoadScene("Comunidad");
                });
        }
        else
        {
            textoConfirmacionAbandonar.text = ("SIN CONEXIÓN A INTERNET, NO ES POSIBLE REALIZAR ESTA OPERACIÓN EN ESTE MOMENTO, INTENTE MÁS TARDE");
            Invoke("OcultarPanelConfirmarAbandonarComunidad", 4f);
        }
    }
    void ConfirmarEliminarComunidad()
    {
        bool hayConexion = Application.internetReachability != NetworkReachability.NotReachable;
        if (hayConexion)
        {


            if (string.IsNullOrEmpty(comunidadActualId) || string.IsNullOrEmpty(usuarioActualId))
            {
                Debug.LogError("Falta información para eliminar la comunidad");
                return;
            }

            panelConfirmacionAbandonar.SetActive(false);
            DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadActualId);

            comunidadRef.DeleteAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogError("Error al eliminar comunidad: " + task.Exception);
                        return;
                    }

                    SceneManager.LoadScene("Comunidad");
                });
        }
        else
        {
            textoConfirmacionAbandonar.text = ("SIN CONEXIÓN A INTERNET, NO ES POSIBLE REALIZAR ESTA OPERACIÓN EN ESTE MOMENTO, INTENTE MÁS TARDE");
            Invoke("OcultarPanelConfirmarAbandonarComunidad", 4f);
        }
    }


    public void OcultarPanelConfirmarAbandonarComunidad()
    {
        if (panelConfirmacionAbandonar != null)
        {
            panelConfirmacionAbandonar.SetActive(false);
            EventSystem.current.SetSelectedGameObject(btnCancelarAbandonar.gameObject);
        }
    }

    void MostrarMiembros(Dictionary<string, object> dataComunidad = null)
    {
        panelSeñalarMiembros.SetActive(true);
        if( panelSeñalarSolicitudes != null)
        {
            panelSeñalarSolicitudes.SetActive(false);
        }

        var datosAMostrar = dataComunidad ?? datosComunidadActual;
        if (datosAMostrar == null) return;

        // Limpiar el contenedor
        foreach (Transform child in contenedorMiembros)
        {
            Destroy(child.gameObject);
        }

        if (panelMiembros == null || contenedorMiembros == null || prefabMiembro == null) return;

        if (!datosAMostrar.TryGetValue("miembros", out object miembrosObj) ||
            !(miembrosObj is List<object> miembros) ||
            miembros.Count == 0)
        {
            GameObject emptyItem = Instantiate(prefabMiembro, contenedorMiembros);
            emptyItem.GetComponentInChildren<TMP_Text>().text = "No hay miembros en esta comunidad";
            panelMiembros.SetActive(true);
            return;
        }

        StartCoroutine(CargarMiembrosConInfo((List<object>)miembrosObj));
    }

    IEnumerator CargarMiembrosConInfo(List<object> miembros)
    {
        // Limpiar contenedor
        foreach (Transform child in contenedorMiembros)
        {
            Destroy(child.gameObject);
        }

        foreach (object miembro in miembros)
        {
            string idMiembro = miembro.ToString();
            GameObject item = Instantiate(prefabMiembro, contenedorMiembros);

            // Reiniciar la imagen del avatar a un estado por defecto antes de cargar
            Transform avatarTransform = item.transform.Find("AvatarImage");
            if (avatarTransform != null)
            {
                Image avatarImage = avatarTransform.GetComponent<Image>();
                if (avatarImage != null)
                {
                    // Cargar imagen por defecto temporalmente
                    avatarImage.sprite = Resources.Load<Sprite>("Avatares/defecto");
                }
            }

            TMP_Text[] textos = item.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text nombreText = textos.Length > 0 ? textos[0] : null;
            TMP_Text rangoText = textos.Length > 1 ? textos[1] : null;

            if (nombreText == null || rangoText == null)
            {
                Debug.LogError("No se encontraron los componentes TMP_Text necesarios en el prefab");
                continue;
            }

            nombreText.text = "Cargando...";
            rangoText.text = "";

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

            var userData = userDoc.ToDictionary();
            string displayName = userData.ContainsKey("DisplayName") ? userData["DisplayName"].ToString() : "Sin nombre";
            string rango = userData.ContainsKey("Rango") ? userData["Rango"].ToString() : "Sin rango";

            // Cargar avatar
            string avatarPath = ObtenerAvatarPorRango(rango);
            Sprite avatarSprite = Resources.Load<Sprite>(avatarPath) ?? Resources.Load<Sprite>("Avatares/defecto");

            // Actualizar la imagen del avatar en el item actual
            if (avatarTransform != null)
            {
                Image avatarImage = avatarTransform.GetComponent<Image>();
                if (avatarImage != null)
                {
                    avatarImage.sprite = avatarSprite;
                }
            }

            nombreText.text = displayName;
            rangoText.text = rango;
        }

        panelMiembros.SetActive(true);
    }

    void MostrarSolicitudes(Dictionary<string, object> dataComunidad)
    {
        panelSeñalarSolicitudes.SetActive(true);
        if (panelSeñalarMiembros != null)
        {
            panelSeñalarMiembros.SetActive(false);
        }

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
                  InstantiateErrorText("Error al cargar solicitudes");
                  Debug.LogError(task.Exception);
                  return;
              }

              if (task.Result.Count == 0)
              {
                  InstantiateErrorText("No hay solicitudes pendientes");
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
        var itemController = item.AddComponent<SolicitudItemController>();
        itemController.Initialize(this, item, comunidadId, solicitudId);

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

        Button btnAceptar = item.transform.Find("AceptarBtn").GetComponent<Button>();
        Button btnRechazar = item.transform.Find("RechazarBtn").GetComponent<Button>();

        btnAceptar.onClick.AddListener(() => ProcesarSolicitud(item, comunidadId, solicitudId, dataSolicitud["idUsuario"].ToString(), true));
        btnRechazar.onClick.AddListener(() => ProcesarSolicitud(item, comunidadId, solicitudId, dataSolicitud["idUsuario"].ToString(), false));
    }

    void ProcesarSolicitud(GameObject itemSolicitud, string comunidadId, string solicitudId, string usuarioId, bool aceptar)
    {
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
                    return;
                }

                // Actualizar datos locales
                ActualizarDatosComunidad(comunidadActualizada => 
                {
                    // Actualizar UI
                    ActualizarInterfazConDatos(comunidadActualizada);
                    
                    // Mostrar feedback visual
                    StartCoroutine(MostrarFeedbackMiembroAgregado(usuarioId));
                    
                    // Si el panel de miembros está visible, actualizarlo
                    if (panelMiembros.activeSelf)
                    {
                        MostrarMiembros(comunidadActualizada);
                    }
                });
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

    private void ActualizarDatosComunidad(Action<Dictionary<string, object>> callback = null)
    {
        if (string.IsNullOrEmpty(comunidadActualId)) return;

        DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadActualId);
        comunidadRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result.Exists)
            {
                datosComunidadActual = task.Result.ToDictionary();
                callback?.Invoke(datosComunidadActual);
            }
        });
    }

    IEnumerator MostrarFeedbackMiembroAgregado(string usuarioId)
    {
        var userRef = db.Collection("users").Document(usuarioId);
        var task = userRef.GetSnapshotAsync();
        
        yield return new WaitUntil(() => task.IsCompleted);
        
        if (task.IsCompletedSuccessfully && task.Result.Exists)
        {
            var userData = task.Result.ToDictionary();
            string nombre = userData.ContainsKey("DisplayName") ? userData["DisplayName"].ToString() : "Nuevo miembro";
            
            GameObject feedbackItem = Instantiate(prefabMiembro, contenedorMiembros);
            feedbackItem.GetComponentInChildren<TMP_Text>().text = $"{nombre} se ha unido al grupo";
            feedbackItem.GetComponent<Image>().color = new Color(0.8f, 1f, 0.8f);
            
            yield return new WaitForSeconds(2f);
            Destroy(feedbackItem);
        }
    }
    private string ObtenerAvatarPorRango(string rango)
    {
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }
    void LimpiarYCerrarPaneles()
    {
        foreach (Transform child in contenedorMiembros)
        {
            Destroy(child.gameObject);
        }
        panelMiembros.SetActive(false);

        foreach (Transform child in contenedorSolicitudes)
        {
            Destroy(child.gameObject);
        }
        panelSolicitudes.SetActive(false);

        if (btnVerMiembros != null && btnVerSolicitudes != null)
        {
            ActualizarEstadoBotones(false, false);
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

        Button[] buttons = errorItem.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            btn.gameObject.SetActive(false);
        }

        panelSolicitudes.SetActive(true);
        return errorItem;
    }

    void CerrarPanelDetalle()
    {
        if (PanelDetalleGrupo != null)
        {
            PanelDetalleGrupo.SetActive(false);
            // Actualizar datos cuando se cierre el panel
            ActualizarDatosComunidad();
            
        }
    }
}