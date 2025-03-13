using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FirestoreBotones : MonoBehaviour
{
    public Transform contenedorBotones; // Contenedor en el Scroll View
    public GameObject prefabBoton; // Prefab del botón

    public TextMeshProUGUI tituloTMP;
    public TextMeshProUGUI nombreTMP;
    public TextMeshProUGUI descripcionTMP;

    public Button botonCambiarEscena; // Botón para cambiar de escena

    private string juegoEscenaActual;
    private Button botonSeleccionado;
    private bool primerBoton = true;
    private Color colorNormal = Color.gray;
    private Color colorSeleccionado = new Color(81f / 255f, 178f / 255f, 124f / 255f); // #51B27C

    FirebaseFirestore db;

    void Start()
    {
        Debug.Log("Iniciando FirestoreBotones...");
        db = FirebaseFirestore.DefaultInstance;
        botonCambiarEscena.interactable = false; // Desactivar botón hasta que haya un nivel seleccionado
        CargarDatosDesdeFirestore();
    }

    async void CargarDatosDesdeFirestore()
    {
        Debug.Log("🚀 Iniciando carga de datos desde Firestore...");
        CollectionReference gruposRef = db.Collection("grupos");
        bool primerBotonSeleccionado = false;  // Nuevo flag local

        for (int i = 1; i <= 18; i++)
        {
            string grupoID = "Grupo " + i;
            Debug.Log($"🔍 Obteniendo datos de: {grupoID}");

            DocumentReference grupoRef = gruposRef.Document(grupoID);
            DocumentSnapshot snapshot = await grupoRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"⚠️ Documento '{grupoID}' no encontrado en Firestore.");
                continue;
            }

            Debug.Log($"✅ Datos encontrados en {grupoID}");

            Dictionary<string, object> datos = snapshot.ToDictionary();
            string titulo = datos.ContainsKey("titulo") ? datos["titulo"].ToString() : "Sin título";
            string nombre = datos.ContainsKey("nombre") ? datos["nombre"].ToString() : "Sin nombre";
            string descripcion = datos.ContainsKey("descripcion") ? datos["descripcion"].ToString() : "Sin descripción";
            string juegoEscena = datos.ContainsKey("juegoEscena") ? datos["juegoEscena"].ToString() : "";

            Debug.Log($"📝 Procesando {grupoID} -> Titulo: {titulo}, Nombre: {nombre}, Desc: {descripcion}, JuegoEscena: {juegoEscena}");

            GameObject nuevoBoton = CrearBoton(i, titulo, nombre, descripcion, juegoEscena);

            if (nuevoBoton == null)
            {
                Debug.LogError($"❌ No se pudo crear el botón para grupo {i}");
                continue;
            }

            Debug.Log($"✅ Botón {i} creado correctamente.");

            if (!primerBotonSeleccionado)  // 🔥 Solo seleccionamos el primer botón una vez
            {
                Debug.Log($"🎯 Intentando seleccionar primer botón: {nuevoBoton.name}");
                SeleccionarNivel(nuevoBoton.GetComponent<Button>(), titulo, nombre, descripcion, juegoEscena);
                primerBotonSeleccionado = true;
            }
        }

        Debug.Log("✅ Finalizó la carga de datos.");
    }


    GameObject CrearBoton(int numeroGrupo, string titulo, string nombre, string descripcion, string juegoEscena)
    {
        Debug.Log($"🛠️ Creando botón para grupo {numeroGrupo}");

        GameObject nuevoBoton = Instantiate(prefabBoton, contenedorBotones);
        nuevoBoton.SetActive(true); // <-- Asegura que esté activo

        if (nuevoBoton == null)
        {
            Debug.LogError("❌ Error: No se pudo instanciar el botón.");
            return null;
        }

        TextMeshProUGUI textoBoton = nuevoBoton.GetComponentInChildren<TextMeshProUGUI>();
        Button boton = nuevoBoton.GetComponent<Button>();

        if (textoBoton == null)
        {
            Debug.LogError("❌ Error: No se encontró TextMeshProUGUI en el botón.");
            return null;
        }

        if (boton == null)
        {
            Debug.LogError("❌ Error: No se encontró el componente Button en el botón.");
            return null;
        }

        textoBoton.text = numeroGrupo.ToString();
        boton.onClick.AddListener(() => SeleccionarNivel(boton, titulo, nombre, descripcion, juegoEscena));

        Debug.Log($"✅ Botón creado correctamente: Grupo {numeroGrupo}");

        return nuevoBoton;
    }

    void SeleccionarPrimerBoton(GameObject nuevoBoton, string titulo, string nombre, string descripcion, string juegoEscena)
    {
        if (!primerBoton) return; // Evita que se ejecute más de una vez

        Debug.Log($"🎯 Intentando seleccionar primer botón: {nuevoBoton.name}");
        SeleccionarNivel(nuevoBoton.GetComponent<Button>(), titulo, nombre, descripcion, juegoEscena);
        primerBoton = false;
        Debug.Log($"🔵 Estado de primerBoton ahora: {primerBoton}");
    }
    void SeleccionarNivel(Button boton, string titulo, string nombre, string descripcion, string juegoEscena)
    {
        Debug.Log($"Seleccionando nivel: {nombre}");

        if (botonSeleccionado != null)
            botonSeleccionado.GetComponent<Image>().color = colorNormal;

        botonSeleccionado = boton;
        botonSeleccionado.GetComponent<Image>().color = colorSeleccionado;

        tituloTMP.text = titulo;
        nombreTMP.text = nombre;
        descripcionTMP.text = descripcion;
        juegoEscenaActual = juegoEscena;

        botonCambiarEscena.interactable = true;
        botonCambiarEscena.onClick.RemoveAllListeners();
        botonCambiarEscena.onClick.AddListener(CambiarEscena);
    }

    void CambiarEscena()
    {
        Debug.Log($"Cambiando a la escena: {juegoEscenaActual}");

        if (!string.IsNullOrEmpty(juegoEscenaActual))
        {
            SceneManager.LoadScene(juegoEscenaActual);
        }
        else
        {
            Debug.LogWarning("No hay una escena asignada para este nivel.");
        }
    }
}
