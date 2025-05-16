using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class NavigationController : MonoBehaviour
{
    private static NavigationController instance;
    private Stack<NavigationItem> navigationHistory = new Stack<NavigationItem>();
    private float edgeThreshold;
    private Vector2 touchStartPos;
    private float touchStartTime;

    // Para manejar paneles dentro de la escena actual
    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    private class NavigationItem
    {
        public string sceneName;
        public GameObject panel;

        public NavigationItem(string scene, GameObject panel = null)
        {
            sceneName = scene;
            this.panel = panel;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        edgeThreshold = Screen.width * 0.05f; // 5% del ancho de pantalla
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        // Si estamos en la escena "CombateQuimico", ignoramos toda la funcionalidad
        if (SceneManager.GetActiveScene().name == "CombateQuimico" || SceneManager.GetActiveScene().name == "Quimicados" || SceneManager.GetActiveScene().name == "QuimicadosGame" || SceneManager.GetActiveScene().name == "Cuestionario")
        {
            return;
        }

        // Botón "Atrás" en Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
            return;
        }

        // Gestos táctiles en los bordes (solo para móviles)
        if (Application.isMobilePlatform && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Verificar si el toque comenzó en el borde izquierdo o derecho
                    if (touch.position.x < edgeThreshold || touch.position.x > Screen.width - edgeThreshold)
                    {
                        touchStartPos = touch.position;
                        touchStartTime = Time.time;
                    }
                    break;

                case TouchPhase.Ended:
                    // Solo procesar si comenzó en el borde
                    if (touchStartPos != Vector2.zero)
                    {
                        float swipeDistance = touch.position.x - touchStartPos.x;
                        float swipeDuration = Time.time - touchStartTime;

                        // Validar que sea un gesto rápido y con suficiente distancia
                        if (Mathf.Abs(swipeDistance) > Screen.width * 0.1f && swipeDuration < 0.5f)
                        {
                            // Determinar dirección (izquierda o derecha)
                            bool isBackSwipe = (touchStartPos.x < edgeThreshold && swipeDistance > 0) ||
                                              (touchStartPos.x > Screen.width - edgeThreshold && swipeDistance < 0);

                            if (isBackSwipe)
                            {
                                GoBack();
                            }
                        }

                        touchStartPos = Vector2.zero; // Resetear
                    }
                    break;
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar una nueva escena, reiniciamos el historial de paneles
        panelHistory.Clear();
        currentPanel = null;

        // Solo guardamos en el historial si es una nueva escena (no al volver atrás)
        if (navigationHistory.Count == 0 || navigationHistory.Peek().sceneName != scene.name)
        {
            navigationHistory.Push(new NavigationItem(scene.name));
        }
    }

    // Método para cambiar de panel dentro de la misma escena
    public void ShowPanel(GameObject panel)
    {
        // Si estamos en la escena "CombateQuimico", no hacer nada
        if (SceneManager.GetActiveScene().name == "CombateQuimico" || SceneManager.GetActiveScene().name == "Quimicados" || SceneManager.GetActiveScene().name == "QuimicadosGame" || SceneManager.GetActiveScene().name == "Cuestionario" || panel == null)
        {
            return;
        }

        // Desactivar el panel actual si existe
        if (currentPanel != null)
        {
            panelHistory.Push(currentPanel);
            currentPanel.SetActive(false);
        }

        // Activar el nuevo panel
        currentPanel = panel;
        currentPanel.SetActive(true);

        // Actualizar el último item del historial con el panel actual
        if (navigationHistory.Count > 0)
        {
            navigationHistory.Peek().panel = currentPanel;
        }
    }

    public void GoBack()
    {
        // Si estamos en la escena "CombateQuimico", no hacer nada
        if (SceneManager.GetActiveScene().name == "CombateQuimico" || SceneManager.GetActiveScene().name == "Quimicados" || SceneManager.GetActiveScene().name == "QuimicadosGame" || SceneManager.GetActiveScene().name == "Cuestionario")
        {
            return;
        }

        // Primero intentamos manejar paneles dentro de la misma escena
        if (panelHistory.Count > 0)
        {
            // Desactivar panel actual
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }

            // Reactivar panel anterior
            currentPanel = panelHistory.Pop();
            currentPanel.SetActive(true);

            // Actualizar referencia en el historial
            if (navigationHistory.Count > 0)
            {
                navigationHistory.Peek().panel = currentPanel;
            }
            return;
        }

        // Si no hay paneles para retroceder, manejamos el cambio de escena
        if (navigationHistory.Count > 1)
        {
            NavigationItem current = navigationHistory.Pop();
            NavigationItem previous = navigationHistory.Peek();

            // Cargar la escena anterior
            SceneManager.LoadScene(previous.sceneName);

            // Reactivar el panel que estaba activo en esa escena (si había uno)
            if (previous.panel != null)
            {
                StartCoroutine(ActivatePanelAfterSceneLoad(previous.panel));
            }
        }
        else
        {
            // Si no hay más historial, salir de la aplicación
            Application.Quit();
        }
    }

    private IEnumerator ActivatePanelAfterSceneLoad(GameObject panel)
    {
        // Esperar hasta que la escena esté completamente cargada
        while (!SceneManager.GetActiveScene().isLoaded)
        {
            yield return null;
        }

        // Buscar el panel en la escena (asumiendo que tiene el mismo nombre/path)
        GameObject panelInScene = GameObject.Find(panel.name);
        if (panelInScene != null)
        {
            panelInScene.SetActive(true);
            currentPanel = panelInScene;
        }
    }
}