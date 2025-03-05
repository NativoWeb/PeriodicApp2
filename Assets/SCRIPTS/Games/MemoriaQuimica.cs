using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MemoriaQuimica : MonoBehaviour
{
    public GameObject tarjetaPrefab;
    public Transform panelCartas;

    private List<string> elementos = new List<string> { "Litio", "Sodio", "Potasio", "Rubidio", "Cesio", "Francio" };
    private List<string> simbolos = new List<string> { "Li", "Na", "K", "Rb", "Cs", "Fr" };

    private Dictionary<string, string> parejasDiccionario = new Dictionary<string, string>();

    private Tarjeta primeraSeleccionada;
    private Tarjeta segundaSeleccionada;
    private bool puedeSeleccionar = true;
    public GameObject botonContinuar; // Referencia al botón "Continuar"

    private int parejasEncontradas = 0; // Contador de parejas encontradas


    void Start()
    {
        Debug.Log("Iniciando juego de memoria química...");

        botonContinuar.SetActive(false); // Ocultar el botón al iniciar
        Debug.Log("🔄 Botón 'Continuar' oculto.");

        for (int i = 0; i < elementos.Count; i++)
        {
            parejasDiccionario[elementos[i]] = simbolos[i];
            parejasDiccionario[simbolos[i]] = elementos[i];
        }

        CrearCartas();
    }


    void CrearCartas()
    {
        Debug.Log("📌 Iniciando la creación de cartas...");

        List<GameObject> tarjetasCreadas = new List<GameObject>();

        for (int i = 0; i < elementos.Count; i++)
        {
            // Crear tarjeta con el nombre
            GameObject tarjetaNombre = Instantiate(tarjetaPrefab);
            Tarjeta scriptNombre = tarjetaNombre.GetComponent<Tarjeta>();
            scriptNombre.ConfigurarTarjeta(elementos[i], simbolos[i], this);
            tarjetaNombre.name = $"Tarjeta_{elementos[i]}";
            tarjetasCreadas.Add(tarjetaNombre);

            // Crear tarjeta con el símbolo
            GameObject tarjetaSimbolo = Instantiate(tarjetaPrefab);
            Tarjeta scriptSimbolo = tarjetaSimbolo.GetComponent<Tarjeta>();
            scriptSimbolo.ConfigurarTarjeta(simbolos[i], elementos[i], this);
            tarjetaSimbolo.name = $"Tarjeta_{simbolos[i]}";
            tarjetasCreadas.Add(tarjetaSimbolo);
        }

        // Mezclar las tarjetas antes de agregarlas al panel
        Shuffle(tarjetasCreadas);
        Debug.Log($"🔄 Se crearon y mezclaron {tarjetasCreadas.Count} tarjetas.");

        foreach (GameObject tarjeta in tarjetasCreadas)
        {
            tarjeta.transform.SetParent(panelCartas, false);  // Asignar al Canvas con posiciones aleatorias
        }

        Debug.Log("✅ Cartas colocadas en posiciones aleatorias.");
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

            parejasEncontradas++; // Aumentar el contador de parejas encontradas
            Debug.Log($"✅ Parejas encontradas: {parejasEncontradas}");

            if (parejasEncontradas == elementos.Count) // Si se encontraron todas
            {
                Debug.Log("🎉 ¡Juego completado! Mostrando botón 'Continuar'.");
                botonContinuar.SetActive(true); // Mostrar botón
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
