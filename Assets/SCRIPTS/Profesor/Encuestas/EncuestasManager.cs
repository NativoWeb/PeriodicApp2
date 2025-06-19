// EncuestaUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Collections;
using System.Net;
using System.Linq;
using Firebase.Extensions;
using System.IO;
using System;

public class EncuestasManager : MonoBehaviour
{
    #region Referencias Inspector

    [Header("Creación de Encuestas")]
    public TMP_InputField inputTituloEncuesta;
    public TMP_InputField inputDescripcion;
    public Transform contenedorPreguntas;
    public GameObject preguntaPrefab;
    public Button btnGuardarEncuesta;
    public Button btnActualizarEncuesta;
    public Button btnAñadirPregunta;

    [Header("Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

    [Header("Paneles para navegacion")]
    public GameObject PanelEncuesta;
    public GameObject PanelListar;
    public Button BtnSalir;
    public Button BtnCancelar;

    #endregion

    #region Variables Privadas

    private List<PreguntaController> listaPreguntas = new();
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;
    private string IdEncuestaEditando;
    #endregion

    #region Unity Methods

    public void InicializarEncuesta()
    {
        InicializarFirebase();

        BtnCancelar.onClick.RemoveAllListeners();
        BtnSalir.onClick.RemoveAllListeners();
        btnAñadirPregunta.onClick.RemoveAllListeners();

        BtnCancelar.onClick.AddListener(FinalizarEdicion);
        BtnSalir.onClick.AddListener(FinalizarEdicion);
        btnAñadirPregunta.onClick.AddListener(AgregarPregunta);

        // Verificar modo edición
        string modoEditar = PlayerPrefs.GetString("ModoEditar");
        IdEncuestaEditando = PlayerPrefs.GetString("IdEncuesta");


        if (modoEditar == "Activado" && !string.IsNullOrEmpty(IdEncuestaEditando))
        {
            IniciarEdicionEncuesta(IdEncuestaEditando);
        }
        else
        {
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

        string encuestaID = esEdicion ? IdEncuestaEditando : System.Guid.NewGuid().ToString();
        string titulo = inputTituloEncuesta.text;
        string descripcion = inputDescripcion.text;
        var preguntasData = PrepararDatosPreguntas();

        if (HayInternet())
        {
            if (esEdicion)
                ActualizarEncuestaEnFirebase(encuestaID, titulo, descripcion, preguntasData);
            else
            {
                GuardarEnFirebase(encuestaID, titulo, descripcion, preguntasData);
                LimpiarCampos();
            }
        }
        else
        {
            if (esEdicion)
                ActualizarEncuestaLocalmente(encuestaID, titulo, descripcion, preguntasData);
            else
            {
                GuardarLocalmente(encuestaID, titulo, descripcion, preguntasData);
                LimpiarCampos();
            }
        }
        if (esEdicion) FinalizarEdicion();
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
            var pregunta = listaPreguntas[i];
            if (string.IsNullOrWhiteSpace(pregunta.inputPregunta.text))
            {
                ShowMessage($"La pregunta {i + 1} está vacía", Color.red);
                return false;
            }

            var opciones = pregunta.ObtenerOpciones();
            if (opciones.Count == 0)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones", Color.red);
                return false;
            }

            for (int j = 0; j < opciones.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(opciones[j]))
                {
                    ShowMessage($"La opción {j + 1} de la pregunta '{pregunta.inputPregunta.text}' está vacía", Color.red);
                    return false;
                }
            }

            bool tieneCorrecta = pregunta.ObtenerPregunta().opciones.Any(o => o.esCorrecta);
            if (!tieneCorrecta)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opción correcta", Color.red);
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Utilidades

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
    private void GuardarEnFirebase(string encuestaID, string titulo, string descripcion, List<Dictionary<string, object>> preguntasData)
    {
        var data = new Dictionary<string, object>
    {
        { "id", encuestaID },
        { "titulo", titulo },
        { "descripcion", descripcion },
        { "preguntas", preguntasData },
        { "activo", false },
        { "fechaCreacion", FieldValue.ServerTimestamp }
    };

        db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID)
          .SetAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompletedSuccessfully)
              {
                  ShowMessage("✅ Encuesta guardada en Firebase", Color.green);
              }
              else if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError($"❌ Error guardando encuesta en Firebase: {task.Exception?.Message}");
                  ShowMessage("⚠️ Error al guardar en Firebase. Se guardará localmente.", Color.red);
                  GuardarLocalmente(encuestaID, titulo, descripcion, preguntasData);
              }
          });
    }

    private void GuardarLocalmente(string encuestaID, string titulo, string descripcion, List<Dictionary<string, object>> preguntasData)
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");

        // Crear la carpeta si no existe
        if (!Directory.Exists(carpetaEncuestas))
        {
            Directory.CreateDirectory(carpetaEncuestas);
            Debug.Log("📁 Carpeta 'Encuestas' creada en: " + carpetaEncuestas);
        }

        // Crear el objeto de datos
        var preguntasConvertidas = ConvertirADatosDePreguntas(preguntasData);
        var data = new EncuestaData(encuestaID, titulo, descripcion, preguntasConvertidas, false);
        string json = JsonUtility.ToJson(data, true); // pretty print

        // Guardar en archivo individual
        string rutaArchivo = Path.Combine(carpetaEncuestas, $"{encuestaID}.json");
        File.WriteAllText(rutaArchivo, json);

        Debug.Log("💾 Encuesta guardada localmente en: " + rutaArchivo);
    }

    private void ActualizarEncuestaEnFirebase(string encuestaID, string titulo, string descripcion, List<Dictionary<string, object>> preguntasData)
    {
        var updates = new Dictionary<string, object>
    {
        { "titulo", titulo },
        { "descripcion", descripcion },
        { "preguntas", preguntasData },
        { "fechaActualizacion", FieldValue.ServerTimestamp }
    };

        db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaID)
          .UpdateAsync(updates)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompletedSuccessfully)
              {
                  ShowMessage("✅ Encuesta actualizada en Firebase", Color.green);
              }
              else if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError($"❌ Error al actualizar encuesta: {task.Exception?.Message}");
                  ShowMessage("⚠️ Error actualizando en Firebase. Se actualizará localmente.", Color.red);
                  ActualizarEncuestaLocalmente(encuestaID, titulo, descripcion, preguntasData);
              }
          });
    }

    private void ActualizarEncuestaLocalmente(string encuestaID, string titulo, string descripcion, List<Dictionary<string, object>> preguntasData)
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");

        // Crear la carpeta si no existe
        if (!Directory.Exists(carpetaEncuestas))
        {
            Directory.CreateDirectory(carpetaEncuestas);
        }

        string rutaArchivo = Path.Combine(carpetaEncuestas, $"{encuestaID}.json");

        // ✅ Convertir a List<PreguntaData>
        List<PreguntaData> preguntasConvertidas = new List<PreguntaData>();
        foreach (var dic in preguntasData)
        {
            var pregunta = new PreguntaData
            {
                pregunta = dic.ContainsKey("pregunta") ? dic["pregunta"].ToString() : "",
                opciones = dic.ContainsKey("opciones") ? ((List<object>)dic["opciones"]).Select(o => o.ToString()).ToList() : new List<string>(),
                respuestaCorrecta = dic.ContainsKey("respuestaCorrecta") ? dic["respuestaCorrecta"].ToString() : ""
            };
            preguntasConvertidas.Add(pregunta);
        }

        // 🧩 Construir encuesta con preguntas convertidas
        EncuestaData encuestaActualizada = new EncuestaData(
            encuestaID,
            titulo,
            descripcion,
            preguntasConvertidas,
            false
        );

        string nuevoJson = JsonUtility.ToJson(encuestaActualizada, true);
        File.WriteAllText(rutaArchivo, nuevoJson);

        Debug.Log($"✏️ Encuesta actualizada localmente: {rutaArchivo}");
    }

    private List<string> ObtenerListaDeEncuestas()
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");

        if (!Directory.Exists(carpetaEncuestas))
        {
            return new List<string>(); // No hay carpeta = no hay encuestas
        }

        List<string> encuestasJson = new List<string>();
        string[] archivos = Directory.GetFiles(carpetaEncuestas, "*.json");

        foreach (string rutaArchivo in archivos)
        {
            try
            {
                string contenido = File.ReadAllText(rutaArchivo);
                encuestasJson.Add(contenido);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ No se pudo leer el archivo {rutaArchivo}: {e.Message}");
            }
        }

        return encuestasJson;
    }

    #endregion

    #region Edición y Limpieza
    private void IniciarEdicionEncuesta(string id)
    {
        IdEncuestaEditando = id;

        btnGuardarEncuesta.gameObject.SetActive(false);
        btnActualizarEncuesta.gameObject.SetActive(true);

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
                        MostrarDatosEdicion(task.Result.ToDictionary());
                });
        }
        else
        {
            var encuestas = ObtenerListaDeEncuestas();
            foreach (var json in encuestas)
            {
                var encuesta = JsonUtility.FromJson<EncuestaData>(json);
                if (encuesta.id == id)
                {
                    MostrarDatosEdicion(encuesta.ToDictionary());
                    break;
                }
            }
        }
    }

    private void MostrarDatosEdicion(Dictionary<string, object> data)
    {
        inputTituloEncuesta.text = data["titulo"].ToString();
        inputDescripcion.text = data["descripcion"].ToString();

        var preguntas = (List<object>)data["preguntas"];
        foreach (var pObj in preguntas)
        {
            var pData = (Dictionary<string, object>)pObj;
            AgregarPregunta();
            var controller = listaPreguntas.Last();
            controller.inputPregunta.text = pData["textoPregunta"].ToString();

            foreach (var oObj in (List<object>)pData["opciones"])
            {
                var oData = (Dictionary<string, object>)oObj;
                controller.AgregarOpcionUI(oData["texto"].ToString(), (bool)oData["esCorrecta"]);
            }
        }

        btnActualizarEncuesta.onClick.AddListener(() => ProcesarEncuesta(true));

    }

    public void FinalizarEdicion()
    {
        IdEncuestaEditando = null;
        LimpiarCampos();
        PanelListar.SetActive(true);
        PanelEncuesta.SetActive(false);
    }

    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        inputDescripcion.text = "";

        // Eliminar todas las preguntas del contenedor
        foreach (Transform child in contenedorPreguntas)
        {
            Destroy(child.gameObject);
        }

        // Asegurar que listaPreguntas esté limpia
        listaPreguntas.Clear();

        // Refuerza que no hay encuesta activa
        IdEncuestaEditando = null;

        // Limpia los PlayerPrefs si aún estaban activos
        PlayerPrefs.DeleteKey("ModoEditar");
        PlayerPrefs.DeleteKey("IdEncuesta");
        PlayerPrefs.Save();
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

    public void PreguntaEliminada(PreguntaController preguntaEliminada)
    {
        listaPreguntas.Remove(preguntaEliminada);
        Debug.Log($"Pregunta eliminada. Total restantes: {listaPreguntas.Count}");
    }

    public static List<PreguntaData> ConvertirADatosDePreguntas(List<Dictionary<string, object>> preguntasRaw)
    {
        var lista = new List<PreguntaData>();
        foreach (var dic in preguntasRaw)
        {
            var pregunta = new PreguntaData
            {
                pregunta = dic.ContainsKey("pregunta") ? dic["pregunta"].ToString() : "",
                opciones = dic.ContainsKey("opciones") ? ((List<object>)dic["opciones"]).Select(o => o.ToString()).ToList() : new List<string>(),
                respuestaCorrecta = dic.ContainsKey("respuestaCorrecta") ? dic["respuestaCorrecta"].ToString() : ""
            };
            lista.Add(pregunta);
        }
        return lista;
    }

    #endregion

    #region Funciones Datos
    private List<Dictionary<string, object>> PrepararDatosPreguntas()
    {
        List<Dictionary<string, object>> preguntasData = new();

        foreach (var controller in listaPreguntas)
        {
            List<Dictionary<string, object>> opcionesData = new();
            var opciones = controller.ObtenerOpciones();

            foreach (var opcionTexto in opciones)
            {
                if (string.IsNullOrWhiteSpace(opcionTexto)) continue;

                bool esCorrecta = controller.ObtenerPregunta().opciones
                    .FirstOrDefault(o => o.textoOpcion == opcionTexto)?.esCorrecta ?? false;

                opcionesData.Add(new Dictionary<string, object>
                {
                    { "texto", opcionTexto },
                    { "esCorrecta", esCorrecta }
                });
            }

            preguntasData.Add(new Dictionary<string, object>
            {
                { "textoPregunta", controller.inputPregunta.text },
                { "opciones", opcionesData }
            });
        }

        return preguntasData;
    }

    [System.Serializable]
    public class PreguntaData
    {
        public string pregunta;
        public List<string> opciones;
        public string respuestaCorrecta;
    }

    [System.Serializable]

    public class EncuestaData
    {
        public string id;
        public string titulo;
        public string descripcion;
        public List<PreguntaData> preguntas;
        public bool publicada;

        public EncuestaData() { }

        // Constructor usando directamente List<PreguntaData>
        public EncuestaData(string id, string titulo, string descripcion, List<PreguntaData> preguntas, bool publicada)
        {
            this.id = id;
            this.titulo = titulo;
            this.descripcion = descripcion;
            this.preguntas = preguntas;
            this.publicada = publicada;
        }

        public Dictionary<string, object> ToDictionary()
        {
            var preguntasList = new List<Dictionary<string, object>>();
            foreach (var pregunta in preguntas)
            {
                preguntasList.Add(new Dictionary<string, object>
        {
            { "pregunta", pregunta.pregunta },
            { "opciones", pregunta.opciones },
            { "respuestaCorrecta", pregunta.respuestaCorrecta }
        });
            }

            return new Dictionary<string, object>
    {
        { "id", id },
        { "titulo", titulo },
        { "descripcion", descripcion },
        { "publicada", publicada },
        { "preguntas", preguntasList }
    };
        }

    }

    #endregion
}
