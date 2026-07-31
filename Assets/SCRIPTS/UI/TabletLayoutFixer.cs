using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton persistente que ajusta el layout de todas las escenas para tablet.
/// Se auto-crea al iniciar la app. Ajusta CanvasScaler y centra contenido.
/// </summary>
public class TabletLayoutFixer : MonoBehaviour
{
    private static TabletLayoutFixer _instance;
    private const float TabletAspectThreshold = 1.5f;
    private const float MaxContentWidth = 650f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("TabletLayoutFixer");
        _instance = go.AddComponent<TabletLayoutFixer>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        AjustarEscenaActual();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AjustarEscenaActual();
    }

    private void AjustarEscenaActual()
    {
        if (!EsTablet()) return;

        var scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            if (scaler.gameObject == gameObject) continue;
            if (scaler.GetComponentInParent<SceneTransition>() != null) continue;

            AjustarCanvasScaler(scaler);
            AjustarContenidoCanvas(scaler.GetComponent<Canvas>());
        }
    }

    private void AjustarCanvasScaler(CanvasScaler scaler)
    {
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void AjustarContenidoCanvas(Canvas canvas)
    {
        if (canvas == null) return;

        foreach (Transform child in canvas.transform)
        {
            var rect = child.GetComponent<RectTransform>();
            if (rect == null) continue;

            bool isStretched = rect.anchorMin.x < 0.1f && rect.anchorMax.x > 0.9f;
            if (!isStretched) continue;

            if (child.GetComponent<MaxWidthContainer>() == null)
            {
                var container = child.gameObject.AddComponent<MaxWidthContainer>();
                container.maxWidth = MaxContentWidth;
            }
        }
    }

    private bool EsTablet()
    {
        float aspect = (float)Screen.width / Screen.height;
        if (aspect < 1f) aspect = 1f / aspect;
        return aspect < TabletAspectThreshold;
    }
}
