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

    // Referencias a los otros paneles y botones para poder activar/desactivar
    [SerializeField] private GameObject panelRankingGeneral;
    [SerializeField] private GameObject panelRankingAmigos;
    [SerializeField] private Button btnGeneral;
    [SerializeField] private Button btnAmigos;
    [SerializeField] private Button btnComunidades; // Añadido para poder marcar/desmarcar

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
    public string comunidadSeleccionadaID;

    // Referencia al contenedor del ranking de comunidades para el ScrollToUser
    public Transform rankingContentComunidades;

    // Flag para evitar múltiples llamadas
    private bool isUpdatingRanking = false;

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

        // Asignar el content al ScrollToUser si aún no está asignado
        if (scrollToUser != null && content != null)
        {
            scrollToUser.rankingContentComunidades = content;
        }

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

        // Suscribirse a eventos de activación de otros paneles
        if (rankingGeneralManager != null)
        {
            // Buscar método para suscribirse a evento de activación del panel general
            Button btnRankingGeneral = GameObject.FindWithTag("BtnRankingGeneral")?.GetComponent<Button>();

            if (btnRankingGeneral != null)
            {
                btnRankingGeneral.onClick.AddListener(ResetearPanelComunidades);
            }
        }

        if (rankingAmigosManager != null)
        {
            // Buscar método para suscribirse a evento de activación del panel amigos
            Button btnRankingAmigos = GameObject.FindWithTag("BtnRankingAmigos")?.GetComponent<Button>();
            if (btnRankingAmigos != null)
            {
                btnRankingAmigos.onClick.AddListener(ResetearPanelComunidades);
            }
        }
    }

    // Método para resetear el panel cuando se active otro ranking
    public void ResetearPanelComunidades()
    {
        // Desactivar el panel de comunidades
        if (panelRankingComunidades != null)
            panelRankingComunidades.SetActive(false);

        // Resetear el dropdown a la primera opción
        if (comunidadesDropdown != null)
        {
            comunidadesDropdown.value = 0;
            comunidadesDropdown.RefreshShownValue();
        }

        // Limpiar el ranking
        LimpiarRanking();

        // Desmarcar el botón de comunidades si está marcado
        if (rankingGeneralManager != null && btnComunidades != null)
        {
            rankingGeneralManager.DesmarcarBoton(btnComunidades);
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
                else
                {
                    // Si hay comunidades, seleccionar la primera por defecto (valor 0 = "Selecciona una comunidad")
                    comunidadesDropdown.value = 0;
                    comunidadesDropdown.RefreshShownValue();
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
            if (rankingGeneralManager != null)
            {
                // Desmarcar los otros botones
                if (btnGeneral != null)
                    rankingGeneralManager.DesmarcarBoton(btnGeneral);

                if (btnAmigos != null)
                    rankingGeneralManager.DesmarcarBoton(btnAmigos);

                // Marcar el botón de comunidades si existe
                //if (btnComunidades != null)
                //    rankingGeneralManager.MarcarBoton(btnComunidades);
            }

            // Si tenemos una comunidad seleccionada, actualizar ranking
            if (!string.IsNullOrEmpty(comunidadSeleccionadaID) && comunidadesDropdown.value > 0)
            {
                ObtenerRankingComunidad(comunidadSeleccionadaID);
            }
            else
            {
                // Si no hay comunidad seleccionada, limpiar ranking
                LimpiarRanking();
            }

            // Si tenemos referencia al ScrollToUser, actualizar el modo
            if (scrollToUser != null)
            {
                scrollToUser.CambiarModoRanking(ScrollToUser.ModoRanking.Comunidades);
                scrollToUser.ActualizarUISegunModo();

                // Esperar un momento y hacer scroll a la posición del usuario
                StartCoroutine(HacerScrollDespuesDeActualizar());
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
        // Ignorar si ya estamos actualizando el ranking
        if (isUpdatingRanking)
            return;

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

                // Activar el panel de comunidades automáticamente
                ActivarRankingComunidades();
            }
        }
        else
        {
            // Limpiar la lista si se selecciona "Selecciona una comunidad"
            LimpiarRanking();
            comunidadSeleccionadaID = null;
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
        // Evitar múltiples llamadas simultáneas
        if (isUpdatingRanking)
            return;

        isUpdatingRanking = true;

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
                else
                {
                    // Si no hay miembros, terminar actualización
                    isUpdatingRanking = false;
                }
            }
            else
            {
                Debug.LogError("Error al obtener la comunidad: " + task.Exception);
                isUpdatingRanking = false;
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
            isUpdatingRanking = false;
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
                    isUpdatingRanking = false;
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

        // Variable para almacenar la posición del usuario actual en la comunidad
        int posicionUsuarioEnComunidad = 0;

        // Agregar jugadores a la lista desde la posición 4 en adelante
        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            GameObject jugadorUI = CrearElementoRanking(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp);

            // Resaltar al usuario actual
            if (listaOrdenada[i].id == miUserID)
            {
                ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                jugadorUI.GetComponent<Image>().color = customColor;
                posicionUsuarioEnComunidad = i + 1;
            }
        }

        // Si el usuario no está entre los primeros 3, buscamos su posición
        if (posicionUsuarioEnComunidad == 0)
        {
            posicionUsuarioEnComunidad = listaOrdenada.FindIndex(j => j.id == miUserID) + 1;
        }

        // Si el usuario está entre los primeros 3, resaltamos su posición en el podio
        if (posicionUsuarioEnComunidad > 0 && posicionUsuarioEnComunidad <= 3)
        {
            // Aquí podrías agregar un efecto visual para resaltar al usuario en el podio
        }

        // Actualizar el ScrollToUser con los datos del usuario en la comunidad
        if (scrollToUser != null)
        {
            // Actualiza la posición del usuario en comunidades
            scrollToUser.ActualizarPosicionComunidades(posicionUsuarioEnComunidad);

            // Forzar la actualización del layout
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

            // Si estamos en modo comunidades, hacer scroll a la posición del usuario
            if (scrollToUser.GetModoActual() == ScrollToUser.ModoRanking.Comunidades)
            {
                StartCoroutine(HacerScrollDespuesDeActualizar());
            }
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