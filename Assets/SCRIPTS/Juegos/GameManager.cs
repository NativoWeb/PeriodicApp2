using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static GeneradorElementosUI;
using SimpleJSON;
using System.Threading.Tasks;
using Firebase.Extensions; // Al principio del archivo

[System.Serializable]
public class ElementoInfo
{
    public string nombre;
    public string simbolo;
    public int numeroAtomico;
    public string categoria;
}

[System.Serializable]
public class Reaccion
{
    public string con;
    public int daño;
}

[System.Serializable]
public class ElementoReaccion
{
    public string nombre;
    public string categoria;
    public int daño_base;
    public Reaccion[] reacciones;
}

[System.Serializable]
public class ElementoReaccionLista
{
    public ElementoReaccion[] elementos;
}

[System.Serializable]
public class ElementoInfoLista
{
    public ElementoInfo[] elementos;
}


public class GameManager : MonoBehaviour
{
    private ElementoInfoLista datosInfo;
    private ElementoReaccionLista datosReaccion;

    private bool modoCombinacion = false;
    private ElementoSeleccionable cartaSeleccionada1;
    private ElementoSeleccionable cartaSeleccionada2;

    // Diccionarios para acceder por nombre
    public Dictionary<string, ElementoInfo> infoPorNombre = new Dictionary<string, ElementoInfo>();
    public Dictionary<string, ElementoReaccion> reaccionPorNombre = new Dictionary<string, ElementoReaccion>();

    public static GameManager instancia;

    [Header("Nombres jugadores")]
    public TextMeshProUGUI txtNombreJugador;
    public TextMeshProUGUI txtNombreEnemigo;

    [Header("Referencias de UI")]
    public TextMeshProUGUI textoTurno;
    public Slider barraVidaJugador;
    public Slider barraVidaEnemigo;
    public GameObject panelSeleccion;
    public GameObject panelEspera;
    public TMP_Text TxtNRonda;

    [Header("Datos del jugador")]
    public string miUID;
    public string enemigoUID;
    public string partidaId;

    [Header("Panel Selección")]
    public GameObject panelOpciones;
    public Button btnLanzar;
    public Button btnCombinar;
    public Button btnCancelar;

    public GameObject PanelRonda;
    public TMP_Text TxtRonda;

    private string primerElemento = null;
    private string segundoElemento = null;

    private int vidaJugador = 100;
    private int vidaEnemigo = 100;

    private bool esMiTurno;
    private DatabaseReference partidaRef;
    FirebaseFirestore db;
    private DatabaseReference realtime;


    public GameObject PrefabCarta;
    public Transform contenidoScroll; // el Content del ScrollView

    private bool esJugadorA;


    //Ruleta
    public GameObject combate;
    public GameObject PanelRuleta;
    public RectTransform ruleta;
    public TextMeshProUGUI textoCategoria;
    public string[] Categorias = new string[]
    {
        "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
        "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
        "Lantánidos", "Actínoides", "Propiedades Desconocidas"
    };

    private bool girando = false;

