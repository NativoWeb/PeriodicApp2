using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class RankingManager2 : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    [SerializeField] private GameObject RankingPanel = null;

    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        
    }

    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            RankingPanel.SetActive(true);
            ObtenerRanking();

        }else
        {
            return;
        }


    }

    public void DesactivarRanking()
    {
       
        RankingPanel.SetActive(false);

        if (estaActualizando)
        {
            estaActualizando = false;
            StopCoroutine(rankingCoroutine);
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

                  
                  List<(string, int)> listaJugadores = new List<(string, int)>();

                  foreach (DocumentSnapshot document in task.Result.Documents)
                  {
                      string nombre = document.GetValue<string>("DisplayName");
                      int xp = document.GetValue<int>("xp");
                      listaJugadores.Add((nombre, xp));
                  }

                  // Asignar valores al podio
                  if (listaJugadores.Count > 0)
                  {
                      primeroNombre.text = listaJugadores[0].Item1;
                      primeroXP.text = listaJugadores[0].Item2 + " xp";
                  }
                  if (listaJugadores.Count > 1)
                  {
                      segundoNombre.text = listaJugadores[1].Item1;
                      segundoXP.text = listaJugadores[1].Item2 + " xp";
                  }
                  if (listaJugadores.Count > 2)
                  {
                      terceroNombre.text = listaJugadores[2].Item1;
                      terceroXP.text = listaJugadores[2].Item2 + " xp";
                  }

                  // Agregar jugadores a la lista desde la posición 4 en adelante
                  for (int i = 3; i < listaJugadores.Count; i++)
                  {
                      GameObject jugadorUI = CrearElementoRanking(i + 1, listaJugadores[i].Item1, listaJugadores[i].Item2);
                      if (listaJugadores[i].Item1 == usuarioActual)
                      {
                          ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                          jugadorUI.GetComponent<Image>().color = customColor;
                      }
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
