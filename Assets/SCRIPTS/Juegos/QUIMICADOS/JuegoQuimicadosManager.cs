using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Extensions;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening.Core.Easing;
using System.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine.Networking;

public class JuegoPreguntadosManager : MonoBehaviour
{
    [Header("PopUp Seleccion Categoria")]

    [Header("Paneles")]
    public GameObject PopUpSeleccionarCategoria;
    public Transform ContentCategorias;

    [Header("Buttons")]
    public Button BtnConquistar;

    [Header("Quimicados Principal")]

    [Header("Text")]
    public TMP_Text TxtResultado;
    public TMP_Text TxtExp;
    public TMP_Text txtCoronasA;
    public TMP_Text txtCoronasB;

    [Header("Paneles")]
    public GameObject PanelResultado;
    public GameObject PanelInfoLogro;
    public Transform ContentCategoriasCompletadasA;
    public Transform ContentCategoriasCompletadasB;

    [Header("Panel de Error")]
    public GameObject PanelSinInternet;

    [Header("Texts")]
    public TMP_Text txtNombreJugadorA;
    public TMP_Text txtNombreJugadorB;
    public TMP_Text txtTurno;
    public TMP_Text TxtRonda;

    [Header("Buttons")]
    public Button BtnVolver;
    public Button BtnGirar;
    public Button BtnComenzar;
    public Button BtnActivarCategoria;

    [Header("Imagenes")]
    public Image ImgAvatarUserA;
    public Image ImgAvatarUserB;

    [Header("Progreso UI")]
    public Image progressImage;     // Image Type = Filled
    public int requiredCorrect = 3; // cuántas correctas para llenarla
    public Image ImgLogro;

    [Header("PREFABS")]
    public GameObject PrefabSeleccionarCategoria;
    public GameObject PrefabCategoriaCompletada;

    private int correctCount = 0;

    int coronasA = 0;
    int coronasB = 0;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private ListenerRegistration listenerTurno;
    ListenerRegistration listenerCambiosPartida;
    private string partidaId;
    private string uidActual;

    private string uidJugadorA;
    private string uidJugadorB;
    private string turnoActual;
    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
        uidActual = auth.CurrentUser.UserId;

        partidaId = PlayerPrefs.GetString("partidaIdQuimicados");
        BtnActivarCategoria.interactable = false;

        StartCoroutine(VerificarConexionPeriodicamente());

