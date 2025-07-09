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

    [Header("Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

    [Header("Paneles para navegacion")]
    public GameObject PanelEncuesta;
    public GameObject PanelCancelarEncuesta;
    public GameObject PanelListar;
    public GameObject panelFormularioPregunta; // Panel que contiene el PreguntaController (desactivado por defecto)
    public Button BtnSalir;
    public Button BtnCancelar;
    public Button btnSalirCreacionE;
    public Button btnPermanecerE;
    public ListarEncuestas listarencuestas;
    #endregion

    #region Variables Privadas
    // --- CAMBIO CLAVE: Nombre y tipo de la variable corregidos ---

    private List<PreguntaModelo> listaPreguntas = new();          // Datos
    private List<PreguntaController> listaPreguntasUI = new();    // Controladores de la UI

    private int indiceEdicion = -1;
    private PreguntaController controladorPregunta;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;
    private string IdEncuestaEditando;
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

    public void InicializarEncuesta()
    {
        InicializarFirebase();

        panelFormularioPregunta.SetActive(false);

        btnGuardarEncuesta.onClick.RemoveAllListeners();
        btnActualizarEncuesta.onClick.RemoveAllListeners();
        btnAñadirPregunta.onClick.RemoveAllListeners();
        BtnCancelar.onClick.RemoveAllListeners();
        BtnSalir.onClick.RemoveAllListeners();

        BtnCancelar.onClick.AddListener(FinalizarEdicion);
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
            db.Collection("users").Document(userId).Collection("encuestas").Document(id).GetSnapshotAsync()
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
        listaPreguntasUI.Clear();

        // Limpia primero
        foreach (Transform t in contenedorPreguntas)
            Destroy(t.gameObject);
        listaPreguntas.Clear();

        // Carga cada pregunta como ítem visual
        for (int i = 0; i < encuesta.Preguntas.Count; i++)
        {
            var preguntaModelo = encuesta.Preguntas[i];

            GameObject nuevaPreguntaGO = Instantiate(itemPreguntaPrefab, contenedorPreguntas);
            PreguntaItemUI itemUI = nuevaPreguntaGO.GetComponent<PreguntaItemUI>();

            if (itemUI != null)
                itemUI.Configurar(preguntaModelo, i, this);

            listaPreguntas.Add(preguntaModelo);
        }
    }

    public void FinalizarEdicion()
    {
        IdEncuestaEditando = null;
        PanelCancelarEncuesta.SetActive(true);
        btnSalirCreacionE.onClick.AddListener(() =>
        {
            listarencuestas.CargarEncuestas();
            LimpiarCampos();
            PanelCancelarEncuesta.SetActive(false);
            PanelListar.SetActive(true);
            PanelEncuesta.SetActive(false);
        });
        btnPermanecerE.onClick.AddListener(() => { PanelCancelarEncuesta.SetActive(false); });
        listarencuestas.CargarEncuestas();
    }

    private void LimpiarPreguntasUI()
    {
        foreach (Transform child in contenedorPreguntas)
        {
            Destroy(child.gameObject);
        }
        listaPreguntas.Clear();
    }

    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        inputDescripcion.text = "";
        LimpiarPreguntasUI();
        IdEncuestaEditando = null;
        PlayerPrefs.DeleteKey("ModoEditar");
        PlayerPrefs.DeleteKey("IdEncuesta");
    }

    public void AbrirPanelCrearPregunta()
    {
        indiceEdicion = -1;
        panelFormularioPregunta.SetActive(true);

        // Asegúrate de limpiar el formulario
        controladorPregunta = panelFormularioPregunta.GetComponent<PreguntaController>();
        controladorPregunta.InicializarParaCrear();
    }

    // === LLAMADO DESDE PreguntaItemUI PARA EDITAR ===
    public void AbrirPanelEditarPregunta(int indice)
    {
        if (indice < 0 || indice >= listaPreguntas.Count) return;

        panelFormularioPregunta.SetActive(true);
        indiceEdicion = indice;

        controladorPregunta = panelFormularioPregunta.GetComponent<PreguntaController>();
        controladorPregunta.encuestasManager = this;
        controladorPregunta.PoblarUIDesdeModelo(listaPreguntas[indice]);
    }

    // === LLAMADO DESDE PreguntaController AL GUARDAR ===
    public void GuardarPregunta(PreguntaModelo nuevaPregunta)
    {

        if (indiceEdicion >= 0 && indiceEdicion < listaPreguntas.Count)
        {
            listaPreguntas[indiceEdicion] = nuevaPregunta;
        }
        else
        {
            listaPreguntas.Add(nuevaPregunta);
        }
        indiceEdicion = -1; // resetea después

        ActualizarListado();
        controladorPregunta = panelFormularioPregunta.GetComponent<PreguntaController>();
        controladorPregunta.LimpiarCampos();
        panelFormularioPregunta.SetActive(false);
    }

    // === LLAMADO DESDE PreguntaItemUI AL CONFIRMAR ELIMINAR ===
    public void EliminarPregunta(int indice)
    {
        if (indice < 0 || indice >= listaPreguntas.Count) return;
        listaPreguntas.RemoveAt(indice);
        ActualizarListado();
    }

    public void PreguntaEliminada(PreguntaController pregunta)
    {
        listaPreguntasUI.Remove(pregunta); // Asegúrate que esta lista sea List<PreguntaController>
        ActualizarListado();             // O actualiza visualmente si usas un método diferente
        Debug.Log("Pregunta eliminada correctamente.");
    }

    private void ActualizarListado()
    {
        foreach (Transform t in contenedorPreguntas)
            Destroy(t.gameObject);

        for (int i = 0; i < listaPreguntas.Count; i++)
        {
            var go = Instantiate(itemPreguntaPrefab, contenedorPreguntas);
            var ui = go.GetComponent<PreguntaItemUI>();
            ui.Configurar(listaPreguntas[i], i, this);
        }
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

        return new EncuestaModelo(encuestaID, titulo, descripcion, preguntas, false, tipoEncuesta, categoriaMision, elementoMision);
    }
    #endregion

    // --- ¡IMPORTANTE! LAS CLASES ANTIGUAS HAN SIDO ELIMINADAS DE AQUÍ ---
    // Las clases PreguntaData y EncuestaData ya no deben estar definidas en este archivo.
    // Deben estar en el script "ModelosEncuesta.cs" que creaste.
}