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
using Firebase.Extensions;
using UnityEngine.SceneManagement; // Al principio del archivo
using System.Linq;
using System;                // Necesario para .ToList(), .Count(), .FirstOrDefault(), etc.
using UnityEngine.Networking;


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

public class GameManager2 : MonoBehaviour
{
    private ElementoInfoLista datosInfo;
    private ElementoReaccionLista datosReaccion;

    private bool modoCombinacion = false;
    private ElementoSeleccionable cartaSeleccionada1;
    private ElementoSeleccionable cartaSeleccionada2;

    // Diccionarios para acceder por nombre
    public Dictionary<string, ElementoInfo> infoPorNombre = new Dictionary<string, ElementoInfo>();
    public Dictionary<string, ElementoReaccion> reaccionPorNombre = new Dictionary<string, ElementoReaccion>();

    public static GameManager2 instancia;

    [Header("Nombres jugadores")]
    public TextMeshProUGUI txtNombreJugador;
    public TextMeshProUGUI txtNombreEnemigo;

    [Header("Referencias de UI")]
    public TextMeshProUGUI textoTurno;
    public Slider barraVidaJugador;
    public Slider barraVidaEnemigo;
    public GameObject panelSeleccion;
    public GameObject panelEspera;
    public GameObject PanelSinInternet;
    public TMP_Text TxtNRonda;
    public TMP_Text vidaA;
    public TMP_Text vidaB;

    [Header("Pausa")]
    public Button BtnPausa;
    public Button BtnContinuar;
    public Button BtnReiniciar;
    public Button BtnSalir;
    public GameObject PanelPausa;
    private bool pausa = false;

    [Header("Confirmación pausa")]
    public GameObject PanelConfirmacion;
    public TMP_Text TxtInfo;
    public TMP_Text Si;
    public TMP_Text No;
    public Button BtnSi;
    public Button BtnNo;

    [Header("Datos del jugador")]
    public string miUID;
    public string enemigoUID;
    public string partidaId;

    private string IdA;
    private string IdB;

    [Header("Panel Selección")]
    public GameObject panelOpciones;
    public Button btnLanzar;
    public Button btnCombinar;
    public Button btnCancelar;

    public GameObject PanelRonda;
    public TMP_Text TxtRonda;

    private string primerElemento = null;
    private string segundoElemento = null;

    private int vidaJugador;
    private int vidaEnemigo;

    private bool esMiTurno;
    private DatabaseReference partidaRef;
    FirebaseFirestore db;
    private DatabaseReference realtime;

    public GameObject PrefabCarta;
    public Transform contenidoScroll; // el Content del ScrollView

    private bool esJugadorA;

    private DatabaseReference presenciaJugadorRef;
    private bool estabaDesconectado = false;

    [Header("Ruleta")]
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
    private bool escuchandoJugadas = false;

    private int rondaProcesadaLocal = -1;
    int rondaCPU = 1;
    private bool listenerJugadorB = false;

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
        Screen.orientation = ScreenOrientation.LandscapeLeft;


        miUID = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        db = FirebaseFirestore.DefaultInstance;
        realtime = FirebaseDatabase.DefaultInstance.RootReference;

        bool modoCPU = PlayerPrefs.GetString("modoJuego") == "cpu";
        BtnPausa.onClick.AddListener(AbrirPausa);
        PanelRuleta.SetActive(true);
        barraVidaJugador.maxValue = 100;
        barraVidaEnemigo.maxValue = 100;

