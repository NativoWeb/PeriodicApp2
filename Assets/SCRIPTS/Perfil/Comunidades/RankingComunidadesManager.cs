using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
using TMPro;
using System.Linq;
using UnityEngine.EventSystems;
using System.Collections;

public class RankingComunidadesManager : MonoBehaviour
{
    //instanciamos dropdown
    [SerializeField] public TMP_Dropdown comunidadesDropdown;

    // Referencia a los otros scripts de ranking
    [SerializeField] private RankingGeneralManager rankingGeneralManager;
    [SerializeField] private RankingAmigosManager rankingAmigosManager;

    // Prefab y contenedor para la lista de jugadores
    [SerializeField] private GameObject prefabJugador;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject panelRankingComunidades;

    // Referencias al podio
    [SerializeField] private TMP_Text primeroNombre, segundoNombre, terceroNombre;
    [SerializeField] private TMP_Text primeroXP, segundoXP, terceroXP;

    // Botón para este ranking
    [SerializeField] private Button btnComunidades;

    // Referencias a los otros paneles y botones para poder activar/desactivar
    [SerializeField] private GameObject panelRankingGeneral;
    [SerializeField] private GameObject panelRankingAmigos;
    [SerializeField] private Button btnGeneral;
    [SerializeField] private Button btnAmigos;

    // Referencia al ScrollToUser para coordinar las actualizaciones
    [SerializeField] private ScrollToUser scrollToUser;

    // Variables para Firebase
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string miUserID;
    private string miUserName;
    private int miUserXP;

    // Almacenar las comunidades
    private Dictionary<string, string> comunidadesDict = new Dictionary<string, string>(); // <Nombre, ID>
    private string comunidadSeleccionadaID;

    void Start()
    {
        // Inicializar Firebase Auth y Firestore
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Buscar referencias si no están asignadas
        if (rankingGeneralManager == null)
            rankingGeneralManager = FindFirstObjectByType<RankingGeneralManager>();

        if (rankingAmigosManager == null)
            rankingAmigosManager = FindFirstObjectByType<RankingAmigosManager>();

        if (scrollToUser == null)
            scrollToUser = FindFirstObjectByType<ScrollToUser>();

        // Verificar si hay un usuario autenticado
        if (auth.CurrentUser != null)
        {
            // Obtener el ID del usuario actual
            miUserID = auth.CurrentUser.UserId;
            miUserName = auth.CurrentUser.DisplayName;
            Debug.Log("Usuario autenticado: " + miUserID);

            // Obtener XP del usuario actual
            ObtenerXPUsuarioActual();

            // Configurar el dropdown
            ConfigurarDropdown();

            // Configurar el listener del botón
            if (btnComunidades != null)
            {
                btnComunidades.onClick.RemoveAllListeners();
                btnComunidades.onClick.AddListener(ActivarRankingComunidades);
            }

            // Configurar listener del dropdown
            comunidadesDropdown.onValueChanged.AddListener(OnComunidadSeleccionada);

            // Por defecto, panel desactivado
            if (panelRankingComunidades != null)
                panelRankingComunidades.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No hay ningún usuario autenticado");
            if (comunidadesDropdown != null)
                comunidadesDropdown.AddOptions(new List<string> { "Inicia sesión para ver tus comunidades" });
        }
    }

