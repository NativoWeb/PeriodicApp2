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
    public UnityEngine.UI.Button btnActivarEncuesta;
    public UnityEngine.UI.Button btnDesactivarEncuesta;
    public UnityEngine.UI.Button btnCancelar;
    public GameObject PanelGris;
    public vistaController vistaController;
    private bool isDragging = false;
    private Vector2 pointerStartPosition;
    private string encuestaActualID;
    private FirebaseFirestore db;
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        StartCoroutine(VerificarConexionPeriodicamente());

        // Escuchar cambios en la colección "encuestas"
        db.Collection("encuestas").Listen(snapshot =>
        {
            CargarEncuestas(); // Llamar a la función cuando haya cambios
        });
    }


    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return new WaitForSeconds(10); // Verifica cada 10 segundos
            if (HayInternet())
            {
                SincronizarEncuestasConFirebase();
            }
        }
    }
    public void AgregarPregunta()
    {
        GameObject nuevaPregunta = Instantiate(preguntaPrefab, contenedorPreguntas);
        PreguntaController controlador = nuevaPregunta.GetComponent<PreguntaController>();
        listaPreguntas.Add(controlador);
    }
    public void GuardarEncuesta()
    {
        string usuarioActual = PlayerPrefs.GetString("userId", ""); // Obtener usuario actual
        if (string.IsNullOrEmpty(usuarioActual))
        {
            Debug.LogError("⚠ No hay un usuario autenticado.");
            return;
        }

        string encuestaID = System.Guid.NewGuid().ToString();
        string titulo = inputTituloEncuesta.text;
        string codigoAcceso = CodeGenerator.GenerateCode();
        List<Dictionary<string, object>> preguntasData = new List<Dictionary<string, object>>();

        foreach (PreguntaController preguntaController in listaPreguntas)
        {
            List<Dictionary<string, object>> opcionesData = new List<Dictionary<string, object>>();
            foreach (var opcionTexto in preguntaController.ObtenerOpciones())
            {
                bool esCorrecta = false;
                foreach (var opcion in preguntaController.ObtenerPregunta().opciones)
                {
                    if (opcion.textoOpcion == opcionTexto)
                    {
                        esCorrecta = opcion.esCorrecta;
                        break;
                    }
                }
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

        Dictionary<string, object> encuesta = new Dictionary<string, object>()
    {
        { "id", encuestaID },
        { "titulo", titulo },
        { "codigoAcceso", codigoAcceso },
        { "preguntas", preguntasData },
        { "activo", false }
    };

        // Convertir a JSON
        string jsonEncuesta = JsonUtility.ToJson(new EncuestaData(encuestaID, titulo, codigoAcceso, preguntasData, false));

        // Guardar en PlayerPrefs con una clave única basada en el usuario
        string claveUsuario = $"Encuestas_{usuarioActual}";
        List<string> encuestasUsuario = ObtenerListaDeEncuestas(usuarioActual);
        encuestasUsuario.Add(jsonEncuesta);

        // Guardar la nueva lista de encuestas en PlayerPrefs
        PlayerPrefs.SetString(claveUsuario, JsonUtility.ToJson(new ListaEncuestas(encuestasUsuario)));
        PlayerPrefs.Save();

        Debug.Log($"📂 Encuesta guardada para el usuario {usuarioActual}: {titulo}");

        LimpiarCampos();
        CargarEncuestasOffline(); // Mostrar las encuestas almacenadas localmente
    }
    private List<string> ObtenerListaDeEncuestas(string usuario)
    {
        string claveUsuario = $"Encuestas_{usuario}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (string.IsNullOrEmpty(json))
            return new List<string>();

        return JsonUtility.FromJson<ListaEncuestas>(json).encuestas;
    }


    public void CargarEncuestasOffline()
    {
        string usuarioActual = PlayerPrefs.GetString("UsuarioActual", "");
        if (string.IsNullOrEmpty(usuarioActual))
        {
            Debug.LogError("⚠ No hay un usuario autenticado.");
            return;
        }

        string claveUsuario = $"Encuestas_{usuarioActual}";
        string json = PlayerPrefs.GetString(claveUsuario, "");

        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("📭 No hay encuestas para este usuario.");
            return;
        }

        ListaEncuestas listaEncuestas = JsonUtility.FromJson<ListaEncuestas>(json);
        int index = 0;
        foreach (string jsonEncuesta in listaEncuestas.encuestas)
        {
            EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(jsonEncuesta);
            MostrarEncuestaEnInterfaz(encuesta); // Método que dibuja la encuesta en la UI
            index++;
        }
    }

    private void MostrarEncuestaEnInterfaz(EncuestaData encuesta)
    {
        int numeroPreguntas = encuesta.preguntas.Count;
        CrearTarjetaEncuesta(encuesta.titulo, encuesta.codigoAcceso, numeroPreguntas, 0, encuesta.id, encuesta.activo);
    }



    // Método para obtener todas las claves de PlayerPrefs (IDs de encuestas guardadas)
    private List<string> PlayerPrefsKeys()
    {
        List<string> keys = new List<string>();
        foreach (string key in PlayerPrefs.GetString("EncuestasGuardadas", "").Split(','))
        {
            if (!string.IsNullOrEmpty(key))
            {
                keys.Add(key);
            }
        }
        return keys;
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
        if (!HayInternet())
        {
            Debug.Log("🚫 No hay conexión a Internet. No se puede sincronizar.");
            return;
        }

        foreach (var key in PlayerPrefsKeys())
        {
            string json = PlayerPrefs.GetString(key);
            EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(json);

            if (encuesta != null)
            {
                Dictionary<string, object> encuestaData = new Dictionary<string, object>
            {
                { "titulo", encuesta.titulo },
                { "codigoAcceso", encuesta.codigoAcceso },
                { "preguntas", encuesta.preguntas },
                { "activo", encuesta.activo }
            };

                db.Collection("encuestas").Document(encuesta.id).SetAsync(encuestaData).ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        Debug.Log($"✅ Encuesta {encuesta.id} subida a Firebase.");
                        PlayerPrefs.DeleteKey(encuesta.id);
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        Debug.LogError("❌ Error al subir la encuesta: " + task.Exception);
                    }
                });
            }
        }
    }



    public void CargarEncuestas()
    {
        foreach (Transform child in contenedorEncuestas)
        {
            Destroy(child.gameObject);
        }
        db.Collection("encuestas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot encuestas = task.Result;
                Debug.Log($"🔍 Se encontraron {encuestas.Count} encuestas en la BD."); // Verifica la cantidad de encuestas
                int index = 0;
                foreach (DocumentSnapshot doc in encuestas.Documents)
                {
                    if (doc.Exists)
                    {
                        string titulo = doc.GetValue<string>("titulo");
                        string codigoAcceso = doc.ContainsField("codigoAcceso") ? doc.GetValue<string>("codigoAcceso") : "";
                        int numeroPreguntas = 0;
                        if (doc.ContainsField("preguntas"))
                        {
                            object objPreguntas = doc.GetValue<object>("preguntas");
                            if (objPreguntas is List<object> lista)
                            {
                                numeroPreguntas = lista.Count;
                            }
                        }
                        bool activo = doc.ContainsField("activo") ? doc.GetValue<bool>("activo") : false;
                        Debug.Log($"📌 Llamando a CrearTarjetaEncuesta: {titulo} - Activo: {activo}");
                        CrearTarjetaEncuesta(titulo, codigoAcceso, numeroPreguntas, index, doc.Id, activo);
                        index++;
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Documento sin datos válidos.");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Error al cargar encuestas: " + task.Exception);
            }
        });
    }
    void CrearTarjetaEncuesta(string titulo, string codigoAcceso, int numeroPreguntas, int index, string encuestaID, bool activo)
    {
        Debug.Log($"🛠️ Intentando instanciar tarjeta: {titulo}"); // Verifica que se ejecuta esta línea
        // Instanciar la tarjeta y asignarla al contenedor
        GameObject nuevaTarjeta = Instantiate(tarjetaEncuestaPrefab, contenedorEncuestas);
        if (nuevaTarjeta == null)
        {
            Debug.LogError("❌ Error: No se pudo instanciar tarjetaEncuestaPrefab.");
            return;
        }
        TMP_Text[] textosTMP = nuevaTarjeta.GetComponentsInChildren<TMP_Text>();


        if (textosTMP.Length >= 3)
        {
            textosTMP[0].text = titulo;
            textosTMP[1].text = titulo;
            textosTMP[2].text = "" + numeroPreguntas;
            textosTMP[3].text = codigoAcceso;
        }
        else
        {
            Debug.LogError($"❌ Error: No se encontraron suficientes TMP_Text en {titulo}");
        }

        // Agregar Componente Image si no existe
        Image fondoTarjeta = nuevaTarjeta.GetComponent<Image>();
        if (fondoTarjeta == null)
        {
            fondoTarjeta = nuevaTarjeta.AddComponent<Image>(); // Añadir Image al GameObject
        }

        // Asignar color inicial según el estado "activo"
        fondoTarjeta.color = activo ? new Color(233f / 255f, 246f / 255f, 239f / 255f, 1f) : new Color(254f / 255f, 245f / 255f, 228f / 255f, 1f);



        // Buscar el botón dentro de la tarjeta y agregar el evento
        Button botonVerEncuesta = nuevaTarjeta.GetComponentInChildren<Button>();
        botonVerEncuesta.onClick.AddListener(() => MostrarDetallesEncuesta(titulo, codigoAcceso, encuestaID, activo));
        Debug.Log($"✅ Tarjeta creada: {titulo} - Activo: {activo}");
    }
    public void MostrarDetallesEncuesta(string titulo, string codigo, string encuestaID, bool activo)
    {
        encuestaActualID = encuestaID;
        txtTituloEncuesta.text = "Título: " + titulo;
        txtCodigoEncuesta.text = "Código: " + codigo;
        btnActivarEncuesta.interactable = !activo; // Desactivar si ya está activa
        btnActivarEncuesta.onClick.RemoveAllListeners();
        btnActivarEncuesta.onClick.AddListener(() => ActivarEncuesta(encuestaActualID));
        btnDesactivarEncuesta.onClick.AddListener(() => DesactivarEncuesta(encuestaActualID));
        EventTrigger trigger = panelDetallesEncuesta.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { vistaController.Inicio(); });
        trigger.triggers.Add(entry);
        panelDetallesEncuesta.SetActive(true);
    }
    void ActivarEncuesta(string encuestaID)
    {
        Dictionary<string, object> updateData = new Dictionary<string, object>
    {
        { "activo", true }
    };
        db.Collection("encuestas").Document(encuestaID).UpdateAsync(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ Encuesta activada correctamente.");

                // Buscar la tarjeta en la UI y actualizar su color
                foreach (Transform child in contenedorEncuestas)
                {
                    TMP_Text[] textosTMP = child.GetComponentsInChildren<TMP_Text>();
                    if (textosTMP.Length >= 3 && textosTMP[2].text == txtCodigoEncuesta.text.Replace("Código: ", ""))
                    {
                        Image fondoTarjeta = child.GetComponent<Image>();
                        if (fondoTarjeta != null)
                        {
                            fondoTarjeta.color = new Color(0.7f, 1f, 0.7f, 1f); // Color de activa
                        }
                        break;
                    }
                }

                panelDetallesEncuesta.SetActive(false);
            }
            else
            {
                Debug.LogError("❌ Error al activar la encuesta: " + task.Exception);
            }
        });
    }

    void DesactivarEncuesta(string encuestaID)
    {
        Dictionary<string, object> updateData = new Dictionary<string, object>
    {
        { "activo", false }
    };
        db.Collection("encuestas").Document(encuestaID).UpdateAsync(updateData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("✅ Encuesta desactivada correctamente.");

                // Buscar la tarjeta en la UI y actualizar su color
                foreach (Transform child in contenedorEncuestas)
                {
                    TMP_Text[] textosTMP = child.GetComponentsInChildren<TMP_Text>();
                    if (textosTMP.Length >= 3 && textosTMP[2].text == txtCodigoEncuesta.text.Replace("Código: ", ""))
                    {
                        Image fondoTarjeta = child.GetComponent<Image>();
                        if (fondoTarjeta != null)
                        {
                            fondoTarjeta.color = new Color(1f, 0.7f, 0.7f, 1f); // Color de inactiva
                        }
                        break;
                    }
                }

                panelDetallesEncuesta.SetActive(false);
            }
            else
            {
                Debug.LogError("❌ Error al desactivar la encuesta: " + task.Exception);
            }
        });
    }



    public void LimpiarCampos()
    {
        inputTituloEncuesta.text = "";
        foreach (Transform child in contenedorPreguntas)
        {
            Destroy(child.gameObject);
        }
        listaPreguntas.Clear();
    }
}
