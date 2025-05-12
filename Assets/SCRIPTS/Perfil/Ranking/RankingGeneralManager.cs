using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RankingGeneralManager : BaseRankingManager
{
    [Header("General Configuration")]
    [SerializeField] private Color colorBotonSeleccionado = new Color(0.0f, 0.4f, 0.0f);
    [SerializeField] private Color colorBotonNormal = Color.white;
    [SerializeField] private RectTransform rankingContentGeneral;

    private ScrollToUser scrollToUser;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;
    private RankingMode currentMode;

    // instanciamos el PanelRanking
    [SerializeField] public GameObject RankingPanel = null;

    // Variables para control de carga
    private bool firstLoadCompleted = false;
    private int pendingUserPosition = -1;
    private bool layoutUpdated = false;
    private const float LAYOUT_UPDATE_DELAY = 0.5f;

    
    protected override void Start()
    {
        base.Start();
        currentMode = RankingMode.General; // Inicializar el modo

        // Buscar el ScrollToUser al inicio
        scrollToUser = FindFirstObjectByType<ScrollToUser>();
        if (scrollToUser == null)
        {
            Debug.LogError("ScrollToUser no encontrado en la escena!");
        }

        if (panel != null)
        {
            panel.SetActive(true);
            MarkButtonAsSelected(true);

            // Iniciar la carga con una pequeña demora para permitir que otros componentes se inicialicen
            StartCoroutine(DelayedInitialLoad(0.2f));
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

  

    private IEnumerator DelayedInitialLoad(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartCoroutine(InitialLoadSequence());
    }

    // Secuencia de carga inicial mejorada
    private IEnumerator InitialLoadSequence()
    {
        Debug.Log("Iniciando secuencia de carga inicial del ranking general");

        // Notificar al ScrollToUser qué content usar
        if (scrollToUser != null && rankingContentGeneral != null)
        {
            Debug.Log("Configurando RankingContentGeneral en ScrollToUser");
            scrollToUser.SetActiveContent(rankingContentGeneral);
        }
        else
        {
            Debug.LogWarning("ScrollToUser o rankingContentGeneral es null!");
        }

        // Obtener datos del ranking
        yield return StartCoroutine(ObtenerRankingCoroutine());

        // Esperar para asegurar que el layout se actualice
        yield return StartCoroutine(EnsureLayoutUpdate());

        // Notificar que el contenido está listo
        if (scrollToUser != null)
        {
            Debug.Log("Notificando que el contenido está listo. Posición del usuario: " + pendingUserPosition);
            scrollToUser.UpdateUserPosition(pendingUserPosition);
            scrollToUser.SetContentReady(RankingMode.General);
        }

        firstLoadCompleted = true;
        Debug.Log("Carga inicial del ranking general completada");
    }

    // Método para asegurar que el layout se actualice correctamente
    private IEnumerator EnsureLayoutUpdate()
    {
        Debug.Log("Esperando actualización del layout");
        layoutUpdated = false;

        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        // Espera adicional para garantizar que el layout se actualice completamente
        yield return new WaitForSeconds(LAYOUT_UPDATE_DELAY);

        layoutUpdated = true;
        Debug.Log("Layout actualizado correctamente");
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
        string ocupacion = PlayerPrefs.GetString("TempOcupacion", "");

        if (RankingPanel != null)
        {
            if (ocupacion == "Profesor")
            {
                SceneManager.LoadScene("InicioProfesor");
                RankingPanel.SetActive(false);
            }
            else
            {
                RankingPanel.SetActive(false);
            }
        }
        if (estaActualizando)
        {
            estaActualizando = false;
            if (rankingCoroutine != null)
            {
                StopCoroutine(rankingCoroutine);
            }
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

        if (shouldActivate && panel != null && panel.activeSelf)
        {
            Debug.Log("Cargando ranking general debido a cambio de estado");
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

        Debug.Log("Obteniendo datos de ranking desde Firestore...");
        var task = FirebaseFirestore.DefaultInstance.Collection("users")
                   .OrderByDescending("xp")
                   .Limit(1000)
                   .GetSnapshotAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError("Error al obtener datos de ranking: " + task.Exception.Message);
            estaActualizando = false;
            yield break;
        }

        if (task.IsCompleted)
        {
            Debug.Log("Datos de ranking obtenidos. Procesando...");
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
            Debug.Log("Posición del usuario encontrada: " + userPosition);

            if (firstLoadCompleted && scrollToUser != null)
            {
                Debug.Log("Actualizando posición del usuario en ScrollToUser");
                scrollToUser.UpdateUserPosition(userPosition);

                // Esperar a que el layout se actualice antes de desplazarse
                StartCoroutine(ScrollAfterFullUpdate());
            }
        }

        estaActualizando = false;
    }

    private IEnumerator ScrollAfterFullUpdate()
    {
        // Esperar a que el layout se actualice
        yield return StartCoroutine(EnsureLayoutUpdate());

        // Luego desplazarse a la posición del usuario
        if (scrollToUser != null)
        {
            Debug.Log("Ejecutando desplazamiento al usuario");
            scrollToUser.ScrollToUserPosition();
        }
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
}