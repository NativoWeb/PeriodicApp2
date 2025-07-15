using UnityEngine;

// El enum puede seguir aquí o en su propio archivo.
public enum TipoPregunta { VerdaderoFalso = 0, OpcionMultiple = 1 }

public class EditorPreguntaManager : MonoBehaviour
{
    // --- INICIO DE LA SECCIÓN SINGLETON ---
    public static EditorPreguntaManager Instance { get; private set; }

    [Header("Referencias a Paneles")]
    public GameObject panelSeleccionTipo;
    public GameObject panelDetallePregunta;
    public GameObject panelEncuestaPrincipal;

    [Header("Referencias a Controladores")]
    public PanelTipoPregunta panelTipoController;
    public PanelDetallePregunta panelDetalleController;

    [Header("Manager Principal")]
    public EncuestasManager encuestasManager;

    private PreguntaModelo modeloEnEdicion;
    void Awake()
    {
        // --- INICIO DE LA SECCIÓN DE DIAGNÓSTICO AVANZADO ---
        if (Instance != null && Instance != this)
        {
            // Esto nos dice quién es el objeto que se está destruyendo.
            Debug.LogError($"Se encontró una instancia DUPLICADA de EditorPreguntaManager en el objeto '{this.gameObject.name}'. Este objeto será destruido. El original es '{Instance.gameObject.name}'.", this.gameObject);
            Destroy(this.gameObject);
            return;
        }

        // Si somos la primera instancia, nos registramos.
        Instance = this;
        Debug.Log($"EditorPreguntaManager.Instance ha sido asignado al objeto: '{this.gameObject.name}'.", this.gameObject);



        // Comprobaciones de referencias (se ejecutarán solo en la instancia original)
        if (panelSeleccionTipo == null) Debug.LogError("ERROR: panelSeleccionTipo no está asignado en el Inspector.", this.gameObject);
        if (panelDetallePregunta == null) Debug.LogError("ERROR: panelDetallePregunta no está asignado en el Inspector.", this.gameObject);
        if (panelEncuestaPrincipal == null) Debug.LogError("ERROR: panelEncuestaPrincipal no está asignado en el Inspector.", this.gameObject);
        if (panelTipoController == null) Debug.LogError("ERROR: panelTipoController no está asignado en el Inspector.", this.gameObject);
        if (panelDetalleController == null) Debug.LogError("ERROR: panelDetalleController no está asignado en el Inspector.");
        if (encuestasManager == null) Debug.LogError("ERROR: encuestasManager no está asignado en el Inspector.", this.gameObject);

        // Inicializar el estado de los paneles al principio.
        panelSeleccionTipo.SetActive(false);
        panelDetallePregunta.SetActive(false);
    }


    // --- PUNTOS DE ENTRADA ---
    public void IniciarCreacionPregunta()
    {
        modeloEnEdicion = null;
        panelDetallePregunta.SetActive(false);
        panelSeleccionTipo.SetActive(true);
        panelTipoController.Inicializar();
    }

    public void IniciarEdicionPregunta(PreguntaModelo modelo)
    {
        modeloEnEdicion = modelo;
        panelSeleccionTipo.SetActive(false);
        panelDetallePregunta.SetActive(true);
        panelDetalleController.InicializarParaEditar(modelo);
    }

    // --- FLUJO DE NAVEGACIÓN ---
    public void AvanzarAPanelDetalles(TipoPregunta tipoSeleccionado)
    {
        panelSeleccionTipo.SetActive(false);
        panelDetallePregunta.SetActive(true);

        if (modeloEnEdicion == null)
        {
            panelDetalleController.InicializarParaCrear(tipoSeleccionado);
        }
    }

    public void GuardarPregunta(PreguntaModelo modelo)
    {
        encuestasManager.GuardarPregunta(modelo);
        CerrarEditor();
    }

    public void CerrarEditor()
    {
        panelSeleccionTipo.SetActive(false);
        panelDetallePregunta.SetActive(false);
    }
}