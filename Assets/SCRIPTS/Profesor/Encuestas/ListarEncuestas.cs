// EncuestaManager.cs (Refactorizado)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System;

public class ListarEncuestas : MonoBehaviour
{
    #region Referencias Inspector

    [Header("Visualización de Encuestas")]
    public Transform contenedorEncuestas;
    public GameObject tarjetaEncuestaPrefab;

    [Header("Detalles y Edición")]
    public GameObject panelDetallesEncuesta;
    public TMP_Text txtTituloEncuesta;
    public TMP_Text txtNumeroPreguntas;
    public Button btnEditarEncuesta;
    public Button btnEliminarEncuesta;

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
    public Button btnNuevaEncuesta;

    #endregion

    #region Variables Privadas
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;
    private string encuestaActualID;
    private ListenerRegistration encuestasListener;
    private HashSet<string> encuestasCargadas = new();
    private EncuestasManager encuestasManager;

    #endregion

    #region Unity Methods
    void Start()
    {
        InicializarFirebase();
        StartCoroutine(VerificarConexionPeriodicamente());

        // Obtenemos el componente EncuestasManager que está EN ESTE MISMO GameObject.
        encuestasManager = GetComponent<EncuestasManager>();

        // Es una buena práctica verificar si se encontró el componente.
        if (encuestasManager == null)
        {
            Debug.LogError("¡Error! No se encontró el componente EncuestasManager en el GameObject.");
        }

        btnNuevaEncuesta.onClick.AddListener(AbrirPanelEncuestaCrearEncuesta);
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
        btnConfirmarEliminar.onClick.AddListener(EliminarEncuestaConfirmada);
        btnCancelarEliminar.onClick.AddListener(OcultarPanelConfirmacionEliminar);

        PanelListar.SetActive(true);
        panelConfirmacionEliminar.SetActive(false);
    }

    void OnDestroy()
    {
        encuestasListener?.Stop();
    }
    #endregion

    #region Firebase Init y Sincronización
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

