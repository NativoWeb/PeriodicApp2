using UnityEngine;
using System.Collections.Generic;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Firebase.Auth;
using System.Net;
using System.Collections;

public class EncuestaManager : MonoBehaviour
{
    [Header("Referencias para Crear Encuestas")]
    public TMP_InputField inputTituloEncuesta;
    public TMP_InputField inputDescripcion;
    public Transform contenedorPreguntas;
    public GameObject preguntaPrefab;
    public Button btnGuardarEncuesta;
    private List<PreguntaController> listaPreguntas = new List<PreguntaController>();

    [Header("Referencias para Mostrar Encuestas")]
    public Transform contenedorEncuestas;
    public GameObject tarjetaEncuestaPrefab;

    [Header("Referencias de Detalles")]
    public GameObject panelDetallesEncuesta;
    public TMP_Text txtTituloEncuesta;
    public TMP_Text txtCodigoEncuesta;
    public TMP_Text txtNumeroPreguntas;
    public Button btnActivarEncuesta;
    public Button btnDesactivarEncuesta;
    public Button btnCancelar;
    public GameObject PanelGris;
    public vistaController vistaController;

    [Header("Referencias para Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

    [Header("Referencias para Edición")]
    public Button btnEditarEncuesta; // Añade este botón en tu panel de detalles
    private bool modoEdicion = false;
    private string encuestaEditandoID = null;

    [Header("Referencia btn actualizar encuesta")]
    public Button btnActualizarEncuesta;

    [Header("Referencias para Eliminar Encuesta")]
    public Button btnEliminarEncuesta; // Botón en el panel de detalles
    public GameObject panelConfirmacionEliminar; // Panel de confirmación
    public Button btnConfirmarEliminar; // Botón de confirmación en el panel
    public Button btnCancelarEliminar; // Botón para cancelar eliminación

    private string encuestaActualID;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;

    private HashSet<string> encuestasCargadas = new HashSet<string>();
    private ListenerRegistration encuestasListener;
    void Start()
    {
        InitializeFirebase();
        StartCoroutine(VerificarConexionPeriodicamente());

        // Configurar listeners de botones existentes
        btnGuardarEncuesta.onClick.AddListener(GuardarEncuesta);
        btnActualizarEncuesta.onClick.AddListener(ActualizarEncuesta);

        // Configurar nuevos botones de eliminación
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
        btnConfirmarEliminar.onClick.AddListener(EliminarEncuestaConfirmada);
        btnCancelarEliminar.onClick.AddListener(OcultarPanelConfirmacionEliminar);

        // Estado inicial
        btnGuardarEncuesta.gameObject.SetActive(true);
        btnActualizarEncuesta.gameObject.SetActive(false);
        panelConfirmacionEliminar.SetActive(false);
        modoEdicion = false;
        encuestaEditandoID = null;
    }
    private void InitializeFirebase()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        userId = currentUser != null ? currentUser.UserId : null;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Usuario no autenticado");
            return;
        }

        if (encuestasListener != null)
        {
            encuestasListener.Stop();
        }

        // 🟢 Solo el listener se encargará de llamar a CargarEncuestas()
        encuestasListener = db.Collection("users").Document(userId).Collection("encuestas")
            .Listen(snapshot =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    CargarEncuestas();
                });
            });
    }

    void OnDestroy()
    {
        // Limpiar el listener cuando el objeto se destruya
        if (encuestasListener != null)
        {
            encuestasListener.Stop();
        }
    }
    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(10);
            if (HayInternet())
            {
                SincronizarEncuestasConFirebase();
            }
        }
    }

    public void PreguntaEliminada(PreguntaController preguntaEliminada)
    {
        listaPreguntas.Remove(preguntaEliminada);
        Debug.Log($"Pregunta eliminada. Total restantes: {listaPreguntas.Count}");
    }

    public void AgregarPregunta()
    {
        if (preguntaPrefab == null || contenedorPreguntas == null)
        {
            Debug.LogError("Referencias no asignadas en el inspector");
            return;
        }

        GameObject nuevaPregunta = Instantiate(preguntaPrefab, contenedorPreguntas);
        PreguntaController controlador = nuevaPregunta.GetComponent<PreguntaController>();

        if (controlador != null)
        {
            listaPreguntas.Add(controlador);
        }
        else
        {
            Debug.LogError("El prefab de pregunta no tiene el componente PreguntaController");
        }
    }

    public void GuardarEncuesta()
    {
        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: No hay un usuario autenticado.", Color.red);
            Debug.LogError("No hay un usuario autenticado.");
            return;
        }

        if (string.IsNullOrEmpty(inputTituloEncuesta.text))
        {
            ShowMessage("El título de la encuesta no puede estar vacío", Color.red);
            Debug.LogError("El título de la encuesta no puede estar vacío.");
            return;
        }

        if (string.IsNullOrEmpty(inputDescripcion.text))
        {
            ShowMessage("La descripción no puede estar vacía", Color.red);
            Debug.LogError("La descripción no puede estar vacía.");
            return;
        }

        if (listaPreguntas.Count == 0)
        {
            ShowMessage("Debes agregar al menos una pregunta", Color.red);
            Debug.LogError("Debes agregar al menos una pregunta.");
            return;
        }

        // Validar cada pregunta individualmente
        for (int i = 0; i < listaPreguntas.Count; i++)
        {
            PreguntaController pregunta = listaPreguntas[i];

            if (string.IsNullOrEmpty(pregunta.inputPregunta.text))
            {
                ShowMessage($"La pregunta {i + 1} no tiene texto", Color.red);
                Debug.LogError($"La pregunta {i + 1} no tiene texto.");
                return;
            }

            // Validar opciones de la pregunta
            var opciones = pregunta.ObtenerOpciones();
            if (opciones.Count == 0)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones", Color.red);
                Debug.LogError($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones.");
                return;
            }

            // Validar que todas las opciones tengan texto
            for (int j = 0; j < opciones.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(opciones[j]))
                {
                    ShowMessage($"La opción {j + 1} de la pregunta '{pregunta.inputPregunta.text}' está vacía", Color.red);
                    Debug.LogError($"La opción {j + 1} de la pregunta '{pregunta.inputPregunta.text}' está vacía.");
                    return;
                }
            }

            bool tieneOpcionCorrecta = pregunta.ObtenerPregunta().opciones.Any(o => o.esCorrecta);
            if (!tieneOpcionCorrecta)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones correctas", Color.red);
                Debug.LogError($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones correctas.");
                return;
            }
            tieneOpcionCorrecta = false;
        }

        string titulo = inputTituloEncuesta.text;
        string descripcion = inputDescripcion.text;
        List<Dictionary<string, object>> preguntasData = PrepararDatosPreguntas();

        string encuestaID = modoEdicion ? encuestaEditandoID : System.Guid.NewGuid().ToString();
        string codigoAcceso = modoEdicion ? txtCodigoEncuesta.text : GenerarCodigoAcceso();

        if (HayInternet())
        {
            if (modoEdicion)
            {
                ActualizarEncuestaEnFirebase(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
            }
            else
            {
                GuardarEnFirebase(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
            }
        }
        else
        {
            if (modoEdicion)
            {
                ActualizarEncuestaLocalmente(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
            }
            else
            {
                GuardarLocalmente(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
            }
        }

        LimpiarCampos();
    }
    public void ActualizarEncuesta()
    {
        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: No hay un usuario autenticado.", Color.red);
            Debug.LogError("No hay un usuario autenticado.");
            return;
        }

        if (string.IsNullOrEmpty(inputTituloEncuesta.text))
        {
            ShowMessage("El título de la encuesta no puede estar vacío", Color.red);
            Debug.LogError("El título de la encuesta no puede estar vacío.");
            return;
        }

        if (string.IsNullOrEmpty(inputDescripcion.text))
        {
            ShowMessage("La descripción no puede estar vacía", Color.red);
            Debug.LogError("La descripción no puede estar vacía.");
            return;
        }

        if (listaPreguntas.Count == 0)
        {
            ShowMessage("Debes agregar al menos una pregunta", Color.red);
            Debug.LogError("Debes agregar al menos una pregunta.");
            return;
        }

        // Validar cada pregunta individualmente
        for (int i = 0; i < listaPreguntas.Count; i++)
        {
            PreguntaController pregunta = listaPreguntas[i];

            if (string.IsNullOrEmpty(pregunta.inputPregunta.text))
            {
                ShowMessage($"La pregunta {i + 1} no tiene texto", Color.red);
                Debug.LogError($"La pregunta {i + 1} no tiene texto.");
                return;
            }

            // Validar opciones de la pregunta
            var opciones = pregunta.ObtenerOpciones();
            if (opciones.Count == 0)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones", Color.red);
                Debug.LogError($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones.");
                return;
            }

            // Validar que todas las opciones tengan texto
            for (int j = 0; j < opciones.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(opciones[j]))
                {
                    ShowMessage($"La opción {j + 1} de la pregunta '{pregunta.inputPregunta.text}' está vacía", Color.red);
                    Debug.LogError($"La opción {j + 1} de la pregunta '{pregunta.inputPregunta.text}' está vacía.");
                    return;
                }
            }

            bool tieneOpcionCorrecta = pregunta.ObtenerPregunta().opciones.Any(o => o.esCorrecta);
            if (!tieneOpcionCorrecta)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones correctas", Color.red);
                Debug.LogError($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones correctas.");
                return;
            }
            tieneOpcionCorrecta = false;
        }

        // Preparar datos
        string titulo = inputTituloEncuesta.text;
        string descripcion = inputDescripcion.text;
        string codigoAcceso = txtCodigoEncuesta.text; // Mantener el código original
        List<Dictionary<string, object>> preguntasData = PrepararDatosPreguntas();

        if (HayInternet())
        {
            ActualizarEncuestaEnFirebase(encuestaEditandoID, titulo, descripcion, codigoAcceso, preguntasData);
        }
        else
        {
            ActualizarEncuestaLocalmente(encuestaEditandoID, titulo, descripcion, codigoAcceso, preguntasData);
        }
        
        FinalizarEdicion();
    }


    private List<Dictionary<string, object>> PrepararDatosPreguntas()
    {
        List<Dictionary<string, object>> preguntasData = new List<Dictionary<string, object>>();

        foreach (PreguntaController preguntaController in listaPreguntas)
        {
            if (preguntaController == null) continue;

            List<Dictionary<string, object>> opcionesData = new List<Dictionary<string, object>>();
            var opciones = preguntaController.ObtenerOpciones();

            foreach (var opcionTexto in opciones)
            {
                // Filtrar opciones vacías
                if (string.IsNullOrWhiteSpace(opcionTexto)) continue;

                bool esCorrecta = preguntaController.ObtenerPregunta().opciones
                    .FirstOrDefault(o => o.textoOpcion == opcionTexto)?.esCorrecta ?? false;

                opcionesData.Add(new Dictionary<string, object>()
            {
                { "texto", opcionTexto },
                { "esCorrecta", esCorrecta }
            });
            }

            preguntasData.Add(new Dictionary<string, object>()
        {
            { "textoPregunta", preguntaController.inputPregunta.text },
            { "opciones", opcionesData }
        });
        }

        return preguntasData;
    }

    private void GuardarEnFirebase(string encuestaID, string titulo, string descripcion,
                                 string codigoAcceso, List<Dictionary<string, object>> preguntasData)
    {
        Dictionary<string, object> encuesta = new Dictionary<string, object>()
        {
            { "id", encuestaID },
            { "titulo", titulo },
            { "descripcion", descripcion },
            { "codigoAcceso", codigoAcceso },
            { "preguntas", preguntasData },
            { "activo", false },
            { "fechaCreacion", FieldValue.ServerTimestamp }
        };

        // Guardar en la subcolección del usuario
        db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID).SetAsync(encuesta)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    ShowMessage("Error al guardar en Firebase, guardando localmente...", Color.red);
                    Debug.LogError("Error al guardar en Firebase, guardando localmente...");
                    GuardarLocalmente(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
                }
                else
                {
                    ShowMessage("Encuesta guardada correctamente", Color.green);
                    Debug.Log("Encuesta guardada en Firebase correctamente");
                    Invoke("volverinicioVistaController", 2f);
                }
            });
    }

    private void GuardarLocalmente(string encuestaID, string titulo, string descripcion,
                                 string codigoAcceso, List<Dictionary<string, object>> preguntasData)
    {
        string claveUsuario = $"Encuestas_{userId}";
        List<string> encuestasUsuario = ObtenerListaDeEncuestas(userId);

        EncuestaData encuestaData = new EncuestaData(encuestaID, titulo, descripcion, codigoAcceso, preguntasData, false);
        string jsonEncuesta = JsonUtility.ToJson(encuestaData);
        encuestasUsuario.Add(jsonEncuesta);

        PlayerPrefs.SetString(claveUsuario, JsonUtility.ToJson(new ListaEncuestas(encuestasUsuario)));
        PlayerPrefs.Save();
        Debug.Log("Encuesta guardada localmente");
    }

    private string GenerarCodigoAcceso()
    {
        const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        System.Text.StringBuilder codigo = new System.Text.StringBuilder();
        System.Random random = new System.Random();

        for (int i = 0; i < 6; i++)
        {
            codigo.Append(caracteres[random.Next(caracteres.Length)]);
        }

        return codigo.ToString();
    }

    public void CargarEncuestas()
    {
        if (contenedorEncuestas == null)
        {
            Debug.LogError("Contenedor de encuestas no asignado");
            return;
        }

        // ✅ Limpiar contenedor y lista de IDs
        foreach (Transform child in contenedorEncuestas)
        {
            Destroy(child.gameObject);
        }
        encuestasCargadas.Clear(); // Limpiar HashSet antes de volver a cargar

        if (HayInternet())
        {
            CargarEncuestasDesdeFirebase();
        }
        else
        {
            CargarEncuestasOffline();
        }
    }


    private void CargarEncuestasDesdeFirebase()
    {
        if (string.IsNullOrEmpty(userId)) return;

        db.Collection("users").Document(userId).Collection("encuestas").GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al cargar encuestas: " + task.Exception);
                    CargarEncuestasOffline();
                    return;
                }

                QuerySnapshot snapshot = task.Result;

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (!doc.Exists) continue;

                    string encuestaID = doc.Id;

                    // 🚫 Evitar duplicados
                    if (encuestasCargadas.Contains(encuestaID)) continue;
                    encuestasCargadas.Add(encuestaID);

                    string titulo = doc.GetValue<string>("titulo");
                    string codigoAcceso = doc.GetValue<string>("codigoAcceso");
                    bool activo = doc.GetValue<bool>("activo");

                    List<Dictionary<string, object>> preguntas = new List<Dictionary<string, object>>();
                    if (doc.ContainsField("preguntas"))
                    {
                        var preguntasData = doc.GetValue<List<object>>("preguntas");
                        foreach (var pregunta in preguntasData)
                        {
                            preguntas.Add((Dictionary<string, object>)pregunta);
                        }
                    }

                    CrearTarjetaEncuesta(titulo, codigoAcceso, preguntas.Count, 0, encuestaID, activo);

                }
            });
    }

    public void CargarEncuestasOffline()
    {
        if (string.IsNullOrEmpty(userId)) return;

        string claveUsuario = $"Encuestas_{userId}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (string.IsNullOrEmpty(json)) return;

        ListaEncuestas listaEncuestas = JsonUtility.FromJson<ListaEncuestas>(json);
        foreach (string jsonEncuesta in listaEncuestas.encuestas)
        {
            try
            {
                EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(jsonEncuesta);
                MostrarEncuestaEnInterfaz(encuesta);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar encuesta: {e.Message}");
            }
        }
    }

    private void MostrarEncuestaEnInterfaz(EncuestaData encuesta)
    {
        CrearTarjetaEncuesta(encuesta.titulo, encuesta.codigoAcceso, encuesta.preguntas.Count, 0, encuesta.id, encuesta.activo);
    }

    void CrearTarjetaEncuesta(string titulo, string codigoAcceso, int numeroPreguntas, int index, string encuestaID, bool activo)
    {
        if (tarjetaEncuestaPrefab == null || contenedorEncuestas == null)
        {
            Debug.LogError("Prefab o contenedor no asignado");
            return;
        }

        GameObject nuevaTarjeta = Instantiate(tarjetaEncuestaPrefab, contenedorEncuestas);
        TMP_Text[] textosTMP = nuevaTarjeta.GetComponentsInChildren<TMP_Text>();

        if (textosTMP.Length >= 3)
        {
            textosTMP[0].text = titulo;
            textosTMP[1].text = codigoAcceso;
            textosTMP[2].text = numeroPreguntas.ToString();
            
        }

        Image fondoTarjeta = nuevaTarjeta.GetComponent<Image>();
        if (fondoTarjeta != null)
        {
            // Solo aplica color si está activa, de lo contrario deja el color normal
            if (activo)
            {
                ColorUtility.TryParseHtmlString("#A5EFE5", out Color colorActivo);
                fondoTarjeta.color = colorActivo;
            }
            else
            {
                // Esto restablecerá el color al original del prefab
                fondoTarjeta.color = Color.white; // o el color que tenga por defecto
            }
        }

        Button botonVerEncuesta = nuevaTarjeta.GetComponentInChildren<Button>();
        if (botonVerEncuesta != null)
        {
            botonVerEncuesta.onClick.AddListener(() => MostrarDetallesEncuesta(titulo,numeroPreguntas, codigoAcceso, encuestaID, activo));
        }
    }

    public void MostrarDetallesEncuesta(string titulo, int numeropreguntas, string codigo, string encuestaID, bool activo)
    {
        encuestaActualID = encuestaID;
        txtTituloEncuesta.text = titulo;
        txtCodigoEncuesta.text = codigo;
        txtNumeroPreguntas.text = numeropreguntas.ToString();

        // Limpiar listeners anteriores
        btnActivarEncuesta.onClick.RemoveAllListeners();
        btnDesactivarEncuesta.onClick.RemoveAllListeners();
        btnEditarEncuesta.onClick.RemoveAllListeners();
        btnEliminarEncuesta.onClick.RemoveAllListeners();

        // Configurar listeners
        btnActivarEncuesta.interactable = !activo;
        btnActivarEncuesta.onClick.AddListener(() => CambiarEstadoEncuesta(encuestaID, true));

        btnDesactivarEncuesta.interactable = activo;
        btnDesactivarEncuesta.onClick.AddListener(() => CambiarEstadoEncuesta(encuestaID, false));

        btnEditarEncuesta.onClick.AddListener(() => IniciarEdicionEncuesta(encuestaID));
        btnEliminarEncuesta.onClick.AddListener(() => MostrarPanelConfirmacionEliminar());

        panelDetallesEncuesta.SetActive(true);
    }

    // Metodos para edicion de encuesta _______________________________________________________________
    private void IniciarEdicionEncuesta(string encuestaID)
    {
        Debug.Log($"Iniciando edición de encuesta ID: {encuestaID}");

        // Establecer estado de edición
        modoEdicion = true;
        encuestaEditandoID = encuestaID;
        encuestaActualID = encuestaID;

        // Ocultar panel de detalles
        panelDetallesEncuesta.SetActive(false);

        // Cambiar visibilidad de botones - SIN UnityMainThreadDispatcher
        btnGuardarEncuesta.gameObject.SetActive(false);
        btnActualizarEncuesta.gameObject.SetActive(true);

        Debug.Log($"Botones actualizados - Guardar: {btnGuardarEncuesta.gameObject.activeSelf}, Actualizar: {btnActualizarEncuesta.gameObject.activeSelf}");

        // Limpiar y cargar datos para edición
        LimpiarCampos();
        CargarEncuestaParaEditar(encuestaID);

        // Cambiar a vista de edición
        vistaController.CambiarAVistaEdicion();
    }
    private void CargarEncuestaParaEditar(string encuestaID)
    {
        Debug.Log($"Cargando encuesta para editar ID: {encuestaID}");

        if (HayInternet())
        {
            Debug.Log("Cargando desde Firebase...");
            db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID).GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log("Tarea de Firebase completada");
                        DocumentSnapshot snapshot = task.Result;
                        if (snapshot.Exists)
                        {
                            Debug.Log("Documento encontrado en Firebase");
                            Dictionary<string, object> encuesta = snapshot.ToDictionary();
                            MostrarEncuestaParaEditar(encuesta);
                        }
                        else
                        {
                            Debug.LogWarning("Documento no existe en Firebase");
                        }
                    }
                    else if (task.IsFaulted)
                    {
                        Debug.LogError($"Error al cargar encuesta: {task.Exception}");
                    }
                });
        }
        else
        {
            Debug.Log("Cargando desde datos locales...");
            string claveUsuario = $"Encuestas_{userId}";
            string json = PlayerPrefs.GetString(claveUsuario, "");

            if (!string.IsNullOrEmpty(json))
            {
                Debug.Log("Datos locales encontrados");
                ListaEncuestas listaEncuestas = JsonUtility.FromJson<ListaEncuestas>(json);
                bool encontrada = false;

                foreach (string jsonEncuesta in listaEncuestas.encuestas)
                {
                    EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(jsonEncuesta);
                    if (encuesta.id == encuestaID)
                    {
                        Debug.Log("Encuesta encontrada en datos locales");
                        encontrada = true;
                        MostrarEncuestaParaEditar(encuesta.ToDictionary());
                        break;
                    }
                }

                if (!encontrada)
                {
                    Debug.LogWarning("Encuesta no encontrada en datos locales");
                }
            }
            else
            {
                Debug.LogWarning("No hay datos locales guardados");
            }
        }
    }

    private void MostrarEncuestaParaEditar(Dictionary<string, object> encuesta)
    {
        try
        {
            Debug.Log("Mostrando encuesta para editar...");

            // Verificar datos básicos
            if (!encuesta.ContainsKey("titulo") || !encuesta.ContainsKey("descripcion") || !encuesta.ContainsKey("preguntas"))
            {
                ShowMessage("Error: Datos de encuesta incompletos", Color.red);
                Debug.LogError("Datos de encuesta incompletos");
                return;
            }

            // Asignar título y descripción
            inputTituloEncuesta.text = encuesta["titulo"].ToString();
            inputDescripcion.text = encuesta["descripcion"].ToString();

            // Cargar preguntas
            var preguntasData = (List<object>)encuesta["preguntas"];
            foreach (var preguntaObj in preguntasData)
            {
                var preguntaData = (Dictionary<string, object>)preguntaObj;

                // Validar estructura de pregunta
                if (!preguntaData.ContainsKey("textoPregunta") || !preguntaData.ContainsKey("opciones"))
                {
                    Debug.LogWarning("Estructura de pregunta inválida, omitiendo...");
                    continue;
                }

                AgregarPregunta();
                PreguntaController nuevaPregunta = listaPreguntas.Last();
                nuevaPregunta.inputPregunta.text = preguntaData["textoPregunta"].ToString();

                var opcionesData = (List<object>)preguntaData["opciones"];
                foreach (var opcionObj in opcionesData)
                {
                    var opcionData = (Dictionary<string, object>)opcionObj;

                    // Validar estructura de opción
                    if (!opcionData.ContainsKey("texto") || !opcionData.ContainsKey("esCorrecta"))
                    {
                        Debug.LogWarning("Estructura de opción inválida, omitiendo...");
                        continue;
                    }

                    string textoOpcion = opcionData["texto"].ToString();
                    bool esCorrecta = (bool)opcionData["esCorrecta"];

                    // Solo agregar si tiene texto
                    if (!string.IsNullOrWhiteSpace(textoOpcion))
                    {
                        nuevaPregunta.AgregarOpcionUI(textoOpcion, esCorrecta);
                    }
                }
            }

            vistaController.CambiarAVistaEdicion();
        }
        catch (System.Exception e)
        {
            ShowMessage("Error al cargar encuesta para editar", Color.red);
            Debug.LogError($"Error en MostrarEncuestaParaEditar: {e.Message}\n{e.StackTrace}");
        }
    }

    public void FinalizarEdicion()
    {
        Debug.Log("Finalizando edición...");

        // Restablecer estado
        modoEdicion = false;
        encuestaEditandoID = null;
        encuestaActualID = null;

        // Cambiar visibilidad de botones
        btnGuardarEncuesta.gameObject.SetActive(true);
        btnActualizarEncuesta.gameObject.SetActive(false);

        Debug.Log($"Botones finalizados - Guardar: {btnGuardarEncuesta.gameObject.activeSelf}, Actualizar: {btnActualizarEncuesta.gameObject.activeSelf}");

        // Limpiar campos
        LimpiarCampos();

        // Volver a la vista principal
        Invoke("volverinicioVistaController", 2f);
    }

    void volverinicioVistaController()
    {
        vistaController.Inicio();
    }
    private void ActualizarEncuestaEnFirebase(string encuestaID, string titulo, string descripcion,
                                       string codigoAcceso, List<Dictionary<string, object>> preguntasData)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>()
    {
        { "titulo", titulo },
        { "descripcion", descripcion },
        { "preguntas", preguntasData },
        { "fechaActualizacion", FieldValue.ServerTimestamp }
    };

        db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID)
            .UpdateAsync(updates).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    ShowMessage("Encuesta actualizada exitosamente!", Color.green);
                    LimpiarCampos();
                    CargarEncuestas();
                }
                else if (task.IsFaulted)
                {
                    ShowMessage("Error al actualizar encuesta, guardando localmente...", Color.red);
                    ActualizarEncuestaLocalmente(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
                }
            });
    }

    private void ActualizarEncuestaLocalmente(string encuestaID, string titulo, string descripcion,
                                            string codigoAcceso, List<Dictionary<string, object>> preguntasData)
    {
        string claveUsuario = $"Encuestas_{userId}";
        List<string> encuestasUsuario = ObtenerListaDeEncuestas(userId);

        // Buscar y actualizar la encuesta existente
        for (int i = 0; i < encuestasUsuario.Count; i++)
        {
            EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(encuestasUsuario[i]);
            if (encuesta.id == encuestaID)
            {
                encuesta.titulo = titulo;
                encuesta.descripcion = descripcion;
                encuesta.preguntas = preguntasData;
                encuestasUsuario[i] = JsonUtility.ToJson(encuesta);
                break;
            }
        }

        PlayerPrefs.SetString(claveUsuario, JsonUtility.ToJson(new ListaEncuestas(encuestasUsuario)));
        PlayerPrefs.Save();
        ShowMessage("Cambios guardados localmente. Se sincronizarán cuando haya conexión.", new Color(1f, 0.5f, 0f));
        LimpiarCampos();
        CargarEncuestas();
    }
    private void CambiarEstadoEncuesta(string encuestaID, bool activo)
    {
        Dictionary<string, object> updateData = new Dictionary<string, object>
        {
            { "activo", activo }
        };

        if (HayInternet())
        {
            db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID).UpdateAsync(updateData)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log($"Encuesta {(activo ? "activada" : "desactivada")} correctamente");
                        ActualizarEstadoTarjeta(encuestaID, activo);
                        panelDetallesEncuesta.SetActive(false);
                    }
                    else
                    {
                        Debug.LogError($"Error al {(activo ? "activar" : "desactivar")} encuesta: {task.Exception}");
                    }
                });
        }
        else
        {
            string msg = "No hay conexión a internet. El cambio se aplicará cuando se restablezca la conexión.";
            ShowMessage(msg, new Color(1f, 0.5f, 0f));
            Debug.Log(msg);
            ActualizarEstadoTarjeta(encuestaID, activo);
            panelDetallesEncuesta.SetActive(false);
        }
    }

    private void ActualizarEstadoTarjeta(string encuestaID, bool activo)
    {
        foreach (Transform child in contenedorEncuestas)
        {
            Button btn = child.GetComponentInChildren<Button>();
            if (btn != null)
            {
                var listeners = btn.onClick.GetPersistentEventCount();
                for (int i = 0; i < listeners; i++)
                {
                    if (btn.onClick.GetPersistentMethodName(i) == "MostrarDetallesEncuesta")
                    {
                        Image img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            if (activo)
                            {
                                ColorUtility.TryParseHtmlString("#A5EFE5", out Color colorActivo);
                                img.color = colorActivo;
                            }
                            else
                            {
                                img.color = Color.white; // o el color que tenga por defecto
                            }
                        }
                        break;
                    }
                }
            }
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

    public void SincronizarEncuestasConFirebase()
    {
        if (!HayInternet() || string.IsNullOrEmpty(userId)) return;

        string claveUsuario = $"Encuestas_{userId}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (string.IsNullOrEmpty(json)) return;

        ListaEncuestas listaEncuestas = JsonUtility.FromJson<ListaEncuestas>(json);
        var encuestasParaEliminar = new List<string>();

        foreach (string jsonEncuesta in listaEncuestas.encuestas)
        {
            try
            {
                EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(jsonEncuesta);
                GuardarEnFirebase(encuesta.id, encuesta.titulo, encuesta.descripcion, encuesta.codigoAcceso, encuesta.preguntas);
                encuestasParaEliminar.Add(encuesta.id);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al sincronizar encuesta: {e.Message}");
            }
        }

        if (encuestasParaEliminar.Count > 0)
        {
            listaEncuestas.encuestas.RemoveAll(e => encuestasParaEliminar.Contains(JsonUtility.FromJson<EncuestaData>(e).id));
            PlayerPrefs.SetString(claveUsuario, JsonUtility.ToJson(listaEncuestas));
            PlayerPrefs.Save();
        }
    }

    private List<string> ObtenerListaDeEncuestas(string usuario)
    {
        string claveUsuario = $"Encuestas_{usuario}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (string.IsNullOrEmpty(json))
            return new List<string>();

        return JsonUtility.FromJson<ListaEncuestas>(json).encuestas;
    }
    private void MostrarPanelConfirmacionEliminar()
    {
        panelConfirmacionEliminar.SetActive(true);
        PanelGris.SetActive(true); // Oscurecer fondo
    }

    private void OcultarPanelConfirmacionEliminar()
    {
        panelConfirmacionEliminar.SetActive(false);
        
    }

    private void EliminarEncuestaConfirmada()
    {
        if (string.IsNullOrEmpty(encuestaActualID))
        {
            ShowMessage("Error: No hay encuesta seleccionada", Color.red);
            return;
        }

        if (HayInternet())
        {
            EliminarEncuestaDeFirebase(encuestaActualID);
        }
        else
        {
            EliminarEncuestaLocalmente(encuestaActualID);
        }

        OcultarPanelConfirmacionEliminar();
        panelDetallesEncuesta.SetActive(false);
    }

    private void EliminarEncuestaDeFirebase(string encuestaID)
    {
        db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID)
            .DeleteAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    ShowMessage("Encuesta eliminada correctamente", Color.green);
                    CargarEncuestas(); // Refrescar la lista
                }
                else
                {
                    ShowMessage("Error al eliminar, intentando eliminar localmente...", Color.red);
                    EliminarEncuestaLocalmente(encuestaID);
                }
            });
    }

    private void EliminarEncuestaLocalmente(string encuestaID)
    {
        string claveUsuario = $"Encuestas_{userId}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (!string.IsNullOrEmpty(json))
        {
            ListaEncuestas listaEncuestas = JsonUtility.FromJson<ListaEncuestas>(json);
            listaEncuestas.encuestas = listaEncuestas.encuestas
                .Where(e => JsonUtility.FromJson<EncuestaData>(e).id != encuestaID)
                .ToList();

            PlayerPrefs.SetString(claveUsuario, JsonUtility.ToJson(listaEncuestas));
            PlayerPrefs.Save();
            ShowMessage("Encuesta eliminada localmente", new Color(1f, 0.5f, 0f));
            CargarEncuestas(); // Refrescar la lista
        }
    }
    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        inputDescripcion.text = "";

        // Limpiar preguntas
        foreach (Transform child in contenedorPreguntas)
        {
            Destroy(child.gameObject);
        }
        listaPreguntas.Clear();

        // Solo restablecer botones si NO estamos en modo edición
        if (!modoEdicion)
        {
            btnGuardarEncuesta.gameObject.SetActive(true);
            btnActualizarEncuesta.gameObject.SetActive(false);
        }

        Debug.Log($"Campos limpiados - ModoEdicion: {modoEdicion}, BtnGuardar: {btnGuardarEncuesta.gameObject.activeSelf}, BtnActualizar: {btnActualizarEncuesta.gameObject.activeSelf}");
    }

    public void cerrarmodoedicion()
    {
        modoEdicion = false;
    }
    private void ShowMessage(string message, Color color)
    {
        if (messageText == null) return;

        messageText.text = message;
        messageText.color = color;
        messageText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), messageDuration);
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }
}