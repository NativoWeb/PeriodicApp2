using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Firebase.Extensions;

public class CrearPartidaManager : MonoBehaviour
{
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    public string jugadorSeleccionadoUID;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    public void CrearPartida()
    {
        string jugadorActualUID = auth.CurrentUser.UserId;

        if (string.IsNullOrEmpty(jugadorSeleccionadoUID))
        {
            Debug.LogWarning("No se ha seleccionado ningún jugador.");
            return;
        }
        string[] categorias = new string[]
        {
            "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
            "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
            "Lantánidos", "Actínoides", "Propiedades Desconocidas"
        };

        // Crear diccionario por jugador
        var categoriasJugadorA = new Dictionary<string, bool>();
        var categoriasJugadorB = new Dictionary<string, bool>();

        string partidaId = System.Guid.NewGuid().ToString();

        foreach (string cat in categorias)
        {
            categoriasJugadorA[cat] = false;
            categoriasJugadorB[cat] = false;
        }

        var datosPartida = new Dictionary<string, object>
        {
            { "jugadorA", jugadorActualUID },
            { "jugadorB", jugadorSeleccionadoUID },
            { "turnoActual", jugadorActualUID },
            { "estado", "jugando" },
            { "rondaActual", 1 },
            { "CoronaJugadorA" , 0 },
            { "CoronaJugadorB" , 0 },
            { "CategoriasJugadorA", categoriasJugadorA },
            { "CategoriasJugadorB", categoriasJugadorB },
            { "fallos", new Dictionary<string, bool>
            {
                { jugadorActualUID, false },
                { jugadorSeleccionadoUID, false }
            }
            },
            { "creado", Timestamp.GetCurrentTimestamp() }
        };


        db.Collection("partidasQuimicados").Document(partidaId).SetAsync(datosPartida).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Partida creada con éxito.");
                PlayerPrefs.SetString("partidaIdQuimicados", partidaId);
                SceneManager.LoadScene("QuimicadosGame");
            }
            else
            {
                Debug.LogError("Error al crear partida: " + task.Exception);
            }
        });
    }
}
