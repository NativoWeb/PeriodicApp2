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
    public GameObject PanelTipoEncuesta;
    public GameObject PanelElementoMision;
    public Button btnNuevaEncuesta;
    public Button btnEncuestaRecreativa;
    public Button btnEncuestaMision;
    public Button btnSalirTipoEncuesta;
    public Button btnSalirElemento;

    [Header("Panel Elemento Mision")]
    public Button btnContinuarMision;



    public ControladorSeleccionMision controladorSeleccion; // Arrastra el GameObject "ControladorSeleccion" aquí en el Inspector


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
        encuestasManager = GetComponent<EncuestasManager>();
        InicializarFirebase();

        txtNombre.text = auth.CurrentUser.DisplayName;

        //StartCoroutine(VerificarConexionPeriodicamente());

        // --- FLUJO DE INICIO MEJORADO ---
        CargarYsincronizarDatos();
        ConfigurarBotones();
    }
    void OnDestroy()
    {
        encuestasListener?.Stop();
    }


    private async Task CargarYsincronizarDatos()
    {
        // Primero cargamos lo que tengamos localmente para que el usuario vea algo rápido
        CargarDesdeLocal();

        if (HayInternet())
        {
            // Luego, en segundo plano, sincronizamos todo
            await SincronizarDatosCompletos();
            // Y refrescamos la UI con los datos actualizados
            CargarDesdeLocal();
        }
    }

    private void ConfigurarBotones()
    {
        btnNuevaEncuesta.onClick.AddListener(AbrirPanelEncuestaCrearEncuesta);
        btnEliminarEncuesta.onClick.AddListener(MostrarPanelConfirmacionEliminar);
        btnConfirmarEliminar.onClick.AddListener(EliminarEncuestaConfirmada);
        btnCancelarEliminar.onClick.AddListener(OcultarPanelConfirmacionEliminar);
        // ... otros botones ...
    }
    // --- SINCRONIZACIÓN PRINCIPAL ---
    private async Task SincronizarDatosCompletos()
    {
        await SincronizarEncuestasConFirebase();
        await SincronizarEliminacionesPendientes();
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


            if (HayInternet())
            {
                //SincronizarEncuestasConFirebase();
                //SincronizarEliminacionesPendientes();
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
                    bool activo = doc.GetValue<bool>("publicada");
                    var preguntas = doc.ContainsField("preguntas") ? doc.GetValue<List<object>>("preguntas").Count : 0;

                    CrearTarjetaEncuesta(titulo, preguntas, id);
                    encuestasCargadas.Add(id);
                }
            });
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
        PlayerPrefs.SetString("ModoEditar", "Desactivado");
        PlayerPrefs.Save();

        PanelTipoEncuesta.SetActive(true);
        btnSalirTipoEncuesta.onClick.AddListener(() => { PanelTipoEncuesta.SetActive(false); });
        btnSalirElemento.onClick.AddListener(() => { PanelElementoMision.SetActive(false); });

        // --- MODIFICACIÓN AQUÍ ---
        btnEncuestaMision.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("TipoEncuesta", "Mision");

            PanelElementoMision.SetActive(true);
            PanelTipoEncuesta.SetActive(false);

            // Llamamos al método de nuestro controlador para que prepare el panel
            controladorSeleccion.IniciarPanel();

            // Limpiamos el listener del botón continuar para evitar que se acumulen
            btnContinuarMision.onClick.RemoveAllListeners();
            btnContinuarMision.onClick.AddListener(() => {

                // Usamos nuestro controlador para obtener los datos
                var seleccion = controladorSeleccion.ObtenerSeleccion();
                if (seleccion.categoria != null && seleccion.elemento != null)
                {
                    // Guardar la selección
                    Debug.Log($"Categoría: {seleccion.categoria}, Elemento: {seleccion.elemento}");
                    PlayerPrefs.SetString("CategoriaMision", seleccion.categoria);
                    PlayerPrefs.SetString("ElementoMision", seleccion.elemento);
                    PlayerPrefs.Save();

                    // Continuar al siguiente panel
                    PanelElementoMision.SetActive(false);
                    PanelEncuesta.SetActive(true);
                }
            });

        });

        btnEncuestaRecreativa.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("TipoEncuesta", "recreativa");
            // ... Lógica para la encuesta recreativa
        });

        panelDetallesEncuesta.SetActive(false);
        encuestasManager.InicializarEncuesta(); // Probablemente quieras llamar esto después de seleccionar el elemento
    }

    // --- MÉTODO DE SINCRONIZACIÓN TOTALMENTE CORREGIDO Y UNIFICADO ---
    public async Task SincronizarEncuestasConFirebase()
    {
        if (!HayInternet() || string.IsNullOrEmpty(userId)) return;

        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas)) Directory.CreateDirectory(carpetaEncuestas);

        // PARTE 1: SUBIR ENCUESTAS LOCALES QUE NO ESTÁN EN FIREBASE
        string[] archivosLocales = Directory.GetFiles(carpetaEncuestas, "*.json");
        foreach (string rutaArchivo in archivosLocales)
        {
            try
            {
                // --- AÑADIMOS VALIDACIÓN DE NOMBRE DE ARCHIVO ---
                string nombreArchivo = Path.GetFileNameWithoutExtension(rutaArchivo);
                if (string.IsNullOrEmpty(nombreArchivo))
                {
                    Debug.LogWarning($"Se encontró un archivo JSON sin nombre en '{rutaArchivo}'. Se ignorará y se eliminará.");
                    File.Delete(rutaArchivo); // Eliminamos el archivo corrupto
                    continue; // Saltamos al siguiente archivo
                }

                string contenidoJson = File.ReadAllText(rutaArchivo);
                if (string.IsNullOrWhiteSpace(contenidoJson))
                {
                    Debug.LogWarning($"El archivo JSON en '{rutaArchivo}' está vacío. Se ignorará.");
                    continue;
                }

                EncuestaModelo encuestaLocal = JsonUtility.FromJson<EncuestaModelo>(contenidoJson);

                // --- AÑADIMOS VALIDACIÓN DEL ID DENTRO DEL JSON ---
                if (encuestaLocal == null || string.IsNullOrEmpty(encuestaLocal.Id))
                {
                    Debug.LogWarning($"El JSON en '{rutaArchivo}' no contiene un ID válido o no se pudo deserializar. Se ignorará.");
                    continue;
                }

                DocumentReference docRef = db.Collection("users").Document(userId).Collection("encuestas").Document(encuestaLocal.Id);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.Log($"⬆️ Subiendo encuesta local '{encuestaLocal.Titulo}' a Firebase.");
                    await docRef.SetAsync(encuestaLocal);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error procesando el archivo local {Path.GetFileName(rutaArchivo)}: {e.Message}");
            }
        }

        // PARTE 2 (sin cambios, ya es bastante robusta)
        try
        {
            QuerySnapshot qSnap = await db.Collection("users").Document(userId).Collection("encuestas").GetSnapshotAsync();
            foreach (DocumentSnapshot doc in qSnap.Documents)
            {
                string rutaLocal = Path.Combine(carpetaEncuestas, doc.Id + ".json");
                if (!File.Exists(rutaLocal))
                {
                    Debug.Log($"📥 Descargando encuesta remota '{doc.GetValue<string>("titulo")}' a local.");
                    try
                    {
                        EncuestaModelo encuestaRemota = doc.ConvertTo<EncuestaModelo>();
                        string jsonParaGuardar = JsonUtility.ToJson(encuestaRemota, true);
                        File.WriteAllText(rutaLocal, jsonParaGuardar);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error al convertir la encuesta {doc.Id}. ¿Están los atributos [FirestoreData] y [FirestoreProperty] en el modelo? Error: {e.Message}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error en la fase de descarga de Firebase: {e.Message}");
        }
    }

    // El CargarDesdeLocal ahora solo se usa para la carga INICIAL.
    private void CargarDesdeLocal()
    {
        foreach (Transform child in contenedorEncuestas) Destroy(child.gameObject);
        string carpetaEncuestas = Path.Combine(Application.persistentDataPath, "Encuestas");
        if (!Directory.Exists(carpetaEncuestas)) return;

        string[] archivos = Directory.GetFiles(carpetaEncuestas, "*.json");
        foreach (string rutaArchivo in archivos)
        {
            try
            {
                string jsonEncuesta = File.ReadAllText(rutaArchivo);
                // --- Usamos el modelo unificado ---
                EncuestaModelo encuesta = JsonUtility.FromJson<EncuestaModelo>(jsonEncuesta);
                CrearTarjetaEncuesta(encuesta.Titulo, encuesta.Preguntas.Count, encuesta.Id);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al cargar encuesta '{Path.GetFileName(rutaArchivo)}': {e.Message}");
            }
        }
    }
    #endregion
}
