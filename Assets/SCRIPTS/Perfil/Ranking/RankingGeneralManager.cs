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

public class RankingGeneralManager: MonoBehaviour
{
    // Referencia al script RankingAmigosManager
    [SerializeField] private RankingAmigosManager rankingAmigosManager;

    // Colores para los estados de bot�n
    [SerializeField] private Color colorBotonSeleccionado = new Color(0.0f, 0.4f, 0.0f);
    [SerializeField] private Color colorBotonNormal = Color.white;

    public GameObject prefabJugador;
    public Transform content;
    public GameObject panelRankignGeneral;
    FirebaseFirestore db;
    private Coroutine rankingCoroutine;
    private bool estaActualizando = false;

    [SerializeField] private GameObject RankingPanel = null;

    // Referencia al bot�n general que ya existe en el panel
    [SerializeField] private Button botonGeneral = null;

    // Referencia al script ScrollToUser para coordinar las actualizaciones
    [SerializeField] private ScrollToUser scrollToUser;


    // instanciar btn y panel amigos para desactivar
    public Button BtnRankingAmigos;
    public GameObject panelRankingAmigos;
    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        MostrarPanel();

        // Buscar la referencia a ScrollToUser si no est� asignada
        if (scrollToUser == null)
        {
            scrollToUser = FindFirstObjectByType<ScrollToUser>();
        }

        // Buscar la referencia a RankingAmigosManager si no est� asignada
        if (rankingAmigosManager == null)
        {
            rankingAmigosManager = FindFirstObjectByType<RankingAmigosManager>();
        }

        // Configurar listener del bot�n general una sola vez al inicio
        if (botonGeneral != null)
        {
            // Eliminar listeners existentes para evitar duplicados
            botonGeneral.onClick.RemoveAllListeners();
            // A�adir nuestro listener
            botonGeneral.onClick.AddListener(OnBotonGeneralClick);
        }

        // Configurar listener para el bot�n de amigos si tenemos la referencia
        if (BtnRankingAmigos != null)
        {
            BtnRankingAmigos.onClick.RemoveAllListeners();
            BtnRankingAmigos.onClick.AddListener(OnBotonAmigosClick);
        }
    }
    private void OnBotonGeneralClick()
    {
        // Marcar este bot�n como seleccionado visualmente
        MarcarBotonSeleccionado(botonGeneral);

        // Desmarcar el bot�n de amigos si existe
        if (BtnRankingAmigos != null)
        {
            DesmarcarBoton(BtnRankingAmigos);
        }

        panelRankingAmigos.SetActive(false);
        panelRankignGeneral.SetActive(true);

        // Ejecutar la l�gica del ranking general
        ObtenerRanking();

        // Si tenemos referencia al ScrollToUser, asegurarnos de actualizar la UI y cambiar al modo general
        if (scrollToUser != null)
        {
            // Cambiar al modo ranking general
            scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.General);

            // Asegurarnos de que la UI se actualice
            scrollToUser.ActualizarUISegunModo();

            // Llamar al m�todo para actualizar el contenido del ranking general
            scrollToUser.ActualizarContenidoRankingGeneral();

            // Hacer scroll a la posici�n del usuario despu�s de que se haya actualizado el contenido
            StartCoroutine(HacerScrollDespuesDeActualizar());
        }
    }
    private void OnBotonAmigosClick()
    {
        // Marcar este bot�n como seleccionado visualmente
        MarcarBotonSeleccionado(BtnRankingAmigos);

        // Desmarcar el bot�n general
        DesmarcarBoton(botonGeneral);

        // Activar el panel de amigos y desactivar el general
        panelRankingAmigos.SetActive(true);
        panelRankignGeneral.SetActive(false);

        // Llamar al m�todo para obtener el ranking de amigos
        if (rankingAmigosManager != null)
        {
            rankingAmigosManager.ObtenerRankingAmigos();
        }

        // Si tenemos referencia al ScrollToUser, podemos actualizar el modo
        if (scrollToUser != null)
        {
            scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.Amigos);
            scrollToUser.ActualizarUISegunModo();
        }

    }
    private void SeleccionarBotonGeneral()
    {
        if (botonGeneral != null)
        {
            // Establecer el bot�n como elemento seleccionado en el sistema de eventos de Unity
            EventSystem.current.SetSelectedGameObject(botonGeneral.gameObject);

            // Marcar visualmente el bot�n como seleccionado
            MarcarBotonSeleccionado(botonGeneral);

            if (BtnRankingAmigos != null)
            {
                DesmarcarBoton(BtnRankingAmigos);
            }

            // Simular un clic para ejecutar la l�gica del bot�n
            botonGeneral.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n general en el Inspector");
        }
    }

    // M�todo para marcar un bot�n como seleccionado visualmente
    public void MarcarBotonSeleccionado(Button boton)
    {
        if (boton != null)
        {
            // Cambiar el color del bot�n
            Image imagenBoton = boton.GetComponent<Image>();
            if (imagenBoton != null)
            {
                imagenBoton.color = colorBotonSeleccionado;
            }

            // Tambi�n puedes cambiar el texto del bot�n si tiene
            TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
            if (textoBoton != null)
            {
                textoBoton.fontStyle = FontStyles.Bold;
            }
        }
    }

    // M�todo para desmarcar un bot�n
    public void DesmarcarBoton(Button boton)
    {
        if (boton != null)
        {
            // Restaurar el color normal del bot�n
            Image imagenBoton = boton.GetComponent<Image>();
            if (imagenBoton != null)
            {
                imagenBoton.color = colorBotonNormal;
            }

            // Restaurar el texto normal
            TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
            if (textoBoton != null)
            {
                textoBoton.fontStyle = FontStyles.Normal;
            }
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

            // Seleccionar el bot�n general por defecto
            SeleccionarBotonGeneral();

            ObtenerRanking();
        }
        else
        {
            return;
        }
    }

    //private void SeleccionarBotonGeneral()
    //{
    //    if (botonGeneral != null)
    //    {
    //        // Establecer el bot�n como elemento seleccionado en el sistema de eventos de Unity
    //        EventSystem.current.SetSelectedGameObject(botonGeneral.gameObject);

    //        // Simular un clic para ejecutar la l�gica del bot�n
    //        botonGeneral.onClick.Invoke();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("No se ha asignado el bot�n general en el Inspector");
    //    }
    //}

    //private void OnBotonGeneralClick()
    //{

    //    panelRankingAmigos.SetActive(false);
    //    panelRankignGeneral.SetActive(true);

    //    // Ejecutar la l�gica del ranking general
    //    ObtenerRanking();

    //    // Si tenemos referencia al ScrollToUser, asegurarnos de actualizar la UI y cambiar al modo general
    //    if (scrollToUser != null)
    //    {
    //        // Cambiar al modo ranking general
    //        scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.General);

    //        // Asegurarnos de que la UI se actualice
    //        scrollToUser.ActualizarUISegunModo();

    //        // Llamar al m�todo para actualizar el contenido del ranking general
    //        scrollToUser.ActualizarContenidoRankingGeneral();

    //        // Hacer scroll a la posici�n del usuario despu�s de que se haya actualizado el contenido
    //        StartCoroutine(HacerScrollDespuesDeActualizar());
    //    }
    //}

    private IEnumerator HacerScrollDespuesDeActualizar()
    {
        // Esperar un momento para que se actualice el contenido
        yield return new WaitForSeconds(0.5f);

        // Hacer scroll a la posici�n del usuario
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

                  // Agregar jugadores a la lista desde la posici�n 4 en adelante
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