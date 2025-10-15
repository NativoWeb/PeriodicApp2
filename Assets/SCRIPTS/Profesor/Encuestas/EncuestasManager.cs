// --- Archivo: EncuestasManager.cs (Versión Completa y Corregida) ---
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Net;
using Firebase.Extensions;
using System.IO;
using System;


public class EncuestasManager : MonoBehaviour
{
    #region Referencias Inspector
    [Header("Creación de Encuestas")]
    public TMP_Text TextoTituloEncuesta;
    public TMP_InputField inputTituloEncuesta;
    public TMP_InputField inputDescripcion;
    public Transform contenedorPreguntas;
    public GameObject itemPreguntaPrefab;
    public Button btnGuardarEncuesta;
    public Button btnActualizarEncuesta;
    public Button btnAñadirPregunta;

    [Header("Eliminar Pregunta")]
    public GameObject panelConfirmarEliminarPregunta; 
    public Button btnConfirmarEliminarPregunta;
    public Button btnCancelarEliminarPregunta;

    [Header("Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

    [Header("Paneles para navegacion")]
    public GameObject PanelEncuesta;
    public GameObject PanelCancelarEncuesta;
    public GameObject PanelListar;
    public Button BtnSalir;
    public Button btnSalirCreacionE;
    public Button btnPermanecerE;
    public ListarEncuestas listarencuestas;
    
    #endregion

    #region Variables Privadas
    // --- CAMBIO CLAVE: Nombre y tipo de la variable corregidos ---

    private List<PreguntaModelo> listaPreguntas = new();
    private int indiceEdicion = -1;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;
    private string IdEncuestaEditando;
    private int indicePreguntaAEliminar = -1;
    #endregion

    #region Unity Methods

    void Awake()
    {
        if (listarencuestas == null)
        {
            listarencuestas = FindAnyObjectByType<ListarEncuestas>();
            if (listarencuestas == null)
                Debug.LogError("¡No existe ningún EncuestasManager en la escena!");
        }
    }

    void OnEnable()
    {
        InicializarEncuesta();
    }

    public void InicializarEncuesta()
    {
        InicializarFirebase();

        if (btnAñadirPregunta != null && btnAñadirPregunta.transform.parent != contenedorPreguntas)
        {
            btnAñadirPregunta.transform.SetParent(contenedorPreguntas);
        }

        btnGuardarEncuesta.onClick.RemoveAllListeners();
        btnActualizarEncuesta.onClick.RemoveAllListeners();
        btnAñadirPregunta.onClick.RemoveAllListeners();
        BtnSalir.onClick.RemoveAllListeners();

        BtnSalir.onClick.AddListener(FinalizarEdicion);
        btnAñadirPregunta.onClick.AddListener(AbrirPanelCrearPregunta);

        string modoEditar = PlayerPrefs.GetString("ModoEditar", "Desactivado");
        IdEncuestaEditando = PlayerPrefs.GetString("IdEncuesta", "");

        if (modoEditar == "Activado" && !string.IsNullOrEmpty(IdEncuestaEditando))
        {
            IniciarEdicionEncuesta(IdEncuestaEditando);
        }
        else
        {
            TextoTituloEncuesta.text = "Nueva Encuesta";
            LimpiarCampos();
            btnGuardarEncuesta.gameObject.SetActive(true);
            btnActualizarEncuesta.gameObject.SetActive(false);
            btnGuardarEncuesta.onClick.AddListener(() => ProcesarEncuesta(false));
        }
    }

    #endregion

    #region Inicializar y verificar conexion a internet
    private void InicializarFirebase()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        userId = currentUser?.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("Usuario no autenticado");
            return;
        }
    }

    private bool HayInternet()
    {
        try
        {
            using var client = new WebClient();
            using var stream = client.OpenRead("http://www.google.com");
            return true;
        }
        catch { return false; }
    }
    #endregion

