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
    public enum ModoRanking { General, Amigos, Comunidades }

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
    [SerializeField] private Color highlightColor = new Color(0.9f, 1f, 0.9f, 1f);
    [SerializeField] private float highlightFlashDuration = 0.3f;
    [SerializeField] private int highlightFlashCount = 3;

    private string currentUserId;
    private string currentUserName;
    private int currentUserXP;
    private int posicionGeneral;
    private int posicionAmigos;
    private int posicionComunidades;
    private ModoRanking currentMode = ModoRanking.General;

    private bool isInitialLoad = true;

    private void Start()
    {
        InitializeUserData();
        SetupButtonListeners();
        RankingStateManager.Instance.RegisterObserver(this);
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
            currentUserName = auth.CurrentUser.DisplayName;
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

    public void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        switch (newMode)
        {
            case RankingMode.General:
                currentMode = ModoRanking.General;
                SetActiveContent(rankingContentGeneral);
                break;
            case RankingMode.Amigos:
                currentMode = ModoRanking.Amigos;
                SetActiveContent(rankingContentAmigos);
                break;
            case RankingMode.Comunidades:
                currentMode = ModoRanking.Comunidades;
                SetActiveContent(rankingContentComunidades);
                break;
        }

        UpdateUserUI();

        // Solo hacer scroll automático en la carga inicial
        if (isInitialLoad)
        {
            isInitialLoad = false;
            StartCoroutine(ScrollAfterLayoutUpdate(0.7f)); // Pequeño delay para asegurar carga
        }
    }

    private void SetActiveContent(RectTransform activeContent)
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
            case ModoRanking.General:
                posicionGeneral = position;
                break;
            case ModoRanking.Amigos:
                posicionAmigos = position;
                break;
            case ModoRanking.Comunidades:
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
            case ModoRanking.General:
                currentPosition = posicionGeneral;
                break;
            case ModoRanking.Amigos:
                currentPosition = posicionAmigos;
                break;
            case ModoRanking.Comunidades:
                currentPosition = posicionComunidades;
                break;
        }

        if (nombreUsuarioText != null)
            nombreUsuarioText.text = currentUserName ?? "Usuario";

        if (xpUsuarioText != null)
            xpUsuarioText.text = $"XP: {currentUserXP}";

        if (posicionUsuarioText != null)
            posicionUsuarioText.text = $"#{currentPosition}";
    }

    public void ScrollToUserPosition()
    {
        StartCoroutine(ScrollAfterLayoutUpdate());
    }

    private IEnumerator ScrollAfterLayoutUpdate(float initialDelay = 0f)
    {
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // Esperar a que se actualice el layout
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // Encontrar el elemento del usuario
        var userElement = FindUserElementInContent();
        if (userElement == null) yield break;

        // Calcular posición de scroll (centrado)
        float normalizedPosition = CalculateCenteredScrollPosition(userElement);

        // Animación suave
        yield return StartCoroutine(SmoothScroll(normalizedPosition));

        // Efecto de resaltado
        yield return StartCoroutine(HighlightElement(userElement));
    }

    private float CalculateCenteredScrollPosition(RectTransform target)
    {
        if (content == null || target == null) return 1f;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float targetY = Mathf.Abs(target.anchoredPosition.y);
        float targetHeight = target.rect.height;

        // Calcular posición centrada
        float centeredPosition = 1 - ((targetY - (viewportHeight / 2) + (targetHeight / 2)) / (contentHeight - viewportHeight));
        return Mathf.Clamp01(centeredPosition);
    }

    private RectTransform FindUserElementInContent()
    {
        if (content == null) return null;

        // Buscar por color de resaltado primero
        foreach (RectTransform child in content)
        {
            Image img = child.GetComponent<Image>();
            if (img != null && img.color == highlightColor)
            {
                return child;
            }
        }

        // Buscar por nombre si no se encontró por color
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

    private float CalculateNormalizedScrollPosition(RectTransform target)
    {
        if (content == null || target == null) return 1f;

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float targetY = Mathf.Abs(target.anchoredPosition.y);

        // Calcular posición normalizada (0-1) donde 0 es abajo y 1 es arriba
        float normalizedPosition = 1 - (targetY / (contentHeight - viewportHeight));
        return Mathf.Clamp01(normalizedPosition);
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