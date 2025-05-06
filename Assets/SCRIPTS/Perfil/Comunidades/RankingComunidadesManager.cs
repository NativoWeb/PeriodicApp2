using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Extensions;
using TMPro;
using System.Linq;
using Firebase.Auth;

public class RankingComunidadManager : MonoBehaviour
{
    public GameObject prefabJugador;
    public Transform content;
    public TMP_Dropdown comunidadesDropdown;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string usuarioActualID;
    private string usuarioActualNombre;
    private int usuarioActualXP;

    // Referencias al podio
    public TMP_Text primeroNombre, segundoNombre, terceroNombre;
    public TMP_Text primeroXP, segundoXP, terceroXP;

    // Referencias a los paneles
    [SerializeField] private GameObject RankingComunidadPanel = null;
    public GameObject PanelRankingGeneral;

    // Referencias a los botones
    public Button btnComunidad;
    public Button btnGeneral;
    public Button btnAmigos;

    // Referencia al rankingGeneralManager
    [SerializeField] private RankingGeneralManager rankingGeneralManager;

    // Lista para almacenar IDs y nombres de las comunidades
    private List<string> comunidadesIDs = new List<string>();

    void Start()
    {
        // Inicializar Firebase
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Obtener datos del usuario
        if (auth.CurrentUser != null)
        {
            usuarioActualID = auth.CurrentUser.UserId;
            usuarioActualNombre = auth.CurrentUser.DisplayName;
            ObtenerXPUsuarioActual();
        }

        // Buscar la referencia a rankingGeneralManager si no está asignada
        if (rankingGeneralManager == null)
        {
            rankingGeneralManager = FindFirstObjectByType<RankingGeneralManager>();
        }

        // Configurar el dropdown de comunidades
        if (comunidadesDropdown != null)
        {
            // Limpiar opciones existentes
            comunidadesDropdown.ClearOptions();

            // Agregar el listener para cuando cambie la selección
            comunidadesDropdown.onValueChanged.AddListener(delegate {
                ComunidadSeleccionadaChanged();
            });

            // Cargar las comunidades
            CargarComunidades();
        }

        // Asignar listeners a los botones
        if (btnComunidad != null)
        {
            btnComunidad.onClick.RemoveAllListeners();
            btnComunidad.onClick.AddListener(ActivarRankingComunidad);
        }

        if (btnGeneral != null)
        {
            btnGeneral.onClick.RemoveAllListeners();
            btnGeneral.onClick.AddListener(ActivarRankingGeneral);
        }

        // Desactivar panel inicialmente
        if (RankingComunidadPanel != null)
        {
            RankingComunidadPanel.SetActive(false);
        }
    }