    #region Procesamiento Encuesta
    private void ProcesarEncuesta(bool esEdicion)
    {
        if (!ValidarCamposEncuesta()) return;

        // --- CAMBIO CLAVE: Se usa el nuevo modelo de datos ---
        EncuestaModelo encuesta = CrearModeloDesdeUI(esEdicion);

        if (HayInternet())
        {
            // Para Firebase, convertimos el modelo a diccionario
            var dataForFirebase = encuesta.ToDictionary();
            if (esEdicion)
            {
                ActualizarEncuestaEnFirebase(encuesta.Id, dataForFirebase);
                GuardarLocalmente(encuesta);
            }

            else
            {
                GuardarEnFirebase(encuesta.Id, dataForFirebase);
                GuardarLocalmente(encuesta);
            }
        }
        else
        {
            // Para guardado local, usamos el objeto modelo directamente
            GuardarLocalmente(encuesta);
        }

        if (!esEdicion) LimpiarCampos();
        else FinalizarEdicion();
    }
    #endregion

    #region Validaciones
    private bool ValidarCamposEncuesta()
    {
        if (string.IsNullOrEmpty(userId))
        {
            ShowMessage("Error: No hay un usuario autenticado.", Color.red);
            return false;
        }
        if (string.IsNullOrWhiteSpace(inputTituloEncuesta.text))
        {
            ShowMessage("El título está vacío", Color.red);
            return false;
        }
        if (string.IsNullOrWhiteSpace(inputDescripcion.text))
        {
            ShowMessage("La descripción está vacía", Color.red);
            return false;
        }
        if (listaPreguntas.Count == 0)
        {
            ShowMessage("Debe haber al menos una pregunta", Color.red);
            return false;
        }
        return ValidarPreguntas();
    }

