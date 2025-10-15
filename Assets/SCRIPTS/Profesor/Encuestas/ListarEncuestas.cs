using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;

public class ListarEncuestas : MonoBehaviour
{
    #region Referencias Inspector
    public TMP_Text txtNombre;

    [Header("Visualización de Encuestas")]
    public Transform contenedorEncuestas;
    public GameObject tarjetaEncuestaPrefab;

    [Header("Detalles y Edición")]
    public GameObject panelDetallesEncuesta;
    public Button btnEditarEncuesta;
    public Button btnEliminarEncuesta;
    public Button btnAsignarEncuesta;
    public GestorAsignacionEncuesta gestorAsignacion;
    public Transform contenedorPreguntasDetalles;
    public GameObject preguntaDetalleItemPrefab;
    public TMP_Text txtDescripcionDetalles;
    public TMP_Text txtTituloDetalles;
    public Button botonVistaPrevia;
    public Button botonResultados;
    public GameObject panelVistaPrevia;
    public GameObject panelResultados;

    [Header("Panel de Resultados")]
    public TMP_Dropdown dropdownComunidades;
    public Transform contenedorTablaResultados; 
    public GameObject filaResultadoPrefab;      

    [Header("Panel Confirmar Eliminacion")]
    public GameObject panelConfirmacionEliminar;
    public Button btnConfirmarEliminar;
    public Button btnCancelarEliminar;
    public GameObject PanelGris;

    [Header("Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

    [Header("Paneles para navegacion")]
    public GameObject PanelEncuesta;
    public GameObject PanelListar;
    public GameObject PanelTipoEncuesta;
    public GameObject PanelElementoMision;
    public GameObject PanelSinEncuestas;
    public Button btnNuevaEncuesta;
    public Button btnEncuestaRecreativa;
    public Button btnEncuestaMision;
    public Button btnSalirTipoEncuesta;
    public Button btnSalirElemento;
    public Button btnSalirVerDetalles;

    [Header("Panel Elemento Mision")]
    public Button btnContinuarMision;
    public ControladorSeleccionMision controladorSeleccion;
    #endregion

    #region Variables Privadas
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;
    private string encuestaActualID;
    private ListenerRegistration encuestasListener;
    private HashSet<string> encuestasCargadas = new();
    public EncuestasManager encuestasManager;
    // --- Colores para las pestañas de detalles ---
    private Color colorBotonSeleccionado = new Color(0x51 / 255f, 0xB2 / 255f, 0x7C / 255f); // #51B27C
    private Color colorLetraSeleccionado = Color.white;
    private Color colorBotonNoSeleccionado = new Color(0xF9 / 255f, 0xF9 / 255f, 0xF9 / 255f); // #F9F9F9
    private Color colorLetraNoSeleccionado = Color.black;

    [Tooltip("La altura mínima que tendrá la lista desplegable.")]
    [SerializeField] private float minDropdownHeight = 60f;

    [Tooltip("La altura de cada item individual en la lista.")]
    [SerializeField] private float dropdownItemHeight = 60f;

    [Tooltip("El número máximo de items visibles antes de que aparezca el scroll.")]
    [SerializeField] private int maxVisibleDropdownItems = 20;

    #endregion

    #region Ciclo de Vida de Unity
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        userId = currentUser?.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Usuario no autenticado, deteniendo inicialización.");
            VerificarSiHayEncuestas(); // Asegurarse de mostrar el panel si no hay usuario
            return;
        }

        txtNombre.text = auth.CurrentUser.DisplayName;
        ConfigurarBotones();

        // --- FLUJO DE INICIO CORREGIDO ---
        // 1. Desactivar el panel de "sin encuestas" por defecto.
        PanelSinEncuestas.SetActive(false);

        // 2. Cargar datos locales inmediatamente para una UI rápida.
        CargarDesdeLocal();

        // 3. Si hay internet, sincronizar en segundo plano y activar el listener para datos en tiempo real.
        if (HayInternet())
        {
            ConfigurarListenerDeEncuestas();
            _ = SincronizarDatosCompletos(); // El '_' descarta el warning de no usar await
        }
    }

    void OnDestroy()
    {
        encuestasListener?.Stop();
    }
    #endregion