        encuestasListener = db.Collection("users").Document(userId).Collection("encuestas")
            .Listen(snapshot => UnityMainThreadDispatcher.Instance().Enqueue(CargarEncuestas));
    }

    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            // Espera 10 segundos antes de la siguiente verificación.
            yield return new WaitForSeconds(3);

            Debug.Log("Verificando conexión..."); // Agrega logs para depurar

            if (HayInternet())
            {
                Debug.Log("Hay internet. Sincronizando...");
         
                SincronizarEncuestasConFirebase();
                SincronizarEliminacionesPendientes();
            }
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

    private async void SincronizarEliminacionesPendientes()
    {
        string clave = $"EliminadasOffline_{userId}";
        List<string> eliminadas = ObtenerEliminadasOffline(clave);

        if (eliminadas.Count == 0) return;

        List<string> eliminadasConExito = new();

        foreach (string id in eliminadas)
        {
            try
            {
                var docRef = db.Collection("users").Document(userId).Collection("encuestas").Document(id);
                var snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    await docRef.DeleteAsync();
                    Debug.Log($"☁️ Encuesta eliminada de Firebase tras estar offline: {id}");
                }

                eliminadasConExito.Add(id);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ No se pudo eliminar {id} de Firebase: {e.Message}");
            }
        }

        eliminadas = eliminadas.Except(eliminadasConExito).ToList();
        string nuevoJson = JsonUtility.ToJson(new ListaSimple(eliminadas));
        PlayerPrefs.SetString(clave, nuevoJson);
        PlayerPrefs.Save();
    }

    private List<string> ObtenerEliminadasOffline(string clave)
    {
        string json = PlayerPrefs.GetString(clave, "");

        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
        {
            // Si no es un objeto JSON, devolver lista vacía
            return new List<string>();
        }

        try
        {
            ListaSimple datos = JsonUtility.FromJson<ListaSimple>(json);
            return datos?.ids ?? new List<string>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Error deserializando '{clave}': {e.Message}");
            return new List<string>();
        }
    }

    #endregion

    #region UI y Encuestas
    public void CargarEncuestas()
    {
        foreach (Transform child in contenedorEncuestas)
            Destroy(child.gameObject);

        encuestasCargadas.Clear();

        if (HayInternet())
            CargarDesdeFirebase();
        else
            CargarDesdeLocal();
    }

    private void CargarDesdeFirebase()
    {
        db.Collection("users").Document(userId).Collection("encuestas").GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error al cargar encuestas: " + task.Exception);
                    CargarDesdeLocal();
                    return;
                }

                foreach (var doc in task.Result.Documents)
                {
                    if (!doc.Exists) continue;
                    string id = doc.Id;
                    if (encuestasCargadas.Contains(id)) continue;

                    string titulo = doc.GetValue<string>("titulo");
                    bool activo = doc.GetValue<bool>("activo");
                    var preguntas = doc.ContainsField("preguntas") ? doc.GetValue<List<object>>("preguntas").Count : 0;

                    CrearTarjetaEncuesta(titulo, preguntas, id);
                    encuestasCargadas.Add(id);
                }
            });
    }

    private void CargarDesdeLocal()
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");

        if (!Directory.Exists(carpetaEncuestas))
        {
            Debug.Log("📁 Carpeta de encuestas local no existe.");
            return;
        }

        string[] archivos = Directory.GetFiles(carpetaEncuestas, "*.json");

        foreach (string rutaArchivo in archivos)
        {
            try
            {
                string jsonEncuesta = File.ReadAllText(rutaArchivo);
                EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(jsonEncuesta);

                CrearTarjetaEncuesta(
                    encuesta.titulo,
                    encuesta.preguntas.Count,
                    encuesta.id
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al cargar encuesta desde archivo '{rutaArchivo}': {e.Message}");
            }
        }
    }

    private void CrearTarjetaEncuesta(string titulo, int numPreguntas, string id)
    {
        GameObject tarjeta = Instantiate(tarjetaEncuestaPrefab, contenedorEncuestas);
        TMP_Text[] textos = tarjeta.GetComponentsInChildren<TMP_Text>();

        if (textos.Length >= 3)
        {
            textos[0].text = titulo;
            textos[1].text = numPreguntas.ToString();
        }

        var boton = tarjeta.GetComponentInChildren<Button>();
        if (boton != null)
            boton.onClick.AddListener(() => MostrarDetallesEncuesta(titulo, numPreguntas, id));
    }
    #endregion

    #region Otros Métodos UI
    private void MostrarDetallesEncuesta(string titulo, int numPreguntas, string id)
    {
        encuestaActualID = id;
        txtTituloEncuesta.text = titulo;
        txtNumeroPreguntas.text = numPreguntas.ToString();

        panelDetallesEncuesta.SetActive(true);

        btnEditarEncuesta.onClick.RemoveAllListeners();
        btnEliminarEncuesta.onClick.RemoveAllListeners();

        btnEditarEncuesta.onClick.AddListener(() => AbrirPanelEncuesta(id));
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
    }

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

    private void EliminarEncuestaConfirmada()
    {
        if (string.IsNullOrEmpty(encuestaActualID)) return;

        // Intentar eliminar desde Firebase si hay Internet
        if (HayInternet())
        {
            db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaActualID).DeleteAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        Debug.Log($"☁️ Encuesta eliminada de Firebase: {encuestaActualID}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No se pudo eliminar de Firebase: {task.Exception?.Message}");
                    }

                    // Luego de Firebase, eliminar también localmente
                    EliminarEncuestaLocal(encuestaActualID);
                    CargarEncuestas();
                });
        }
        else
        {
            // Sin internet, elimina directamente del sistema de archivos
            EliminarEncuestaLocal(encuestaActualID);
            CargarEncuestas();
        }

        OcultarPanelConfirmacionEliminar();
        panelDetallesEncuesta.SetActive(false);
    }

    private void EliminarEncuestaLocal(string encuestaID)
    {
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        string rutaArchivo = Path.Combine(carpetaEncuestas, $"{encuestaID}.json");

        if (File.Exists(rutaArchivo))
        {
            try
            {
                File.Delete(rutaArchivo);
                Debug.Log($"🗑️ Encuesta local eliminada: {rutaArchivo}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al eliminar encuesta local: {e.Message}");
            }
        }

        // Registrar para eliminación posterior si estamos offline
        if (!HayInternet())
        {
            RegistrarEliminacionPendiente(encuestaID);
        }
    }

    private void RegistrarEliminacionPendiente(string encuestaID)
    {
        string clave = $"EliminadasOffline_{userId}";
        string json = PlayerPrefs.GetString(clave, "");

        List<string> eliminadas = new();

        // Intentar deserializar si parece válido
        if (!string.IsNullOrEmpty(json) && json.TrimStart().StartsWith("{"))
        {
            try
            {
                eliminadas = JsonUtility.FromJson<ListaSimple>(json)?.ids ?? new List<string>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ Error al cargar eliminaciones offline: {e.Message}");
            }
        }

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
        PanelListar.SetActive(false);
        PanelEncuesta.SetActive(true);

        PlayerPrefs.SetString("ModoEditar", "Desactivado");
        PlayerPrefs.Save();
        panelDetallesEncuesta.SetActive(false);
        encuestasManager.InicializarEncuesta();
    }

    public async void SincronizarEncuestasConFirebase()
    {

        if (!HayInternet() || string.IsNullOrEmpty(userId)) return;

        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas))
            Directory.CreateDirectory(carpetaEncuestas);

        Debug.Log(carpetaEncuestas);

        // SUBIR encuestas locales
        string[] archivosLocales = Directory.GetFiles(carpetaEncuestas, "*.json");
        foreach (string rutaArchivo in archivosLocales)
        {
            try
            {
                string contenidoJson = File.ReadAllText(rutaArchivo);
                EncuestaData encuesta = JsonUtility.FromJson<EncuestaData>(contenidoJson);

                DocumentReference docRef = FirebaseFirestore.DefaultInstance
                    .Collection("users").Document(userId)
                    .Collection("encuestas").Document(encuesta.id);

                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    // Convertir preguntas a diccionario para subir
                    List<Dictionary<string, object>> preguntasDic = new List<Dictionary<string, object>>();
                    foreach (var p in encuesta.preguntas)
                    {
                        preguntasDic.Add(new Dictionary<string, object>
                {
                    { "pregunta", p.pregunta },
                    { "opciones", p.opciones },
                    { "respuestaCorrecta", p.respuestaCorrecta }
                });
                    }

                    Dictionary<string, object> datos = new Dictionary<string, object>
            {
                { "titulo", encuesta.titulo },
                { "descripcion", encuesta.descripcion },
                { "publicada", encuesta.publicada },
                { "preguntas", preguntasDic }
            };

                    await docRef.SetAsync(datos);
                    Debug.Log($"☁️ Encuesta subida: {encuesta.id}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error subiendo {Path.GetFileName(rutaArchivo)}: {e.Message}");
            }
        }

        CollectionReference encuestasRef = FirebaseFirestore.DefaultInstance
            .Collection("users").Document(userId)
            .Collection("encuestas");

        QuerySnapshot encuestasSnapshot = await encuestasRef.GetSnapshotAsync();

        foreach (DocumentSnapshot doc in encuestasSnapshot.Documents)
        {
            try
            {
                string encuestaId = doc.Id;
                string rutaLocal = Path.Combine(carpetaEncuestas, encuestaId + ".json");

                if (!File.Exists(rutaLocal))
                {
                    var data = doc.ToDictionary();

                    // Extraer datos principales de la encuesta
                    string titulo = data.ContainsKey("titulo") ? data["titulo"].ToString() : "";
                    string descripcion = data.ContainsKey("descripcion") ? data["descripcion"].ToString() : "";
                    // Asumo que 'publicada' puede no existir en Firebase, así que la manejo con seguridad
                    bool publicada = data.ContainsKey("publicada") ? Convert.ToBoolean(data["publicada"]) : false;

                    List<PreguntaData> preguntas = new List<PreguntaData>();

                    // Comprobar si existe la clave "preguntas" y si es una lista
                    if (data.TryGetValue("preguntas", out object preguntasObj) && preguntasObj is List<object> listaPreguntasFirebase)
                    {
                        // Iterar sobre cada pregunta (que es un diccionario/mapa)
                        foreach (var preguntaItem in listaPreguntasFirebase)
                        {
                            if (preguntaItem is Dictionary<string, object> preguntaDict)
                            {
                                // OBTENER EL TEXTO DE LA PREGUNTA
                                // En Firebase se llama 'textoPregunta', en tu clase es 'pregunta'
                                string textoDeLaPregunta = preguntaDict.ContainsKey("textoPregunta") ? preguntaDict["textoPregunta"].ToString() : "";

                                List<string> opcionesParaJson = new List<string>();
                                string respuestaCorrectaParaJson = "";

                                // PROCESAR LA LISTA DE OPCIONES
                                // En Firebase es una lista de mapas, hay que convertirla a una lista de strings
                                if (preguntaDict.TryGetValue("opciones", out object opcionesObj) && opcionesObj is List<object> listaOpcionesFirebase)
                                {
                                    foreach (var opcionItem in listaOpcionesFirebase)
                                    {
                                        if (opcionItem is Dictionary<string, object> opcionDict)
                                        {
                                            // Extraer el texto de la opción
                                            if (opcionDict.TryGetValue("texto", out object textoOpcionObj))
                                            {
                                                string textoOpcion = textoOpcionObj.ToString();
                                                opcionesParaJson.Add(textoOpcion); // Añadir el texto a la lista

                                                // Comprobar si esta es la respuesta correcta
                                                if (opcionDict.TryGetValue("esCorrecta", out object esCorrectaObj) && Convert.ToBoolean(esCorrectaObj))
                                                {
                                                    respuestaCorrectaParaJson = textoOpcion; // Guardar el texto de la respuesta correcta
                                                }
                                            }
                                        }
                                    }
                                }

                                // Crear el objeto PreguntaData con la estructura que tu clase espera
                                PreguntaData p = new PreguntaData
                                {
                                    pregunta = textoDeLaPregunta,
                                    opciones = opcionesParaJson,
                                    respuestaCorrecta = respuestaCorrectaParaJson
                                };

                                preguntas.Add(p);
                            }
                        }
                    }

                    // Crear el objeto EncuestaData final
                    EncuestaData encuesta = new EncuestaData(encuestaId, titulo, descripcion, preguntas, publicada);

                    // Serializar a JSON y guardar en el archivo local
                    string json = JsonUtility.ToJson(encuesta, true);
                    File.WriteAllText(rutaLocal, json);
                    Debug.Log($"📥 Encuesta descargada y adaptada: {encuestaId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error descargando y adaptando {doc.Id}: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    [System.Serializable]
    public class ListaSimple
    {
        public List<string> ids;

        public ListaSimple(List<string> lista)
        {
            ids = lista;
        }
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