    private bool ValidarPreguntas()
    {
        for (int i = 0; i < listaPreguntas.Count; i++)
        {
            var preguntaModelo = listaPreguntas[i];

            if (string.IsNullOrWhiteSpace(preguntaModelo.TextoPregunta))
            {
                ShowMessage($"La pregunta {i + 1} está vacía", Color.red);
                return false;
            }

            if (preguntaModelo.Opciones.Count < 2)
            {
                ShowMessage($"La pregunta '{preguntaModelo.TextoPregunta}' debe tener al menos 2 opciones", Color.red);
                return false;
            }

            for (int j = 0; j < preguntaModelo.Opciones.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(preguntaModelo.Opciones[j].Texto))
                {
                    ShowMessage($"La opción {j + 1} de la pregunta '{preguntaModelo.TextoPregunta}' está vacía", Color.red);
                    return false;
                }
            }

            if (!preguntaModelo.Opciones.Exists(o => o.EsCorrecta))
            {
                ShowMessage($"La pregunta '{preguntaModelo.TextoPregunta}' no tiene opción correcta marcada", Color.red);
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Utilidades (Sin cambios)
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
            messageText.gameObject.SetActive(false);
    }
    #endregion

    #region Guardado y Actualización Firebase/Local
    // --- CAMBIO CLAVE: Métodos de guardado simplificados, usan el nuevo modelo ---
    private void GuardarEnFirebase(string encuestaID, Dictionary<string, object> data)
    {
        data["fechaCreacion"] = FieldValue.ServerTimestamp;
        db.Collection("Encuestas").Document(encuestaID)
          .SetAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompletedSuccessfully)
              {
                  ShowMessage("✅ Encuesta guardada en Firebase", Color.green);
              }
              else
              {
                  Debug.LogError($"❌ Error guardando encuesta en Firebase: {task.Exception?.Message}");
                  ShowMessage("⚠️ Error al guardar en Firebase. Se guardará localmente.", Color.red);
                  // Si falla, creamos el modelo desde el diccionario y guardamos localmente
                  EncuestaModelo encuesta = CrearModeloDesdeUI(false); // Recreamos el modelo para guardado local
                  GuardarLocalmente(encuesta);
                  PanelEncuesta.SetActive(false);
                  PanelListar.SetActive(true);
              }
          });
    }

    private void GuardarLocalmente(EncuestaModelo encuesta)
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas)) Directory.CreateDirectory(carpetaEncuestas);

        string json = JsonUtility.ToJson(encuesta, true);
        string rutaArchivo = Path.Combine(carpetaEncuestas, $"{encuesta.Id}.json");
        File.WriteAllText(rutaArchivo, json);

        Debug.Log("💾 Encuesta guardada/actualizada localmente en: " + rutaArchivo);
        PanelEncuesta.SetActive(false);
        PanelListar.SetActive(true);
    }

    private void ActualizarEncuestaEnFirebase(string encuestaID, Dictionary<string, object> data)
    {
        data["fechaActualizacion"] = FieldValue.ServerTimestamp;
        db.Collection("Encuestas").Document(encuestaID)
          .UpdateAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompletedSuccessfully)
              {
                  ShowMessage("✅ Encuesta actualizada en Firebase", Color.green);

                  PanelEncuesta.SetActive(false);
                  PanelListar.SetActive(true);
              }
              else
              {
                  Debug.LogError($"❌ Error al actualizar encuesta: {task.Exception?.Message}");
                  ShowMessage("⚠️ Error actualizando en Firebase. Se actualizará localmente.", Color.red);
                  EncuestaModelo encuesta = CrearModeloDesdeUI(true); // Recreamos el modelo para guardado local
                  GuardarLocalmente(encuesta);
                  PanelEncuesta.SetActive(false);
                  PanelListar.SetActive(true);
              }
          });
    }

    // El método de actualización local ahora es el mismo que el de guardado
    // private void ActualizarEncuestaLocalmente(...) ya no es necesario, GuardarLocalmente() hace ambas cosas.

    #endregion

    #region Edición y Limpieza
    private void IniciarEdicionEncuesta(string id)
    {
        TextoTituloEncuesta.text = "Editando encuesta";
        IdEncuestaEditando = id;
        btnGuardarEncuesta.gameObject.SetActive(false);
        btnActualizarEncuesta.gameObject.SetActive(true);
        btnActualizarEncuesta.onClick.AddListener(() => ProcesarEncuesta(true));
        CargarDatosEdicion(id);
    }

    private void CargarDatosEdicion(string id)
    {
        if (HayInternet())
        {
            db.Collection("Encuestas").Document(id).GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.Exists)
                    {
                        // --- CAMBIO CLAVE: Se convierte directo al modelo unificado ---
                        EncuestaModelo encuesta = task.Result.ConvertTo<EncuestaModelo>();
                        PoblarUIDesdeModelo(encuesta);
                    }
                });
        }
        else
        {
            // --- CAMBIO CLAVE: Se carga directo al modelo unificado ---
            string rutaArchivo = Path.Combine(Application.persistentDataPath, "Encuestas", $"{id}.json");
            if (File.Exists(rutaArchivo))
            {
                var json = File.ReadAllText(rutaArchivo);
                var encuesta = JsonUtility.FromJson<EncuestaModelo>(json);
                PoblarUIDesdeModelo(encuesta);
            }
        }
    }

    private void PoblarUIDesdeModelo(EncuestaModelo encuesta)
    {
        inputTituloEncuesta.text = encuesta.Titulo;
        inputDescripcion.text = encuesta.Descripcion;

        // Llama a la nueva función de limpieza simplificada
        LimpiarPreguntasUI();

        // Carga los nuevos datos en la lista
        foreach (var preguntaModelo in encuesta.Preguntas)
        {
            listaPreguntas.Add(preguntaModelo);
        }

        // Dibuja la UI actualizada. ActualizarListado se encargará de
        // volver a poner el botón al final.
        ActualizarListado(true);
    }

    // --- Reemplaza tu método FinalizarEdicion con este ---

    public void FinalizarEdicion()
    {
        // 1. Resetea el estado si es necesario
        IdEncuestaEditando = null;

        // 2. Muestra el panel de confirmación
        PanelCancelarEncuesta.SetActive(true);

        // 3. ¡IMPORTANTE! Limpia los listeners anteriores para evitar acciones duplicadas
        btnSalirCreacionE.onClick.RemoveAllListeners();
        btnPermanecerE.onClick.RemoveAllListeners();

        // 4. Configura el botón de "Confirmar Salida"
        btnSalirCreacionE.onClick.AddListener(() =>
        {
            // Esta lógica SÓLO se ejecuta cuando el usuario hace clic

            // a. Llama al nuevo método seguro para refrescar la lista
            listarencuestas.RefrescarListaDesdeCache();

            // b. Limpia los campos del panel de edición
            LimpiarCampos();

            // c. Gestiona la visibilidad de los paneles
            PanelCancelarEncuesta.SetActive(false);
            PanelListar.SetActive(true);
            PanelEncuesta.SetActive(false);
        });

        // 5. Configura el botón de "Permanecer"
        btnPermanecerE.onClick.AddListener(() =>
        {
            PanelCancelarEncuesta.SetActive(false);
        });
    }

    private void LimpiarPreguntasUI()
    {
        // Limpia solo los ítems, respetando el botón.
        for (int i = contenedorPreguntas.childCount - 1; i >= 0; i--)
        {
            Transform child = contenedorPreguntas.GetChild(i);
            // La condición clave: si el hijo NO es el GameObject del botón, lo destruimos.
            if (child.gameObject != btnAñadirPregunta.gameObject) // <-- ¡CORREGIDO!
            {
                Destroy(child.gameObject);
            }
        }
        // Limpia la lista de datos.
        listaPreguntas.Clear();
    }

    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        inputDescripcion.text = "";

        // Llama a la función de limpieza de UI.
        LimpiarPreguntasUI();

        // Llama a ActualizarListado para asegurarse de que el botón se coloque al final.
        ActualizarListado(true);

        IdEncuestaEditando = null;
        PlayerPrefs.DeleteKey("ModoEditar");
        PlayerPrefs.DeleteKey("IdEncuesta");
    }

    public void AbrirPanelCrearPregunta()
    {
        // Reseteamos el índice de edición, por si acaso.
        indiceEdicion = -1;

        // YA NO desactivamos el PanelEncuesta.
        // Simplemente le decimos al editor de preguntas que se inicie.
        EditorPreguntaManager.Instance.IniciarCreacionPregunta();
    }

    // === LLAMADO DESDE PreguntaItemUI PARA EDITAR ===
    public void AbrirPanelEditarPregunta(int indice)
    {
        if (indice < 0 || indice >= listaPreguntas.Count) return;

        // Guardamos el índice que estamos editando.
        indiceEdicion = indice;
        PreguntaModelo modeloAEditar = listaPreguntas[indice];

        // YA NO desactivamos el PanelEncuesta.
        // Delegamos la lógica de edición al nuevo manager.
        EditorPreguntaManager.Instance.IniciarEdicionPregunta(modeloAEditar);
    }

    // === LLAMADO DESDE PreguntaController AL GUARDAR ===
    public void GuardarPregunta(PreguntaModelo nuevaPregunta)
    {
        if (indiceEdicion >= 0 && indiceEdicion < listaPreguntas.Count)
        {
            // Es una edición: reemplazamos la pregunta en el índice guardado.
            listaPreguntas[indiceEdicion] = nuevaPregunta;
        }
        else
        {
            // Es una pregunta nueva: la añadimos al final.
            listaPreguntas.Add(nuevaPregunta);
        }

        // Reseteamos el índice para la próxima operación.
        indiceEdicion = -1;

        // Actualizamos la lista visual de preguntas en el panel de la encuesta.
        ActualizarListado(true);
    }

    // === LLAMADO DESDE PreguntaItemUI AL CONFIRMAR ELIMINAR ===
    public void EliminarPregunta(int indice)
    {
        // 1. Validar que el índice sea correcto.
        if (indice < 0 || indice >= listaPreguntas.Count)
        {
            Debug.LogError($"Índice de pregunta a eliminar ({indice}) está fuera de rango.");
            return;
        }

        // 2. Eliminar el *modelo de datos* de la lista.
        listaPreguntas.RemoveAt(indice);

        // 3. Re-dibujar la lista de ítems en la UI para que refleje el cambio.
        ActualizarListado(true);

        Debug.Log($"Pregunta en el índice {indice} eliminada correctamente.");
    }

    public List<PreguntaModelo> ObtenerPreguntas()
    {
        return listaPreguntas;
    }

    #endregion

    #region Funciones Datos
    // --- CAMBIO CLAVE: Esta es la función más importante que ha cambiado ---
    private EncuestaModelo CrearModeloDesdeUI(bool esEdicion)
    {
        string encuestaID;
        if (esEdicion)
        {
            // AÑADIMOS UNA VALIDACIÓN AQUÍ
            if (string.IsNullOrEmpty(IdEncuestaEditando))
            {
                Debug.LogError("Se intentó crear un modelo para edición pero el ID de la encuesta era nulo o vacío. Se generará un nuevo ID.");
                // Como plan B, le asignamos un nuevo ID para evitar el error null.
                encuestaID = Guid.NewGuid().ToString();
            }
            else
            {
                encuestaID = IdEncuestaEditando;
            }
        }
        else
        {
            encuestaID = Guid.NewGuid().ToString();
        }

        // El resto del método sigue igual...
        string titulo = inputTituloEncuesta.text;
        string descripcion = inputDescripcion.text;
        string tipoEncuesta = PlayerPrefs.GetString("TipoEncuesta", "");
        string categoriaMision = (tipoEncuesta == "Mision") ? PlayerPrefs.GetString("CategoriaMision") : null;
        string elementoMision = (tipoEncuesta == "Mision") ? PlayerPrefs.GetString("ElementoMision") : null;

        List<PreguntaModelo> preguntas = new(listaPreguntas);

        string fechaActual = DateTime.UtcNow.ToString("o"); 
        string fechaDeCreacionParaElModelo;
        if (esEdicion)
        {
            
            fechaDeCreacionParaElModelo = ""; 
        }
        else
        {
            fechaDeCreacionParaElModelo = fechaActual;
        }

        // Llamamos al nuevo constructor con la fecha.
        return new EncuestaModelo(encuestaID, userId, titulo, descripcion, preguntas, false, tipoEncuesta, categoriaMision, elementoMision, fechaDeCreacionParaElModelo);
    }

    // --- Nuevo Método Público ---
    public void MostrarConfirmacionEliminarPregunta(int indice)
    {
        indicePreguntaAEliminar = indice;
        panelConfirmarEliminarPregunta.SetActive(true);

        // Configurar listeners
        btnConfirmarEliminarPregunta.onClick.RemoveAllListeners();
        btnCancelarEliminarPregunta.onClick.RemoveAllListeners();

        btnConfirmarEliminarPregunta.onClick.AddListener(ConfirmarEliminacionPregunta);
        btnCancelarEliminarPregunta.onClick.AddListener(OcultarConfirmacionEliminarPregunta);
    }

    private void ConfirmarEliminacionPregunta()
    {
        EliminarPregunta(indicePreguntaAEliminar);
        OcultarConfirmacionEliminarPregunta();
    }

    private void OcultarConfirmacionEliminarPregunta()
    {
        panelConfirmarEliminarPregunta.SetActive(false);
        indicePreguntaAEliminar = -1;
    }

    // Y tu función ActualizarListado ahora debe pasar el booleano 'esEditable'
    private void ActualizarListado(bool esEditable)
    {
        foreach (Transform t in contenedorPreguntas)
        {
            if (t.gameObject != btnAñadirPregunta.gameObject) // Si respetas el botón
            {
                Destroy(t.gameObject);
            }
        }

        for (int i = 0; i < listaPreguntas.Count; i++)
        {
            var go = Instantiate(itemPreguntaPrefab, contenedorPreguntas);
            var ui = go.GetComponent<PreguntaItemUI>();
            // Le pasamos el modo al item de la pregunta
            ui.Configurar(listaPreguntas[i], i, esEditable, this);
        }

        if (esEditable)
        {
            btnAñadirPregunta.transform.SetAsLastSibling();
        }
    }
    #endregion

    // --- ¡IMPORTANTE! LAS CLASES ANTIGUAS HAN SIDO ELIMINADAS DE AQUÍ ---
    // Las clases PreguntaData y EncuestaData ya no deben estar definidas en este archivo.
    // Deben estar en el script "ModelosEncuesta.cs" que creaste.
}