        if (string.IsNullOrEmpty(partidaId))
        {
            Debug.LogError("No se encontró el ID de la partida");
            return;
        }
        BtnComenzar.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Cuestionario");
        });
        BtnVolver.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Quimicados");
        });

        CargarPartida();

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

        if (request.result != UnityWebRequest.Result.Success)
        {
            PanelSinInternet.SetActive(true);
        }
        else
        {
            PanelSinInternet.SetActive(false);
        }
    }
    void OnDestroy()
    {
        if (listenerCambiosPartida != null)
            listenerCambiosPartida.Stop();
    }
    void CargarPartida()
    {
        string[] Categorias = new string[]
        {
        "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
        "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
        "Lantánidos", "Actínoides", "Propiedades Desconocidas"
        };
        string[] CategoriasImg = new string[]
        {
        "MetalesAlcalinos", "MetalesAlcalinoterreos", "MetalesTransicion",
        "MetalesPotransicionales", "Metaloides", "NoMetalesReactivos", "GasesNobles",
        "Lantanidos", "Actinoides", "PropiedadesDesconocidas"
        };

        db.Collection("partidasQuimicados").Document(partidaId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                DocumentSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Dictionary<string, object> datos = snapshot.ToDictionary();

                    uidJugadorA = datos["jugadorA"].ToString();
                    uidJugadorB = datos["jugadorB"].ToString();
                    turnoActual = datos["turnoActual"].ToString();
                    TxtRonda.text = "Ronda " + datos["rondaActual"].ToString();

                    PlayerPrefs.SetString("uidJugadorAQuimicados", uidJugadorA);
                    PlayerPrefs.SetString("uidJugadorBQuimicados", uidJugadorB);

                    MostrarNombresJugadores();
                    MostrarTurno();
                    CalcularLogro();
                    //Actualizar turno en tiempo real
                    EscucharCambiosPartida(partidaId);

                    LoadCoronaProgress();
                    coronasA = 0;
                    coronasB = 0;

                    // Cargar para jugador A
                    if (datos.ContainsKey("CategoriasJugadorA"))
                    {
                        int i = 0;
                        Dictionary<string, object> categoriasA = datos["CategoriasJugadorA"] as Dictionary<string, object>;

                        foreach (string categoria in Categorias)
                        {
                            bool completada = categoriasA.ContainsKey(categoria) && Convert.ToBoolean(categoriasA[categoria]);
                            if (completada) coronasA++;
                            InstanciarCategoria(CategoriasImg[i], completada, ContentCategoriasCompletadasA);
                            i++;
                        }
                    }

                    // Cargar para jugador B
                    if (datos.ContainsKey("CategoriasJugadorB"))
                    {
                        int i = 0;
                        Dictionary<string, object> categoriasB = datos["CategoriasJugadorB"] as Dictionary<string, object>;

                        foreach (string categoria in Categorias)
                        {
                            bool completada = categoriasB.ContainsKey(categoria) && Convert.ToBoolean(categoriasB[categoria]);
                            if (completada) coronasB++;
                            InstanciarCategoria(CategoriasImg[i], completada, ContentCategoriasCompletadasB);
                            i++;
                        }
                    }

                    // ✅ Verificar victoria
                    VerificarVictoria(partidaId, coronasA, coronasB, uidJugadorA, uidJugadorB);

                }
                else
                {
                    Debug.LogError("La partida no existe en Firestore.");
                }
            }
            else
            {
                Debug.LogError("Error al cargar partida: " + task.Exception);
            }
        });
    }
    void VerificarVictoria(string partidaId, int coronasA, int coronasB, string uidA, string uidB)
    {
        string miUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        if (coronasA >= 10)
        {
            if (miUid == uidA)
                MostrarVictoria(true);
            else
                MostrarVictoria(false);

            EliminarPartida(partidaId);
        }
        else if (coronasB >= 10)
        {
            if (miUid == uidB)
                MostrarVictoria(true);
            else
                MostrarVictoria(false);

            EliminarPartida(partidaId);
        }
    }
    void EliminarPartida(string partidaId)
    {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        db.Collection("partidasQuimicados").Document(partidaId).DeleteAsync();
    }
    void MostrarVictoria(bool gane)
    {
        PanelResultado.SetActive(true);
        if (gane)
        {
            Debug.Log("🎉 ¡Ganaste la partida!");
            TxtResultado.text = "¡GANASTE!";
            TxtExp.text = "Exp Ganada \n 30 EXP";
            // Mostrar panel de victoria, cambiar de escena, sumar puntos, etc.
        }
        else
        {
            Debug.Log("😢 Perdiste la partida.");
            TxtResultado.text = "¡PERDISTE :(!";
            TxtExp.text = "Exp Ganada \n 5 EXP";
            // Mostrar mensaje de derrota si quieres.
        }
    }

    void EscucharCambiosPartida(string partidaId)
    {
        listenerCambiosPartida = db.Collection("partidasQuimicados").Document(partidaId)
            .Listen(snapshot =>
            {
                if (!snapshot.Exists)
                {
                    Debug.LogWarning("La partida ya no existe.");
                    return;
                }

                var datos = snapshot.ToDictionary();
                turnoActual = datos["turnoActual"].ToString();
                // Verificar si ambos jugadores fallaron
                if (datos.ContainsKey("fallos"))
                {
                    Dictionary<string, object> fallos = datos["fallos"] as Dictionary<string, object>;

                    bool falloA = fallos.ContainsKey(uidJugadorA) && Convert.ToBoolean(fallos[uidJugadorA]);
                    bool falloB = fallos.ContainsKey(uidJugadorB) && Convert.ToBoolean(fallos[uidJugadorB]);

                    if (falloA && falloB)
                    {
                        int rondaActual = Convert.ToInt32(datos["rondaActual"]);
                        int nuevaRonda = rondaActual + 1;

                        // Actualiza ronda y reinicia fallos
                        Dictionary<string, object> actualizaciones = new Dictionary<string, object>
                        {
                        { "rondaActual", nuevaRonda },
                        { $"fallos.{uidJugadorA}", false },
                        { $"fallos.{uidJugadorB}", false }
                        };

                        db.Collection("partidasQuimicados").Document(partidaId)
                            .UpdateAsync(actualizaciones).ContinueWithOnMainThread(task =>
                            {
                                if (task.IsCompletedSuccessfully)
                                {
                                    Debug.Log($"✅ Nueva ronda: {nuevaRonda} | Fallos reiniciados");
                                    TxtRonda.text = "Ronda " + nuevaRonda.ToString();
                                }
                                else
                                {
                                    Debug.LogError("❌ Error al actualizar la ronda: " + task.Exception);
                                }
                            });
                    }
                }

                // Actualizar visual ronda siempre que cambie
                if (datos.ContainsKey("rondaActual"))
                {
                    TxtRonda.text = "Ronda " + datos["rondaActual"].ToString();
                }

                // Actualizar nombres, progreso, turno, etc.
                MostrarTurno();
                LoadCoronaProgress();
            });
    }
    void MostrarNombresJugadores()
    {
        // Obtener nombres desde la colección "usuarios"
        db.Collection("users").Document(uidJugadorA).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                txtNombreJugadorA.text = task.Result.ContainsField("DisplayName") ?
                    task.Result.GetValue<string>("DisplayName") : "Jugador A";

                ImgAvatarUserA.gameObject.SetActive(true);

                string rango = task.Result.ContainsField("Rango") ?
                    task.Result.GetValue<string>("Rango") : "Novato de laboratorio";

                // Cargar el sprite desde la carpeta Resources (asegúrate de que la imagen esté en Assets/Resources/Avatares/)
                Sprite avatarSprite = Resources.Load<Sprite>(ObtenerRutaAvatar(rango));

                if (avatarSprite != null)
                    ImgAvatarUserA.sprite = avatarSprite;
                else
                    Debug.LogWarning("No se encontró el sprite para el rango: " + rango);
            }
        });

        db.Collection("users").Document(uidJugadorB).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                txtNombreJugadorB.text = task.Result.ContainsField("DisplayName") ?
                    task.Result.GetValue<string>("DisplayName") : "Jugador B";

                ImgAvatarUserB.gameObject.SetActive(true);

                string rango = task.Result.ContainsField("Rango") ?
                    task.Result.GetValue<string>("Rango") : "Novato de laboratorio";

                // Cargar el sprite desde la carpeta Resources (asegúrate de que la imagen esté en Assets/Resources/Avatares/)
                Sprite avatarSprite = Resources.Load<Sprite>(ObtenerRutaAvatar(rango));

                if (avatarSprite != null)
                    ImgAvatarUserB.sprite = avatarSprite;
                else
                    Debug.LogWarning("No se encontró el sprite para el rango: " + rango);
            }
        });
    }
    void InstanciarCategoria(string nombreCategoria, bool completada, Transform content)
    {
        GameObject instancia = Instantiate(PrefabCategoriaCompletada, content);

        // Obtener referencias dentro del prefab
        Image img = instancia.transform.Find("ImgCategoria").GetComponent<Image>();

        // Cargar imagen desde Resources si tienes imágenes nombradas como las categorías
        Sprite sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreCategoria}");
        if (sprite != null)
        {
            img.sprite = sprite;
            img.gameObject.SetActive(true);
        }

        // Cambiar color si está completado
        if (completada)
        {
            img.color = Color.green;
            txtCoronasA.text = coronasA.ToString();
            txtCoronasB.text = coronasB.ToString();
        }
        else
        {
            img.color = Color.gray;
        }
    }
    string ObtenerRutaAvatar(string rango)
    {
        switch (rango)
        {
            case "Novato de laboratorio": return "Avatares/Rango1";
            case "Aprendiz Atomico": return "Avatares/Rango2";
            case "Promesa quimica": return "Avatares/Rango3";
            case "Cientifico en Formacion": return "Avatares/Rango4";
            case "Experto Molecular": return "Avatares/Rango5";
            case "Maestro de Laboratorio": return "Avatares/Rango6";
            case "Sabio de la tabla": return "Avatares/Rango7";
            case "Leyenda química": return "Avatares/Rango8";
            default: return "Avatares/Rango1";
        }
    }
    void MostrarTurno()
    {
        if (turnoActual == uidActual)
        {
            txtTurno.text = "¡Es tu turno!";
            BtnGirar.interactable = true;
        }
        else
        {
            txtTurno.text = "Turno del oponente...";
            BtnGirar.interactable = false;
        }
    }
    public void CalcularLogro()
    {
        int wasCorrect = PlayerPrefs.GetInt("wasCorrect", 0);
        int wasIncorrect = PlayerPrefs.GetInt("wasIncorrect", 0);
        string miUid = auth.CurrentUser.UserId;
        string uidJugador = (miUid == uidJugadorA) ? uidJugadorA : uidJugadorB;
        string campoCorona = (miUid == uidJugadorA)
                              ? "CoronaJugadorA"
                              : "CoronaJugadorB";

        // actualización atómica en Firestore
        var partidaRef = db.Collection("partidasQuimicados")
                            .Document(partidaId);

        if (wasIncorrect == 1)
        {
            progressImage.fillAmount = 0;
            PlayerPrefs.SetInt("wasIncorrect", 0);

            // ⚠️ Actualizamos fallos[uidJugador] = true y reiniciamos la corona
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { $"fallos.{uidJugador}", true },
                { campoCorona, 0 }
            };

            partidaRef.UpdateAsync(updates)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                        Debug.LogError($"Error al actualizar fallos y coronas: {task.Exception}");
                    else
                        Debug.Log($"✅ Se marcó fallo y reinició corona para {uidJugador}");
                });

            PlayerPrefs.SetInt("wasCorrect", 0);
            LoadCoronaProgress();
            return;
        }

        if (wasCorrect == 0) return;

        partidaRef.UpdateAsync(campoCorona, FieldValue.Increment(1))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError($"Error al incrementar {campoCorona}: {task.Exception}");
                else
                    Debug.Log($"✅ Se incrementó {campoCorona} en +1");
            });
        // Luego de actualizar en server, recarga visual:
        PlayerPrefs.SetInt("wasCorrect", 0);
        LoadCoronaProgress();
    }
    void LoadCoronaProgress()
    {
        string miUid = auth.CurrentUser.UserId;
        // Calcula cuál es el campo que te interesa
        string campoCorona = (miUid == uidJugadorA)
                              ? "CoronaJugadorA"
                              : "CoronaJugadorB";

        var partidaRef = db.Collection("partidasQuimicados")
                            .Document(partidaId);

        partidaRef.GetSnapshotAsync()
            .ContinueWithOnMainThread(async task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error leyendo progreso de corona: " + task.Exception);
                    return;
                }

                var snap = task.Result;
                if (!snap.Exists)
                {
                    Debug.LogError("La partida no existe al leer corona.");
                    return;
                }

                // Lee el valor (entero)
                int coronaCount = snap.GetValue<int>(campoCorona);

                if (coronaCount == 1)
                {
                    string nombreArchivo = "ImgProgresoo1";
                    progressImage.sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreArchivo}");
                }else if (coronaCount == 2)
                {
                    string nombreArchivo = "ImgProgresoo2";
                    progressImage.sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreArchivo}");
                }else if (coronaCount == 3)
                {
                    string nombreArchivo = "progresofull";
                    progressImage.sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreArchivo}");
                    BtnGirar.interactable = false;
                    BtnActivarCategoria.interactable = true;
                    PanelInfoLogro.SetActive(true);
                    await Task.Delay(3000);
                    PanelInfoLogro.SetActive(false);
                }
                else if (coronaCount == 0)
                {
                    string nombreArchivo = "ImgProgresoo";
                    progressImage.sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{nombreArchivo}");
                }
            });
    }
    public void seleccionarCategoriaLogro()
    {
        PopUpSeleccionarCategoria.SetActive(true);

        int i = 0;
        string[] Categorias = new string[]
        {
        "Metales Alcalinos", "Metales Alcalinotérreos", "Metales de Transición",
        "Metales Postransicionales", "Metaloides", "No Metales Reactivos", "Gases Nobles",
        "Lantánidos", "Actínoides", "Propiedades Desconocidas"
        };
        string[] CategoriasImg = new string[]
        {
        "MetalesAlcalinos", "MetalesAlcalinoterreos", "MetalesTransicion",
        "MetalesPotransicionales", "Metaloides", "NoMetalesReactivos", "GasesNobles",
        "Lantanidos", "Actinoides", "PropiedadesDesconocidas"
        };

        // Limpia contenido previo del scroll
        foreach (Transform child in ContentCategorias)
        {
            Destroy(child.gameObject);
        }

        // Lista para rastrear todos los prefabs instanciados
        List<GameObject> categoriasInstanciadas = new List<GameObject>();

        foreach (string categoria in Categorias)
        {
            GameObject categoriaGO = Instantiate(PrefabSeleccionarCategoria, ContentCategorias);
            categoriasInstanciadas.Add(categoriaGO); // Guardar referencia

            var ImgCategoria = categoriaGO.transform.Find("ImgCategoria").GetComponent<Image>();
            var NombreCategoria = categoriaGO.transform.Find("TxtNombreCategoria").GetComponent<TMP_Text>();
            var selectButton = categoriaGO.transform.Find("BtnSeleccionCategioria").GetComponent<Button>();

            // Establecer nombre
            NombreCategoria.text = categoria;

            // Cargar imagen desde Resources/images/CategoriasQuimicados/NOMBRE.png
            Sprite sprite = Resources.Load<Sprite>($"images/CategoriasQuimicados/{CategoriasImg[i]}");
            if (sprite != null)
            {
                ImgCategoria.sprite = sprite;
                ImgCategoria.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"⚠️ No se encontró imagen para la categoría: {CategoriasImg[i]}");
            }

            // Evitar problema con variable de captura en lambda
            string categoriaSeleccionada = categoria;
            GameObject categoriaGOSeleccionada = categoriaGO;

            selectButton.onClick.AddListener(() =>
            {
                Debug.Log($"Seleccionaste la categoría: {categoriaSeleccionada}");
                PlayerPrefs.SetString("CategoriaRuleta", categoriaSeleccionada);

                // Resetear visual de todos
                foreach (GameObject go in categoriasInstanciadas)
                {
                    go.transform.localScale = Vector3.one;
                    go.GetComponent<Image>().color = Color.white;
                }

                // Aplicar efecto visual al seleccionado
                categoriaGOSeleccionada.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                categoriaGOSeleccionada.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f); // gris claro

                BtnConquistar.onClick.RemoveAllListeners();
                BtnConquistar.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetInt("CompletarLogro", 1);
                    PlayerPrefs.SetInt("wasIncorrect", 1);
                    PlayerPrefs.SetInt("wasCorrect", 0);
                    SceneManager.LoadScene("Cuestionario");
                });
            });

            i++;
        }
    }
}
