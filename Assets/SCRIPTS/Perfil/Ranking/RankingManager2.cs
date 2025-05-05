using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class RankingManager2 : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    [SerializeField] private GameObject RankingPanel = null;

    // Referencia al botón general que ya existe en el panel
    [SerializeField] private Button botonGeneral = null;

    // Referencia al script ScrollToUser para coordinar las actualizaciones
    [SerializeField] private ScrollToUser scrollToUser;

    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        MostrarPanel();

        // Buscar la referencia a ScrollToUser si no está asignada
        if (scrollToUser == null)
        {
            scrollToUser = FindFirstObjectByType<ScrollToUser>();
        }
        
        // Configurar listener del botón general una sola vez al inicio
        if (botonGeneral != null)
        {
            // Eliminar listeners existentes para evitar duplicados
            botonGeneral.onClick.RemoveAllListeners();
            // Añadir nuestro listener
            botonGeneral.onClick.AddListener(OnBotonGeneralClick);
        }
    }

    private void MostrarPanel()
    {
        string escenaguardada = PlayerPrefs.GetString("PanelRanking");

        if (escenaguardada == "PanelRanking")
        {
            ActivarRanking();
            PlayerPrefs.SetString("PanelRanking", "");
        }
        else
        {
            DesactivarRanking();
        }
    }

    public void ActivarRanking()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            RankingPanel.SetActive(true);

            // Seleccionar el botón general por defecto
            SeleccionarBotonGeneral();

            ObtenerRanking();
        }
        else
        {
            return;
        }
    }

    private void SeleccionarBotonGeneral()
    {
        if (botonGeneral != null)
        {
            // Establecer el botón como elemento seleccionado en el sistema de eventos de Unity
            EventSystem.current.SetSelectedGameObject(botonGeneral.gameObject);
            
            // Simular un clic para ejecutar la lógica del botón
            botonGeneral.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("No se ha asignado el botón general en el Inspector");
        }
    }

    private void OnBotonGeneralClick()
    {
        // activamos el content nuevamente
        content.gameObject.SetActive(true);

        // Ejecutar la lógica del ranking general
        ObtenerRanking();

        // Si tenemos referencia al ScrollToUser, asegurarnos de actualizar la UI y cambiar al modo general
        if (scrollToUser != null)
        {
            // Cambiar al modo ranking general
            scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.General);

            // Asegurarnos de que la UI se actualice
            scrollToUser.ActualizarUISegunModo();

            // Llamar al método para actualizar el contenido del ranking general
            scrollToUser.ActualizarContenidoRankingGeneral();

            // Hacer scroll a la posición del usuario después de que se haya actualizado el contenido
            StartCoroutine(HacerScrollDespuesDeActualizar());
        }
    }

    private IEnumerator HacerScrollDespuesDeActualizar()
    {
        // Esperar un momento para que se actualice el contenido
        yield return new WaitForSeconds(0.5f);

        // Hacer scroll a la posición del usuario
        if (scrollToUser != null)
        {
            scrollToUser.ScrollToUserPosition();
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