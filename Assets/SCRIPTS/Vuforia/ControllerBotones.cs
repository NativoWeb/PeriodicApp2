using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using Vuforia;

public class ControllerBotones : MonoBehaviour
{
    public GameObject PanelRegresarUI;
    public GameObject PanelBotonUI;
    public Button botonCompletarMision;
    public Button Regresar;

    [Header("Panel de Resultados")]
    public GameObject PanelMisionCompletada;
    public TextMeshProUGUI TxtMisionResultado;
    public TextMeshProUGUI TxtXpResultado;
    public TextMeshProUGUI TxtPuntuacionResultado;
    public TextMeshProUGUI TxtMotivacionResultado;
    public TextMeshProUGUI TxtRefuerzo1;
    public TextMeshProUGUI TxtRefuerzo2;
    //public Button botonContinuar;

    [Header("Referencias para Animación")]
    public GameObject PanelContinuar;
    public GameObject panelAnimacionMision;
    public GameObject imagenAnimacionMision;
    public AudioSource audioMisionCompletada;
    public Button btnContinuarPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (PanelMisionCompletada != null)
        {
            PanelMisionCompletada.SetActive(false);
        }

        // Asignar el listener al botón de completar misión (solo una vez)
        if (botonCompletarMision != null)
        {
            botonCompletarMision.onClick.AddListener(CompletarMision);
        }

        string ruta = PlayerPrefs.GetString("CargarVuforia", ""); // Obtiene la ruta almacenada

