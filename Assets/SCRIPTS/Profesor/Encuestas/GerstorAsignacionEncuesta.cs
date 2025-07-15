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

    [Header("Comunidades")]
    [SerializeField] private GameObject comunidadTogglePrefab;
    [SerializeField] private Transform contenedorComunidadesScroll; // El objeto "Content" del ScrollView

    [Header("Configuración de Asignación")]
    [SerializeField] private TMP_Dropdown dropdownMinimoPreguntas;
    [SerializeField] private Toggle toggleAleatorizarPreguntas;
    [SerializeField] private Toggle toggleAleatorizarRespuestas;
    [SerializeField] private TMP_InputField inputIntentos;

    [Header("Botones")]
    [SerializeField] private Button btnGuardarAsignacion; // Botón para "Activar" o guardar
    [SerializeField] private Button btnDesactivarEncuesta; // Botón para "Activar" o guardar
    [SerializeField] private Button btnCerrarPanel; // Botón para "Desactivar" o cancelar

    // Variables internas
    private string encuestaIdActual;
    private int numTotalPreguntas;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private Dictionary<string, Toggle> togglesDeComunidades = new Dictionary<string, Toggle>();

    void Start()
    {
        // Inicialización de Firebase
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Configurar listeners de los botones del panel
        btnGuardarAsignacion.onClick.AddListener(GuardarConfiguracionAsignacion);
        btnCerrarPanel.onClick.AddListener(() => panelAsignacion.SetActive(false));
        btnDesactivarEncuesta.onClick.AddListener(DesactivarEncuesta); 

        // Configurar restricciones del InputField de intentos
        inputIntentos.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputIntentos.onValueChanged.AddListener(ValidarIntentos);
    }

    /// <summary>
    /// Método público para ser llamado desde el exterior. Inicia todo el proceso.
    /// </summary>
    public async void AbrirPanelDeAsignacion(string idEncuesta, string titulo, int totalPreguntas)
    {
        encuestaIdActual = idEncuesta;
        numTotalPreguntas = totalPreguntas;

        LimpiarPanel();

        PoblarDropdownPreguntas(totalPreguntas);
        panelDetalles.SetActive(false);
        panelAsignacion.SetActive(true);

        // Cargar datos de forma asíncrona. Ahora el proceso es combinado.
        await CargarDatosEncuestaYComunidades();
    }

    private void LimpiarPanel()
    {
        foreach (Transform child in contenedorComunidadesScroll)
        {
            Destroy(child.gameObject);
        }
        togglesDeComunidades.Clear();

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

    /// <summary>
    /// Carga la configuración de la propia encuesta y las comunidades del usuario,
    /// marcando aquellas que ya tienen esta encuesta asignada.
    /// </summary>
    private async Task CargarDatosEncuestaYComunidades()
    {
        // --- PASO 1: Cargar la configuración guardada en la propia encuesta ---
        DocumentReference encuestaRef = db.Collection("Encuestas").Document(encuestaIdActual);
        DocumentSnapshot encuestaSnapshot = await encuestaRef.GetSnapshotAsync();

        if (encuestaSnapshot.Exists)
        {
            Debug.Log("Cargando configuración desde el documento de la encuesta...");
            Dictionary<string, object> data = encuestaSnapshot.ToDictionary();

            if (data.TryGetValue("minimoPreguntasAprobar", out object minPreguntas))
            {
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
        }

        // --- PASO 2: Cargar las comunidades del usuario y marcar las ya asignadas ---
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
            // ¿Estamos obteniendo el nombre correctamente desde Firebase?
            string nombreComunidad = doc.GetValue<string>("nombre");
            Debug.Log($"Procesando comunidad ID: {idComunidad}, Nombre: '{nombreComunidad}'");

            if (string.IsNullOrEmpty(nombreComunidad))
            {
                Debug.LogWarning($"El nombre para la comunidad {idComunidad} está vacío o no existe en Firestore.");
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
                // Si no se encontró, probamos con el componente de Texto antiguo
                Debug.LogWarning($"No se encontró TMP_Text para {idComunidad}. Intentando buscar UnityEngine.UI.Text...");
                Text labelLegacy = toggleObj.GetComponentInChildren<Text>(true);

                if (labelLegacy != null)
                {
                    Debug.Log($"Componente UnityEngine.UI.Text encontrado para {idComunidad}. Asignando texto.");
                    labelLegacy.text = nombreComunidad;
                }
                else
                {
                    // Si ninguno de los dos se encontró, el problema está en el prefab.
                    Debug.LogError($"¡ERROR GRAVE! No se encontró NINGÚN componente de texto (ni TMP_Text ni Text) en el prefab instanciado para la comunidad {idComunidad}. Revisa la estructura del prefab '{comunidadTogglePrefab.name}'.");
                }
            }

            togglesDeComunidades.Add(idComunidad, toggle);
        }
    }


    /// <summary>
    /// Guarda la configuración en el documento de la encuesta y actualiza el mapa
    /// de 'encuestasAsignadas' en cada documento de comunidad afectado.
    /// </summary>
    private async void GuardarConfiguracionAsignacion()
    {
        btnGuardarAsignacion.interactable = false; // Deshabilitar para evitar doble click
        Debug.Log("Guardando asignación con la nueva estructura...");

        // --- PASO 1: Guardar la configuración general en el documento de la encuesta ---
        Dictionary<string, object> configData = new Dictionary<string, object>
        {
            { "minimoPreguntasAprobar", dropdownMinimoPreguntas.value + 1 },
            { "aleatorizarPreguntas", toggleAleatorizarPreguntas.isOn },
            { "aleatorizarRespuestas", toggleAleatorizarRespuestas.isOn },
            { "intentosMaximos", int.Parse(inputIntentos.text) },
            { "estaActiva", true }
        };

        DocumentReference encuestaRef = db.Collection("Encuestas").Document(encuestaIdActual);
        // Usamos SetAsync con MergeAll para crear o actualizar los campos sin borrar los existentes (como las preguntas)
        await encuestaRef.SetAsync(configData, SetOptions.MergeAll);
        Debug.Log($"Configuración guardada en la encuesta '{encuestaIdActual}'.");


        // --- PASO 2: Actualizar las comunidades usando un WriteBatch para eficiencia y atomicidad ---
        WriteBatch batch = db.StartBatch();

        foreach (var par in togglesDeComunidades)
        {
            string comunidadId = par.Key;
            bool estaAsignada = par.Value.isOn;
            DocumentReference comunidadRef = db.Collection("comunidades").Document(comunidadId);

            // Usamos la notación de punto para modificar un campo dentro de un mapa.
            // Esto es mucho más eficiente que leer, modificar y reescribir todo el mapa.
            string campoEncuestaEnMapa = $"encuestasAsignadas.{encuestaIdActual}";

            if (estaAsignada)
            {
                // Si el toggle está activo, añadimos o actualizamos la entrada en el mapa.
                // Firestore creará el mapa 'encuestasAsignadas' si no existe.
                batch.Update(comunidadRef, campoEncuestaEnMapa, true);
            }
            else
            {
                // Si el toggle está inactivo, eliminamos la entrada del mapa.
                batch.Update(comunidadRef, campoEncuestaEnMapa, FieldValue.Delete);
            }
        }

        // Ejecutar todas las operaciones de actualización de comunidades en un solo viaje de red.
        await batch.CommitAsync();

        Debug.Log("¡Asignación a comunidades actualizada con éxito!");
        panelAsignacion.SetActive(false);
        btnGuardarAsignacion.interactable = true; // Rehabilitar el botón
    }

    private void ValidarIntentos(string valor)
    {
        if (int.TryParse(valor, out int numIntentos))
        {
            if (numIntentos > 10) inputIntentos.text = "10";
            else if (numIntentos < 1) inputIntentos.text = "1";
        }
    }

    public async void DesactivarEncuesta()
    {
        // Deshabilitar botones para evitar acciones múltiples
        btnDesactivarEncuesta.interactable = false;
        btnGuardarAsignacion.interactable = false;

        Debug.Log($"Iniciando desactivación para la encuesta: {encuestaIdActual}");

        // --- PASO 1: Marcar la encuesta como inactiva en su propio documento ---
        DocumentReference encuestaRef = db.Collection("Encuestas").Document(encuestaIdActual);
        Dictionary<string, object> desactivarData = new Dictionary<string, object>
    {
        { "estaActiva", false }
    };
        // Usamos Merge para solo actualizar este campo sin tocar el resto.
        await encuestaRef.SetAsync(desactivarData, SetOptions.MergeAll);
        Debug.Log($"Encuesta '{encuestaIdActual}' marcada como inactiva.");


        // --- PASO 2: Encontrar TODAS las comunidades que tienen esta encuesta asignada ---
        // Creamos la clave para buscar dentro del mapa 'encuestasAsignadas'
        string campoEncuestaEnMapa = $"encuestasAsignadas.{encuestaIdActual}";

        // La consulta busca cualquier documento en 'comunidades' donde el campo anidado exista.
        // Usamos '>' a un valor imposible para filtrar por existencia del campo.
        Query comunidadesConEncuestaQuery = db.Collection("comunidades").WhereGreaterThan(campoEncuestaEnMapa, false);

        QuerySnapshot comunidadesSnapshot = await comunidadesConEncuestaQuery.GetSnapshotAsync();
        Debug.Log($"Se encontraron {comunidadesSnapshot.Documents.Count()} comunidades con la encuesta asignada. Eliminando...");


        // --- PASO 3: Eliminar la encuesta de cada comunidad usando un WriteBatch ---
        if (comunidadesSnapshot.Documents.Any())
        {
            WriteBatch batch = db.StartBatch();
            foreach (var comunidadDoc in comunidadesSnapshot.Documents)
            {
                DocumentReference comunidadRef = comunidadDoc.Reference;
                // Usamos FieldValue.Delete para eliminar la clave del mapa.
                batch.Update(comunidadRef, campoEncuestaEnMapa, FieldValue.Delete);
            }
            // Ejecutamos todas las eliminaciones en una sola operación.
            await batch.CommitAsync();
        }

        Debug.Log("¡Desactivación completada! La encuesta ha sido eliminada de todas las comunidades.");

        // Cerrar el panel y rehabilitar los botones
        panelAsignacion.SetActive(false);
        btnDesactivarEncuesta.interactable = true;
        btnGuardarAsignacion.interactable = true;
    }
}