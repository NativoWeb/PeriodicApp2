using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using System.Linq;
using UnityEngine.SceneManagement;


public class RankingComunidadesManager : BaseRankingManager
{
    [Header("Communities Configuration")]
    [SerializeField] private TMP_Dropdown comunidadesDropdown;
    [SerializeField] private Image dropdownBackground;
    

    private Dictionary<string, string> comunidadesDict = new Dictionary<string, string>();
    private ScrollToUser scrollToUser;
    private bool isUpdatingRanking = false;
    private string comunidadSeleccionadaID;

    [Header("Dropdown Styling")]
    [SerializeField] private Color textColorNormal = Color.black;
    [SerializeField] private Color textColorSelected = new Color(0f, 0.4f, 0f);
    [SerializeField] private TMP_Text dropdownLabel;

    [Header("Mover Panel Seleccionar")]
    public RectTransform PanelSeleccionar;
    public float nuevaPosY = -329f;
    public float anteriorPosY = -429f;
    public float duracion = 1f;

    [Header("panel sugerir unirse a comunidad")]
    [SerializeField] public GameObject panelSinComunidades;
    protected override void Start()
    {
        base.Start();
        scrollToUser = FindFirstObjectByType<ScrollToUser>();

        // Desactivar el dropdown al inicio
        if (comunidadesDropdown != null)
        {
            comunidadesDropdown.gameObject.SetActive(false);
        }

        if (associatedButton != null)
        {
            associatedButton.onClick.AddListener(() =>
            {
                if (!panel.activeSelf)
                {
                    RankingStateManager.Instance.SwitchToComunidades();
                    ClearRanking();
                    // Activar el dropdown cuando se hace clic en el botón
                    if (comunidadesDropdown != null)
                    {
                        comunidadesDropdown.gameObject.SetActive(true);
                        StartCoroutine(MoverSuavemente());

                    }
                }
            });
        }

        comunidadesDropdown.onValueChanged.AddListener(OnComunidadSeleccionada);
        
        ConfigurarDropdown();
    }
    // para mover el panelSeleccionar y darle paso al buscador 
    IEnumerator MoverSuavemente()
    {
        Vector2 posInicial = PanelSeleccionar.anchoredPosition;
        Vector2 posFinal = new Vector2(posInicial.x, nuevaPosY);
        float tiempo = 0;

        while (tiempo < duracion)
        {
            PanelSeleccionar.anchoredPosition = Vector2.Lerp(posInicial, posFinal, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }

        PanelSeleccionar.anchoredPosition = posFinal; // Asegura que termine exactamente en el punto final
    }
    IEnumerator DeVoverSuavemente()
    {
        Vector2 posInicial = PanelSeleccionar.anchoredPosition;
        Vector2 posFinal = new Vector2(posInicial.x, anteriorPosY);
        float tiempo = 0;

        while (tiempo < duracion)
        {
            PanelSeleccionar.anchoredPosition = Vector2.Lerp(posInicial, posFinal, tiempo / duracion);
            tiempo += Time.deltaTime;
            yield return null;
        }

        PanelSeleccionar.anchoredPosition = posFinal; // Asegura que termine exactamente en el punto final
    }


    public override void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        if (newMode == RankingMode.Comunidades)
        {
            panel.SetActive(true);
            MarkButtonAsSelected(true);

            // Resaltar dropdown si ya hay comunidad seleccionada
            if (!string.IsNullOrEmpty(comunidadSeleccionadaID))
            {
                HighlightDropdown();
            }
            else
            {
                ResetDropdownAppearance();
            }
        }
        else
        {
            panel.SetActive(false);
            MarkButtonAsSelected(false);

            // Resetear dropdown cuando se cambia a General/Amigos
            ResetDropdownToDefault();
            // Desactivar el dropdown al cambiar de modo
            if (comunidadesDropdown != null)
            {
                comunidadesDropdown.gameObject.SetActive(false);
                StartCoroutine(DeVoverSuavemente());
            }
        }
    }

    // Nuevo método para resetear el dropdown
    private void ResetDropdownToDefault()
    {
        if (comunidadesDropdown != null)
        {
            comunidadesDropdown.value = 0;
            comunidadesDropdown.RefreshShownValue();
            ResetDropdownAppearance();
        }
    }

