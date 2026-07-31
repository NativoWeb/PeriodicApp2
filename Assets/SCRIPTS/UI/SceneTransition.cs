using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

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
                _instance = FindObjectOfType<SceneTransition>();
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

    [Header("Configuracion")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private Color fadeColor = Color.black;

    private const float FadeMaxAlpha = 0.7f;

    private Canvas _canvas;
    private Image _fadeImage;
    private Text _loadingText;
    private bool _isTransitioning;
    private Coroutine _dotsCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SetupFadeUI();
    }

    private void SetupFadeUI()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(transform, false);

        RectTransform rect = fadePanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        _fadeImage = fadePanel.AddComponent<Image>();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        GameObject loadingObj = new GameObject("LoadingText");
        loadingObj.transform.SetParent(fadePanel.transform, false);

        RectTransform textRect = loadingObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(400f, 60f);
        textRect.anchoredPosition = Vector2.zero;

        _loadingText = loadingObj.AddComponent<Text>();
        _loadingText.text = "";
        _loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _loadingText.fontSize = 32;
        _loadingText.color = Color.white;
        _loadingText.alignment = TextAnchor.MiddleCenter;
        _loadingText.raycastTarget = false;
        loadingObj.SetActive(false);
    }

    public void LoadScene(string sceneName)
    {
        if (!_isTransitioning)
            StartCoroutine(TransitionToScene(sceneName));
    }

    public void LoadScene(string sceneName, LoadSceneMode mode)
    {
        if (!_isTransitioning)
            StartCoroutine(TransitionToScene(sceneName, mode));
    }

    private IEnumerator TransitionToScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        _isTransitioning = true;

        yield return FadeOut();
        yield return new WaitForSeconds(0.05f);

        ShowLoadingIndicator();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);
        asyncLoad.allowSceneActivation = false;

        while (asyncLoad.progress < 0.9f)
            yield return null;

        HideLoadingIndicator();

        asyncLoad.allowSceneActivation = true;

        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.05f);

        yield return FadeIn();
        _isTransitioning = false;
    }

    private void ShowLoadingIndicator()
    {
        if (_loadingText != null)
        {
            _loadingText.gameObject.SetActive(true);
            _dotsCoroutine = StartCoroutine(AnimateDots());
        }
    }

    private void HideLoadingIndicator()
    {
        if (_dotsCoroutine != null)
        {
            StopCoroutine(_dotsCoroutine);
            _dotsCoroutine = null;
        }

        if (_loadingText != null)
            _loadingText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateDots()
    {
        string[] frames = { "Cargando", "Cargando.", "Cargando..", "Cargando..." };
        int index = 0;
        while (true)
        {
            _loadingText.text = frames[index % frames.Length];
            index++;
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    public Coroutine FadeOut()
    {
        return StartCoroutine(FadeCoroutine(FadeMaxAlpha));
    }

    public Coroutine FadeIn()
    {
        return StartCoroutine(FadeCoroutine(0f));
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        if (_fadeImage == null) yield break;

        _fadeImage.raycastTarget = targetAlpha > 0f;

        yield return _fadeImage.DOFade(targetAlpha, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .WaitForCompletion();

        if (targetAlpha == 0f)
            _fadeImage.raycastTarget = false;
    }

    public bool IsTransitioning() => _isTransitioning;

    public void SetFadeDuration(float duration)
    {
        fadeDuration = Mathf.Max(0.1f, duration);
    }

    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (_fadeImage != null)
        {
            Color currentColor = _fadeImage.color;
            _fadeImage.color = new Color(color.r, color.g, color.b, currentColor.a);
        }
    }
}
