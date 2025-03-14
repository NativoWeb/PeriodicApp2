using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Auth;

public class MemoriaQuimicaOffline : MonoBehaviour
{
    public GameObject TarjetaPrefab;
    public Transform panelCartas;
    public GameObject botonContinuar;

    private FirebaseAuth auth;
    private FirebaseUser user;

    private int xpGanadoPorNivel = 100;
    private int numeroNivel = 2; // Nivel actual del juego

    private List<string> elementos = new List<string> { "Litio", "Sodio", "Potasio", "Rubidio", "Cesio", "Francio" };
    private List<string> simbolos = new List<string> { "Li", "Na", "K", "Rb", "Cs", "Fr" };
    private Dictionary<string, string> parejasDiccionario = new Dictionary<string, string>();

    private TarjetaOffliine primeraSeleccionada;
    private TarjetaOffliine segundaSeleccionada;
    private bool puedeSeleccionar = true;
    private int parejasEncontradas = 0;

    private DatabaseReference referenciaFirebase;

    private int nuevoXp;
    private int nuevoNivel;

    void Start()
    {
        botonContinuar.SetActive(false);

        // Inicializa Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                referenciaFirebase = FirebaseDatabase.DefaultInstance.RootReference;
            }
            else
            {
                Debug.LogError("❌ No se pudo conectar con Firebase: " + task.Result);
            }
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    referenciaFirebase = FirebaseDatabase.DefaultInstance.RootReference;
                    auth = FirebaseAuth.DefaultInstance;
                    user = auth.CurrentUser;

                    if (user == null)
                    {
                        Debug.LogError("❌ No hay usuario autenticado.");
                    }
                    else
                    {
                        Debug.Log("✅ Usuario autenticado: " + user.UserId);
                    }
                }
                else
                {
                    Debug.LogError("❌ No se pudo conectar con Firebase: " + task.Result);
                }
            });
        });

        // Crear diccionario de parejas
        for (int i = 0; i < elementos.Count; i++)
        {
            parejasDiccionario[elementos[i]] = simbolos[i];
            parejasDiccionario[simbolos[i]] = elementos[i];
        }

        CrearCartas();
        CargarDatosLocales();
    }

    // Método para crear y mezclar las tarjetas
    void CrearCartas()
    {
        List<GameObject> tarjetasCreadas = new List<GameObject>();

        for (int i = 0; i < elementos.Count; i++)
        {
            GameObject tarjetaNombre = Instantiate(TarjetaPrefab);
            tarjetaNombre.GetComponent<TarjetaOffliine>().ConfigurarTarjeta(simbolos[i], elementos[i], this);
            tarjetasCreadas.Add(tarjetaNombre);

            GameObject tarjetaSimbolo = Instantiate(TarjetaPrefab);
            tarjetaSimbolo.GetComponent<TarjetaOffliine>().ConfigurarTarjeta(elementos[i], simbolos[i], this);
            tarjetasCreadas.Add(tarjetaSimbolo);
        }

        MezclarLista(tarjetasCreadas);

        foreach (GameObject tarjeta in tarjetasCreadas)
        {
            tarjeta.transform.SetParent(panelCartas, false);
        }
    }

    // Método para mezclar la lista de cartas
    void MezclarLista<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int indiceAleatorio = Random.Range(0, i + 1);
            (lista[i], lista[indiceAleatorio]) = (lista[indiceAleatorio], lista[i]);
        }
    }

    // Verificar pareja seleccionada
    public void VerificarPareja(TarjetaOffliine seleccionada)
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

    // Comparar si las tarjetas son pareja
    IEnumerator CompararParejas()
    {
        yield return new WaitForSeconds(1f);

        if (EsPareja(primeraSeleccionada, segundaSeleccionada))
        {
            primeraSeleccionada.botonTarjeta.interactable = false;
            segundaSeleccionada.botonTarjeta.interactable = false;
            parejasEncontradas++;

            if (parejasEncontradas == elementos.Count)
            {
                botonContinuar.SetActive(true);
                GuardarProgresoOffline();

                if (HayConexionInternet())
                {
                    SubirDatosAFirebase(nuevoXp, nuevoNivel);
                }
            }
        }
        else
        {
            primeraSeleccionada.OcultarTarjeta();
            segundaSeleccionada.OcultarTarjeta();
        }

        // Reiniciar selección
        primeraSeleccionada = null;
        segundaSeleccionada = null;
        puedeSeleccionar = true;
    }

    // Comprobar si las tarjetas son pareja correcta
    bool EsPareja(TarjetaOffliine a, TarjetaOffliine b)
    {
        return (parejasDiccionario.ContainsKey(a.elementoNombre) && parejasDiccionario[a.elementoNombre] == b.elementoNombre) ||
               (parejasDiccionario.ContainsKey(b.elementoNombre) && parejasDiccionario[b.elementoNombre] == a.elementoNombre);
    }



    // Permitir o no la selección
    public bool PuedeSeleccionar()
    {
        return puedeSeleccionar;
    }

    // Guardar progreso local
    void GuardarProgresoOffline()
    {
        int xpActual = PlayerPrefs.GetInt("xp", 0);
        int nivelActual = PlayerPrefs.GetInt("nivelCompletado", 1);
        bool datosSubidos = PlayerPrefs.GetInt("datosSubidos", 0) == 1;

        if (!datosSubidos)
        {
            nuevoXp = xpActual + xpGanadoPorNivel;
            nuevoNivel = numeroNivel;

            PlayerPrefs.SetInt("xp", nuevoXp);
            PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
            PlayerPrefs.SetInt("datosSubidos", 0);
            PlayerPrefs.Save();

            Debug.Log($"✅ Progreso guardado localmente: Nivel {nuevoNivel}, XP Total {nuevoXp}");
        }
        else
        {
            nuevoXp = xpActual;
            nuevoNivel = nivelActual;
        }
    }

    // Cargar progreso local
    void CargarDatosLocales()
    {
        int xp = PlayerPrefs.GetInt("xp", 0);
        int nivel = PlayerPrefs.GetInt("nivelCompletado", 1);
        Debug.Log($"✅ Datos cargados: Nivel {nivel}, XP Total {xp}");
    }

    // Verificar conexión a Internet
    bool HayConexionInternet()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    // Subir progreso a Firebase
    void SubirDatosAFirebase(int xpActual, int nivelActual)
    {
        if (user == null)
        {
            Debug.LogError("❌ No se puede subir datos porque no hay usuario autenticado.");
            return;
        }

        Dictionary<string, object> datosUsuario = new Dictionary<string, object>
    {
        { "xp", xpActual },
        { "nivelCompletado", nivelActual }
    };

        referenciaFirebase.Child("usuarios").Child(user.UserId).SetValueAsync(datosUsuario).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                PlayerPrefs.SetInt("datosSubidos", 1);
                PlayerPrefs.Save();
                Debug.Log("✅ Datos subidos a Firebase con éxito.");
            }
            else
            {
                Debug.LogError("❌ Error al subir los datos a Firebase: " + task.Exception);
            }
        });
    }

}
