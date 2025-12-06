using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Maneja transiciones suaves entre escenas con fade in/out
/// Persiste entre escenas para evitar el parpadeo
/// </summary>
public class SceneTransition : MonoBehaviour
{
    #region Singleton

    private static SceneTransition _instance;
    public static SceneTransition Instance
    {
        get
        {
            if (_instance == null)
            {
                // Buscar en la escena
                _instance = FindObjectOfType<SceneTransition>();

                // Si no existe, crear uno
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransition");
                    _instance = go.AddComponent<SceneTransition>();
                }
            }
            return _instance;
        }
    }

    #endregion

    [Header("Configuración")]
    [SerializeField] private float fadeDuration = 0.15f; // Transición muy rápida
    [SerializeField] private Color fadeColor = Color.white; // Blanco para transición suave

    private Canvas canvas;
    private Image fadeImage;
    private bool isTransitioning = false;

    private void Awake()
    {
        // Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear UI de fade
        SetupFadeUI();
    }

    private void SetupFadeUI()
    {
        // Crear Canvas
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000; // Por encima de TODO

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Crear panel de fade
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(transform);

        RectTransform rect = fadePanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        fadeImage = fadePanel.AddComponent<Image>();
        // Color blanco con alpha muy bajo (casi imperceptible)
        fadeImage.color = new Color(1f, 1f, 1f, 0f); // Transparente al inicio
        fadeImage.raycastTarget = false; // NO bloquear interacción cuando no hay transición

        Debug.Log("[SceneTransition] Sistema de transición inicializado");
    }

    /// <summary>
    /// Cambia de escena con transición fade suave
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName));
        }
    }

    /// <summary>
    /// Cambia de escena con transición fade suave (sobrecarga con LoadSceneMode)
    /// </summary>
    public void LoadScene(string sceneName, LoadSceneMode mode)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName, mode));
        }
    }

    private IEnumerator TransitionToScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        isTransitioning = true;

        // FADE OUT (oscurecer)
        yield return FadeOut();

        // Pausa mínima
        yield return new WaitForSeconds(0.05f);

        // CARGAR ESCENA de forma asíncrona
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);
        asyncLoad.allowSceneActivation = false;

        // Esperar a que cargue (mínimo 90%)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activar la escena
        asyncLoad.allowSceneActivation = true;

        // Esperar un frame más para que todo se inicialice
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.05f);

        // FADE IN (aclarar)
        yield return FadeIn();

        isTransitioning = false;
    }

    /// <summary>
    /// Fade a negro (oscurecer)
    /// </summary>
    public Coroutine FadeOut()
    {
        return StartCoroutine(FadeCoroutine(1f));
    }

    /// <summary>
    /// Fade desde negro (aclarar)
    /// </summary>
    public Coroutine FadeIn()
    {
        return StartCoroutine(FadeCoroutine(0f));
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[SceneTransition] fadeImage es null!");
            yield break;
        }

        // Reducir el alpha objetivo para que sea casi imperceptible
        float subtleAlpha = targetAlpha * 0.3f; // Solo 30% de opacidad máxima

        // Bloquear interacción SOLO durante el fade out (cuando aparece)
        fadeImage.raycastTarget = targetAlpha > 0f;

        // Animar con DOTween - fade muy sutil
        yield return fadeImage.DOFade(subtleAlpha, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true) // Ignorar Time.timeScale
            .WaitForCompletion();

        // Desbloquear interacción después del fade in
        if (targetAlpha == 0f)
        {
            fadeImage.raycastTarget = false;
        }
    }

    /// <summary>
    /// Verifica si hay una transición en progreso
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    #region Métodos de utilidad

    /// <summary>
    /// Cambia la duración del fade
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        fadeDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Cambia el color del fade
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            Color currentColor = fadeImage.color;
            fadeImage.color = new Color(color.r, color.g, color.b, currentColor.a);
        }
    }

    #endregion
}
