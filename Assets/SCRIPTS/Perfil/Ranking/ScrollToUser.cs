using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Auth;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;

public class ScrollToUser : MonoBehaviour, IRankingObserver
{
    [Header("UI References")]
    public ScrollRect scrollRect;
    public RectTransform content;
    public TMP_Text nombreUsuarioText;
    public TMP_Text xpUsuarioText;
    public TMP_Text posicionUsuarioText;

    [Header("Content References")]
    public RectTransform rankingContentGeneral;
    public RectTransform rankingContentAmigos;
    public RectTransform rankingContentComunidades;

    [Header("Button References")]
    public Button btnRankingGeneral;
    public Button btnRankingAmigos;
    public Button btnRankingComunidades;

    [Header("Animation Settings")]
    [SerializeField] private float scrollDuration = 0.5f;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float highlightFlashDuration = 0.3f;
    [SerializeField] private int highlightFlashCount = 3;

    private string currentUserId;
    private string currentUserName;
    private int currentUserXP;
    private int posicionGeneral;
    private int posicionAmigos;
    private int posicionComunidades;
    private RankingMode currentMode = RankingMode.General; // Cambiado a RankingMode

    private bool isInitialLoad = true;

    private void Start()
    {
        InitializeUserData();
        SetupButtonListeners();
        RankingStateManager.Instance.RegisterObserver(this);
    }

    public void SetContentReady(RankingMode mode)
    {
        if (this.currentMode == mode)
        {
            StartCoroutine(ScrollAfterLayoutUpdate(0.1f));
        }
    }

    private void OnDestroy()
    {
        RankingStateManager.Instance?.UnregisterObserver(this);
    }

    private void InitializeUserData()
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser != null)
        {
            currentUserId = auth.CurrentUser.UserId;
            GetUserXP();
        }
    }

    private void GetUserXP()
    {
        FirebaseFirestore.DefaultInstance.Collection("users").Document(currentUserId)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    currentUserXP = task.Result.GetValue<int>("xp");
                    currentUserName = task.Result.GetValue<string>("DisplayName");
                    UpdateUserUI();
                }
            });
    }

    private void SetupButtonListeners()
    {
        if (btnRankingGeneral != null)
            btnRankingGeneral.onClick.AddListener(() => RankingStateManager.Instance.SwitchToGeneral());

        if (btnRankingAmigos != null)
            btnRankingAmigos.onClick.AddListener(() => RankingStateManager.Instance.SwitchToAmigos());

        if (btnRankingComunidades != null)
            btnRankingComunidades.onClick.AddListener(() => RankingStateManager.Instance.SwitchToComunidades());
    }

    // Implementación correcta de la interfaz IRankingObserver
    public void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        currentMode = newMode;

        switch (newMode)
        {
            case RankingMode.General:
                SetActiveContent(rankingContentGeneral);
                break;
            case RankingMode.Amigos:
                SetActiveContent(rankingContentAmigos);
                break;
            case RankingMode.Comunidades:
                SetActiveContent(rankingContentComunidades);
                break;
        }

        UpdateUserUI();

        if (isInitialLoad)
        {
            isInitialLoad = false;
            StartCoroutine(ScrollAfterLayoutUpdate(0.7f));
        }
    }

    public void SetActiveContent(RectTransform activeContent)
    {
        if (rankingContentGeneral != null)
            rankingContentGeneral.gameObject.SetActive(activeContent == rankingContentGeneral);
        if (rankingContentAmigos != null)
            rankingContentAmigos.gameObject.SetActive(activeContent == rankingContentAmigos);
        if (rankingContentComunidades != null)
            rankingContentComunidades.gameObject.SetActive(activeContent == rankingContentComunidades);

        content = activeContent;
        scrollRect.content = activeContent;
    }

    public void UpdateUserPosition(int position)
    {
        switch (currentMode)
        {
            case RankingMode.General:
                posicionGeneral = position;
                break;
            case RankingMode.Amigos:
                posicionAmigos = position;
                break;
            case RankingMode.Comunidades:
                posicionComunidades = position;
                break;
        }

        UpdateUserUI();
    }

    private void UpdateUserUI()
    {
        int currentPosition = 0;

        switch (currentMode)
        {
            case RankingMode.General:
                currentPosition = posicionGeneral;
                break;
            case RankingMode.Amigos:
                currentPosition = posicionAmigos;
                break;
            case RankingMode.Comunidades:
                currentPosition = posicionComunidades;
                break;
        }

        if (nombreUsuarioText != null)
            nombreUsuarioText.text = currentUserName ?? "Usuario";

        if (xpUsuarioText != null)
            xpUsuarioText.text = $"XP {currentUserXP}";

        if (posicionUsuarioText != null)
            posicionUsuarioText.text = currentPosition.ToString();
    }

    public void ScrollToUserPosition()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollAfterLayoutUpdate(0.1f));
        }
    }

    private IEnumerator ScrollAfterLayoutUpdate(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        var userElement = FindUserElementInContent();
        if (userElement == null) yield break;

        float normalizedPosition = CalculateCenteredScrollPosition(userElement);
        yield return StartCoroutine(SmoothScroll(normalizedPosition));
        yield return StartCoroutine(HighlightElement(userElement));
    }

    private float CalculateCenteredScrollPosition(RectTransform target)
    {
        if (content == null || target == null) return 1f;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float targetY = Mathf.Abs(target.anchoredPosition.y);
        float targetHeight = target.rect.height;

        float centeredPosition = 1 - ((targetY - (viewportHeight / 2) + (targetHeight / 2)) / (contentHeight - viewportHeight));
        return Mathf.Clamp01(centeredPosition);
    }

    private RectTransform FindUserElementInContent()
    {
        if (content == null) return null;

        foreach (RectTransform child in content)
        {
            Image img = child.GetComponent<Image>();
            if (img != null && img.color == highlightColor)
            {
                return child;
            }
        }

        foreach (RectTransform child in content)
        {
            TMP_Text nameText = child.GetComponentInChildren<TMP_Text>();
            if (nameText != null && nameText.text.Equals(currentUserName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private IEnumerator SmoothScroll(float targetPosition)
    {
        float startPosition = scrollRect.verticalNormalizedPosition;
        float elapsedTime = 0f;

        while (elapsedTime < scrollDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / scrollDuration);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        scrollRect.verticalNormalizedPosition = targetPosition;
    }

    private IEnumerator HighlightElement(RectTransform element)
    {
        Image img = element.GetComponent<Image>();
        if (img == null) yield break;

        Color originalColor = img.color;

        for (int i = 0; i < highlightFlashCount; i++)
        {
            img.color = highlightColor;
            yield return new WaitForSeconds(highlightFlashDuration);
            img.color = originalColor;
            yield return new WaitForSeconds(highlightFlashDuration);
        }
    }
}