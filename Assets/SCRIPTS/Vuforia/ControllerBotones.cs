using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Threading.Tasks;

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
        }
        else if (ruta == "Profesor")
        {
            PanelRegresarUI.SetActive(true);
            Regresar.onClick.AddListener(CargarVuforiaProfesor);
            PanelBotonUI.SetActive(false);
            botonCompletarMision.interactable = false;
        }
    }
    void CargarVuforiaProfesor()
    {
        SceneManager.LoadScene("InicioProfesor");
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

        // 3. Iniciar la corutina para la transición automática
        StartCoroutine(MostrarResultadosYContinuar(5f));
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

        // Limpiar el PlayerPrefs que indica desde dónde se cargó Vuforia
        // Esto evita que el sistema piense que debe regresar automáticamente
        PlayerPrefs.SetString("PanelDestino", "PanelMisiones");
        string elementoActual = PlayerPrefs.GetString("ElementoSeleccionado", "");
        PlayerPrefs.SetString("VolverAElemento", elementoActual);
        PlayerPrefs.Save();

        Debug.Log($"📝 [ControllerBotones] Guardando instrucción de volver al elemento: {elementoActual}");

        // IMPORTANTE: Destruir el objeto GuardarMisionCompletada antes de cambiar de escena
        // Este objeto tiene DontDestroyOnLoad y puede causar conflictos
        if (GuardarMisionCompletada.instancia != null)
        {
            Debug.Log("🗑️ [ControllerBotones] Destruyendo instancia de GuardarMisionCompletada...");
            Destroy(GuardarMisionCompletada.instancia.gameObject);
            GuardarMisionCompletada.instancia = null;
        }

        // Cargar la escena de Categorías (donde está el panel de misiones)
        Debug.Log("🔄 [ControllerBotones] Cargando escena 'Categorías'...");
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

        if (ruta == "Inicio")
        {
            SceneManager.LoadScene("Perfil_Usuario");
        }
        else if (ruta == "Misiones")
        {
            SceneManager.LoadScene("Categorías");
        }
        else if (ruta == "Profesor")
        {
            SceneManager.LoadScene("InicioProfesor");
        }
        else
        {
            // Por defecto, volver a Categorías
            SceneManager.LoadScene("Categorías");
        }
    }

}