    void Awake()
    {
        if (instancia == null)
            instancia = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Forzar orientación vertical
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        PanelRuleta.SetActive(true);

        partidaId = PlayerPrefs.GetString("PartidaId");

        partidaRef = FirebaseDatabase.DefaultInstance.GetReference("partidas").Child(partidaId);

        miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        db = FirebaseFirestore.DefaultInstance;
        realtime = FirebaseDatabase.DefaultInstance.RootReference;

        TxtNRonda.text = "Ronda 1";
        barraVidaJugador.maxValue = 100;
        barraVidaEnemigo.maxValue = 100;
        btnLanzar.onClick.AddListener(LanzarElemento);
        btnCombinar.onClick.AddListener(CombinarElemento);
        btnCancelar.onClick.AddListener(CancelarSeleccion);


        FirebaseDatabase.DefaultInstance.GetReference("partidas").Child(partidaId)
            .Child("jugadorA").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    string jugadorA = task.Result.Value.ToString();
                    Debug.Log($"🔍 Comparando UID. miUID: {miUID}, jugadorA: {jugadorA}");

                    if (miUID == jugadorA)
                    {
                        // Yo soy el jugador A, giro la ruleta
                        GirarRuleta();
                    }
                    else
                    {
                        EmpezarEscuchaCategoriaDesdeFirebasee(); // Yo soy jugador B, espero la categoría desde Firebase
                    }
                }
            });

        StartCoroutine(CargarDatosPartida());
    }


    void EmpezarEscuchaCategoriaDesdeFirebasee()
    {
        textoCategoria.text = "Esperando selección de categoría...";
        Debug.Log("alksdjlkasjdklasd");
        FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .ValueChanged += OnCategoriaSeleccionadaRecibida;
        Debug.Log("alksdjlkasjdklasd");
    }

    private void OnCategoriaSeleccionadaRecibida(object sender, ValueChangedEventArgs args)
    {
        Debug.Log("alksdjlkasjdklasd");
        if (args.DatabaseError != null)
        {
            Debug.LogError("❌ Error al escuchar cambios de categoría: " + args.DatabaseError.Message);
            return;
        }

        if (!args.Snapshot.Exists)
        {
            Debug.LogWarning("⚠️ Snapshot de partida no existe.");
            return;
        }

        if (!args.Snapshot.HasChild("categoriaSeleccionada"))
        {
            Debug.Log("⏳ 'categoriaSeleccionada' aún no ha sido creada.");
            return;
        }

        string categoria = args.Snapshot.Child("categoriaSeleccionada").Value.ToString();
        Debug.Log("✅ Categoría recibida: " + categoria);

        textoCategoria.text = "Categoría: " + categoria;
        PlayerPrefs.SetString("CategoriaRuleta", categoria);
        CargarJsons();
        QuitarPanel();
    }


    public void GirarRuleta()
    {
        if (!girando)
            StartCoroutine(GirarAnimacion());
    }

    IEnumerator GirarAnimacion()
    {
        girando = true;

        float tiempo = 4f;

        float anguloTotal = Random.Range(3, 6) * 360 + Random.Range(0, 360); // vueltas + aleatorio
        float anguloInicial = ruleta.eulerAngles.z;
        float anguloFinal = anguloInicial + anguloTotal;

        float tiempoActual = 0f;

        while (tiempoActual < tiempo)
        {
            float t = tiempoActual / tiempo;
            float rotacion = Mathf.Lerp(anguloInicial, anguloFinal, t);
            ruleta.eulerAngles = new Vector3(0, 0, rotacion);
            tiempoActual += Time.deltaTime;
            yield return null;
        }

        ruleta.eulerAngles = new Vector3(0, 0, anguloFinal);

        // Determinar categoría
        float anguloFinalZ = ruleta.eulerAngles.z % 360f;
        float anguloSector = 360f / Categorias.Length;
        int indice = Mathf.FloorToInt((360f - anguloFinalZ + (anguloSector / 2)) % 360f / anguloSector);
        string categoriaSeleccionada = Categorias[indice];
        textoCategoria.text = $"Categoría: {categoriaSeleccionada}";

        // GUARDAR EN FIREBASE
        FirebaseDatabase.DefaultInstance
        .GetReference("partidas")
        .Child(partidaId)
        .Child("categoriaSeleccionada")
        .SetValueAsync(categoriaSeleccionada);
        PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);
        girando = false;
        CargarJsons();
        QuitarPanel();
    }

    private async void QuitarPanel()
    {
        await Task.Delay(3000);
        PanelRuleta.SetActive(false);
        combate.SetActive(true);
        await Task.Delay(3000);
        combate.SetActive(false);
    }

    public void CargarJsons()
    {
        TextAsset jsonInfo = Resources.Load<TextAsset>("Misiones_Categorias");
        TextAsset jsonReacciones = Resources.Load<TextAsset>("JuegoCombate");

        // Parseamos el JSON usando SimpleJSON
        var root = JSON.Parse(jsonInfo.text);
        var categorias = root["Misiones_Categorias"]["Categorias"];

        infoPorNombre.Clear();
        reaccionPorNombre.Clear();

        // Extraemos la categoría seleccionada
        string categoriaSeleccionada = PlayerPrefs.GetString("CategoriaRuleta");

        List<ElementoInfo> elementosDisponibles = new List<ElementoInfo>();

        if (categorias.HasKey(categoriaSeleccionada))
        {
            var elementosJson = categorias[categoriaSeleccionada]["Elementos"];

            foreach (var kv in elementosJson)
            {
                var data = kv.Value;

                ElementoInfo elemento = new ElementoInfo
                {
                    nombre = data["nombre"],
                    simbolo = data["simbolo"],
                    numeroAtomico = data["numero_atomico"].AsInt,
                    categoria = categoriaSeleccionada
                };

                infoPorNombre[elemento.nombre] = elemento;
                elementosDisponibles.Add(elemento);
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró la categoría: {categoriaSeleccionada}");
        }

        // Reacciones sigue usando JsonUtility si está bien formado
        datosReaccion = JsonUtility.FromJson<ElementoReaccionLista>(jsonReacciones.text);
        foreach (var reaccion in datosReaccion.elementos)
        {
            if (!reaccionPorNombre.ContainsKey(reaccion.nombre))
                reaccionPorNombre[reaccion.nombre] = reaccion;
        }

        MostrarElementosEnScroll(elementosDisponibles);

        Debug.Log($"📘 Elementos cargados: {infoPorNombre.Count}");
        Debug.Log($"🔥 Reacciones cargadas: {reaccionPorNombre.Count}");
    }

    void MostrarElementosEnScroll(List<ElementoInfo> elementos)
    {
        foreach (Transform hijo in contenidoScroll)
        {
            Destroy(hijo.gameObject); // Limpia el scroll
        }

        foreach (var elemento in elementos)
        {
            GameObject obj = Instantiate(PrefabCarta, contenidoScroll);

            // Buscar los textos dentro del prefab
            TextMeshProUGUI txtNumero = obj.transform.Find("TxtNumero").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI txtSimbolo = obj.transform.Find("TxtSimbolo").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI txtNombre = obj.transform.Find("TxtNombre").GetComponent<TextMeshProUGUI>();
            // Asignar valores
            txtNumero.text = elemento.numeroAtomico.ToString();
            txtSimbolo.text = elemento.simbolo;
            txtNombre.text = elemento.nombre;
        }
    }

    public void ElementoSeleccionado(string nombre, ElementoSeleccionable carta)
    {
        if (!modoCombinacion)
        {
            // Si no está en modo combinación, solo se puede elegir una carta a la vez
            if (cartaSeleccionada1 != null)
                cartaSeleccionada1.ResetVisual();

            primerElemento = nombre;
            cartaSeleccionada1 = carta;
            cartaSeleccionada1.ToggleSeleccionVisual();
        }
        else
        {
            if (cartaSeleccionada2 != null)
                cartaSeleccionada2.ResetVisual();

            segundoElemento = nombre;
            cartaSeleccionada2 = carta;
            cartaSeleccionada2.ToggleSeleccionVisual();
        }
    }


    IEnumerator CargarDatosPartida()
    {

        float timeout = 5f; // espera máxima de 5 segundos
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            var task = partidaRef.GetValueAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result.Exists && task.Result.HasChild("jugadorA") && task.Result.HasChild("jugadorB"))
            {
                var snapshot = task.Result;

                string jugadorA = snapshot.Child("jugadorA").Value.ToString();
                string jugadorB = snapshot.Child("jugadorB").Value.ToString();

                esJugadorA = (jugadorA == miUID);
                enemigoUID = (jugadorA == miUID) ? jugadorB : jugadorA;

                // Nombres
                db.Collection("users").Document(jugadorA).GetSnapshotAsync().ContinueWith(task => {
                    if (task.IsCompleted && task.Result.Exists)
                        txtNombreJugador.text = task.Result.GetValue<string>("DisplayName");
                });

                db.Collection("users").Document(jugadorB).GetSnapshotAsync().ContinueWith(task => {
                    if (task.IsCompleted && task.Result.Exists)
                        txtNombreEnemigo.text = task.Result.GetValue<string>("DisplayName");
                });

                vidaJugador = int.Parse(snapshot.Child($"vida{(jugadorA == miUID ? "A" : "B")}").Value.ToString());
                vidaEnemigo = int.Parse(snapshot.Child($"vida{(jugadorA == miUID ? "B" : "A")}").Value.ToString());

                yield break; // listo
            }

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            var rondaTask = partidaRef.Child("ronda").GetValueAsync();
            yield return new WaitUntil(() => rondaTask.IsCompleted);
            if (rondaTask.Result.Exists)
            {
                int rondaActual = int.Parse(rondaTask.Result.Value.ToString());
                TxtNRonda.text = $"Ronda {rondaActual}";
            }
        }

        Debug.LogError("❌ No se encontró la partida con datos completos tras el timeout.");
    }
    public void RealizarJugada(string elemento1, string elemento2 = "")
    {
        panelSeleccion.SetActive(false);
        textoTurno.text = "Esperando al oponente...";
        Debug.Log("🎯 RealizarJugada llamada. Esperando ronda...");

        int rondaActual = 1;

        partidaRef.Child("ronda").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result.Exists)
            {
                rondaActual = int.Parse(task.Result.Value.ToString());
            }

            var jugada = new Dictionary<string, object>
        {
            { "elemento1", elemento1 },
            { "elemento2", string.IsNullOrEmpty(elemento2) ? null : elemento2 },
            { "ronda", rondaActual }
        };

            FirebaseDatabase.DefaultInstance
                .GetReference("jugadas")
                .Child(partidaId)
                .Child(miUID)
                .SetValueAsync(jugada)
                .ContinueWithOnMainThread(setTask =>
                {
                    if (setTask.IsCompleted && !setTask.IsFaulted)
                    {
                        Debug.Log("✅ Jugada guardada correctamente.");
                        EsperarJugadaEnemigo(); // 🟢 Solo escuchar después de guardar
                    }
                    else
                    {
                        Debug.LogError("❌ Error al guardar jugada: " + setTask.Exception);
                    }
                });
        });

        modoCombinacion = false;
        LimpiarSeleccion();
    }


    void EsperarJugadaEnemigo()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("jugadas")
            .Child(partidaId)
            .ValueChanged += OnJugadasActualizadas;
    }
    void OnJugadasActualizadas(object sender, ValueChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        if (args.Snapshot.HasChild(miUID) && args.Snapshot.HasChild(enemigoUID))
        {
            var jugadaMi = args.Snapshot.Child(miUID);
            var jugadaEnemigo = args.Snapshot.Child(enemigoUID);

            int rondaMi = int.Parse(jugadaMi.Child("ronda").Value.ToString());
            int rondaEnemigo = int.Parse(jugadaEnemigo.Child("ronda").Value.ToString());

            if (rondaMi == rondaEnemigo)
            {
                // ✅ Muy importante: quitar el listener apenas validamos que ambas jugadas están listas
                FirebaseDatabase.DefaultInstance
                    .GetReference("jugadas")
                    .Child(partidaId)
                    .ValueChanged -= OnJugadasActualizadas;

                Debug.Log("🛑 Listener eliminado. Procesando ronda...");
                ProcesarRonda(jugadaMi, jugadaEnemigo);
            }
        }
    }


    void ProcesarRonda(DataSnapshot jugadaMi, DataSnapshot jugadaEnemigo)
    {
        string miElemento1 = jugadaMi.Child("elemento1").Value.ToString();
        string miElemento2 = jugadaMi.Child("elemento2").Value?.ToString() ?? "";
        int miDaño = CalcularDaño(miElemento1, miElemento2);

        string eElemento1 = jugadaEnemigo.Child("elemento1").Value.ToString();
        string eElemento2 = jugadaEnemigo.Child("elemento2").Value?.ToString() ?? "";
        int dañoEnemigo = CalcularDaño(eElemento1, eElemento2);

        // Aplica daño mutuo
        vidaJugador -= dañoEnemigo;
        vidaEnemigo -= miDaño;

        barraVidaJugador.value = vidaJugador;
        barraVidaEnemigo.value = vidaEnemigo;

        // Actualiza vidas en Firebase
        string claveVidaYo = (txtNombreJugador.text == "nombreA") ? "vidaA" : "vidaB";
        string claveVidaEnemigo = (claveVidaYo == "vidaA") ? "vidaB" : "vidaA";

        partidaRef.Child(claveVidaYo).SetValueAsync(vidaJugador);
        partidaRef.Child(claveVidaEnemigo).SetValueAsync(vidaEnemigo);

        // Mostrar panel "Ronda terminada"
        StartCoroutine(MostrarResultadoYRonda());
        LimpiarSeleccion();
        FirebaseDatabase.DefaultInstance
        .GetReference("jugadas")
        .Child(partidaId)
        .RemoveValueAsync();

    }

    IEnumerator MostrarResultadoYRonda()
    {
        PanelRonda.SetActive(true);

        var rondaTask = partidaRef.Child("ronda").GetValueAsync();
        yield return new WaitUntil(() => rondaTask.IsCompleted);

        int rondaActual = 1;

        if (rondaTask.Result.Exists)
        {
            rondaActual = int.Parse(rondaTask.Result.Value.ToString());
            TxtRonda.text = $"Ronda {rondaActual} finalizada";

            if (TxtNRonda != null)
                TxtNRonda.text = $"Ronda {rondaActual}";
        }

        int nuevaRonda = rondaActual + 1;

        if (esJugadorA)
        {
            partidaRef.Child("ronda").SetValueAsync(nuevaRonda);
        }

        yield return new WaitForSeconds(2.5f);

        // Esperar un momento a que se actualice la ronda si no soy jugadorA
        if (!esJugadorA)
        {
            var rondaSyncTask = partidaRef.Child("ronda").GetValueAsync();
            yield return new WaitUntil(() => rondaSyncTask.IsCompleted);

            if (rondaSyncTask.Result.Exists)
            {
                int rondaSincronizada = int.Parse(rondaSyncTask.Result.Value.ToString());
                if (TxtNRonda != null)
                    TxtNRonda.text = $"Ronda {rondaSincronizada}";
            }
        }
        PanelRonda.SetActive(false);

        // Si aún hay vida, habilitamos el panel de selección
        if (vidaJugador > 0 && vidaEnemigo > 0)
        {
            Debug.Log("🔄 Preparando nueva ronda. Puedes volver a seleccionar.");

            // Limpiar selección previa por seguridad
            LimpiarSeleccion();

            // Mostrar panel de selección
            panelSeleccion.SetActive(true);
        }
        else
        {
            string resultado = vidaJugador <= 0 ? "¡Perdiste!" : "¡Ganaste!";
            TxtRonda.text = resultado;
            PanelRonda.SetActive(true);
        }
    }




    public void LanzarElemento()
    {
        if (string.IsNullOrEmpty(primerElemento))
        {
            Debug.Log("No hay elemento seleccionado.");
            return;
        }

        if (modoCombinacion && !string.IsNullOrEmpty(segundoElemento))
        {
            RealizarJugada(primerElemento, segundoElemento);
            Debug.Log($"Lanzaste combinación {primerElemento} + {segundoElemento}");
        }
        else
        {
            RealizarJugada(primerElemento);
            Debug.Log($"Lanzaste {primerElemento}");
        }
    }

    int CalcularDaño(string elemento1, string elemento2 = "")
    {
        if (!reaccionPorNombre.ContainsKey(elemento1))
        {
            Debug.LogWarning($"Elemento {elemento1} no encontrado.");
            return 0;
        }

        var datos = reaccionPorNombre[elemento1];

        if (string.IsNullOrEmpty(elemento2))
        {
            // Jugada simple
            return datos.daño_base;
        }
        else
        {
            foreach (var reaccion in datos.reacciones)
            {
                if (reaccion.con == elemento2)
                {
                    // Es una combinación válida
                    return reaccion.daño;
                }
            }

            // Combinación inválida
            return 5; // daño bajo
        }
    }
    public void CombinarElemento()
    {
        if (string.IsNullOrEmpty(primerElemento))
        {
            Debug.Log("Selecciona un primer elemento antes de combinar.");
            return;
        }

        modoCombinacion = true;
        Debug.Log("Modo combinación activado. Selecciona un segundo elemento.");
    }


    public void CancelarSeleccion()
    {
        modoCombinacion = false;
        primerElemento = null;
        segundoElemento = null;

        if (cartaSeleccionada1 != null) cartaSeleccionada1.ResetVisual();
        if (cartaSeleccionada2 != null) cartaSeleccionada2.ResetVisual();

        cartaSeleccionada1 = null;
        cartaSeleccionada2 = null;

        Debug.Log("Selección cancelada. Puedes volver a seleccionar.");
    }


    void LimpiarSeleccion()
    {
        if (cartaSeleccionada1 != null) cartaSeleccionada1.ResetVisual();
        if (cartaSeleccionada2 != null) cartaSeleccionada2.ResetVisual();

        cartaSeleccionada1 = null;
        cartaSeleccionada2 = null;
        primerElemento = null;
        segundoElemento = null;
    }
    void ActualizarUI()
    {
        barraVidaJugador.value = vidaJugador;
        barraVidaEnemigo.value = vidaEnemigo;

        textoTurno.text = esMiTurno ? "¡Tu turno!" : "Turno del oponente";
        panelSeleccion.SetActive(esMiTurno);
        panelEspera.SetActive(!esMiTurno);
    }

}