    #region Lógica de Carga y Sincronización
    private void ConfigurarListenerDeEncuestas()
    {
        if (encuestasListener != null) return;

        encuestasListener = db.Collection("Encuestas").Listen(snapshot =>
        {
            if (this != null && this.enabled) // Prevenir errores si el objeto se destruye
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() => ProcesarSnapshotDeEncuestas(snapshot));
            }
        });
    }

    private void ProcesarSnapshotDeEncuestas(QuerySnapshot snapshot)
    {
        LimpiarEncuestasEnPantalla();

        foreach (var doc in snapshot.Documents)
        {
            try
            {
                // --- USAMOS NUESTRA FUNCIÓN SEGURA ---
                EncuestaModelo encuesta = ConvertirDocumentoAEncuesta(doc);

                if (encuesta != null)
                {
                    CrearTarjetaEncuesta(encuesta.Titulo, encuesta.Preguntas.Count, encuesta.Id);
                    encuestasCargadas.Add(encuesta.Id);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fatal al procesar documento {doc.Id} desde el listener: {e.Message}");
            }
        }

        VerificarSiHayEncuestas();
    }

    private void CargarDesdeLocal()
    {
        LimpiarEncuestasEnPantalla();

        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas))
        {
            VerificarSiHayEncuestas();
            return;
        }

        string[] archivos = Directory.GetFiles(carpetaEncuestas, "*.json");
        foreach (string rutaArchivo in archivos)
        {
            try
            {
                string jsonEncuesta = File.ReadAllText(rutaArchivo);
                EncuestaModelo encuesta = JsonUtility.FromJson<EncuestaModelo>(jsonEncuesta);

                if (!encuestasCargadas.Contains(encuesta.Id))
                {
                    CrearTarjetaEncuesta(encuesta.Titulo, encuesta.Preguntas.Count, encuesta.Id);
                    encuestasCargadas.Add(encuesta.Id);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al cargar encuesta local '{Path.GetFileName(rutaArchivo)}': {e.Message}");
            }
        }

        VerificarSiHayEncuestas();
    }

    public void RefrescarListaDesdeCache()
    {
        CargarDesdeLocal();
    }

    private async Task SincronizarDatosCompletos()
    {
        await SincronizarEncuestasConFirebase();
        await SincronizarEliminacionesPendientes();
    }

    public async Task SincronizarEncuestasConFirebase()
    {
        if (!HayInternet()) return;

        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas)) Directory.CreateDirectory(carpetaEncuestas);

        // Subir JSONs locales que no estén en Firebase
        string[] archivosLocales = Directory.GetFiles(carpetaEncuestas, "*.json");
        foreach (var rutaArchivo in archivosLocales)
        {
            try
            {
                var id = Path.GetFileNameWithoutExtension(rutaArchivo);
                var docRef = db.Collection("Encuestas").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    var contenido = File.ReadAllText(rutaArchivo);
                    var encuestaLocal = JsonUtility.FromJson<EncuestaModelo>(contenido);
                    await docRef.SetAsync(encuestaLocal);
                }
            }
            catch (Exception e) { Debug.LogError($"[Sync] Error subiendo '{Path.GetFileName(rutaArchivo)}': {e.Message}"); }
        }

        // Descargar encuestas de Firebase que no estén localmente
        QuerySnapshot remotoSnap = await db.Collection("Encuestas").GetSnapshotAsync();
        foreach (var doc in remotoSnap.Documents)
        {
            try
            {
                var rutaLocal = Path.Combine(carpetaEncuestas, doc.Id + ".json");
                if (!File.Exists(rutaLocal))
                {
                    var encuesta = doc.ConvertTo<EncuestaModelo>();
                    var json = JsonUtility.ToJson(encuesta, true);
                    File.WriteAllText(rutaLocal, json);
                }
            }
            catch (Exception e) { Debug.LogError($"[Sync] Error guardando encuesta '{doc.Id}': {e.Message}"); }
        }
    }
    #endregion

    #region Manejo de UI y Encuestas
    private void ConfigurarBotones()
    {
        btnNuevaEncuesta.onClick.AddListener(AbrirPanelEncuestaCrearEncuesta);
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
        btnConfirmarEliminar.onClick.AddListener(EliminarEncuestaConfirmada);
        btnCancelarEliminar.onClick.AddListener(OcultarPanelConfirmacionEliminar);
        btnSalirVerDetalles.onClick.AddListener(CerrarPanelVerDetalles);
        ConfigurarPestanasDetalles();
    }

    private void CrearTarjetaEncuesta(string titulo, int numPreguntas, string id)
    {
        GameObject tarjeta = Instantiate(tarjetaEncuestaPrefab, contenedorEncuestas);
        if (tarjeta.TryGetComponent<TarjetaEncuestaUI>(out var tarjetaUI))
        {
            tarjetaUI.Configurar(titulo, numPreguntas, () => MostrarDetallesEncuesta(id));
        }
        else // Fallback si no tienes un script específico en la tarjeta
        {
            TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();
            if (textos.Length >= 2)
            {
                textos[0].text = titulo;
                textos[1].text = $"{numPreguntas} Preguntas";
            }
            var boton = tarjeta.GetComponent<Button>();
            if (boton != null)
                boton.onClick.AddListener(() => MostrarDetallesEncuesta(id));
        }
    }

    private async void MostrarDetallesEncuesta(string id)
    {
        encuestaActualID = id;
        panelDetallesEncuesta.SetActive(true);
        LimpiarPanelDetalles();

        // 1. Cambia a la pestaña de vista previa por defecto
        CambiarPestaña(true);

        // 2. Carga los datos de la encuesta (esto no cambia)
        EncuestaModelo encuesta = await CargarEncuestaPorId(id);
        if (encuesta == null)
        {
            Debug.LogError($"No se pudo cargar la encuesta con ID: {id}");
            panelDetallesEncuesta.SetActive(false);
            return;
        }

        // 3. Puebla la vista previa (esto no cambia)
        txtTituloDetalles.text = encuesta.Titulo;
        txtDescripcionDetalles.text = encuesta.Descripcion;
        PoblarListaPreguntasDetalles(encuesta.Preguntas);

        // 4. Configura los botones principales (esto no cambia)
        btnEditarEncuesta.onClick.RemoveAllListeners();
        btnEliminarEncuesta.onClick.RemoveAllListeners();
        btnAsignarEncuesta.onClick.RemoveAllListeners();
        btnEditarEncuesta.onClick.AddListener(() => AbrirPanelEncuesta(id));
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
        btnAsignarEncuesta.onClick.AddListener(() => gestorAsignacion.AbrirPanelDeAsignacion(id, encuesta.Titulo, encuesta.Preguntas.Count));

        // 5. NUEVO: Cargar los datos para el panel de resultados
        await PoblarPanelDeResultados(id);
    }

    public void OnDropdownClicked_AdjustHeight()
    {
        // El objeto de la lista se crea en la raíz del Canvas, por eso usamos GameObject.Find.
        GameObject dropdownList = GameObject.Find("Dropdown List");

        if (dropdownList == null)
        {
            // Si no lo encuentra inmediatamente, es porque tarda un frame en crearse.
            // Usamos una corrutina para intentarlo de nuevo en el siguiente frame.
            StartCoroutine(AdjustHeightNextFrame());
            return;
        }

        // Si lo encuentra, ajustamos la altura inmediatamente.
        AjustarAlturaLista(dropdownList);
    }

    private System.Collections.IEnumerator AdjustHeightNextFrame()
    {
        yield return null; // Espera un frame.

        GameObject dropdownList = GameObject.Find("Dropdown List");
        if (dropdownList != null)
        {
            AjustarAlturaLista(dropdownList);
        }
    }

    /// <summary>
    /// Función de ayuda que calcula y aplica la altura a la lista del dropdown.
    /// </summary>
    private void AjustarAlturaLista(GameObject dropdownList)
    {
        // Lógica de cálculo (usa las variables que ya añadimos al script)
        float heightFromOptions = dropdownComunidades.options.Count * dropdownItemHeight;
        float maxAllowedHeight = maxVisibleDropdownItems * dropdownItemHeight;
        float calculatedHeight = Mathf.Min(heightFromOptions, maxAllowedHeight);
        float finalHeight = Mathf.Max(calculatedHeight, minDropdownHeight);

        // Obtenemos el RectTransform y aplicamos el nuevo tamaño.
        RectTransform listRectTransform = dropdownList.GetComponent<RectTransform>();
        if (listRectTransform != null)
        {
            listRectTransform.sizeDelta = new Vector2(listRectTransform.sizeDelta.x, finalHeight);
            Debug.Log($"[Dropdown] Altura ajustada a: {finalHeight}");
        }
    }

    private void AjustarAlturaPlantillaDropdown()
    {
        // La plantilla del dropdown es un hijo del propio objeto Dropdown.
        // Suele llamarse "Template".
        Transform templateTransform = dropdownComunidades.transform.Find("Template");
        if (templateTransform == null)
        {
            Debug.LogWarning("No se encontró la plantilla ('Template') del Dropdown. No se puede ajustar la altura.");
            return;
        }

        // El objeto que realmente tiene el tamaño es el 'Viewport' dentro de la plantilla.
        RectTransform viewportRect = templateTransform.Find("Viewport")?.GetComponent<RectTransform>();
        if (viewportRect == null)
        {
            // A veces el hijo directo es el que tiene el RectTransform, no un Viewport.
            viewportRect = templateTransform.GetComponent<RectTransform>();
        }

        // --- LÓGICA DE CÁLCULO ---
        // Calcula la altura basada en el número de opciones actuales
        float heightFromOptions = dropdownComunidades.options.Count * dropdownItemHeight;

        // Limita la altura al máximo permitido (basado en maxVisibleItems)
        float maxAllowedHeight = maxVisibleDropdownItems * dropdownItemHeight;

        // La altura deseada es el menor entre la altura calculada y la altura máxima
        float calculatedHeight = Mathf.Min(heightFromOptions, maxAllowedHeight);

        // Pero nunca será menor que la altura mínima definida
        float finalHeight = Mathf.Max(calculatedHeight, minDropdownHeight);

        // Aplicamos la nueva altura al RectTransform
        if (viewportRect != null)
        {
            viewportRect.sizeDelta = new Vector2(viewportRect.sizeDelta.x, finalHeight);
        }
        else
        {
            Debug.LogError("No se pudo encontrar el RectTransform de la plantilla para ajustar la altura.");
        }
    }

    private async Task PoblarPanelDeResultados(string encuestaId)
    {
        Debug.Log($"[DEBUG] Iniciando PoblarPanelDeResultados para encuestaId: <color=yellow>{encuestaId}</color>");

        // Limpiar estado anterior
        dropdownComunidades.ClearOptions();
        LimpiarTablaResultados();

        // --- (TODA TU LÓGICA DE CONSULTA A FIREBASE SE MANTIENE IGUAL) ---
        string campoDeBusqueda = $"encuestasAsignadas.{encuestaId}";
        var query = db.Collection("comunidades").WhereEqualTo(campoDeBusqueda, true);
        QuerySnapshot snapshotComunidades = await query.GetSnapshotAsync();

        // ... (el resto del bloque try-catch se mantiene)

        List<string> nombresComunidades = new List<string>();
        if (snapshotComunidades.Documents.Any())
        {
            // ... (el bucle foreach para llenar nombresComunidades se mantiene)
            foreach (var doc in snapshotComunidades.Documents)
            {
                string nombreComunidad = doc.TryGetValue("nombre", out string nombre) ? nombre : doc.Id;
                nombresComunidades.Add(nombreComunidad);
            }

            // --- AQUÍ APLICAMOS LOS CAMBIOS ---

            // 1. Añadimos las opciones al dropdown
            dropdownComunidades.AddOptions(nombresComunidades);

            // 2. ¡NUEVO! Llamamos a nuestra función para ajustar la altura.
            AjustarAlturaPlantillaDropdown();

            // 3. El resto de la lógica se mantiene igual
            dropdownComunidades.onValueChanged.RemoveAllListeners();
            dropdownComunidades.onValueChanged.AddListener(async (index) => {
                if (index < 0 || index >= dropdownComunidades.options.Count) return;
                string comunidadSeleccionada = dropdownComunidades.options[index].text;
                await CargarResultadosParaComunidad(encuestaId, comunidadSeleccionada);
            });

            if (nombresComunidades.Count > 0)
            {
                dropdownComunidades.value = 0;
                dropdownComunidades.RefreshShownValue();
                await CargarResultadosParaComunidad(encuestaId, nombresComunidades[0]);
            }
        }
        else
        {
            dropdownComunidades.AddOptions(new List<string> { "No asignada" });
            // También ajustamos la altura aquí para el caso de "No asignada"
            AjustarAlturaPlantillaDropdown();
        }

        Debug.Log($"[DEBUG] Proceso finalizado. El dropdown ahora tiene {dropdownComunidades.options.Count} opciones.");
    }

    private async Task CargarResultadosParaComunidad(string encuestaId, string comunidadId)
    {
        LimpiarTablaResultados();

        var query = db.Collection("reportes")
                      .WhereEqualTo("idEncuesta", encuestaId)
                      .WhereEqualTo("idComunidad", comunidadId); // <-- Esta línea ya hace lo que necesito

        QuerySnapshot snapshotReportes = await query.GetSnapshotAsync();

        if (!snapshotReportes.Documents.Any())
        {
            Debug.Log($"No se encontraron reportes para la encuesta {encuestaId} en la comunidad {comunidadId}");
            // Opcional: Mostrar un mensaje en la UI de que no hay resultados.
            return;
        }

        // Lista para agrupar los resultados. Guardaremos el mejor intento de cada usuario.
        Dictionary<string, ReporteIntentos> mejoresIntentos = new Dictionary<string, ReporteIntentos>();

        foreach (var doc in snapshotReportes.Documents)
        {
            try
            {
                ReporteIntentos reporte = doc.ConvertTo<ReporteIntentos>(); // Asume que tienes una clase ReporteIntento

                // Si no tenemos un intento para este usuario, o si este intento es mejor (más respuestas correctas), lo guardamos.
                if (!mejoresIntentos.ContainsKey(reporte.idUsuario) || reporte.respuestasCorrectas > mejoresIntentos[reporte.idUsuario].respuestasCorrectas)
                {
                    mejoresIntentos[reporte.idUsuario] = reporte;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error convirtiendo reporte {doc.Id}: {e.Message}");
            }
        }

        // Ahora, poblamos la tabla con los mejores intentos y obtenemos los nombres de los usuarios.
        foreach (var kvp in mejoresIntentos)
        {
            string idDeUsuario = kvp.Key;
            ReporteIntentos reporte = kvp.Value;

            // Obtener el nombre del usuario desde la colección "users"
            DocumentSnapshot userDoc = await db.Collection("users").Document(idDeUsuario).GetSnapshotAsync();
            string nombreUsuario = userDoc.Exists ? userDoc.GetValue<string>("DisplayName") : "Usuario Desconocido";

            // Instanciamos y configuramos la fila
            GameObject nuevaFila = Instantiate(filaResultadoPrefab, contenedorTablaResultados);

            // Asumimos que el prefab tiene un script, ver Parte 3
            var filaUI = nuevaFila.GetComponent<FilaResultadoUI>();
            if (filaUI != null)
            {
                filaUI.Configurar(nombreUsuario, reporte.resultadoFinal);
            }
        }
    }

    private void LimpiarTablaResultados()
    {
        foreach (Transform child in contenedorTablaResultados)
        {
            // No destruir el encabezado si está dentro del mismo contenedor
            if (child.name != "Fila_Encabezado_Resultados")
            {
                Destroy(child.gameObject);
            }
        }
    }

    private async Task<EncuestaModelo> CargarEncuestaPorId(string id)
    {
        if (HayInternet())
        {
            try
            {
                var docRef = db.Collection("Encuestas").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                // --- USAMOS NUESTRA FUNCIÓN SEGURA ---
                return ConvertirDocumentoAEncuesta(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error cargando encuesta desde Firebase: {e.Message}");
            }
        }

        // La lógica local no necesita cambios, ya que JsonUtility no tiene este problema.
        string rutaArchivo = Path.Combine(Application.persistentDataPath, "Encuestas", $"{id}.json");
        if (File.Exists(rutaArchivo))
        {
            try
            {
                string json = File.ReadAllText(rutaArchivo);
                var encuestaLocal = JsonUtility.FromJson<EncuestaModelo>(json);
                // Si el string de la fecha local es válido, podemos convertirlo a Timestamp para la UI si es necesario
                if (DateTime.TryParse(encuestaLocal.FechaCreacionString, out DateTime fechaLocal))
                {
                    encuestaLocal.FechaCreacion = Timestamp.FromDateTime(fechaLocal);
                }
                return encuestaLocal;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error cargando encuesta desde local: {e.Message}");
            }
        }

        return null;
    }

    private void PoblarListaPreguntasDetalles(List<PreguntaModelo> preguntas)
    {
        for (int i = 0; i < preguntas.Count; i++)
        {
            var pregunta = preguntas[i];
            GameObject itemGO = Instantiate(preguntaDetalleItemPrefab, contenedorPreguntasDetalles);

            // ¡CORRECCIÓN! Usamos el script simple.
            var itemUI = itemGO.GetComponent<PreguntaItemUI>();
            if (itemUI != null)
            {
                itemUI.Configurar(pregunta, i, false, encuestasManager);
            }
            else
            {
                Debug.LogError("El prefab 'preguntaDetalleItemPrefab' debe tener el script 'PreguntaDetalleItemUI'.");
            }
        }
    }

    private void LimpiarPanelDetalles()
    {
        // Limpia el título, descripción y la lista de preguntas.
        txtTituloDetalles.text = "";
        txtDescripcionDetalles.text = "";
        foreach (Transform child in contenedorPreguntasDetalles)
        {
            Destroy(child.gameObject);
        }
    }

    private void EliminarEncuestaConfirmada()
    {
        if (string.IsNullOrEmpty(encuestaActualID)) return;

        string idParaEliminar = encuestaActualID; // Guardar ID en una variable local por si acaso.

        // Primero, eliminar el archivo local siempre.
        EliminarEncuestaLocal(idParaEliminar);

        if (HayInternet())
        {
            // Si hay internet, mandar la orden a Firebase.
            // El listener se encargará de actualizar la UI automáticamente.
            db.Collection("Encuestas").Document(idParaEliminar).DeleteAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogWarning($"⚠️ No se pudo eliminar de Firebase (se reintentará): {task.Exception?.Message}");
                    RegistrarEliminacionPendiente(idParaEliminar); // Si falla, la marcamos como pendiente.
                }
                else
                {
                    Debug.Log($"☁️ Encuesta eliminada de Firebase: {idParaEliminar}");
                }
            });
        }
        else
        {
            // Si no hay internet, registrar para eliminar después.
            RegistrarEliminacionPendiente(idParaEliminar);
            // Y refrescar la UI desde los archivos locales que quedan.
            CargarDesdeLocal();
        }

        OcultarPanelConfirmacionEliminar();
        panelDetallesEncuesta.SetActive(false);
    }

    private void EliminarEncuestaLocal(string encuestaID)
    {
        string rutaArchivo = Path.Combine(Application.persistentDataPath, "Encuestas", $"{encuestaID}.json");
        if (File.Exists(rutaArchivo))
        {
            try
            {
                File.Delete(rutaArchivo);
                Debug.Log($"🗑️ Encuesta local eliminada: {rutaArchivo}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al eliminar encuesta local: {e.Message}");
            }
        }
    }

    // Al final de la sección #region Manejo de UI y Encuestas, añade esto:
    private void ConfigurarPestanasDetalles()
    {
        botonVistaPrevia.onClick.AddListener(() => CambiarPestaña(true));
        botonResultados.onClick.AddListener(() => CambiarPestaña(false));
    }

    private void CambiarPestaña(bool mostrarVistaPrevia)
    {
        // Activar/Desactivar los paneles principales
        panelVistaPrevia.SetActive(mostrarVistaPrevia);
        panelResultados.SetActive(!mostrarVistaPrevia);

        // Obtener los componentes de imagen y texto de cada botón
        // Es un poco más de código, pero muy explícito y seguro.
        Image imagenBotonVistaPrevia = botonVistaPrevia.GetComponent<Image>();
        TextMeshProUGUI textoBotonVistaPrevia = botonVistaPrevia.GetComponentInChildren<TextMeshProUGUI>();

        Image imagenBotonResultados = botonResultados.GetComponent<Image>();
        TextMeshProUGUI textoBotonResultados = botonResultados.GetComponentInChildren<TextMeshProUGUI>();

        // Aplicar estilos según la pestaña seleccionada
        if (mostrarVistaPrevia)
        {
            // Pestaña "Vista Previa" está activa
            imagenBotonVistaPrevia.color = colorBotonSeleccionado;
            textoBotonVistaPrevia.color = colorLetraSeleccionado;

            // Pestaña "Resultados" está inactiva
            imagenBotonResultados.color = colorBotonNoSeleccionado;
            textoBotonResultados.color = colorLetraNoSeleccionado;
        }
        else // Mostrar panel de Resultados
        {
            // Pestaña "Vista Previa" está inactiva
            imagenBotonVistaPrevia.color = colorBotonNoSeleccionado;
            textoBotonVistaPrevia.color = colorLetraNoSeleccionado;

            // Pestaña "Resultados" está activa
            imagenBotonResultados.color = colorBotonSeleccionado;
            textoBotonResultados.color = colorLetraSeleccionado;
        }
    }
    #endregion

    #region Métodos de Ayuda y UI
    private void VerificarSiHayEncuestas()
    {
        if (PanelSinEncuestas != null)
        {
            bool noHayEncuestas = contenedorEncuestas.childCount == 0;
            PanelSinEncuestas.SetActive(noHayEncuestas);
        }
    }

    private void LimpiarEncuestasEnPantalla()
    {
        foreach (Transform child in contenedorEncuestas)
        {
            Destroy(child.gameObject);
        }
        encuestasCargadas.Clear();
    }

    private bool HayInternet() => Application.internetReachability != NetworkReachability.NotReachable;

    private void CerrarPanelVerDetalles() => panelDetallesEncuesta.SetActive(false);
    private void MostrarPanelConfirmacionEliminar()
    {
        panelConfirmacionEliminar.SetActive(true);
        PanelGris.SetActive(true);
    }
    private void OcultarPanelConfirmacionEliminar()
    {
        panelConfirmacionEliminar.SetActive(false);
        PanelGris.SetActive(false);
    }

    // El resto de tus métodos de navegación y sincronización de eliminaciones pendientes.
    // He copiado los que faltaban de tu código original.
    private async Task SincronizarEliminacionesPendientes()
    {
        string clave = $"EliminadasOffline_{userId}";
        List<string> eliminadas = ObtenerEliminadasOffline(clave);
        if (eliminadas.Count == 0) return;

        List<string> eliminadasConExito = new();
        foreach (string id in eliminadas)
        {
            try
            {
                await db.Collection("Encuestas").Document(id).DeleteAsync();
                Debug.Log($"☁️ Encuesta pendiente eliminada de Firebase: {id}");
                eliminadasConExito.Add(id);
            }
            catch (Exception e) { Debug.LogWarning($"⚠️ No se pudo eliminar {id} de Firebase: {e.Message}"); }
        }

        eliminadas = eliminadas.Except(eliminadasConExito).ToList();
        string nuevoJson = JsonUtility.ToJson(new ListaSimple(eliminadas));
        PlayerPrefs.SetString(clave, nuevoJson);
        PlayerPrefs.Save();
    }

    private List<string> ObtenerEliminadasOffline(string clave)
    {
        string json = PlayerPrefs.GetString(clave, "");
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonUtility.FromJson<ListaSimple>(json)?.ids ?? new List<string>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Error deserializando '{clave}': {e.Message}");
            return new List<string>();
        }
    }

    private void RegistrarEliminacionPendiente(string encuestaID)
    {
        string clave = $"EliminadasOffline_{userId}";
        List<string> eliminadas = ObtenerEliminadasOffline(clave);
        if (!eliminadas.Contains(encuestaID))
        {
            eliminadas.Add(encuestaID);
            string nuevoJson = JsonUtility.ToJson(new ListaSimple(eliminadas));
            PlayerPrefs.SetString(clave, nuevoJson);
            PlayerPrefs.Save();
            Debug.Log($"📌 Encuesta marcada para eliminar cuando haya conexión: {encuestaID}");
        }
    }

    private void AbrirPanelEncuesta(string IdEncuesta)
    {
        PanelListar.SetActive(false);
        PanelEncuesta.SetActive(true);
        PlayerPrefs.SetString("IdEncuesta", IdEncuesta);
        PlayerPrefs.SetString("ModoEditar", "Activado");
        PlayerPrefs.Save();
        panelDetallesEncuesta.SetActive(false);
        encuestasManager.InicializarEncuesta();
    }

    private void AbrirPanelEncuestaCrearEncuesta()
    {
        PlayerPrefs.SetString("ModoEditar", "Desactivado");
        PlayerPrefs.Save();
        PanelTipoEncuesta.SetActive(true);
        btnSalirTipoEncuesta.onClick.AddListener(() => { PanelTipoEncuesta.SetActive(false); });
        btnSalirElemento.onClick.AddListener(() => { PanelElementoMision.SetActive(false); });

        btnEncuestaMision.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("TipoEncuesta", "Mision");
            PanelElementoMision.SetActive(true);
            PanelTipoEncuesta.SetActive(false);
            controladorSeleccion.IniciarPanel();
            btnContinuarMision.onClick.RemoveAllListeners();
            btnContinuarMision.onClick.AddListener(() =>
            {
                var seleccion = controladorSeleccion.ObtenerSeleccion();
                if (seleccion.categoria != null && seleccion.elemento != null)
                {
                    PlayerPrefs.SetString("CategoriaMision", seleccion.categoria);
                    PlayerPrefs.SetString("ElementoMision", seleccion.elemento);
                    PlayerPrefs.Save();
                    PanelElementoMision.SetActive(false);
                    encuestasManager.InicializarEncuesta();
                    PanelEncuesta.SetActive(true);
                }
            });
        });

        btnEncuestaRecreativa.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("TipoEncuesta", "recreativa");
            encuestasManager.InicializarEncuesta();
            PanelListar.SetActive(false);
            PanelTipoEncuesta.SetActive(false);
            PanelEncuesta.SetActive(true);
        });
        panelDetallesEncuesta.SetActive(false);
    }

    private EncuestaModelo ConvertirDocumentoAEncuesta(DocumentSnapshot doc)
    {
        if (doc == null || !doc.Exists)
        {
            return null;
        }

        // Creamos una instancia vacía del modelo.
        var encuesta = new EncuestaModelo();
        encuesta.Id = doc.Id;

        // Leemos cada campo simple de forma segura.
        if (doc.TryGetValue("titulo", out string titulo)) encuesta.Titulo = titulo;
        if (doc.TryGetValue("descripcion", out string descripcion)) encuesta.Descripcion = descripcion;
        if (doc.TryGetValue("publicada", out bool publicada)) encuesta.Publicada = publicada;
        if (doc.TryGetValue("idcreador", out string idCreador)) encuesta.IdCreador = idCreador;
        if (doc.TryGetValue("tipoEncuesta", out string tipoEncuesta)) encuesta.TipoEncuesta = tipoEncuesta;
        if (doc.TryGetValue("categoriaMision", out string categoriaMision)) encuesta.CategoriaMision = categoriaMision;
        if (doc.TryGetValue("elementoMision", out string elementoMision)) encuesta.ElementoMision = elementoMision;

        // Para la fecha (Timestamp):
        if (doc.TryGetValue("fechaCreacion", out Timestamp fecha))
        {
            encuesta.FechaCreacion = fecha;
            encuesta.FechaCreacionString = fecha.ToDateTime().ToString("o");
        }

        // --- NUEVA LÓGICA MANUAL PARA LA LISTA DE PREGUNTAS ---
        if (doc.TryGetValue("preguntas", out object preguntasData) && preguntasData is List<object> preguntasList)
        {
            var listaDePreguntasModelo = new List<PreguntaModelo>();

            // Iteramos sobre cada elemento de la lista de preguntas
            foreach (var preguntaObj in preguntasList)
            {
                if (preguntaObj is Dictionary<string, object> preguntaMap)
                {
                    var nuevaPregunta = new PreguntaModelo();

                    if (preguntaMap.TryGetValue("textoPregunta", out object textoPreguntaObj) && textoPreguntaObj is string textoPregunta)
                    {
                        nuevaPregunta.TextoPregunta = textoPregunta;
                    }
                    if (preguntaMap.TryGetValue("tipo", out object tipoObj) && tipoObj is long tipoInt) // Firestore a menudo usa long para números
                    {
                        nuevaPregunta.Tipo = (int)tipoInt;
                    }
                    if (preguntaMap.TryGetValue("tiempoSegundos", out object tiempoObj) && tiempoObj is long tiempoInt)
                    {
                        nuevaPregunta.TiempoSegundos = (int)tiempoInt;
                    }

                    // Ahora, deserializamos la lista de opciones dentro de la pregunta
                    if (preguntaMap.TryGetValue("opciones", out object opcionesData) && opcionesData is List<object> opcionesList)
                    {
                        var listaDeOpcionesModelo = new List<OpcionModelo>();
                        foreach (var opcionObj in opcionesList)
                        {
                            if (opcionObj is Dictionary<string, object> opcionMap)
                            {
                                var nuevaOpcion = new OpcionModelo();
                                if (opcionMap.TryGetValue("texto", out object textoOpcionObj) && textoOpcionObj is string textoOpcion)
                                {
                                    nuevaOpcion.Texto = textoOpcion;
                                }
                                if (opcionMap.TryGetValue("esCorrecta", out object esCorrectaObj) && esCorrectaObj is bool esCorrecta)
                                {
                                    nuevaOpcion.EsCorrecta = esCorrecta;
                                }
                                listaDeOpcionesModelo.Add(nuevaOpcion);
                            }
                        }
                        nuevaPregunta.Opciones = listaDeOpcionesModelo;
                    }
                    listaDePreguntasModelo.Add(nuevaPregunta);
                }
            }
            encuesta.Preguntas = listaDePreguntasModelo;
        }
        // Si no hay campo de preguntas o está mal formado, la lista se quedará vacía (lo cual es seguro).

        return encuesta;
    }
    #endregion
}