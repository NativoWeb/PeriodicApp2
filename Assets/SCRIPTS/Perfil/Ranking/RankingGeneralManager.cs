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

    protected override void Start()
    {
        base.Start();

        scrollToUser = FindFirstObjectByType<ScrollToUser>();

        // Forzar carga inicial
        if (panel != null)
        {
            panel.SetActive(true);
            ObtenerRanking();
            MarkButtonAsSelected(true);
            scrollToUser?.ScrollToUserPosition();
        }

        // Configurar botón
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


    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if ( estadouser == "nube")
        {
                RankingPanel.SetActive(true);
                ObtenerRanking();
                scrollToUser.ScrollToUserPosition();
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

        // Activar/desactivar el panel
        if (panel != null)
        {
            panel.SetActive(shouldActivate);
        }

        // Resaltar el botón correspondiente
        MarkButtonAsSelected(shouldActivate);

        // Solo cargar datos si estamos en modo General y el panel está activo
        if (shouldActivate && panel.activeSelf)
        {
            Debug.Log("Cargando ranking general...");
            ObtenerRanking();

            // Scroll al usuario después de una pequeña espera
            if (scrollToUser != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(ScrollAfterUpdate());
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
            scrollToUser?.UpdateUserPosition(userPosition);

            // Forzar scroll al usuario después de actualizar
            if (scrollToUser != null)
            {
                yield return new WaitForSeconds(0.1f); // Pequeño delay
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