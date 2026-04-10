using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using Vuforia;
using QuantumAI.Core;


public class GuardarMisionCompletada : MonoBehaviour
{
    public static GuardarMisionCompletada instancia;

    [Header("Paneles de UI")]
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonUI;
    public GameObject PanelMisionCompletada;

    [Header("Botones")]
    public Button botonCompletarMision;
    public Button Regresar;

    [Header("Textos de Resultados")]
    public TMP_Text TxtMisionResultado;
    public TMP_Text TxtXpResultado;
    public TMP_Text TxtPuntuacionResultado;
    public TMP_Text TxtMotivacionResultado;
    public TMP_Text TxtRefuerzo1;
    public TMP_Text TxtRefuerzo2;

    [Header("Referencias para Animación")]
    public GameObject PanelContinuar;
    public Button btnContinuarPanel;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    private string appIdioma;

    void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destruir la instancia vieja (que tiene referencias a paneles de escenas anteriores)
            // y usar esta nueva instancia que tiene referencias válidas de la escena actual
            Destroy(instancia.gameObject);
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        var user = auth.CurrentUser;
        if (user != null)
        {
            userId = user.UserId;
        }

        // Obtener idioma
        appIdioma = PlayerPrefs.GetString("appIdioma", "español");

        // Ocultar panel de misión completada al inicio
        if (PanelMisionCompletada != null)
        {
            PanelMisionCompletada.SetActive(false);
        }

        // Asignar el listener al botón de completar misión
        if (botonCompletarMision != null)
        {
            botonCompletarMision.onClick.AddListener(CompletarMision);
        }

        // Configurar paneles según la ruta
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        // Configurar panel de regresar visible y botón de completar oculto/deshabilitado
        if (PanelRegresarUI != null) PanelRegresarUI.SetActive(true);
        if (PanelBotonUI != null) PanelBotonUI.SetActive(false);
        if (botonCompletarMision != null) botonCompletarMision.interactable = false;