    private void ObtenerXPUsuarioActual()
    {
        db.Collection("users").Document(miUserID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                if (task.Result.TryGetValue<int>("xp", out int xp))
                {
                    miUserXP = xp;
                }
                else
                {
                    miUserXP = 0;
                }
            }
        });
    }

    private void ConfigurarDropdown()
    {
        // Limpiar el dropdown y el diccionario
        comunidadesDropdown.ClearOptions();
        comunidadesDict.Clear();

        // Agregar opción por defecto
        List<string> opciones = new List<string> { "Selecciona una comunidad" };
        comunidadesDropdown.AddOptions(opciones);

        // Cargar las comunidades
        CargarComunidades();
    }

    void CargarComunidades()
    {
        // Lista para almacenar las opciones del dropdown
        List<string> opcionesComunidades = new List<string>();

        // Referencia a la colección de comunidades
        CollectionReference comunidadesRef = db.Collection("comunidades");

        comunidadesRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                QuerySnapshot snapshot = task.Result;

                foreach (DocumentSnapshot documento in snapshot.Documents)
                {
                    // Obtener los datos de la comunidad
                    Dictionary<string, object> datos = documento.ToDictionary();

                    // Verificar si la comunidad tiene un campo 'miembros'
                    if (datos.ContainsKey("miembros"))
                    {
                        // Obtener la lista de miembros
                        List<object> miembros = datos["miembros"] as List<object>;

                        // Verificar si mi ID está en la lista de miembros
                        bool soyMiembro = false;

                        foreach (object miembro in miembros)
                        {
                            // Si el miembro es directamente un string
                            if (miembro is string && miembro.ToString() == miUserID)
                            {
                                soyMiembro = true;
                                break;
                            }
                            // Si el miembro es un objeto (como se ve en tu imagen, parece tener una estructura más compleja)
                            else if (miembro is Dictionary<string, object> miembroDict)
                            {
                                foreach (var item in miembroDict)
                                {
                                    if (item.Value != null && item.Value.ToString() == miUserID)
                                    {
                                        soyMiembro = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (soyMiembro)
                        {
                            // Agregar el nombre de la comunidad a las opciones del dropdown
                            string nombreComunidad = datos.ContainsKey("nombre") ? datos["nombre"].ToString() : documento.Id;
                            opcionesComunidades.Add(nombreComunidad);

                            // Guardar la relación nombre-ID
                            comunidadesDict[nombreComunidad] = documento.Id;

                            Debug.Log("Eres miembro de la comunidad: " + nombreComunidad);
                        }
                    }
                }

                // Actualizar el dropdown con las comunidades encontradas
                comunidadesDropdown.AddOptions(opcionesComunidades);

                // Si no hay comunidades, agregar una opción por defecto
                if (opcionesComunidades.Count == 0)
                {
                    comunidadesDropdown.ClearOptions();
                    comunidadesDropdown.AddOptions(new List<string> { "No perteneces a ninguna comunidad" });
                }
            }
            else
            {
                Debug.LogError("Error al obtener las comunidades: " + task.Exception);
            }
        });
    }

    public void ActivarRankingComunidades()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            // Activar nuestro panel
            panelRankingComunidades.SetActive(true);

            // Desactivar los otros paneles
            if (panelRankingGeneral != null)
                panelRankingGeneral.SetActive(false);

            if (panelRankingAmigos != null)
                panelRankingAmigos.SetActive(false);

            // Marcar el botón de comunidades como seleccionado
            if (rankingGeneralManager != null && btnComunidades != null)
            {
                rankingGeneralManager.MarcarBotonSeleccionado(btnComunidades);

                // Desmarcar los otros botones
                if (btnGeneral != null)
                    rankingGeneralManager.DesmarcarBoton(btnGeneral);

                if (btnAmigos != null)
                    rankingGeneralManager.DesmarcarBoton(btnAmigos);
            }

            // Si tenemos una comunidad seleccionada, actualizar ranking
            if (comunidadSeleccionadaID != null && comunidadSeleccionadaID.Length > 0)
            {
                ObtenerRankingComunidad(comunidadSeleccionadaID);
            }

            // Si tenemos referencia al ScrollToUser, actualizar el modo
            if (scrollToUser != null)
            {
                // Asumo que deberías agregar un nuevo modo en ScrollToUser
                if (typeof(ScrollToUser.ModoRanking).GetField("Comunidades") != null)
                {
                    scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.Comunidades);
                    scrollToUser.ActualizarUISegunModo();

                    // Esperar un momento y hacer scroll a la posición del usuario
                    StartCoroutine(HacerScrollDespuesDeActualizar());
                }
            }
        }
    }

    private IEnumerator HacerScrollDespuesDeActualizar()
    {
        // Esperar un momento para que se actualice el contenido
        yield return new WaitForSeconds(0.5f);

        // Hacer scroll a la posición del usuario
        if (scrollToUser != null)
        {
            scrollToUser.ScrollToUserPosition();
        }
    }

    public void OnComunidadSeleccionada(int index)
    {
        // Ignorar la selección por defecto (índice 0)
        if (index > 0)
        {
            string nombreComunidad = comunidadesDropdown.options[index].text;

            if (comunidadesDict.TryGetValue(nombreComunidad, out string comunidadID))
            {
                comunidadSeleccionadaID = comunidadID;
                Debug.Log("Comunidad seleccionada: " + nombreComunidad + " (ID: " + comunidadID + ")");

                // Obtener ranking de esta comunidad
                ObtenerRankingComunidad(comunidadID);
            }
        }
        else
        {
            // Limpiar la lista si se selecciona "Selecciona una comunidad"
            LimpiarRanking();
        }
    }

    private void LimpiarRanking()
    {
        // Limpiar lista anterior
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // Resetear textos del podio
        primeroNombre.text = "---";
        primeroXP.text = "0 xp";
        segundoNombre.text = "---";
        segundoXP.text = "0 xp";
        terceroNombre.text = "---";
        terceroXP.text = "0 xp";
    }

    public void ObtenerRankingComunidad(string comunidadID)
    {
        // Limpiar el ranking anterior
        LimpiarRanking();

        // Obtener los miembros de la comunidad
        db.Collection("comunidades").Document(comunidadID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                // Obtener la lista de miembros
                Dictionary<string, object> datos = task.Result.ToDictionary();

                if (datos.ContainsKey("miembros"))
                {
                    List<string> idsMiembros = new List<string>();

                    // Obtener los IDs de los miembros
                    List<object> miembros = datos["miembros"] as List<object>;

                    foreach (object miembro in miembros)
                    {
                        // Si el miembro es directamente un string
                        if (miembro is string)
                        {
                            idsMiembros.Add(miembro.ToString());
                        }
                        // Si el miembro es un objeto
                        else if (miembro is Dictionary<string, object> miembroDict)
                        {
                            foreach (var item in miembroDict)
                            {
                                if (item.Value != null)
                                {
                                    idsMiembros.Add(item.Value.ToString());
                                    break;
                                }
                            }
                        }
                    }

                    // Una vez tenemos todos los IDs de miembros, obtenemos sus datos
                    ObtenerDatosMiembros(idsMiembros);
                }
            }
            else
            {
                Debug.LogError("Error al obtener la comunidad: " + task.Exception);
            }
        });
    }

    private void ObtenerDatosMiembros(List<string> idsMiembros)
    {
        // Lista para almacenar los datos de los miembros
        List<(string id, string nombre, int xp)> listaMiembros = new List<(string, string, int)>();

        // Si no hay miembros, mostrar mensaje
        if (idsMiembros.Count == 0)
        {
            MostrarRankingFinal(listaMiembros);
            return;
        }

        // Contador para saber cuándo hemos procesado a todos los miembros
        int contadorMiembros = 0;

        foreach (string idMiembro in idsMiembros)
        {
            db.Collection("users").Document(idMiembro).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                contadorMiembros++;

                if (task.IsCompleted && task.Result.Exists)
                {
                    string nombre = task.Result.GetValue<string>("DisplayName");
                    int xp = 0;

                    if (task.Result.TryGetValue<int>("xp", out int xpValue))
                    {
                        xp = xpValue;
                    }

                    listaMiembros.Add((idMiembro, nombre, xp));
                }

                // Si ya procesamos a todos los miembros, mostramos el ranking
                if (contadorMiembros >= idsMiembros.Count)
                {
                    MostrarRankingFinal(listaMiembros);
                }
            });
        }
    }

    private void MostrarRankingFinal(List<(string id, string nombre, int xp)> listaMiembros)
    {
        // Ordenar por XP de mayor a menor
        var listaOrdenada = listaMiembros.OrderByDescending(j => j.xp).ToList();

        // Asignar valores al podio
        if (listaOrdenada.Count > 0)
        {
            primeroNombre.text = listaOrdenada[0].nombre;
            primeroXP.text = listaOrdenada[0].xp + " xp";
        }
        if (listaOrdenada.Count > 1)
        {
            segundoNombre.text = listaOrdenada[1].nombre;
            segundoXP.text = listaOrdenada[1].xp + " xp";
        }
        if (listaOrdenada.Count > 2)
        {
            terceroNombre.text = listaOrdenada[2].nombre;
            terceroXP.text = listaOrdenada[2].xp + " xp";
        }

        // Agregar jugadores a la lista desde la posición 4 en adelante
        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            GameObject jugadorUI = CrearElementoRanking(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp);

            // Resaltar al usuario actual
            if (listaOrdenada[i].id == miUserID)
            {
                ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                jugadorUI.GetComponent<Image>().color = customColor;
            }
        }

        // Si el usuario no está entre los primeros 3, buscamos su posición
        int posicionUsuario = listaOrdenada.FindIndex(j => j.id == miUserID) + 1;

        // Si el usuario está entre los primeros 3, resaltamos su posición en el podio
        if (posicionUsuario <= 3 && posicionUsuario > 0)
        {
            // Aquí podrías agregar un efecto visual para resaltar al usuario en el podio
        }
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