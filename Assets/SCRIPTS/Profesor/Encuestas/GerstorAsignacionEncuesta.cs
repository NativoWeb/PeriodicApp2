using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GestorAsignacionEncuesta : MonoBehaviour
{
    [Header("Referencias del Panel")]
    [SerializeField] private GameObject panelDetalles;
    [SerializeField] private GameObject panelAsignacion;
    [SerializeField] private TMP_Text txtTituloEncuestaAsignar;

    [Header("Comunidades")]
    [SerializeField] private GameObject comunidadTogglePrefab;
    [SerializeField] private Transform contenedorComunidadesScroll; // El objeto "Content" del ScrollView

    [Header("Configuraci�n de Asignaci�n")]
    [SerializeField] private TMP_Dropdown dropdownMinimoPreguntas;
    [SerializeField] private Toggle toggleAleatorizarPreguntas;
    [SerializeField] private Toggle toggleAleatorizarRespuestas;
    [SerializeField] private TMP_InputField inputIntentos;

    [Header("Botones")]
    [SerializeField] private Button btnGuardarAsignacion; // Bot�n para "Activar" o guardar
    [SerializeField] private Button btnCerrarPanel; // Bot�n para "Desactivar" o cancelar

    // Variables internas
    private string encuestaIdActual;
    private int numTotalPreguntas;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private Dictionary<string, Toggle> togglesDeComunidades = new Dictionary<string, Toggle>();

    void Start()
    {
        // Inicializaci�n de Firebase
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Configurar listeners de los botones del panel
        btnGuardarAsignacion.onClick.AddListener(GuardarConfiguracionAsignacion);
        btnCerrarPanel.onClick.AddListener(() => panelAsignacion.SetActive(false));

        // Configurar restricciones del InputField de intentos
        inputIntentos.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputIntentos.onValueChanged.AddListener(ValidarIntentos);
    }

    /// <summary>
    /// M�todo p�blico para ser llamado desde el exterior. Inicia todo el proceso.
    /// </summary>
    public async void AbrirPanelDeAsignacion(string idEncuesta, string titulo, int totalPreguntas)
    {
        encuestaIdActual = idEncuesta;
        numTotalPreguntas = totalPreguntas;

        // Limpiar estado anterior
        LimpiarPanel();

        // Configurar UI b�sica
        txtTituloEncuestaAsignar.text = titulo;
        PoblarDropdownPreguntas(totalPreguntas);
        panelDetalles.SetActive(false);
        panelAsignacion.SetActive(true);

        // Cargar datos de forma as�ncrona
        await CargarComunidadesDelUsuario();
        await CargarConfiguracionExistente();
    }

    private void LimpiarPanel()
    {
        // Limpiar toggles antiguos
        foreach (Transform child in contenedorComunidadesScroll)
        {
            Destroy(child.gameObject);
        }
        togglesDeComunidades.Clear();

        // Resetear controles a su estado por defecto
        dropdownMinimoPreguntas.ClearOptions();
        toggleAleatorizarPreguntas.isOn = false;
        toggleAleatorizarRespuestas.isOn = false;
        inputIntentos.text = "1";
    }

    private void PoblarDropdownPreguntas(int totalPreguntas)
    {
        List<string> opciones = new List<string>();
        for (int i = 1; i <= totalPreguntas; i++)
        {
            opciones.Add(i.ToString());
        }
        dropdownMinimoPreguntas.AddOptions(opciones);
    }

    private async Task CargarComunidadesDelUsuario()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Usuario no autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        Query query = db.Collection("comunidades").WhereArrayContains("miembros", userId);
        Debug.Log($"Buscando comunidades para el usuario {userId} donde sea miembro...");

        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        Debug.Log($"Se encontraron {snapshot.Documents.Count()} comunidades.");

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            string idComunidad = doc.Id;
            if (togglesDeComunidades.ContainsKey(idComunidad)) continue;

            // --- PASO 1: VERIFICAR LOS DATOS ---
            // �Estamos obteniendo el nombre correctamente desde Firebase?
            string nombreComunidad = doc.GetValue<string>("nombre");
            Debug.Log($"Procesando comunidad ID: {idComunidad}, Nombre: '{nombreComunidad}'");

            if (string.IsNullOrEmpty(nombreComunidad))
            {
                Debug.LogWarning($"El nombre para la comunidad {idComunidad} est� vac�o o no existe en Firestore.");
            }

            GameObject toggleObj = Instantiate(comunidadTogglePrefab, contenedorComunidadesScroll);
            Toggle toggle = toggleObj.GetComponent<Toggle>();

            // --- PASO 2: VERIFICAR EL COMPONENTE DE TEXTO ---
            // Intentamos obtener el componente TextMeshPro
            TMP_Text labelTMP = toggleObj.GetComponentInChildren<TMP_Text>(true); // Usamos (true) para incluir inactivos

            if (labelTMP != null)
            {
                Debug.Log($"Componente TMP_Text encontrado para {idComunidad}. Asignando texto.");
                labelTMP.text = nombreComunidad;
            }
            else
            {
                // Si no se encontr�, probamos con el componente de Texto antiguo
                Debug.LogWarning($"No se encontr� TMP_Text para {idComunidad}. Intentando buscar UnityEngine.UI.Text...");
                Text labelLegacy = toggleObj.GetComponentInChildren<Text>(true);

                if (labelLegacy != null)
                {
                    Debug.Log($"Componente UnityEngine.UI.Text encontrado para {idComunidad}. Asignando texto.");
                    labelLegacy.text = nombreComunidad;
                }
                else
                {
                    // Si ninguno de los dos se encontr�, el problema est� en el prefab.
                    Debug.LogError($"�ERROR GRAVE! No se encontr� NING�N componente de texto (ni TMP_Text ni Text) en el prefab instanciado para la comunidad {idComunidad}. Revisa la estructura del prefab '{comunidadTogglePrefab.name}'.");
                }
            }

            togglesDeComunidades.Add(idComunidad, toggle);
        }
    }

    private async Task CargarConfiguracionExistente()
    {
        DocumentReference docRef = db.Collection("asignacionesEncuestas").Document(encuestaIdActual);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Debug.Log("Cargando asignaci�n existente...");
            Dictionary<string, object> data = snapshot.ToDictionary();

            // Cargar configuraci�n guardada
            if (data.TryGetValue("minimoPreguntasAprobar", out object minPreguntas))
            {
                // El dropdown se basa en �ndice (0, 1, 2...) y el valor es (1, 2, 3...)
                dropdownMinimoPreguntas.value = System.Convert.ToInt32(minPreguntas) - 1;
            }
            if (data.TryGetValue("aleatorizarPreguntas", out object aleatorizarP))
            {
                toggleAleatorizarPreguntas.isOn = (bool)aleatorizarP;
            }
            if (data.TryGetValue("aleatorizarRespuestas", out object aleatorizarR))
            {
                toggleAleatorizarRespuestas.isOn = (bool)aleatorizarR;
            }
            if (data.TryGetValue("intentosMaximos", out object intentos))
            {
                inputIntentos.text = intentos.ToString();
            }

            // Seleccionar los toggles de las comunidades ya asignadas
            if (data.TryGetValue("comunidadesAsignadas", out object comunidades))
            {
                List<object> listaComunidades = comunidades as List<object>;
                foreach (object idComunidadObj in listaComunidades)
                {
                    string idComunidad = idComunidadObj.ToString();
                    if (togglesDeComunidades.ContainsKey(idComunidad))
                    {
                        togglesDeComunidades[idComunidad].isOn = true;
                    }
                }
            }
        }
    }

    private async void GuardarConfiguracionAsignacion()
    {
        Debug.Log("Guardando asignaci�n...");

        // 1. Recolectar IDs de comunidades seleccionadas
        List<string> comunidadesSeleccionadas = new List<string>();
        foreach (var par in togglesDeComunidades)
        {
            if (par.Value.isOn)
            {
                comunidadesSeleccionadas.Add(par.Key);
            }
        }

        // 2. Crear el objeto de datos para guardar en Firebase
        Dictionary<string, object> asignacionData = new Dictionary<string, object>
        {
            { "comunidadesAsignadas", comunidadesSeleccionadas },
            { "minimoPreguntasAprobar", dropdownMinimoPreguntas.value + 1 }, // El valor del dropdown es el �ndice
            { "aleatorizarPreguntas", toggleAleatorizarPreguntas.isOn },
            { "aleatorizarRespuestas", toggleAleatorizarRespuestas.isOn },
            { "intentosMaximos", int.Parse(inputIntentos.text) },
            { "estaActiva", true } // Asumimos que al guardar, se activa
        };

        // 3. Guardar en Firestore
        DocumentReference docRef = db.Collection("asignacionesEncuestas").Document(encuestaIdActual);
        await docRef.SetAsync(asignacionData, SetOptions.MergeAll);

        Debug.Log("�Asignaci�n guardada con �xito!");
        panelAsignacion.SetActive(false); // Cerrar el panel despu�s de guardar
    }

    private void ValidarIntentos(string valor)
    {
        if (int.TryParse(valor, out int numIntentos))
        {
            if (numIntentos > 10)
            {
                inputIntentos.text = "10";
            }
            else if (numIntentos < 1)
            {
                inputIntentos.text = "1";
            }
        }
    }
}