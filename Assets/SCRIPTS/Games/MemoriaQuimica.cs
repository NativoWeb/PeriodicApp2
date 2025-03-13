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
    public GuardarProgreso gestorProgreso;

    private int xpGanadoPorNivel = 100; // Ajustable desde el Inspector
    private int numeroNivel = 2; // Número de nivel ajustable

    private List<string> elementos = new List<string> { "Litio", "Sodio", "Potasio", "Rubidio", "Cesio", "Francio" };
    private List<string> simbolos = new List<string> { "Li", "Na", "K", "Rb", "Cs", "Fr" };
    private Dictionary<string, string> parejasDiccionario = new Dictionary<string, string>();

    private Tarjeta primeraSeleccionada;
    private Tarjeta segundaSeleccionada;
    private bool puedeSeleccionar = true;
    private int parejasEncontradas = 0;
    private int nivelSeleccionado = 6;


    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

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
                GameObject gestor = GameObject.Find("GestorProgreso");

                GuardarProgreso gp = gestor.GetComponent<GuardarProgreso>();

                gp.GuardarProgresoFirestore(nivelSeleccionado + 1, parejasEncontradas, auth);
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

    
}
