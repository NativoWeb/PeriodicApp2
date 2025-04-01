using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;

public class RankingManager2 : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    [SerializeField] private GameObject RankingPanel = null;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        ActivarRanking();
    }

    public void ActivarRanking()
    {
        RankingPanel.SetActive(true); // Activa el panel de ranking
        ObtenerRanking(); // Cargar ranking inmediatamente

        if (!estaActualizando)
        {
            estaActualizando = true;
            rankingCoroutine = StartCoroutine(ActualizarRankingCada2Min());
        }
    }

    public void DesactivarRanking()
    {
        RankingPanel.SetActive(false); // Desactiva el panel de ranking

        if (estaActualizando)
        {
            estaActualizando = false;
            StopCoroutine(rankingCoroutine); // Detiene la actualización
        }
    }

    IEnumerator ActualizarRankingCada2Min()
    {
        while (estaActualizando)
        {
            yield return new WaitForSeconds(120f);
            ObtenerRanking();
        }
    }

    public void ObtenerRanking()
    {
        string usuarioActual = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.DisplayName;

        db.Collection("users")
          .OrderByDescending("xp")
          .Limit(1000)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsCompleted)
              {
                  foreach (Transform child in content)
                  {
                      Destroy(child.gameObject);
                  }

                  int posicion = 1;
                  foreach (DocumentSnapshot document in task.Result.Documents)
                  {
                      string nombre = document.GetValue<string>("DisplayName");
                      int xp = document.GetValue<int>("xp");

                      // Mostrar solo desde la posición 4 en adelante
                      if (posicion >= 4)
                      {
                          GameObject jugadorUI = CrearElementoRanking(posicion, nombre, xp);

                          if (nombre == usuarioActual)
                          {
                              ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                              jugadorUI.GetComponent<Image>().color = customColor; // Resalta el jugador actual
                          }
                      }

                      posicion++;
                  }
              }
          });
    }


    GameObject CrearElementoRanking(int posicion, string nombre, int xp)
    {
        GameObject jugadorUI = Instantiate(prefabJugador, content);
        TMP_Text nombreTMP = jugadorUI.transform.Find("Nombre").GetComponent<TMP_Text>();
        TMP_Text xpTMP = jugadorUI.transform.Find("XP").GetComponent<TMP_Text>();
        TMP_Text posicionTMP = jugadorUI.transform.Find("Posicion").GetComponent<TMP_Text>();

        nombreTMP.text = nombre;
        xpTMP.text = "EXP \n" + xp;
        posicionTMP.text = "#" + posicion.ToString();

        return jugadorUI;
    }
}
