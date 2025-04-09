using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NavigationController : MonoBehaviour
{
    private static NavigationController instance;
    private Stack<NavigationItem> navigationHistory = new Stack<NavigationItem>();
    private float edgeThreshold;
    private Vector2 touchStartPos;

    // Para manejar paneles dentro de la escena actual
    private GameObject currentPanel;
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    private class NavigationItem
    {
        public string sceneName;
        public GameObject panel; // Panel activo cuando se guardó este item

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

        edgeThreshold = Screen.width * 0.015f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        // Botón "Atrás" en Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }

        // Gestos táctiles en los bordes
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
            }

            if (touch.phase == TouchPhase.Ended)
            {
                float swipeDistance = Mathf.Abs(touch.position.x - touchStartPos.x);
                bool isSwipe = swipeDistance > Screen.width * 0.1f;

                if (isSwipe && (touchStartPos.x < edgeThreshold || touchStartPos.x > Screen.width - edgeThreshold))
                {
                    GoBack();
                }
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
        if (panel == null) return;

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
                // Necesitamos esperar a que la escena cargue completamente
                // Podrías usar una corrutina para esto
                StartCoroutine(ActivatePanelAfterSceneLoad(previous.panel));
            }
        }
        else
        {
            // Si no hay más historial, salir de la aplicación (o lo que prefieras)
            Application.Quit();
        }
    }

    private System.Collections.IEnumerator ActivatePanelAfterSceneLoad(GameObject panel)
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