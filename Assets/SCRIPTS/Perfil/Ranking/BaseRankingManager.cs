using UnityEngine;
using TMPro;
using Firebase.Firestore;
using System.Collections.Generic;
using UnityEngine.UI;
using Firebase.Extensions;

public abstract class BaseRankingManager : MonoBehaviour, IRankingObserver
{
    [Header("Base Configuration")]
    [SerializeField] protected GameObject prefabJugador;
    [SerializeField] protected Transform content;
    [SerializeField] protected GameObject panel;
    [SerializeField] protected Button associatedButton;

    [Header("Podio References")]
    [SerializeField] protected TMP_Text primeroNombre;
    [SerializeField] protected TMP_Text segundoNombre;
    [SerializeField] protected TMP_Text terceroNombre;
    [SerializeField] protected TMP_Text primeroXP;
    [SerializeField] protected TMP_Text segundoXP;
    [SerializeField] protected TMP_Text terceroXP;

    protected FirebaseFirestore db;
    protected string currentUserId;
    protected string currentUserName;
    protected int currentUserXP;

    protected virtual void Start()
    {
        InitializeFirebase();
        RegisterObserver();
        GetUserData();
    }

    protected virtual void OnDestroy()
    {
        RankingStateManager.Instance?.UnregisterObserver(this);
    }

    private void InitializeFirebase()
    {
        db = FirebaseFirestore.DefaultInstance;
    }

    private void RegisterObserver()
    {
        RankingStateManager.Instance.RegisterObserver(this);
    }

    private void GetUserData()
    {
        var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            currentUserId = user.UserId;
            currentUserName = user.DisplayName;
            GetUserXP();
        }
    }

    private void GetUserXP()
    {
        db.Collection("users").Document(currentUserId).GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    currentUserXP = task.Result.GetValue<int>("xp");
                }
            });
    }

    public abstract void OnRankingStateChanged(RankingMode newMode, string comunidadId);

    protected void ClearRanking()
    {
        if (content == null) return;

        foreach (Transform child in content)
        {
            if (child != null && child.gameObject != null)
            {
                Destroy(child.gameObject);
            }
        }

        ResetPodio();
    }

    protected void ResetPodio()
    {
        primeroNombre.text = "---";
        primeroXP.text = "0 xp";
        segundoNombre.text = "---";
        segundoXP.text = "0 xp";
        terceroNombre.text = "---";
        terceroXP.text = "0 xp";
    }

    protected GameObject CreateRankingElement(int position, string name, int xp, bool highlight = false)
    {
        if (prefabJugador == null || content == null) return null;

        GameObject playerUI = Instantiate(prefabJugador, content);

        // Método tradicional de búsqueda
        Transform nombreTransform = playerUI.transform.Find("Nombre");
        if (nombreTransform != null && nombreTransform.TryGetComponent<TMP_Text>(out var nameTMP))
            nameTMP.text = name;

        Transform xpTransform = playerUI.transform.Find("XP");
        if (xpTransform != null && xpTransform.TryGetComponent<TMP_Text>(out var xpTMP))
            xpTMP.text = $"XP\n{xp}";

        Transform posicionTransform = playerUI.transform.Find("Posicion");
        if (posicionTransform != null && posicionTransform.TryGetComponent<TMP_Text>(out var positionTMP))
            positionTMP.text = position.ToString();

        if (highlight && playerUI.TryGetComponent<Image>(out var image))
        {
            ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
            image.color = customColor;
        }

        return playerUI;
    }

    protected void UpdatePodio(List<(string id, string nombre, int xp)> players)
    {
        if (players == null || players.Count == 0)
        {
            ResetPodio();
            return;
        }

        // Usar Mathf.Min para evitar IndexOutOfRangeException
        if (players.Count > 0) UpdatePosition(primeroNombre, primeroXP, players[0]);
        if (players.Count > 1) UpdatePosition(segundoNombre, segundoXP, players[1]);
        if (players.Count > 2) UpdatePosition(terceroNombre, terceroXP, players[2]);
    }

    private void UpdatePosition(TMP_Text nameField, TMP_Text xpField, (string id, string nombre, int xp) player)
    {
        if (nameField != null) nameField.text = player.nombre;
        if (xpField != null) xpField.text = $"{player.xp} xp";
    }
}