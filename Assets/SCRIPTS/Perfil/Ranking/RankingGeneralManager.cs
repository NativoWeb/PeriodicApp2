using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;

public class RankingGeneralManager : BaseRankingManager
{
    [Header("General Configuration")]
    [SerializeField] private Color colorBotonSeleccionado = new Color(0.0f, 0.4f, 0.0f);
    [SerializeField] private Color colorBotonNormal = Color.white;
    [SerializeField] private RectTransform rankingContentGeneral; // Añadido

    private ScrollToUser scrollToUser;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;
    private RankingMode currentMode; // Añadido

    // instanciamos el PanelRanking
    [SerializeField] public GameObject RankingPanel = null;

    // Añade estas variables al inicio de la clase
    private bool firstLoadCompleted = false;
    private int pendingUserPosition = -1;


    protected override void Start()
    {
        base.Start();
        currentMode = RankingMode.General; // Inicializar el modo

        scrollToUser = FindFirstObjectByType<ScrollToUser>();

        if (panel != null)
        {
            panel.SetActive(true);
            MarkButtonAsSelected(true);
            StartCoroutine(InitialLoadSequence());
        }

        if (associatedButton != null)
        {
            associatedButton.onClick.AddListener(() =>
            {
                if (!panel.activeSelf)
                {
                    RankingStateManager.Instance.SwitchToGeneral();
                }
            });
        }
    }

    // Nuevo método para secuencia de carga inicial
    private IEnumerator InitialLoadSequence()
    {
        // Notificar al ScrollToUser qué content usar
        if (scrollToUser != null && rankingContentGeneral != null)
        {
            scrollToUser.SetActiveContent(rankingContentGeneral);
        }

        yield return StartCoroutine(ObtenerRankingCoroutine());

        // Esperar múltiples frames para asegurar la actualización del layout
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        Canvas.ForceUpdateCanvases();

        if (scrollToUser != null)
        {
            scrollToUser.SetContentReady(RankingMode.General);
        }

        firstLoadCompleted = true;
    }



    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            RankingPanel.SetActive(true);
            ObtenerRanking();
        }
    }

    public void DesactivarRanking()
    {
        if (RankingPanel != null)
        {
            RankingPanel.SetActive(false);
        }
        if (estaActualizando)
        {
            estaActualizando = false;
            StopCoroutine(rankingCoroutine);
        }
    }

    public override void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        bool shouldActivate = (newMode == RankingMode.General);
        currentMode = newMode; // Actualizar el modo actual

        if (panel != null)
        {
            panel.SetActive(shouldActivate);
        }

        MarkButtonAsSelected(shouldActivate);

        if (shouldActivate && panel.activeSelf)
        {
            Debug.Log("Cargando ranking general...");
            if (!firstLoadCompleted)
            {
                StartCoroutine(InitialLoadSequence());
            }
            else
            {
                ObtenerRanking();
            }
        }
    }
    private void ObtenerRanking()
    {
        if (estaActualizando)
        {
            if (rankingCoroutine != null)
            {
                StopCoroutine(rankingCoroutine);
            }
        }

        rankingCoroutine = StartCoroutine(ObtenerRankingCoroutine());
    }

    private IEnumerator ObtenerRankingCoroutine()
    {
        estaActualizando = true;
        ClearRanking();

        var task = FirebaseFirestore.DefaultInstance.Collection("users")
                   .OrderByDescending("xp")
                   .Limit(1000)
                   .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsCompleted)
        {
            List<(string id, string nombre, int xp)> listaJugadores = new List<(string, string, int)>();

            foreach (DocumentSnapshot document in task.Result.Documents)
            {
                string nombre = document.GetValue<string>("DisplayName");
                int xp = document.GetValue<int>("xp");
                string id = document.Id;
                listaJugadores.Add((id, nombre, xp));
            }

            UpdatePodio(listaJugadores);

            for (int i = 3; i < listaJugadores.Count; i++)
            {
                bool highlight = listaJugadores[i].id == currentUserId;
                CreateRankingElement(i + 1, listaJugadores[i].nombre, listaJugadores[i].xp, highlight);
            }

            int userPosition = listaJugadores.FindIndex(j => j.id == currentUserId) + 1;
            pendingUserPosition = userPosition; // Guardamos la posición para usarla después

            if (firstLoadCompleted && scrollToUser != null)
            {
                scrollToUser.UpdateUserPosition(userPosition);
                scrollToUser.ScrollToUserPosition();
            }
        }

        estaActualizando = false;
    }

    private void MarkButtonAsSelected(bool selected)
    {
        if (associatedButton != null)
        {
            Image buttonImage = associatedButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = selected ? colorBotonSeleccionado : colorBotonNormal;
            }

            TextMeshProUGUI buttonText = associatedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private IEnumerator ScrollAfterUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        scrollToUser?.ScrollToUserPosition();
    }
}