using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using System.Linq;

public class RankingComunidadesManager : BaseRankingManager
{
    [Header("Communities Configuration")]
    [SerializeField] private TMP_Dropdown comunidadesDropdown;
    [SerializeField] private Image dropdownBackground;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorSeleccionado = new Color(0.8f, 1f, 0.8f);

    private Dictionary<string, string> comunidadesDict = new Dictionary<string, string>();
    private ScrollToUser scrollToUser;
    private bool isUpdatingRanking = false;
    private string comunidadSeleccionadaID;

    protected override void Start()
    {
        base.Start();

        scrollToUser = FindFirstObjectByType<ScrollToUser>();

        if (associatedButton != null)
        {
            associatedButton.onClick.AddListener(() =>
            {
                if (!panel.activeSelf)
                {
                    RankingStateManager.Instance.SwitchToComunidades();
                }
            });
        }

        comunidadesDropdown.onValueChanged.AddListener(OnComunidadSeleccionada);
        ConfigurarDropdown();
    }

    public override void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        if (newMode == RankingMode.Comunidades)
        {
            if (!panel.activeSelf)
            {
                panel.SetActive(true);
                if (!string.IsNullOrEmpty(comunidadId))
                {
                    ObtenerRankingComunidad(comunidadId);
                }
            }
        }
        else if (panel.activeSelf)
        {
            panel.SetActive(false);
            ResetDropdownAppearance();
        }
    }

    private void OnComunidadSeleccionada(int index)
    {
        if (isUpdatingRanking) return;

        if (index > 0)
        {
            string nombreComunidad = comunidadesDropdown.options[index].text;
            if (comunidadesDict.TryGetValue(nombreComunidad, out string comunidadId))
            {
                comunidadSeleccionadaID = comunidadId;
                HighlightDropdown();
                RankingStateManager.Instance.SwitchToComunidades(comunidadId);
            }
        }
        else
        {
            ClearRanking();
            ResetDropdownAppearance();
            RankingStateManager.Instance.SwitchToGeneral();
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
                }
                else
                {
                    comunidadesDropdown.ClearOptions();
                    comunidadesDropdown.AddOptions(new List<string> { "No perteneces a ninguna comunidad" });
                }

                comunidadesDropdown.value = 0;
                comunidadesDropdown.RefreshShownValue();
            }
        });
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
        if (dropdownBackground != null)
        {
            dropdownBackground.color = colorSeleccionado;
        }
    }

    private void ResetDropdownAppearance()
    {
        if (dropdownBackground != null)
        {
            dropdownBackground.color = colorNormal;
        }
    }
}