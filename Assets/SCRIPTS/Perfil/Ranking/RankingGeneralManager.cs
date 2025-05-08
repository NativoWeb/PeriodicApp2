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

    private ScrollToUser scrollToUser;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;


    // instanciamos el PanelRanking
    [SerializeField] public GameObject RankingPanel = null;

    // Añade estas variables al inicio de la clase
    private bool firstLoadCompleted = false;
    private int pendingUserPosition = -1;

    
    protected override void Start()
    {
        base.Start();

        scrollToUser = FindFirstObjectByType<ScrollToUser>();

        if (panel != null)
        {
            panel.SetActive(true);
            MarkButtonAsSelected(true);

            // Cargar datos pero retrasar el scroll hasta que todo esté listo
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
        // Paso 1: Cargar datos del ranking
        yield return StartCoroutine(ObtenerRankingCoroutine());

        // Paso 2: Esperar a que el layout se actualice completamente
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // Paso 3: Ejecutar scroll si tenemos una posición pendiente
        if (pendingUserPosition != -1 && scrollToUser != null)
        {
            scrollToUser.UpdateUserPosition(pendingUserPosition);
            yield return new WaitForEndOfFrame(); // Esperar un frame más
            scrollToUser.ScrollToUserPosition();
        }

        firstLoadCompleted = true;
    }



    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if ( estadouser == "nube")
        {
                RankingPanel.SetActive(true);
                ObtenerRanking();
                
        }
        else
        {
            return;
        }
    }

    public void DesactivarRanking()
    {
        if( RankingPanel != null)
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

        if (panel != null)
        {
            panel.SetActive(shouldActivate);
        }

        MarkButtonAsSelected(shouldActivate);

        if (shouldActivate && panel.activeSelf)
        {
            Debug.Log("Cargando ranking general...");
            ObtenerRanking();
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