        if (modoCPU)
        {
            // Configurar jugador local
            miUID = "jugadorCPU"; // ID temporal
            esJugadorA = true;

            // Inicializar vidas
            vidaJugador = 100;
            vidaEnemigo = 100;

            // Nombres fake
            txtNombreJugador.text = PlayerPrefs.GetString("DisplayName", "Tú");
            txtNombreEnemigo.text = "CPU";

            vidaA.text = "100/100";
            vidaB.text = "100/100";

            // Girar ruleta directamente
            GirarRuleta();
        }
        else
        {

            partidaId = PlayerPrefs.GetString("PartidaId");
            partidaRef = FirebaseDatabase.DefaultInstance.GetReference("partidas").Child(partidaId);

            RegistrarPresencia();

            StartCoroutine(VerificarConexionPeriodicamente());

            EscucharCambiosVida();

            FirebaseDatabase.DefaultInstance.GetReference("partidas").Child(partidaId)
                .Child("jugadorA").GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                    {
                        string jugadorA = task.Result.Value.ToString();

                        if (miUID == jugadorA)
                        {
                            GirarRuleta();
                        }
                        else
                        {
                            EmpezarEscuchaCategoriaDesdeFirebasee();
                            EscucharRondaProcesada();
                        }
                    }
                });

            StartCoroutine(CargarDatosPartida());

            EscucharDesconexionDelOponente();
        }
    }
    public void AbrirPausa()
    {
        if (pausa == false)
        {
            PanelPausa.SetActive(true);
            Time.timeScale = 0f;
            pausa = true;
        }
    }
    public void resumir()
    {
        PanelPausa.SetActive(false);
        PanelConfirmacion.SetActive(false);
        Time.timeScale = 1f;
        pausa = false;
    }
    public void reiniciar()
    {
        bool modoCPU = PlayerPrefs.GetString("modoJuego") == "cpu";
        if (modoCPU)
        {
            PanelConfirmacion.SetActive(true);
            TxtInfo.text = "Seguro que quieres reiniciar la partida?";
            Si.text = "Si, Reiniciar";
            No.text = "No, Volver";
            BtnSi.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });

            BtnNo.onClick.AddListener(() =>
            {
                PanelConfirmacion.SetActive(false);
            });
        }
        else
        {
            PanelConfirmacion.SetActive(true);
            TxtInfo.text = "No se puede reiniciar una partida multijugador.";
            Si.text = "Voler";
            No.text = "Volver";
            BtnSi.onClick.AddListener(() =>
            {
                PanelConfirmacion.SetActive(false);
            });

            BtnNo.onClick.AddListener(() =>
            {
                PanelConfirmacion.SetActive(false);
            });
        }

    }
    public void salir()
    {
        PanelConfirmacion.SetActive(true);
        TxtInfo.text = "Seguro que quieres salir de la partida?";
        Si.text = "Si, Salir";
        No.text = "No, Jugar";
        BtnSi.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Inicio");
        });

        BtnNo.onClick.AddListener(() =>
        {
            PanelConfirmacion.SetActive(false);
        });
    }
    void EmpezarEscuchaCategoriaDesdeFirebasee()
    {
        textoCategoria.text = "Esperando selección de categoría...";

        FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .ValueChanged += OnCategoriaSeleccionadaRecibida;
    }
    private void OnCategoriaSeleccionadaRecibida(object sender, ValueChangedEventArgs args)
    {
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

        if (!listenerJugadorB)
        {
            listenerJugadorB = true;
            CargarJsons();
            QuitarPanel();
        }
    }
    public void GirarRuleta()
    {
        if (!girando)
            StartCoroutine(GirarAnimacion());
    }
    IEnumerator GirarAnimacion()
    {
        bool modoCPU = PlayerPrefs.GetString("modoJuego") == "cpu";
        girando = true;

        float tiempo = 4f;

        float anguloTotal = UnityEngine.Random.Range(3, 6) * 360 + UnityEngine.Random.Range(0, 360); // vueltas + aleatorio
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

        if (modoCPU)
        {
            PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);
        }
        else
        {
            // GUARDAR EN FIREBASE
            FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .Child("categoriaSeleccionada")
            .SetValueAsync(categoriaSeleccionada);
            PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);
        }

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

        // Reacciones sigue usando JsonUtility si está bien formado
        datosReaccion = JsonUtility.FromJson<ElementoReaccionLista>(jsonReacciones.text);
        foreach (var reaccion in datosReaccion.elementos)
        {
            if (!reaccionPorNombre.ContainsKey(reaccion.nombre))
                reaccionPorNombre[reaccion.nombre] = reaccion;
        }

        MostrarElementosEnScroll(elementosDisponibles);
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
        float timeout = 5f;
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
                enemigoUID = esJugadorA ? jugadorB : jugadorA;

                IdA = jugadorA;
                IdB = jugadorB;

                string uidJugadorLocal = miUID;
                string uidEnemigo = enemigoUID;

                // Cargar nombres desde Firestore
                db.Collection("users").Document(uidJugadorLocal).GetSnapshotAsync().ContinueWith(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                        txtNombreJugador.text = task.Result.GetValue<string>("DisplayName");
                });

                db.Collection("users").Document(uidEnemigo).GetSnapshotAsync().ContinueWith(task =>
                {
                    if (task.IsCompleted && task.Result.Exists)
                        txtNombreEnemigo.text = task.Result.GetValue<string>("DisplayName");
                });

                // Cargar vidas correctamente según si es jugadorA o B
                if (esJugadorA)
                {
                    vidaJugador = int.Parse(snapshot.Child("vidaA").Value.ToString());
                    vidaEnemigo = int.Parse(snapshot.Child("vidaB").Value.ToString());
                }
                else
                {
                    vidaJugador = int.Parse(snapshot.Child("vidaB").Value.ToString());
                    vidaEnemigo = int.Parse(snapshot.Child("vidaA").Value.ToString());
                }

                vidaA.text = vidaJugador + "/100";
                vidaB.text = vidaEnemigo + "/100";
                barraVidaJugador.value = 100;
                barraVidaEnemigo.value = 100;

                // Mostrar ronda actual si existe
                if (snapshot.HasChild("ronda"))
                {
                    int rondaActual = int.Parse(snapshot.Child("ronda").Value.ToString());
                    TxtNRonda.text = $"Ronda {rondaActual}";
                }

                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        Debug.LogWarning("❌ Timeout: No se pudieron cargar los datos de la partida.");
    }
    void RealizarJugadas(string elemento1, string elemento2 = "")
    {
        panelSeleccion.SetActive(false);
        textoTurno.text = "Esperando al oponente...";

        modoCombinacion = false;
        LimpiarSeleccion();

        bool modoCPU = PlayerPrefs.GetString("modoJuego") == "cpu";

        if (modoCPU)
        {
            primerElemento = elemento1;
            segundoElemento = elemento2;
            RealizarJugadaCPU(); // Simula la jugada
        }
        else
        {
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
                            EsperarJugadaEnemigo(); // 🟢 Solo escuchar después de guardar
                        }
                        else
                        {
                            Debug.LogError("❌ Error al guardar jugada: " + setTask.Exception);
                        }
                    });
            });
        }
    }
    void EsperarJugadaEnemigo()
    {
        if (escuchandoJugadas) return; // ✅ ya está escuchando, evitar duplicados

        FirebaseDatabase.DefaultInstance
            .GetReference("jugadas")
            .Child(partidaId)
            .ValueChanged += OnJugadasActualizada;

        escuchandoJugadas = true;
    }
    void OnJugadasActualizada(object sender, ValueChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        if (args.Snapshot.HasChild(miUID) && args.Snapshot.HasChild(enemigoUID))
        {
            var jugadaMi = args.Snapshot.Child(miUID);
            var jugadaEnemigo = args.Snapshot.Child(enemigoUID);

            int rondaMi = int.Parse(jugadaMi.Child("ronda").Value.ToString());
            int rondaEnemigo = int.Parse(jugadaEnemigo.Child("ronda").Value.ToString());
            if (rondaMi == rondaProcesadaLocal) return; // ya procesada

            if (esJugadorA && rondaMi == rondaEnemigo)
            {
                rondaProcesadaLocal = rondaMi; // ✅ ahora sí: justo antes de procesar
                ProcesarRondas(jugadaMi, jugadaEnemigo);
            }
        }
    }
    void ProcesarRondas(DataSnapshot jugadaMi, DataSnapshot jugadaEnemigo)
    {
        FirebaseDatabase.DefaultInstance
        .GetReference("jugadas")
        .Child(partidaId)
        .ValueChanged -= OnJugadasActualizada;

        escuchandoJugadas = false;

        string miElemento1 = jugadaMi.Child("elemento1").Value.ToString();
        string miElemento2 = jugadaMi.Child("elemento2").Value?.ToString() ?? "";
        int miDaño = CalcularDaño(miElemento1, miElemento2);

        string eElemento1 = jugadaEnemigo.Child("elemento1").Value.ToString();
        string eElemento2 = jugadaEnemigo.Child("elemento2").Value?.ToString() ?? "";
        int dañoEnemigo = CalcularDaño(eElemento1, eElemento2);

        // Solo jugadorA aplica el daño y actualiza Firebase
        if (esJugadorA)
        {
            int nuevaVidaA = Mathf.Max(0, vidaJugador - dañoEnemigo);
            int nuevaVidaB = Mathf.Max(0, vidaEnemigo - miDaño);

            partidaRef.Child("vidaA").SetValueAsync(nuevaVidaA);
            partidaRef.Child("vidaB").SetValueAsync(nuevaVidaB);

            // También actualiza ronda en Firebase
            partidaRef.Child("ronda").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                int rondaActual = 1;
                if (task.Result.Exists)
                    rondaActual = int.Parse(task.Result.Value.ToString());

                partidaRef.Child("ronda").SetValueAsync(rondaActual + 1);
                partidaRef.Child("rondaProcesada").SetValueAsync(rondaActual);

                StartCoroutine(MostrarResultadoYRonda(rondaActual));
            });

            // Limpia jugadas para siguiente ronda
            FirebaseDatabase.DefaultInstance
                .GetReference("jugadas")
                .Child(partidaId)
                .RemoveValueAsync();
        }
    }
    void EscucharRondaProcesada()
    {
        partidaRef.Child("rondaProcesada").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null || !args.Snapshot.Exists) return;

            int rondaRecibida = int.Parse(args.Snapshot.Value.ToString());

            if (rondaRecibida != rondaProcesadaLocal)
            {
                rondaProcesadaLocal = rondaRecibida;
                StartCoroutine(MostrarResultadoYRonda(rondaRecibida));
            }
        };
    }

    void EscucharCambiosVida()
    {
        partidaRef.ValueChanged += (sender, args) =>
        {
            if (!args.Snapshot.Exists) return;

            var snap = args.Snapshot;

            if (snap.HasChild("vidaA") && snap.HasChild("vidaB"))
            {
                int vidaAActual = int.Parse(snap.Child("vidaA").Value.ToString());
                int vidaBActual = int.Parse(snap.Child("vidaB").Value.ToString());

                // Actualizar vidas locales según quien soy
                vidaJugador = esJugadorA ? vidaAActual : vidaBActual;
                vidaEnemigo = esJugadorA ? vidaBActual : vidaAActual;

                vidaA.text = vidaJugador + "/100";
                vidaB.text = vidaEnemigo + "/100";

                // En ambos casos el jugador local usa barraVidaJugador
                barraVidaJugador.value = vidaJugador;
                barraVidaEnemigo.value = vidaEnemigo;
            }
        };
    }
    IEnumerator MostrarResultadoYRonda(int rondaActual)
    {
        PanelRonda.SetActive(true);

        TxtRonda.text = $"Ronda {rondaActual} finalizada";
        TxtNRonda.text = $"Ronda {rondaActual + 1}";

        yield return new WaitForSeconds(2.5f);

        PanelRonda.SetActive(false);

        if (vidaJugador > 0 && vidaEnemigo > 0)
        {
            LimpiarSeleccion();
            panelSeleccion.SetActive(true);
            EsperarJugadaEnemigo();
        }
        else
        {
            string resultado = vidaJugador <= 0 ? "¡Perdiste!" : "¡Ganaste!";
            TxtRonda.text = resultado;
            PanelRonda.SetActive(true);
            if (resultado == "¡Perdiste!")
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    Debug.Log("Xp Firebase");
                    SumarXPFirebase(3);
                }
                else
                {
                    Debug.LogWarning("⚠️ GuardarMisionCompletada no está disponible. Usando XP temporario.");
                    PlayerPrefs.SetInt("TempXP", PlayerPrefs.GetInt("TempXP", 0) + 3);
                }
            }
            else
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    Debug.Log("Xp Firebase");
                    SumarXPFirebase(10);
                }
                else
                {
                    Debug.LogWarning("⚠️ GuardarMisionCompletada no está disponible. Usando XP temporario.");
                    PlayerPrefs.SetInt("TempXP", PlayerPrefs.GetInt("TempXP", 0) + 10);
                }
            }
            LimpiarInvitaciones();
            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene("Inicio");
        }
    }
    async void SumarXPFirebase(int xp)
    {
        if (miUID == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }
        DocumentReference userRef = db.Collection("users").Document(miUID);
        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;


            await userRef.UpdateAsync("xp", xpNuevo);
            Debug.Log($"✅ XP actualizado en Firebase: {xpNuevo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }
    public void LanzarElemento()
    {
        if (string.IsNullOrEmpty(primerElemento))
        {

            textoTurno.text = "No hay elemento seleccionado.";
            return;
        }

        if (modoCombinacion && !string.IsNullOrEmpty(segundoElemento))
        {
            RealizarJugadas(primerElemento, segundoElemento);
        }
        else
        {
            RealizarJugadas(primerElemento);
        }
    }
    int CalcularDaño(string elemento1, string elemento2 = "")
    {
        if (!reaccionPorNombre.ContainsKey(elemento1))
            return 0;

        var datos = reaccionPorNombre[elemento1];

        if (string.IsNullOrEmpty(elemento2))
            return datos.daño_base;

        foreach (var reaccion in datos.reacciones)
        {
            if (reaccion.con == elemento2)
                return reaccion.daño;
        }

        return 5; // combinación inválida
    }
    public void CombinarElemento()
    {
        if (string.IsNullOrEmpty(primerElemento))
        {
            textoTurno.text = "Debes seleecionar primero un elemento antes de combinar.";
            return;
        }

        textoTurno.text = "Selecciona otro elemento para combinar.";
        modoCombinacion = true;
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
    void LimpiarInvitaciones()
    {
        FirebaseDatabase.DefaultInstance
            .GetReference("invitaciones")
            .Child(miUID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    foreach (var child in task.Result.Children)
                    {
                        string estado = child.Child("estado").Value.ToString();
                        if (estado == "aceptado" || estado == "pendiente" || estado == "rechazada")
                        {
                            FirebaseDatabase.DefaultInstance
                                .GetReference("invitaciones")
                                .Child(miUID)
                                .Child(child.Key)
                                .RemoveValueAsync();
                        }
                    }
                }
            });
    }

    void RegistrarPresencia()
    {
        presenciaJugadorRef = FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .Child("presencia")
            .Child(miUID);

        Dictionary<string, object> datosPresencia = new Dictionary<string, object>
    {
        { "conectado", true },
        { "timestamp", ServerValue.Timestamp }
    };

        presenciaJugadorRef.SetValueAsync(datosPresencia);
        presenciaJugadorRef.OnDisconnect().RemoveValue();
    }

    private IEnumerator VerificarConexionPeriodicamente()
    {
        while (true)
        {
            yield return VerificarConexionReal();
            yield return new WaitForSeconds(5f);
        }
    }

    private IEnumerator VerificarConexionReal()
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.timeout = 3;
        yield return request.SendWebRequest();

        bool hayConexion = request.result == UnityWebRequest.Result.Success;

        PanelSinInternet.SetActive(!hayConexion);

        // Actualizamos en Firebase solo si el estado cambió
        if (!hayConexion && !estabaDesconectado)
        {
            estabaDesconectado = true;
            presenciaJugadorRef.Child("conectado").SetValueAsync(false);
        }
        else if (hayConexion && estabaDesconectado)
        {
            estabaDesconectado = false;

            Dictionary<string, object> datosPresencia = new Dictionary<string, object>
        {
            { "conectado", true },
            { "timestamp", ServerValue.Timestamp }
        };
            presenciaJugadorRef.UpdateChildrenAsync(datosPresencia);
        }
    }

        //void RegistrarPresencia()
        //{
        //    string partidaId = PlayerPrefs.GetString("PartidaId");

        //    // Ruta al nodo del jugador (no hasta "conectado", sino hasta su UID)
        //    DatabaseReference presenciaJugadorRef = FirebaseDatabase.DefaultInstance
        //        .GetReference("partidas")
        //        .Child(partidaId)
        //        .Child("presencia")
        //        .Child(miUID);

        //    // Crear diccionario de datos de presencia
        //    Dictionary<string, object> datosPresencia = new Dictionary<string, object>
        //    {
        //        { "conectado", true },
        //        { "timestamp", ServerValue.Timestamp }
        //    };

        //    // Subir presencia
        //    presenciaJugadorRef.SetValueAsync(datosPresencia);

        //    // Eliminar TODO el nodo del jugador si se desconecta
        //    presenciaJugadorRef.OnDisconnect().RemoveValue();
        //}
    void EscucharDesconexionDelOponente()
    {
        string partidaId = PlayerPrefs.GetString("PartidaId");
        string idOponente = esJugadorA ? IdB : IdA;

        DatabaseReference presenciaOponenteRef = FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId)
            .Child("presencia")
            .Child(idOponente);

        presenciaOponenteRef.ValueChanged += (object sender, ValueChangedEventArgs args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("❌ Error al escuchar presencia: " + args.DatabaseError.Message);
                return;
            }

            if (!args.Snapshot.Exists || !(bool)args.Snapshot.Value)
            {
                Debug.LogWarning("🔌 El oponente se ha desconectado.");
                TerminarPartidaPorDesconexion();
            }
        };
    }
    void TerminarPartidaPorDesconexion()
    {
        TxtRonda.text = "El oponente se ha desconectado. ¡Ganaste!";
        PanelRonda.SetActive(true);

        // Marcar la partida como terminada (opcional)
        DatabaseReference partidaRef = FirebaseDatabase.DefaultInstance
            .GetReference("partidas")
            .Child(partidaId);

        partidaRef.Child("estado").SetValueAsync("terminada");

        // Regresar al menú
        StartCoroutine(VolverAlMenu());
    }

    IEnumerator VolverAlMenu()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("Inicio");
    }
    //------------------------------------------ Modo CPU --------------------------------------------------
    void RealizarJugadaCPU()
    {
        var elementosDisponibles = infoPorNombre.Values.ToList();
        var elementosValidos = elementosDisponibles.Where(e => reaccionPorNombre.ContainsKey(e.nombre)).ToList();

        string cpuElemento1 = elementosValidos[UnityEngine.Random.Range(0, elementosValidos.Count)].nombre;
        string cpuElemento2 = "";

        // 20% de probabilidad de combinación
        if (UnityEngine.Random.value < 0.5f)
        {
            var posiblesReacciones = reaccionPorNombre[cpuElemento1].reacciones;
            if (posiblesReacciones != null && posiblesReacciones.Count() > 0)
            {
                cpuElemento2 = posiblesReacciones[UnityEngine.Random.Range(0, posiblesReacciones.Count())].con;
            }
        }

        // Simular el procesamiento con una pequeña espera para hacerlo más humano
        StartCoroutine(ProcesarRondaContraCPU(cpuElemento1, cpuElemento2));
    }
    IEnumerator ProcesarRondaContraCPU(string cpuElemento1, string cpuElemento2)
    {
        yield return new WaitForSeconds(1f); // simula que la CPU piensa

        int miDaño = CalcularDaño(primerElemento, segundoElemento);
        int cpuDaño = CalcularDaño(cpuElemento1, cpuElemento2);

        vidaJugador -= cpuDaño;
        vidaEnemigo -= miDaño;

        if (vidaJugador < 0) vidaJugador = 0;
        if (vidaEnemigo < 0) vidaEnemigo = 0;

        barraVidaJugador.value = vidaJugador;
        barraVidaEnemigo.value = vidaEnemigo;

        vidaA.text = vidaJugador + "/100";
        vidaB.text = vidaEnemigo + "/100";

        rondaCPU++;
        TxtNRonda.text = $"Ronda {rondaCPU}";
        TxtRonda.text = $"Ronda {rondaCPU - 1} finalizada";
        PanelRonda.SetActive(true);

        yield return new WaitForSeconds(2.5f);
        PanelRonda.SetActive(false);

        if (vidaJugador > 0 && vidaEnemigo > 0)
        {
            LimpiarSeleccion();
            panelSeleccion.SetActive(true);
        }
        else
        {
            TxtRonda.text = vidaJugador <= 0 ? "¡Perdiste!" : "¡Ganaste!";
            if (vidaJugador <= 0)
            {
                if (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    GuardarMisionCompletada.instancia.SumarXPFirebase(3);
                }
                else
                {
                    GuardarMisionCompletada.instancia.SumarXPTemporario(3);
                }
            }
            PanelRonda.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            SceneManager.LoadScene("Inicio");
        }
    }
}