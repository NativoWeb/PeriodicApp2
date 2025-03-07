using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;

public class MemoriaQuimica : MonoBehaviour
{
    public GameObject tarjetaPrefab;
    public Transform panelCartas;
    public GameObject botonContinuar;

    private int xpGanadoPorNivel = 100; // Ajustable desde el Inspector
    private int nivelActual = 2; // Asigna manualmente el número de nivel

    private List<string> elementos = new List<string> { "Litio", "Sodio", "Potasio", "Rubidio", "Cesio", "Francio" };
    private List<string> simbolos = new List<string> { "Li", "Na", "K", "Rb", "Cs", "Fr" };
    private Dictionary<string, string> parejasDiccionario = new Dictionary<string, string>();

    private Tarjeta primeraSeleccionada;
    private Tarjeta segundaSeleccionada;
    private bool puedeSeleccionar = true;
    private int parejasEncontradas = 0;

    private FirebaseAuth firebaseAuth;
    private FirebaseFirestore firebaseDB;

    void Start()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
        firebaseDB = FirebaseFirestore.DefaultInstance;

        botonContinuar.SetActive(false);


        for (int i = 0; i < elementos.Count; i++)
        {
            parejasDiccionario[elementos[i]] = simbolos[i];
            parejasDiccionario[simbolos[i]] = elementos[i];
        }

        CrearCartas();
    }

    void CrearCartas()
    {
        List<GameObject> tarjetasCreadas = new List<GameObject>();

        for (int i = 0; i < elementos.Count; i++)
        {
            GameObject tarjetaNombre = Instantiate(tarjetaPrefab);
            Tarjeta scriptNombre = tarjetaNombre.GetComponent<Tarjeta>();
            scriptNombre.ConfigurarTarjeta(elementos[i], simbolos[i], this);
            tarjetasCreadas.Add(tarjetaNombre);

            GameObject tarjetaSimbolo = Instantiate(tarjetaPrefab);
            Tarjeta scriptSimbolo = tarjetaSimbolo.GetComponent<Tarjeta>();
            scriptSimbolo.ConfigurarTarjeta(simbolos[i], elementos[i], this);
            tarjetasCreadas.Add(tarjetaSimbolo);
        }

        Shuffle(tarjetasCreadas);

        foreach (GameObject tarjeta in tarjetasCreadas)
        {
            tarjeta.transform.SetParent(panelCartas, false);
        }
    }

    public void VerificarPareja(Tarjeta seleccionada)
    {
        if (!puedeSeleccionar) return;

        if (primeraSeleccionada == null)
        {
            primeraSeleccionada = seleccionada;
        }
        else if (segundaSeleccionada == null)
        {
            segundaSeleccionada = seleccionada;
            puedeSeleccionar = false;
            StartCoroutine(CompararParejas());
        }
    }

    IEnumerator CompararParejas()
    {
        yield return new WaitForSeconds(1);

        if (EsPareja(primeraSeleccionada, segundaSeleccionada))
        {
            primeraSeleccionada.botonTarjeta.interactable = false;
            segundaSeleccionada.botonTarjeta.interactable = false;
            parejasEncontradas++;

            if (parejasEncontradas == elementos.Count)
            {
                botonContinuar.SetActive(true);
                GuardarProgresoEnFirebase(nivelActual);
            }
        }
        else
        {
            primeraSeleccionada.OcultarTarjeta();
            segundaSeleccionada.OcultarTarjeta();
        }

        primeraSeleccionada = null;
        segundaSeleccionada = null;
        puedeSeleccionar = true;
    }

    bool EsPareja(Tarjeta a, Tarjeta b)
    {
        return (a.elementoNombre == b.elementoSimbolo) || (a.elementoSimbolo == b.elementoNombre);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    public bool PuedeSeleccionar()
    {
        return puedeSeleccionar;
    }

    private async void GuardarProgresoEnFirebase(int nivelActualJugado)
    {
        if (firebaseAuth.CurrentUser != null)
        {
            string userId = firebaseAuth.CurrentUser.UserId;
            DocumentReference grupoRef = firebaseDB.Collection("users").Document(userId)
                                                   .Collection("grupos").Document("grupo 1");

            DocumentReference userRef = firebaseDB.Collection("users").Document(userId);

            try
            {
                // Obtener el XP y el nivel más alto registrado en Firestore
                DocumentSnapshot userSnapshot = await userRef.GetSnapshotAsync();
                int xpActual = userSnapshot.Exists && userSnapshot.TryGetValue<int>("xp", out int xp) ? xp : 0;

                DocumentSnapshot grupoSnapshot = await grupoRef.GetSnapshotAsync();
                int nivelAlmacenado = grupoSnapshot.Exists && grupoSnapshot.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;

                // Verificar si el nivel jugado es mayor al almacenado
                bool subirNivel = nivelActualJugado > nivelAlmacenado;

                int nuevoXp = xpActual + xpGanadoPorNivel;

                int nuevoNivel;

                Debug.Log(subirNivel);
                if (subirNivel)
                {
                    nuevoNivel = nivelActualJugado;
                }
                else
                {
                    nuevoNivel = nivelAlmacenado;
                }

                // Guardar XP en users/userId
                Dictionary<string, object> datosUsuario = new Dictionary<string, object>
            {
                { "xp", nuevoXp }
            };

                await userRef.SetAsync(datosUsuario, SetOptions.MergeAll);

                if (subirNivel)
                {
                    Dictionary<string, object> datosGrupo = new Dictionary<string, object>
                {
                    { "nivel", nuevoNivel }
                };

                    // Guardar el nuevo nivel en Firestore
                    await grupoRef.SetAsync(datosGrupo, SetOptions.MergeAll);
                }

                Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");

                // Guardar en PlayerPrefs para uso local
                PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
                PlayerPrefs.SetInt("xp", nuevoXp);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("❌ Usuario no autenticado.");
        }
    }
}