        if (ruta == "Inicio")
        {
            PanelRegresarUI.SetActive(true);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
        else if (ruta == "Misiones")
        {
            PanelRegresarUI.SetActive(true);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
            Regresar.onClick.RemoveAllListeners();
            Regresar.onClick.AddListener(RegresarAMisionesDesdeAR);
        }
        else if (ruta == "Profesor")
        {
            PanelRegresarUI.SetActive(true);
            Regresar.onClick.AddListener(CargarVuforiaProfesor);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
    }
    void RegresarAMisionesDesdeAR()
    {
        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");
        PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
        PlayerPrefs.SetString("VolverAElemento", elemento);
        PlayerPrefs.SetString("VolverACategoria", categoria);
        PlayerPrefs.Save();

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene("Categorías");
        else
            SceneManager.LoadScene("Categorías");
    }

    void CargarVuforiaProfesor()
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("InicioProfesor");
        }
        else
        {
            SceneManager.LoadScene("InicioProfesor");
        }
    }

    public void CompletarMision()
    {
        // 1. Lógica para calcular XP y guardar datos temporales
        int xpGanado = 10;
        PlayerPrefs.SetInt("UltimoQuizGanado", 1);
        PlayerPrefs.SetInt("xpGanado", xpGanado);
        PlayerPrefs.SetInt("xp_mision", xpGanado);
        PlayerPrefs.Save();

        // 2. Configurar y mostrar el panel de resultados
        if (PanelMisionCompletada != null)
        {
            PanelMisionCompletada.SetActive(true);

            // Configurar textos
            if (TxtMisionResultado != null) TxtMisionResultado.text = "¡MISIÓN COMPLETADA!";
            if (TxtXpResultado != null) TxtXpResultado.text = $"+{xpGanado} XP";
            if (TxtPuntuacionResultado != null) TxtPuntuacionResultado.text = "1/1";
            if (TxtMotivacionResultado != null) TxtMotivacionResultado.text = "¡Excelente! Has explorado este elemento.";

            // Ocultar campos de refuerzo
            if (TxtRefuerzo1 != null) TxtRefuerzo1.transform.parent.gameObject.SetActive(false);
            if (TxtRefuerzo2 != null) TxtRefuerzo2.transform.parent.gameObject.SetActive(false);
        }

        // 3. Configurar el botón continuar - que haga lo mismo que regresar pero a Categorías
        if (btnContinuarPanel != null)
        {
            btnContinuarPanel.gameObject.SetActive(true);
            btnContinuarPanel.onClick.RemoveAllListeners();
            btnContinuarPanel.onClick.AddListener(VolverACategorias);
        }

        if (PanelContinuar != null)
        {
            PanelContinuar.SetActive(true);
        }
    }

    // Guardar misión y volver a Categorías
    void VolverACategorias()
    {
        StartCoroutine(GuardarMisionYVolver());
    }

    IEnumerator GuardarMisionYVolver()
    {
        Debug.Log("💾 [ControllerBotones] Guardando misión completada...");

        // Guardar la misión PRIMERO
        if (GuardarMisionCompletada.instancia != null)
        {
            Task saveTask = GuardarMisionCompletada.instancia.MarcarMisionComoCompletada();

            // Esperar a que termine de guardar
            while (!saveTask.IsCompleted)
            {
                yield return null;
            }

            if (saveTask.IsFaulted)
            {
                Debug.LogError("❌ [ControllerBotones] Error al guardar: " + saveTask.Exception);
            }
            else
            {
                Debug.Log("✅ [ControllerBotones] Misión guardada correctamente.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [ControllerBotones] GuardarMisionCompletada.instancia es null");
        }

        // Guardar información de dónde volver
        string elementoActual = PlayerPrefs.GetString("ElementoSeleccionado", "");
        string categoriaActual = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
        PlayerPrefs.SetString("VolverAElemento", elementoActual);
        PlayerPrefs.SetString("VolverACategoria", categoriaActual);
        PlayerPrefs.Save();

        Debug.Log($"📝 [ControllerBotones] Elemento: {elementoActual}, Categoría: {categoriaActual}");

        // Ir a Categorías con transición suave
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("Categorías");
        }
        else
        {
            SceneManager.LoadScene("Categorías");
        }
    }

    // Esta función se encarga de esperar, ocultar el panel y cambiar de escena
    private IEnumerator MostrarResultadosYContinuar(float segundosDeEspera)
    {
        Debug.Log($"⏱️ [ControllerBotones] Iniciando temporizador de {segundosDeEspera} segundos en GameObject: {gameObject.name}");

        // Esperar el tiempo definido
        yield return new WaitForSeconds(segundosDeEspera);

        Debug.Log("⏱️ [ControllerBotones] Tiempo finalizado. Guardando misión y cambiando de escena.");

        // Guardar la misión como completada ANTES de ocultar el panel
        if (GuardarMisionCompletada.instancia != null)
        {
            Debug.Log("💾 [ControllerBotones] Iniciando guardado de misión...");

            // 1. Inicia la tarea de guardado
            Task saveTask = GuardarMisionCompletada.instancia.MarcarMisionComoCompletada();

            // 2. Espera, frame a frame, hasta que la tarea se complete
            while (!saveTask.IsCompleted)
            {
                yield return null;
            }

            if (saveTask.IsFaulted)
            {
                Debug.LogError("❌ [ControllerBotones] Ocurrió un error al guardar la misión: " + saveTask.Exception);
                // Aquí podrías mostrar un panel de error al usuario antes de detenerte
                yield break; // Detiene la corutina aquí
            }

            Debug.Log("✅ [ControllerBotones] Misión guardada correctamente.");
        }
        else
        {
            Debug.LogWarning("⚠️ [ControllerBotones] GuardarMisionCompletada.instancia es null");
        }

        // Ocultar el panel DESPUÉS de guardar
        if (PanelMisionCompletada != null)
        {
            PanelMisionCompletada.SetActive(false);
        }

        // 3. Solo cuando el guardado ha terminado y no hubo errores, cambiamos de escena
        Debug.Log("🔄 [ControllerBotones] Cargando escena 'Categorías'...");

        // Asegurarnos de que el tiempo no está pausado
        Time.timeScale = 1f;

        // Verificar que la escena está en el build
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"📊 [ControllerBotones] Total de escenas en Build Settings: {sceneCount}");

        // DETENER TODAS LAS DEMÁS CORUTINAS DE ESTE OBJETO para evitar conflictos
        StopAllCoroutines();

        // Intentar cargar la escena de forma asíncrona para tener más control
        StartCoroutine(CargarEscenaAsync());
    }

    private IEnumerator CargarEscenaAsync()
    {
        Debug.Log("🔄 [ControllerBotones] Iniciando carga asíncrona de escena...");

        // Guardar información para volver al panel de misiones del elemento
        string elementoActual = PlayerPrefs.GetString("ElementoSeleccionado", "");
        string categoriaActual = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
        PlayerPrefs.SetString("VolverAElemento", elementoActual);
        PlayerPrefs.SetString("VolverACategoria", categoriaActual);
        PlayerPrefs.Save();

        Debug.Log($"📝 [ControllerBotones] Guardando instrucción de volver al elemento: {elementoActual}");

        // CRÍTICO: Detener Vuforia y desactivar todos los ImageTargets ANTES de cambiar de escena
        // Esto evita que los eventos OnTargetStatusChanged se disparen durante el cambio de escena
        Debug.Log("🛑 [ControllerBotones] Deteniendo Vuforia y desactivando ImageTargets...");

        // Buscar todos los ImageTargets y desactivarlos
        DynamicMoleculeLoader[] loaders = FindObjectsOfType<DynamicMoleculeLoader>();
        foreach (var loader in loaders)
        {
            if (loader != null && loader.gameObject != null)
            {
                Debug.Log($"🗑️ [ControllerBotones] Destruyendo DynamicMoleculeLoader en: {loader.gameObject.name}");
                Destroy(loader.gameObject);
            }
        }

        // Detener Vuforia
        try
        {
            Vuforia.VuforiaBehaviour.Instance.enabled = false;
            Debug.Log("✅ [ControllerBotones] Vuforia detenido");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ [ControllerBotones] No se pudo detener Vuforia: {e.Message}");
        }

        // Esperar un frame para que las destrucciones se procesen
        yield return null;

        // IMPORTANTE: Destruir el objeto GuardarMisionCompletada antes de cambiar de escena
        // Este objeto tiene DontDestroyOnLoad y puede causar conflictos
        if (GuardarMisionCompletada.instancia != null)
        {
            Debug.Log("🗑️ [ControllerBotones] Destruyendo instancia de GuardarMisionCompletada...");
            Destroy(GuardarMisionCompletada.instancia.gameObject);
            GuardarMisionCompletada.instancia = null;
        }

        // Cargar la escena de Categorías (donde está el panel de misiones) con transición suave
        Debug.Log("🔄 [ControllerBotones] Cargando escena 'Categorías'...");

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("Categorías");
        }
        else
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Categorías");

            if (asyncLoad == null)
            {
                Debug.LogError("❌ [ControllerBotones] asyncLoad es null - la escena no existe o no está en Build Settings");
                yield break;
            }

            // Esperar hasta que la escena termine de cargar
            while (!asyncLoad.isDone)
            {
                Debug.Log($"⏳ [ControllerBotones] Progreso de carga: {asyncLoad.progress * 100}%");
                yield return null;
            }

            Debug.Log("✅ [ControllerBotones] Escena 'Categorías' cargada exitosamente");
        }
    }


    // Método de prueba simple - úsalo primero para verificar que el botón puede llamar métodos
    public void TestClick()
    {
        Debug.LogWarning("🧪 TEST: El botón SÍ puede llamar métodos!");
    }

    void MostrarBotonContinuar()
    {
        if (PanelContinuar != null) PanelContinuar.SetActive(true);
        if (btnContinuarPanel != null) btnContinuarPanel.onClick.AddListener(VolverAPantallaAnterior);
    }

    void VolverAPantallaAnterior()
    {
        string ruta = PlayerPrefs.GetString("CargarVuforia", "");

        string targetScene = "Categorías"; // Por defecto

        if (ruta == "Inicio")
        {
            targetScene = "Perfil_Usuario";
        }
        else if (ruta == "Misiones")
        {
            targetScene = "Categorías";
        }
        else if (ruta == "Profesor")
        {
            targetScene = "InicioProfesor";
        }

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

}