    private void ObtenerXPUsuarioActual()
    {
        db.Collection("users").Document(usuarioActualID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                if (task.Result.TryGetValue<int>("xp", out int xp))
                {
                    usuarioActualXP = xp;
                }
                else
                {
                    usuarioActualXP = 0;
                }
            }
        });
    }

    void CargarComunidades()
    {
        // Limpiar listas
        comunidadesDropdown.ClearOptions();
        comunidadesIDs.Clear();

        List<TMP_Dropdown.OptionData> opcionesComunidades = new List<TMP_Dropdown.OptionData>();

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
                        // Verificar si mi ID está en la lista de miembros
                        bool soyMiembro = false;

                        // Comprobamos si 'miembros' es una lista
                        if (datos["miembros"] is List<object> miembros)
                        {
                            foreach (object miembro in miembros)
                            {
                                // Si el miembro es directamente un string con ID
                                if (miembro is string && miembro.ToString() == usuarioActualID)
                                {
                                    soyMiembro = true;
                                    break;
                                }
                                // Si el miembro es un objeto que contiene ID
                                else if (miembro is Dictionary<string, object> miembroDict)
                                {
                                    foreach (var item in miembroDict)
                                    {
                                        if (item.Value != null && item.Value.ToString() == usuarioActualID)
                                        {
                                            soyMiembro = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (soyMiembro)
                        {
                            // Obtener el nombre de la comunidad
                            string nombreComunidad = datos.ContainsKey("nombre") ? datos["nombre"].ToString() : documento.Id;

                            // Agregar a lista de opciones
                            opcionesComunidades.Add(new TMP_Dropdown.OptionData(nombreComunidad));

                            // Guardar el ID en la lista de IDs
                            comunidadesIDs.Add(documento.Id);

                            Debug.Log("Comunidad añadida: " + nombreComunidad + " (ID: " + documento.Id + ")");
                        }
                    }
                }

                // Actualizar el dropdown con las comunidades encontradas
                if (opcionesComunidades.Count > 0)
                {
                    comunidadesDropdown.AddOptions(opcionesComunidades);

                    // Cargar el ranking de la primera comunidad automáticamente
                    ComunidadSeleccionadaChanged();
                }
                else
                {
                    // Si no hay comunidades, mostrar opción por defecto
                    comunidadesDropdown.AddOptions(new List<TMP_Dropdown.OptionData>
                    {
                        new TMP_Dropdown.OptionData("No perteneces a ninguna comunidad")
                    });
                }
            }
        });
    }

    void ComunidadSeleccionadaChanged()
    {
        int selectedIndex = comunidadesDropdown.value;

        // Verificar que el índice sea válido
        if (selectedIndex >= 0 && selectedIndex < comunidadesIDs.Count)
        {
            string comunidadID = comunidadesIDs[selectedIndex];
            ObtenerRankingComunidad(comunidadID);
        }
    }

    public void ActivarRankingComunidad()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            // Activar nuestro panel
            if (RankingComunidadPanel != null)
            {
                RankingComunidadPanel.SetActive(true);
            }

            // Desactivar otros paneles
            if (PanelRankingGeneral != null)
            {
                PanelRankingGeneral.SetActive(false);
            }

            // Marcar el botón de comunidad como seleccionado
            if (rankingGeneralManager != null && btnComunidad != null)
            {
                rankingGeneralManager.MarcarBotonSeleccionado(btnComunidad);

                // Desmarcar otros botones
                if (btnGeneral != null)
                {
                    rankingGeneralManager.DesmarcarBoton(btnGeneral);
                }
                if (btnAmigos != null)
                {
                    rankingGeneralManager.DesmarcarBoton(btnAmigos);
                }
            }

            // Cargar el ranking de la comunidad seleccionada
            ComunidadSeleccionadaChanged();
        }
    }

    public void ActivarRankingGeneral()
    {
        string estadouser = PlayerPrefs.GetString("Estadouser", "");
        if (estadouser == "nube")
        {
            // Desactivar nuestro panel
            if (RankingComunidadPanel != null)
            {
                RankingComunidadPanel.SetActive(false);
            }

            // Activar panel de ranking general
            if (PanelRankingGeneral != null)
            {
                PanelRankingGeneral.SetActive(true);

                // Llamar al método ObtenerRanking del rankingGeneralManager
                if (rankingGeneralManager != null)
                {
                    rankingGeneralManager.ObtenerRanking();
                }
            }

            // Marcar el botón general como seleccionado
            if (rankingGeneralManager != null && btnGeneral != null)
            {
                rankingGeneralManager.MarcarBotonSeleccionado(btnGeneral);

                // Desmarcar otros botones
                if (btnComunidad != null)
                {
                    rankingGeneralManager.DesmarcarBoton(btnComunidad);
                }
                if (btnAmigos != null)
                {
                    rankingGeneralManager.DesmarcarBoton(btnAmigos);
                }
            }
        }
    }

    public void ObtenerRankingComunidad(string comunidadID)
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

        // Obtener el documento de la comunidad para acceder a los miembros
        db.Collection("comunidades").Document(comunidadID).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                Dictionary<string, object> datos = task.Result.ToDictionary();

                // Verificar si la comunidad tiene miembros
                if (datos.ContainsKey("miembros") && datos["miembros"] is List<object> miembros)
                {
                    List<string> idsMiembros = new List<string>();

                    // Extraer los IDs de los miembros
                    foreach (object miembro in miembros)
                    {
                        if (miembro is string)
                        {
                            idsMiembros.Add(miembro.ToString());
                        }
                        else if (miembro is Dictionary<string, object> miembroDict)
                        {
                            // Si es un objeto, buscamos el valor del ID
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

                    // Obtener datos de todos los miembros
                    ObtenerDatosMiembros(idsMiembros);
                }
                else
                {
                    Debug.Log("La comunidad no tiene miembros o el formato es incorrecto");
                }
            }
        });
    }

    private void ObtenerDatosMiembros(List<string> idsMiembros)
    {
        List<(string id, string nombre, int xp)> listaMiembros = new List<(string, string, int)>();

        // Si no hay miembros, mostrar mensaje vacío
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

        // Agregar miembros a la lista desde la posición 4 en adelante
        for (int i = 3; i < listaOrdenada.Count; i++)
        {
            GameObject miembroUI = CrearElementoRanking(i + 1, listaOrdenada[i].nombre, listaOrdenada[i].xp);

            // Resaltar al usuario actual
            if (listaOrdenada[i].id == usuarioActualID)
            {
                ColorUtility.TryParseHtmlString("#E6FFED", out Color customColor);
                miembroUI.GetComponent<Image>().color = customColor;
            }
        }

        // Si el usuario no está entre los primeros 3, buscar su posición
        int posicionUsuario = listaOrdenada.FindIndex(j => j.id == usuarioActualID) + 1;

        // Si el usuario está entre los primeros 3, no necesitamos hacer nada adicional
        // El podio ya muestra al usuario resaltado
    }

    GameObject CrearElementoRanking(int posicion, string nombre, int xp)
    {
        GameObject miembroUI = Instantiate(prefabJugador, content);
        TMP_Text nombreTMP = miembroUI.transform.Find("Nombre").GetComponent<TMP_Text>();
        TMP_Text xpTMP = miembroUI.transform.Find("XP").GetComponent<TMP_Text>();
        TMP_Text posicionTMP = miembroUI.transform.Find("Posicion").GetComponent<TMP_Text>();

        nombreTMP.text = nombre;
        xpTMP.text = "EXP \n" + xp;
        posicionTMP.text = "#" + posicion.ToString();

        return miembroUI;
    }
}