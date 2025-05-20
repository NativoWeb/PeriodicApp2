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
    private List<PreguntaController> listaPreguntas = new List<PreguntaController>();

    [Header("Referencias para Mostrar Encuestas")]
    public Transform contenedorEncuestas;
    public GameObject tarjetaEncuestaPrefab;

    [Header("Referencias de Detalles")]
    public GameObject panelDetallesEncuesta;
    public TMP_Text txtTituloEncuesta;
    public TMP_Text txtCodigoEncuesta;
    public Button btnActivarEncuesta;
    public Button btnDesactivarEncuesta;
    public Button btnCancelar;
    public GameObject PanelGris;
    public vistaController vistaController;

    [Header("Referencias para Mensajes")]
    public TMP_Text messageText;
    public float messageDuration = 3f;

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
            Debug.LogError("la Descripción no puede estar vacía");
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
                return;
            }

            bool tieneOpcionCorrecta = pregunta.ObtenerPregunta().opciones.Any(o => o.esCorrecta);
            if (!tieneOpcionCorrecta)
            {
                ShowMessage($"La pregunta '{pregunta.inputPregunta.text}' no tiene opciones correctas", Color.red);
                return;
            }
        }

        string encuestaID = System.Guid.NewGuid().ToString();
        string titulo = inputTituloEncuesta.text;
        string descripcion = inputDescripcion.text;
        string codigoAcceso = GenerarCodigoAcceso();
        List<Dictionary<string, object>> preguntasData = PrepararDatosPreguntas();

        if (HayInternet())
        {
            GuardarEnFirebase(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
        }
        else
        {
            GuardarLocalmente(encuestaID, titulo, descripcion, codigoAcceso, preguntasData);
        }

        LimpiarCampos();
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
            botonVerEncuesta.onClick.AddListener(() => MostrarDetallesEncuesta(titulo, codigoAcceso, encuestaID, activo));
        }
    }

    public void MostrarDetallesEncuesta(string titulo, string codigo, string encuestaID, bool activo)
    {
        encuestaActualID = encuestaID;
        txtTituloEncuesta.text = "Título: " + titulo;
        txtCodigoEncuesta.text = "Código: " + codigo;

        btnActivarEncuesta.onClick.RemoveAllListeners();
        btnDesactivarEncuesta.onClick.RemoveAllListeners();

        btnActivarEncuesta.interactable = !activo;
        btnActivarEncuesta.onClick.AddListener(() => CambiarEstadoEncuesta(encuestaID, true));

        btnDesactivarEncuesta.interactable = activo;
        btnDesactivarEncuesta.onClick.AddListener(() => CambiarEstadoEncuesta(encuestaID, false));

        panelDetallesEncuesta.SetActive(true);
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

    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        inputDescripcion.text = "";
        foreach (Transform child in contenedorPreguntas)
        {
            Destroy(child.gameObject);
        }
        listaPreguntas.Clear();
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