        // Asignar listener al botón de regresar para todas las rutas
        if (Regresar != null)
        {
            Regresar.onClick.RemoveAllListeners();
            Regresar.onClick.AddListener(DevolverAPantallaAnterior);
        }
    }

    public void CompletarMision()
    {
        // 1. Lógica para calcular XP y guardar datos temporales
        int xpGanado = 20; // XP para misiones de QR/Pin
        PlayerPrefs.SetInt("UltimoQuizGanado", 1);
        PlayerPrefs.SetInt("xpGanado", xpGanado);
        PlayerPrefs.SetInt("xp_mision", xpGanado);
        PlayerPrefs.Save();

        // Ocultar el panel de botón de completar (ya completamos la misión)
        if (PanelBotonUI != null)
        {
            PanelBotonUI.SetActive(false);
        }

        // 2. Configurar y mostrar el panel de resultados
        if (PanelMisionCompletada != null)
        {
            PanelMisionCompletada.SetActive(true);

            // Configurar textos
            if (TxtMisionResultado != null)
                TxtMisionResultado.text = (appIdioma == "ingles") ? "MISSION COMPLETED!" : "¡MISIÓN COMPLETADA!";
            if (TxtXpResultado != null) TxtXpResultado.text = $"+{xpGanado} XP";
            if (TxtPuntuacionResultado != null) TxtPuntuacionResultado.text = "1/1";
            if (TxtMotivacionResultado != null)
                TxtMotivacionResultado.text = (appIdioma == "ingles") ? "Excellent! You have explored this element." : "¡Excelente! Has explorado este elemento.";

            // Ocultar campos de refuerzo
            if (TxtRefuerzo1 != null) TxtRefuerzo1.transform.parent.gameObject.SetActive(false);
            if (TxtRefuerzo2 != null) TxtRefuerzo2.transform.parent.gameObject.SetActive(false);
        }

        // 3. Agregar XP al sistema
        if (SistemaXP.Instance != null)
        {
            SistemaXP.Instance.AgregarXP(xpGanado);
        }

        // 4. Configurar botón de continuar
        StartCoroutine(ConfigurarBotonContinuar());
    }

    private IEnumerator ConfigurarBotonContinuar()
    {
        yield return new WaitForEndOfFrame();

        if (btnContinuarPanel != null)
        {
            // Desactivar Raycast Target de TODOS los objetos Image en PanelMisionCompletada excepto el botón continuar
            if (PanelMisionCompletada != null)
            {
                UnityEngine.UI.Image[] todasLasImagenes = PanelMisionCompletada.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                foreach (var img in todasLasImagenes)
                {
                    if (img.transform != btnContinuarPanel.transform && !img.transform.IsChildOf(btnContinuarPanel.transform))
                    {
                        if (img.raycastTarget)
                        {
                            img.raycastTarget = false;
                        }
                    }
                }
            }

            // Mover el panel al final
            Transform elementoAMover = btnContinuarPanel.transform;
            if (PanelContinuar != null)
            {
                elementoAMover = PanelContinuar.transform;
                PanelContinuar.SetActive(true);
            }

            if (PanelMisionCompletada != null)
            {
                elementoAMover.SetParent(PanelMisionCompletada.transform);
            }

            elementoAMover.SetAsLastSibling();

            // Activar y configurar el botón
            btnContinuarPanel.gameObject.SetActive(true);
            btnContinuarPanel.interactable = true;

            // Verificar CanvasGroup que puede bloquear
            var canvasGroups = btnContinuarPanel.GetComponentsInParent<UnityEngine.CanvasGroup>();
            foreach (var cg in canvasGroups)
            {
                if (!cg.interactable || cg.blocksRaycasts == false)
                {
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }

            // Limpiar listeners y agregar el nuestro
            btnContinuarPanel.onClick.RemoveAllListeners();
            btnContinuarPanel.onClick.AddListener(() => {
                ContinuarYVolver();
            });
        }
        else
        {
            Debug.LogError("❌ [GuardarMisionCompletada] btnContinuarPanel es NULL!");
        }
    }

    public void ContinuarYVolver()
    {
        try
        {
            // Detener todos los audios en la escena
            AudioSource[] todosLosAudios = FindObjectsOfType<AudioSource>();
            foreach (AudioSource audio in todosLosAudios)
            {
                if (audio != null && audio.isPlaying)
                {
                    audio.Stop();
                    audio.enabled = false;
                }
            }

            // Ocultar el botón de continuar
            if (btnContinuarPanel != null)
            {
                btnContinuarPanel.gameObject.SetActive(false);
            }

            // Ocultar el panel de misión completada
            if (PanelMisionCompletada != null)
            {
                PanelMisionCompletada.SetActive(false);
            }

            // IMPORTANTE: Guardar la misión Y la información de dónde volver
            StartCoroutine(GuardarMisionYVolverACategorias());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [GuardarMisionCompletada] ERROR en ContinuarYVolver: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
        }
    }

    // Nueva corrutina que guarda todo correctamente
    private IEnumerator GuardarMisionYVolverACategorias()
    {
        Debug.Log("💾 [GuardarMisionCompletada] Guardando misión completada...");

        // Guardar la misión PRIMERO
        Task saveTask = MarcarMisionComoCompletada();

        // Esperar a que termine de guardar
        while (!saveTask.IsCompleted)
        {
            yield return null;
        }

        if (saveTask.IsFaulted)
        {
            Debug.LogError("❌ [GuardarMisionCompletada] Error al guardar: " + saveTask.Exception);
        }
        else
        {
            Debug.Log("✅ [GuardarMisionCompletada] Misión guardada correctamente.");
        }

        // Guardar información de dónde volver (igual que en VuforiaNuevo)
        string elementoActual = PlayerPrefs.GetString("ElementoSeleccionado", "");
        string categoriaActual = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
        PlayerPrefs.SetString("VolverAElemento", elementoActual);
        PlayerPrefs.SetString("VolverACategoria", categoriaActual);
        PlayerPrefs.Save();

        Debug.Log($"📝 [GuardarMisionCompletada] Volviendo a panel misiones - Elemento: {elementoActual}, Categoría: {categoriaActual}");

        // Volver a Categorías
        DevolverAPantallaAnterior();
    }

    private void DevolverAPantallaAnterior()
    {
        // Guardar destino para que Categorías abra directamente el panel de misiones del elemento
        string elementoActual = PlayerPrefs.GetString("ElementoSeleccionado", "");
        string categoriaActual = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        if (!string.IsNullOrEmpty(elementoActual))
        {
            PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
            PlayerPrefs.SetString("VolverAElemento", elementoActual);
            PlayerPrefs.SetString("VolverACategoria", categoriaActual);
            PlayerPrefs.Save();
        }

        // Asegurar que el tiempo está normal
        Time.timeScale = 1f;

        // Detener todos los audios (por si acaso)
        AudioSource[] todosLosAudios = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in todosLosAudios)
        {
            if (audio != null)
            {
                audio.Stop();
                audio.enabled = false;
            }
        }

        // CRÍTICO: Detener Vuforia y destruir los ImageTargets ANTES de cambiar de escena
        Debug.Log("🛑 [GuardarMisionCompletada] Deteniendo Vuforia...");

        // Buscar y destruir todos los DynamicMoleculeLoader
        DynamicMoleculeLoader[] loaders = FindObjectsOfType<DynamicMoleculeLoader>();
        foreach (var loader in loaders)
        {
            if (loader != null && loader.gameObject != null)
            {
                Debug.Log($"🗑️ [GuardarMisionCompletada] Destruyendo DynamicMoleculeLoader en: {loader.gameObject.name}");
                Destroy(loader.gameObject);
            }
        }

        // Detener Vuforia
        try
        {
            if (VuforiaBehaviour.Instance != null)
            {
                VuforiaBehaviour.Instance.enabled = false;
                Debug.Log("✅ [GuardarMisionCompletada] Vuforia detenido");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ [GuardarMisionCompletada] No se pudo detener Vuforia: {e.Message}");
        }

        // Detener todas las corrutinas de esta instancia
        StopAllCoroutines();

        // Destruir la instancia antes de cambiar de escena
        if (instancia != null)
        {
            Destroy(instancia.gameObject);
            instancia = null;
        }

        // Forzar el cambio de escena con transición suave
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("Categorías", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("Categorías", LoadSceneMode.Single);
        }
    }

    public async Task MarcarMisionComoCompletada()
    {
        Debug.Log("🔍 [MarcarMisionComoCompletada] Iniciando guardado de misión...");

        appIdioma = PlayerPrefs.GetString("appIdioma", "español");

        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);

        Debug.Log($"🔍 [MarcarMisionComoCompletada] Elemento: '{elemento}', ID Misión: {idMision}");

        if (string.IsNullOrEmpty(elemento) || idMision == -1)
        {
            Debug.LogError("❌ No se encontraron datos válidos en PlayerPrefs.");
            return;
        }

        await ActualizarMisionEnJSON(elemento, idMision);
        Debug.Log("✅ [MarcarMisionComoCompletada] Guardado completado");
    }

    private async Task ActualizarMisionEnJSON(string elemento, int idMision)
    {
        // 1) Intentar cargar desde archivo
        string jsonString;
        string fileName = "Json_Misiones.json";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // DEBUG: Ver el valor de UltimoQuizGanado
        int valorUltimoQuiz = PlayerPrefs.GetInt("UltimoQuizGanado", 0);
        Debug.Log($"🔍 [ActualizarMisionEnJSON] Valor de UltimoQuizGanado en PlayerPrefs: {valorUltimoQuiz}");

        bool ganoElUltimoQuiz = valorUltimoQuiz == 1;

        if (File.Exists(filePath))
        {
            try
            {
                jsonString = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al leer el archivo JSON: {e.Message}");
                return;
            }
        }
        else
        {
            // 2) Fallback: cargar desde Resources/Plantillas_Json
            var textAsset = Resources.Load<TextAsset>("Plantillas_Json/Json_Misiones");
            if (textAsset != null)
            {
                jsonString = textAsset.text;
            }
            else
            {
                Debug.LogError($"❌ No se encontró '{fileName}' ni en persistentDataPath ni en Resources/Plantillas_Json");
                return;
            }
        }

        // 3) Validaciones iniciales
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON ni en archivo ni en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Misiones") || !json["Misiones"].HasKey("Categorias"))
        {
            Debug.LogError("❌ Estructura del JSON incorrecta o faltan claves principales.");
            return;
        }

        var categorias = json["Misiones"]["Categorias"];
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (appIdioma == "ingles")
            categoriaSeleccionada = devolverCatTrad(categoriaSeleccionada);


        if (!categorias.HasKey(categoriaSeleccionada) ||
            !categorias[categoriaSeleccionada].HasKey("Elementos") ||
            !categorias[categoriaSeleccionada]["Elementos"].HasKey(elemento))
        {
            Debug.LogError($"❌ No se encontró la categoría '{categoriaSeleccionada}' o el elemento '{elemento}' en el JSON.");
            return;
        }

        var elementoJson = categorias[categoriaSeleccionada]["Elementos"][elemento];
        var misiones = elementoJson["misiones"].AsArray;
        bool cambioRealizado = false;
        bool esUltimaMisionPendiente = true;
        int xpGanado = PlayerPrefs.GetInt("xp_mision", 0);

        // 4) Detectar si quedan misiones pendientes distintas a esta
        // IMPORTANTE: Las misiones de tipo "Evaluacion" (del profesor) son OPCIONALES
        // y NO bloquean el desbloqueo del logro del elemento
        Debug.Log($"📊 [GuardarMisionCompletada] Verificando misiones del elemento '{elemento}':");
        foreach (JSONNode m in misiones)
        {
            // Ignorar misiones de tipo "Evaluacion" (del profesor) - son opcionales
            if (m["tipo"] == "Evaluacion")
            {
                Debug.Log($"  ⏭️ Misión {m["id"].AsInt} (tipo: {m["tipo"]}) - OPCIONAL, se ignora");
                continue;
            }

            bool completada = m["completada"].AsBool;
            Debug.Log($"  📋 Misión {m["id"].AsInt} (tipo: {m["tipo"]}) - Completada: {completada}");

            // Si hay alguna misión obligatoria (no Evaluacion) pendiente, no es la última
            if (m["id"].AsInt != idMision && !completada)
            {
                Debug.Log($"  ⚠️ Misión {m["id"].AsInt} está pendiente - NO se desbloqueará el logro aún");
                esUltimaMisionPendiente = false;
                break;
            }
        }

        Debug.Log($"✅ [GuardarMisionCompletada] ¿Es la última misión pendiente? {esUltimaMisionPendiente}");

        // 5) Buscar y marcar la misión
        Debug.Log($"🔍 [ActualizarMisionEnJSON] Buscando misión con ID {idMision}...");
        bool misionEncontrada = false;

        for (int i = 0; i < misiones.Count; i++)
        {
            var mision = misiones[i];
            if (mision["id"].AsInt == idMision)
            {
                misionEncontrada = true;
                Debug.Log($"✅ [ActualizarMisionEnJSON] Misión {idMision} encontrada. Ya completada: {mision["completada"].AsBool}");

                if (mision["completada"].AsBool)
                {
                    Debug.Log($"⚠️ [ActualizarMisionEnJSON] La misión {idMision} ya estaba completada.");

                    Debug.Log($"🔍 [ActualizarMisionEnJSON] ¿Es última misión? {esUltimaMisionPendiente}");

                    if (esUltimaMisionPendiente)
                    {
                        Debug.Log($"🏆 [GuardarMisionCompletada] ¡Todas las misiones completadas! Desbloqueando logro del elemento '{elemento}'");
                        _ = ProcesarXP(15);
                        _ = GuardarLogroElemento(categoriaSeleccionada, elemento);
                    }
                    else
                    {
                        _ = ProcesarXP(3);
                    }

                    return;
                }

                Debug.Log($"🎮 [ActualizarMisionEnJSON] ¿Ganó el quiz? {ganoElUltimoQuiz}");

                if (ganoElUltimoQuiz)
                {
                    mision["completada"] = true;
                    cambioRealizado = true;
                    Debug.Log($"✅ [ActualizarMisionEnJSON] Misión {idMision} marcada como completada");

                    // Guardar localmente primero (instantáneo) para no bloquear la navegación
                    Debug.Log($"💾 [ActualizarMisionEnJSON] Guardando Json_Misiones.json localmente...");
                    GuardarJsonActualizado(filePath, json.ToString());

                    // Sincronizar con Firebase en background (sin bloquear)
                    int xp = PlayerPrefs.GetInt("xp_mision", 0);
                    _ = ProcesarXP(xp);

                    // 🤖 Notificar a Quantum AI que la misión se completó con éxito
                    if (QuantumAICore.Instance != null)
                    {
                        QuantumAICore.Instance.NotifyMissionCompleted(elemento, idMision, true);
                    }

                    Debug.Log($"🔍 [ActualizarMisionEnJSON] ¿Es última misión? {esUltimaMisionPendiente}");

                    if (esUltimaMisionPendiente)
                    {
                        Debug.Log($"🏆 [GuardarMisionCompletada] ¡Todas las misiones completadas! Desbloqueando logro del elemento '{elemento}'");
                        _ = ProcesarXP(15);

                        if (QuantumAICore.Instance != null)
                        {
                            QuantumAICore.Instance.NotifyAchievementUnlocked($"Elemento {elemento} dominado", 15);
                        }

                        _ = GuardarLogroElemento(categoriaSeleccionada, elemento);
                        Debug.Log($"💾 [GuardarMisionCompletada] Logro en proceso de guardado en Firebase");
                    }
                    else
                    {
                        Debug.Log($"📝 [GuardarMisionCompletada] Misión guardada, pero aún quedan misiones pendientes - logro NO desbloqueado");
                    }
                }
                else
                {
                    // El jugador perdió, no marcamos la misión
                    Debug.LogWarning($"⚠️ [ActualizarMisionEnJSON] El jugador NO ganó el quiz. No se marca la misión.");

                    if (QuantumAICore.Instance != null)
                    {
                        QuantumAICore.Instance.NotifyMissionCompleted(elemento, idMision, false);
                    }

                    int xpConsolacion = PlayerPrefs.GetInt("xp_mision", 0);
                    _ = ProcesarXP(xpConsolacion);
                    return;
                }

                return;

            }
        }

        if (!misionEncontrada)
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
    }

    string devolverCatTrad(string categoriaSeleccionada)
    {
        switch (categoriaSeleccionada)
        {
            case "Alkali Metals":
                return "Metales Alcalinos";

            case "Alkaline Earth Metals":
                return "Metales Alcalinotérreos";

            case "Transition Metals":
                return "Metales de Transición";

            case "Post-transition Metals":
                return "Metales postransicionales";

            case "Metalloids":
                return "Metaloides";

            case "Nonmetals":
                return "No Metales";

            case "Noble Gases":
                return "Gases Nobles";

            case "Lanthanides":
                return "Lantánidos";

            case "Actinides":
                return "Actinoides";

            case "Unknown Properties":
                return "Propiedades desconocidas";

            default:
                return categoriaSeleccionada;
        }
    }

    private void GuardarJsonActualizado(string filePath, string json)
    {
        try
        {
            File.WriteAllText(filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error al guardar el archivo: {e.Message}");
        }
        PlayerPrefs.SetString("misionesCategoriasJSON", json);
        PlayerPrefs.Save();
    }

    private async Task ProcesarXP(int xp)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await SubirMisionesJSON();
            SumarXPFirebase(xp);
        }
        else
        {
            SumarXPTemporario(xp);
        }
    }

    // Función nueva: Guardar logro en Json_Logros.json
    private async Task GuardarLogroElemento(string categoria, string elemento)
    {
        Debug.Log($"🏅 [GuardarLogro] Guardando logro para elemento '{elemento}' en categoría '{categoria}'");

        // 1. Cargar el archivo Json_Logros.json
        string fileName = "Json_Logros.json";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);
        string jsonString;

        if (File.Exists(filePath))
        {
            try
            {
                jsonString = File.ReadAllText(filePath);
                Debug.Log($"✅ [GuardarLogro] Json_Logros.json cargado desde persistentDataPath");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [GuardarLogro] Error al leer Json_Logros.json: {e.Message}");
                return;
            }
        }
        else
        {
            // Cargar desde Resources como fallback
            var textAsset = Resources.Load<TextAsset>("Plantillas_Json/Json_Logros");
            if (textAsset != null)
            {
                jsonString = textAsset.text;
                Debug.Log($"✅ [GuardarLogro] Json_Logros.json cargado desde Resources");
            }
            else
            {
                Debug.LogError($"❌ [GuardarLogro] No se encontró Json_Logros.json");
                return;
            }
        }

        // 2. Parsear el JSON
        var json = JSON.Parse(jsonString);
        if (!json.HasKey("Logros") || !json["Logros"].HasKey("Categorias"))
        {
            Debug.LogError("❌ [GuardarLogro] Estructura de logros incorrecta en el JSON");
            return;
        }

        // 3. Marcar el logro
        MarcarLogroElementoComoDesbloqueado(json, categoria, elemento);

        // 4. Guardar el archivo
        try
        {
            File.WriteAllText(filePath, json.ToString());
            Debug.Log($"💾 [GuardarLogro] Json_Logros.json guardado en {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [GuardarLogro] Error al guardar Json_Logros.json: {e.Message}");
        }

        // 5. También guardar en PlayerPrefs como backup
        PlayerPrefs.SetString("logrosJSON", json.ToString());
        PlayerPrefs.Save();

        // 6. Subir a Firebase si hay conexión a internet
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            await SubirLogrosJSON();
            Debug.Log("✅ [GuardarLogro] Logros sincronizados con Firebase");
        }
        else
        {
            Debug.LogWarning("⚠️ [GuardarLogro] Sin conexión a internet, los logros se sincronizarán más tarde");
        }

        await Task.CompletedTask;
    }

    private void MarcarLogroElementoComoDesbloqueado(JSONNode json, string categoria, string elemento)
    {
        Debug.Log($"🏅 [MarcarLogro] Intentando desbloquear logro para elemento '{elemento}' en categoría '{categoria}'");

        if (!json.HasKey("Logros") || !json["Logros"].HasKey("Categorias"))
        {
            Debug.LogWarning("⚠️ [MarcarLogro] Estructura de logros no encontrada en el JSON.");
            return;
        }

        var categoriasLogros = json["Logros"]["Categorias"];

        if (!categoriasLogros.HasKey(categoria))
        {
            Debug.LogWarning($"⚠️ [MarcarLogro] No se encontró la categoría '{categoria}' en logros.");
            return;
        }

        if (!categoriasLogros[categoria].HasKey("logros_elementos"))
        {
            Debug.LogWarning($"⚠️ [MarcarLogro] No se encontró 'logros_elementos' en la categoría '{categoria}'.");
            return;
        }

        if (!categoriasLogros[categoria]["logros_elementos"].HasKey(elemento))
        {
            Debug.LogWarning($"⚠️ [MarcarLogro] No se encontró el elemento '{elemento}' en logros_elementos.");
            return;
        }

        var logroElemento = categoriasLogros[categoria]["logros_elementos"][elemento];

        if (logroElemento["desbloqueado"].AsBool)
        {
            Debug.Log($"ℹ️ [MarcarLogro] El logro del elemento '{elemento}' ya estaba desbloqueado.");
            return;
        }

        logroElemento["desbloqueado"] = true;
        Debug.Log($"✅ [MarcarLogro] ¡Logro del elemento '{elemento}' desbloqueado exitosamente!");
    }

    public void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
    }

    public async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;

            await userRef.UpdateAsync("xp", xpNuevo);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }

    public async Task SubirMisionesJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        string jsonMisiones = "";
        string filePath = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");

        // Primero intentar leer el archivo JSON del almacenamiento del dispositivo
        if (File.Exists(filePath))
        {
            try
            {
                jsonMisiones = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error al leer el archivo JSON: {e.Message}");
                return;
            }
        }
        else
        {
            // Si no existe en el almacenamiento, usar el de PlayerPrefs como respaldo
            jsonMisiones = PlayerPrefs.GetString("misionesCategoriasJSON");
        }

        // Referencias a los documentos dentro de la colección del usuario
        DocumentReference misionesDoc = db.Collection("users").Document(userId).Collection("datos").Document("misiones");

        // Crear tareas para subir ambos JSONs
        List<Task> tareasSubida = new List<Task>();

        if (!string.IsNullOrEmpty(jsonMisiones) && jsonMisiones != "{}")
        {
            Dictionary<string, object> dataMisiones = new Dictionary<string, object>
        {
            { "misiones", jsonMisiones },
            { "timestamp", FieldValue.ServerTimestamp }
        };
            tareasSubida.Add(misionesDoc.SetAsync(dataMisiones, SetOptions.MergeAll));
        }

        if (tareasSubida.Count == 0)
        {
            Debug.LogWarning("⚠️ No hay datos de misiones para subir.");
            return;
        }

        // Esperar a que todas las tareas finalicen
        await Task.WhenAll(tareasSubida);
    }

    public async Task SubirLogrosJSON()
    {
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("❌ [SubirLogrosJSON] No hay usuario autenticado.");
            return;
        }

        string jsonLogros = "";
        string filePath = Path.Combine(Application.persistentDataPath, "Json_Logros.json");

        // Primero intentar leer el archivo JSON del almacenamiento del dispositivo
        if (File.Exists(filePath))
        {
            try
            {
                jsonLogros = File.ReadAllText(filePath);
                Debug.Log($"✅ [SubirLogrosJSON] Json_Logros.json leído desde persistentDataPath");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [SubirLogrosJSON] Error al leer el archivo JSON de logros: {e.Message}");
                return;
            }
        }
        else
        {
            // Si no existe en el almacenamiento, usar el de PlayerPrefs como respaldo
            jsonLogros = PlayerPrefs.GetString("logrosJSON");
            Debug.Log($"ℹ️ [SubirLogrosJSON] Usando logrosJSON desde PlayerPrefs");
        }

        if (string.IsNullOrEmpty(jsonLogros) || jsonLogros == "{}")
        {
            Debug.LogWarning("⚠️ [SubirLogrosJSON] No hay datos de logros para subir.");
            return;
        }

        // Referencia al documento de logros dentro de la colección del usuario
        DocumentReference logrosDoc = db.Collection("users").Document(userId).Collection("datos").Document("logros");

        Dictionary<string, object> dataLogros = new Dictionary<string, object>
        {
            { "logros", jsonLogros },
            { "timestamp", FieldValue.ServerTimestamp }
        };

        try
        {
            await logrosDoc.SetAsync(dataLogros, SetOptions.MergeAll);
            Debug.Log("✅ [SubirLogrosJSON] Logros subidos a Firebase correctamente");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [SubirLogrosJSON] Error al subir logros a Firebase: {e.Message}");
        }
    }
}