    private void MarkButtonAsSelected(bool selected)
    {
        if (associatedButton != null)
        {
            TextMeshProUGUI buttonText = associatedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void OnComunidadSeleccionada(int index)
    {
        if (isUpdatingRanking) return;

        if (index > 0) // Comunidad seleccionada
        {
            string nombreComunidad = comunidadesDropdown.options[index].text;
            if (comunidadesDict.TryGetValue(nombreComunidad, out string comunidadId))
            {
                comunidadSeleccionadaID = comunidadId;
                HighlightDropdown();
                RankingStateManager.Instance.SwitchToComunidades(comunidadId);
                ObtenerRankingComunidad(comunidadId);
            }
        }
        else // "Selecciona una comunidad"
        {
            comunidadSeleccionadaID = null;
            ResetDropdownAppearance();
            ClearRanking();
        }
    }

    private void ConfigurarDropdown()
    {
        comunidadesDropdown.ClearOptions();
        comunidadesDict.Clear();
        List<string> opciones = new List<string> { "Selecciona una comunidad" };
        comunidadesDropdown.AddOptions(opciones);
        CargarComunidades();
    }

    private void CargarComunidades()
    {
        FirebaseFirestore.DefaultInstance.Collection("comunidades").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                List<string> opcionesComunidades = new List<string>();

                foreach (DocumentSnapshot document in task.Result.Documents)
                {
                    if (document.TryGetValue("miembros", out object miembrosObj) && IsUserMember(miembrosObj))
                    {
                        string nombreComunidad = document.GetValue<string>("nombre") ?? document.Id;
                        opcionesComunidades.Add(nombreComunidad);
                        comunidadesDict[nombreComunidad] = document.Id;
                    }
                }

                if (opcionesComunidades.Count > 0)
                {
                    comunidadesDropdown.AddOptions(opcionesComunidades);
                    if (panelSinComunidades != null)
                        panelSinComunidades.SetActive(false);
                }
                else
                {
                    comunidadesDropdown.ClearOptions();
                    comunidadesDropdown.AddOptions(new List<string> { "No perteneces a ninguna comunidad" });
                    panelSinComunidades.SetActive(true); // activar panel sugerir unirse a comunidad
                }

                comunidadesDropdown.value = 0;
                comunidadesDropdown.RefreshShownValue();
            }
        });
    }

    public void IraComunidades()
    {
        SceneManager.LoadScene("Comunidad");
    }

    private bool IsUserMember(object miembrosObj)
    {
        if (miembrosObj is List<object> miembrosList)
        {
            foreach (object miembro in miembrosList)
            {
                if (miembro is string miembroId && miembroId == currentUserId)
                {
                    return true;
                }
                else if (miembro is Dictionary<string, object> miembroDict)
                {
                    foreach (var item in miembroDict.Values)
                    {
                        if (item is string id && id == currentUserId)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private void ObtenerRankingComunidad(string comunidadID)
    {
        if (isUpdatingRanking) return;

        isUpdatingRanking = true;
        ClearRanking();

        FirebaseFirestore.DefaultInstance.Collection("comunidades").Document(comunidadID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                if (task.Result.TryGetValue("miembros", out object miembrosObj))
                {
                    List<string> idsMiembros = ExtractMemberIds(miembrosObj);
                    ObtenerDatosMiembros(idsMiembros);
                }
                else
                {
                    isUpdatingRanking = false;
                }
            }
            else
            {
                isUpdatingRanking = false;
            }
        });
    }

    private List<string> ExtractMemberIds(object miembrosObj)
    {
        List<string> ids = new List<string>();

        if (miembrosObj is List<object> miembrosList)
        {
            foreach (object miembro in miembrosList)
            {
                if (miembro is string miembroId)
                {
                    ids.Add(miembroId);
                }
                else if (miembro is Dictionary<string, object> miembroDict)
                {
                    foreach (var item in miembroDict.Values)
                    {
                        if (item is string id)
                        {
                            ids.Add(id);
                            break;
                        }
                    }
                }
            }
        }
        return ids;
    }

    private void ObtenerDatosMiembros(List<string> idsMiembros)
    {
        List<(string id, string nombre, int xp)> listaMiembros = new List<(string, string, int)>();

        if (idsMiembros.Count == 0)
        {
            MostrarRankingFinal(listaMiembros);
            isUpdatingRanking = false;
            return;
        }

        int contadorMiembros = 0;

        foreach (string idMiembro in idsMiembros)
        {
            FirebaseFirestore.DefaultInstance.Collection("users").Document(idMiembro).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                contadorMiembros++;

                if (task.IsCompleted && task.Result.Exists)
                {
                    string nombre = task.Result.GetValue<string>("DisplayName");
                    int xp = task.Result.GetValue<int>("xp");
                    listaMiembros.Add((idMiembro, nombre, xp));
                }

                if (contadorMiembros >= idsMiembros.Count)
                {
                    MostrarRankingFinal(listaMiembros);
                    isUpdatingRanking = false;
                }
            });
        }
    }

    private void MostrarRankingFinal(List<(string id, string nombre, int xp)> listaMiembros)
    {
        var listaOrdenada = listaMiembros.OrderByDescending(j => j.xp).ToList();
        UpdatePodio(listaOrdenada);

        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            bool highlight = listaOrdenada[i].id == currentUserId;
            CreateRankingElement(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp, highlight);
        }

        int userPosition = listaOrdenada.FindIndex(j => j.id == currentUserId) + 1;
        scrollToUser?.UpdateUserPosition(userPosition);
    }

    private void HighlightDropdown()
    {
        if (dropdownLabel != null)
        {
            dropdownLabel.color = textColorSelected;
        }
    }

    private void ResetDropdownAppearance()
    {
        if (dropdownLabel != null)
        {
            dropdownLabel.color = textColorNormal;
        }
    }
}