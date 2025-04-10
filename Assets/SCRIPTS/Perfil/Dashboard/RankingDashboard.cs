using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class RankingDashboard : MonoBehaviour
{
    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    //referenciar btn puesto del usuario 
    public TMP_Text posiciontxt;
    public TMP_Text nombretxt;
    public TMP_Text xptxt;
    public TMP_Text PosicionRanking;


    // instanciar wifi 
    private bool hayInternet = false;

    // instancias firebase
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser currentUser;
    private string userId;

    void Start()
    {
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;


        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        currentUser = auth.CurrentUser;
        if (currentUser == null)
        {
            Debug.LogWarning("No hay usuario autenticado.");
            return;
        }
        userId = currentUser.UserId;

        ObtenerRanking();
        GetUserdata();
        ObtenerPosicionUsuario();

        if (!hayInternet)
        {
            MostrarDatosOffline();
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
                      string primeronombre = listaJugadores[0].Item1;
                      PlayerPrefs.SetString("primeronombre", primeronombre);
                      
                      primeroXP.text = listaJugadores[0].Item2 + " xp";
                      string primeroxp = listaJugadores[0].Item2 + " xp";
                      PlayerPrefs.SetString("primeroxp",primeroxp);

                  }
                  if (listaJugadores.Count > 1)
                  {
                      segundoNombre.text = listaJugadores[1].Item1;
                      string segundonombre = listaJugadores[0].Item1;
                      PlayerPrefs.SetString("segundonombre", segundonombre);

                      segundoXP.text = listaJugadores[1].Item2 + " xp";
                      string segundoxp = listaJugadores[0].Item2 + " xp";
                      PlayerPrefs.SetString("segundoxp", segundoxp);

                  }
                  if (listaJugadores.Count > 2)
                  {
                      terceroNombre.text = listaJugadores[2].Item1;
                      string tercernombre = listaJugadores[0].Item1;
                      PlayerPrefs.SetString("tercernombre", tercernombre);

                      terceroXP.text = listaJugadores[2].Item2 + " xp";
                      string tercerxp = listaJugadores[0].Item2 + " xp";
                      PlayerPrefs.SetString("tercerxp", tercerxp);
                  }

              }
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError("Error al obtener el ranking: " + task.Exception);
                  return;
              }
          });
    }

    private async void GetUserdata()
    {
        DocumentReference docRef = db.Collection("users").Document(userId);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                //asignamos los variables a la UI
                string nombre= snapshot.GetValue<string>("DisplayName");
                nombretxt.text = nombre;
                int xp= snapshot.GetValue<int>("xp");
                xptxt.text = xp.ToString();

                // guardar datos en player prefs
                PlayerPrefs.SetInt("xp", xp);
                PlayerPrefs.SetString("DisplayName", nombre);
                PlayerPrefs.Save();

            }
        }
        catch(Exception e)
        {
            Debug.LogError("Error obteniendo datos de usuario" + e);
        }
    }
    async void ObtenerPosicionUsuario()
    {
       

        // Realiza una consulta para obtener los usuarios ordenados por XP en orden descendente (de mayor a menor)
        Query rankingQuery = db.Collection("users").OrderByDescending("xp");
        // Ejecuta la consulta y obtiene los datos
        QuerySnapshot snapshot = await rankingQuery.GetSnapshotAsync();

        // Si no hay usuarios en la base de datos
        if (snapshot.Count == 0)
        {
            Debug.LogWarning("No hay usuarios registrados en la base de datos.");
            posiciontxt.text = "Posición: No disponible"; // Muestra mensaje indicando que no hay usuarios
            return; // Sale de la función si no hay usuarios
        }

        int posicion = 1; // Comienza desde la posición 1 en el ranking
        
        // Recorre todos los usuarios del ranking
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Si el ID del documento coincide con el ID del usuario actual
            if (doc.Id == userId)
            {
                PosicionRanking.text = posicion.ToString();
                posiciontxt.text = "#" + posicion; // Muestra la posición en el ranking
                PlayerPrefs.SetInt("posicion", posicion); // guardo posición para mostrarla offline --------------------------------
                PlayerPrefs.Save();
                Debug.Log($"El usuario {userId} está en la posición {posicion} del ranking.");
                break; // Sale del ciclo ya que se encontró al usuario
            }
            posicion++; // Incrementa la posición para el siguiente usuario
        }
    }
    private void MostrarDatosOffline()
    {
        // podio 

        primeroNombre.text = PlayerPrefs.GetString("primernombre", "");
        segundoNombre.text = PlayerPrefs.GetString("segundonombre", "");
        terceroNombre.text = PlayerPrefs.GetString("tercernombre", "");
        primeroXP.text = PlayerPrefs.GetString("primerxp", "");
        segundoXP.text = PlayerPrefs.GetString("segundoxp", "");
        terceroXP.text = PlayerPrefs.GetString("tercerxp", "");

        //boton 
        nombretxt.text = PlayerPrefs.GetString("DisplayName", "");
        xptxt.text = PlayerPrefs.GetInt("xp", 0).ToString();
        posiciontxt.text = PlayerPrefs.GetInt("posicion", 0).ToString();

        PosicionRanking.text = PlayerPrefs.GetInt("posicion", 0).ToString();
    }
}

