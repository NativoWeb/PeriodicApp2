using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class NavigationController : MonoBehaviour
{
    private static NavigationController instance;
    private Stack<string> sceneHistory = new Stack<string>(); // Historial de escenas
    private float edgeThreshold; // Margen de detección de gestos en píxeles
    private Vector2 touchStartPos; // Posición inicial del toque

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

        edgeThreshold = Screen.width * 0.015f; // Solo el 1.5% del borde de la pantalla
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        // Detectar botón "Atrás" en Android
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }

        // Detectar gestos táctiles en los bordes
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
                bool isSwipe = swipeDistance > Screen.width * 0.1f; // Consideramos un swipe válido si se mueve más del 10% de la pantalla

                // Si el gesto empieza en el borde y se mueve hacia el centro, se ejecuta "GoBack()"
                if (isSwipe && (touchStartPos.x < edgeThreshold || touchStartPos.x > Screen.width - edgeThreshold))
                {
                    GoBack();
                }
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // No guardar la escena si es la primera o si estamos regresando
        if (sceneHistory.Count == 0 || sceneHistory.Peek() != scene.name)
        {
            sceneHistory.Push(scene.name);
        }
    }

    void GoBack()
    {
        if (sceneHistory.Count > 1)
        {
            sceneHistory.Pop(); // Quitar la escena actual
            string previousScene = sceneHistory.Peek(); // Obtener la anterior
            SceneManager.LoadScene(previousScene);
        }
    }
}
