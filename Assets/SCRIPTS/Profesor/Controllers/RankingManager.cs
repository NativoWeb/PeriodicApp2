using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;

public class RankingManager : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    public Sprite[] medallas;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    

    [SerializeField] private GameObject RankingPanel = null;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
       
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

                      CrearElementoRanking(posicion, nombre, xp);
                      posicion++;
                  }
              }
          });
    }

    void CrearElementoRanking(int posicion, string nombre, int xp)
    {
        GameObject jugadorUI = Instantiate(prefabJugador, content);
        TMP_Text nombreTMP = jugadorUI.transform.Find("Nombre").GetComponent<TMP_Text>();
        TMP_Text xpTMP = jugadorUI.transform.Find("XP").GetComponent<TMP_Text>();
        TMP_Text posicionTMP = jugadorUI.transform.Find("Posicion").GetComponent<TMP_Text>();
        Image medallaImg = jugadorUI.transform.Find("Medalla").GetComponent<Image>();

        nombreTMP.text = nombre;
        xpTMP.text = "EXP \n" + xp;

        if (posicion <= 3)
        {
            medallaImg.sprite = medallas[posicion - 1];
            medallaImg.gameObject.SetActive(true);
            posicionTMP.gameObject.SetActive(false);
        }
        else
        {
            posicionTMP.text = posicion.ToString();
            medallaImg.gameObject.SetActive(false);
        }
    }
}

