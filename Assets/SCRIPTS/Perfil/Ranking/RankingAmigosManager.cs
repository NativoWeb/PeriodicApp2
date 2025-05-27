using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebase.Extensions;

public class RankingAmigosManager : BaseRankingManager
{
    private ScrollToUser scrollToUser;
    private int usuarioActualXP;

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
                    RankingStateManager.Instance.SwitchToAmigos();
                }
            });
        }

        ObtenerXPUsuarioActual();
    }

    public override void OnRankingStateChanged(RankingMode newMode, string comunidadId)
    {
        if (newMode == RankingMode.Amigos)
        {
            if (!panel.activeSelf)
            {
                panel.SetActive(true);
                MarkButtonAsSelected(true);
                ObtenerRankingAmigos();

                if (scrollToUser != null)
                {
                    StartCoroutine(ScrollAfterUpdate());
                }
            }
        }
        else if (panel.activeSelf)
        {
            panel.SetActive(false);
            MarkButtonAsSelected(false);
        }
    }

    private void ObtenerXPUsuarioActual()
    {
        FirebaseFirestore.DefaultInstance.Collection("users").Document(currentUserId)
            .GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    if (task.Result.TryGetValue<int>("xp", out int xp))
                    {
                        usuarioActualXP = xp;
                    }
                }
            });
    }

    private void ObtenerRankingAmigos()
    {
        ClearRanking();

        // Primero obtenemos las solicitudes de amistad aceptadas donde somos remitentes
        FirebaseFirestore.DefaultInstance.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idRemitente", currentUserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    List<string> idsAmigos = new List<string>();

                    foreach (DocumentSnapshot document in task.Result.Documents)
                    {
                        string idAmigo = document.GetValue<string>("idDestinatario");
                        idsAmigos.Add(idAmigo);
                    }

                    // Luego obtenemos donde somos destinatarios
                    ObtenerSolicitudesComoDestinatario(idsAmigos);
                }
            });
    }

    private void ObtenerSolicitudesComoDestinatario(List<string> idsAmigos)
    {
        FirebaseFirestore.DefaultInstance.Collection("SolicitudesAmistad")
            .WhereEqualTo("estado", "aceptada")
            .WhereEqualTo("idDestinatario", currentUserId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    foreach (DocumentSnapshot document in task.Result.Documents)
                    {
                        string idAmigo = document.GetValue<string>("idRemitente");
                        if (!idsAmigos.Contains(idAmigo))
                        {
                            idsAmigos.Add(idAmigo);
                        }
                    }

                    ObtenerDatosAmigos(idsAmigos);
                }
            });
    }

    private void ObtenerDatosAmigos(List<string> idsAmigos)
    {
        List<(string id, string nombre, int xp)> listaJugadores = new List<(string, string, int)>();
        listaJugadores.Add((currentUserId, currentUserName, usuarioActualXP));

        if (idsAmigos.Count == 0)
        {
            MostrarRankingFinal(listaJugadores);
            return;
        }

        int contadorAmigos = 0;

        foreach (string idAmigo in idsAmigos)
        {
            FirebaseFirestore.DefaultInstance.Collection("users").Document(idAmigo)
                .GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    contadorAmigos++;

                    if (task.IsCompleted && task.Result.Exists)
                    {
                        string nombre = task.Result.GetValue<string>("DisplayName");
                        int xp = task.Result.GetValue<int>("xp");
                        listaJugadores.Add((idAmigo, nombre, xp));
                    }

                    if (contadorAmigos >= idsAmigos.Count)
                    {
                        MostrarRankingFinal(listaJugadores);
                    }
                });
        }
    }

    private void MostrarRankingFinal(List<(string id, string nombre, int xp)> listaJugadores)
    {
        var listaOrdenada = listaJugadores.OrderByDescending(j => j.xp).ToList();
        UpdatePodio(listaOrdenada);

        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            bool highlight = listaOrdenada[i].id == currentUserId;
            CreateRankingElement(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp, highlight);
        }

        int userPosition = listaOrdenada.FindIndex(j => j.id == currentUserId) + 1;
        scrollToUser?.UpdateUserPosition(userPosition);
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

    private IEnumerator ScrollAfterUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        scrollToUser?.ScrollToUserPosition();
    }
}