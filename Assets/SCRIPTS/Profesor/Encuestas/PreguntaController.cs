using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;


// NOTA: Moví el enum fuera de la clase para que sea accesible globalmente de forma más limpia,
// o puedes mantenerlo dentro si prefieres (PreguntaController.TipoPregunta).
// He eliminado la copia duplicada que tenías al final del archivo.
public enum TipoPregunta { VerdaderoFalso = 0, OpcionMultiple = 1 }

public class PreguntaController : MonoBehaviour
{
    [Header("UI Pregunta")]
    public TMP_Text Titulo;
    public TMP_InputField inputPregunta;
    public TMP_Dropdown dropdownTipoPregunta;
    public TMP_Dropdown dropdownTiempo;
    public Button btnAgregarOpcion;
    public Transform contenedorOpciones;
    public GameObject opcionPrefab;
    public Button btnGuardar;
    public Button btnCerrar;

    [Header("Paneles de navegacion")]
    public GameObject panelPregunta;
    public GameObject panelEncuesta;
    public EncuestasManager encuestasManager;

    private List<OpcionUI> opcionesUI = new List<OpcionUI>();
    private int tiempoRespuesta = 15;
    private const int MIN_OPCIONES_MULTIPLE = 2;
    private const int MAX_OPCIONES_MULTIPLE = 4;

    void Awake()
    {
        if (encuestasManager == null)
        {
            encuestasManager = FindAnyObjectByType<EncuestasManager>();
            if (encuestasManager == null)
                Debug.LogError("¡No existe ningún EncuestasManager en la escena!");
        }
    }

    public void InicializarParaCrear()
    {
        LimpiarCampos();
        Titulo.text = "Crear Pregunta";
    }

    public void InicializarParaEditar(PreguntaModelo modelo)
    {
        PoblarUIDesdeModelo(modelo);
        Titulo.text = "Editar Pregunta";
    }

    // He renombrado tu `Inicializar` original a `SetupListeners` para mayor claridad.
    // Esto solo se debe llamar UNA VEZ, por ejemplo en Awake.
    public void SetupListeners()
    {
        // 1. Configurar dropdown de tipo de pregunta
        dropdownTipoPregunta.ClearOptions();
        dropdownTipoPregunta.AddOptions(new List<string> { "Verdadero/Falso", "Opción Múltiple" });
        dropdownTipoPregunta.onValueChanged.RemoveAllListeners();
        dropdownTipoPregunta.onValueChanged.AddListener((i) => OnTipoPreguntaChanged(i, true));

        // 2. Configurar dropdown de tiempo
        dropdownTiempo.ClearOptions();
        dropdownTiempo.AddOptions(new List<string> { "15", "30", "45", "60" });
        dropdownTiempo.onValueChanged.RemoveAllListeners();
        dropdownTiempo.onValueChanged.AddListener(OnTiempoChanged);

        // 3. Configurar botones
        btnAgregarOpcion.onClick.RemoveAllListeners();
        btnAgregarOpcion.onClick.AddListener(() => AgregarOpcionUI("", false, true, true));
        btnGuardar.onClick.RemoveAllListeners();
        btnGuardar.onClick.AddListener(Guardar);
        btnCerrar.onClick.RemoveAllListeners();
        btnCerrar.onClick.AddListener(CerrarPanelPregunta);
    }

    private void OnTipoPreguntaChanged(int idx, bool generarOpcionesPorDefecto = true)
    {
        // Limpiar opciones actuales
        foreach (Transform t in contenedorOpciones) Destroy(t.gameObject);
        opcionesUI.Clear();

        bool esVF = (idx == (int)TipoPregunta.VerdaderoFalso);
        btnAgregarOpcion.interactable = !esVF;

        if (generarOpcionesPorDefecto)
        {
            if (esVF)
            {
                AgregarOpcionUI("Verdadero", true, false, false); // Marcar una por defecto
                AgregarOpcionUI("Falso", false, false, false);
            }
            else
            {
                for (int i = 0; i < MIN_OPCIONES_MULTIPLE; i++)
                    AgregarOpcionUI("", i == 0, true, false); // Marcar la primera por defecto
            }
        }

        ActualizarAgregarInteractivo();
        ValidarGuardar();
    }

    private void OnTiempoChanged(int idx)
    {
        int[] tiempos = { 15, 30, 45, 60 };
        tiempoRespuesta = (idx >= 0 && idx < tiempos.Length) ? tiempos[idx] : 15;
    }

    public void AgregarOpcionUI(string texto, bool esCorrecta, bool editable, bool eliminable, bool forzar = false)
    {
        if (!forzar && dropdownTipoPregunta.value == (int)TipoPregunta.OpcionMultiple && opcionesUI.Count >= MAX_OPCIONES_MULTIPLE) return;

        GameObject go = Instantiate(opcionPrefab, contenedorOpciones);
        OpcionUI ui = go.GetComponent<OpcionUI>();
        opcionesUI.Add(ui);

        ui.inputOpcion.text = texto;
        ui.inputOpcion.interactable = editable;

        // Primero, quita listeners para evitar eventos mientras se asignan valores
        ui.toggleCorrecta.onValueChanged.RemoveAllListeners();
        ui.inputOpcion.onValueChanged.RemoveAllListeners();
        ui.BtnEliminar.onClick.RemoveAllListeners();

        // Asigna el valor del toggle. Esto no disparará el evento.
        ui.toggleCorrecta.isOn = esCorrecta;
        ui.toggleCorrecta.interactable = editable ? !string.IsNullOrWhiteSpace(texto) : true;

        // Ahora, añade los listeners
        ui.toggleCorrecta.onValueChanged.AddListener(on => {
            if (on) MarcarSoloEstaCorrecta(ui);
            ValidarGuardar();
        });

        ui.BtnEliminar.gameObject.SetActive(eliminable);
        if (eliminable)
        {
            ui.BtnEliminar.onClick.AddListener(() => {
                opcionesUI.Remove(ui);
                Destroy(go);
                ActualizarAgregarInteractivo();
                ValidarGuardar();
            });
        }

        if (editable)
        {
            ui.inputOpcion.onValueChanged.AddListener(nuevoTexto => {
                ui.toggleCorrecta.interactable = !string.IsNullOrWhiteSpace(nuevoTexto);
                if (string.IsNullOrWhiteSpace(nuevoTexto))
                {
                    ui.toggleCorrecta.isOn = false;
                }
                ActualizarAgregarInteractivo();
                ValidarGuardar();
            });
        }

        ActualizarAgregarInteractivo();
        ValidarGuardar();
    }

    private void ValidarGuardar()
    {
        bool preguntaValida = !string.IsNullOrWhiteSpace(inputPregunta.text);
        bool todasOpcionesConTexto = opcionesUI.All(ui => !string.IsNullOrWhiteSpace(ui.inputOpcion.text));
        bool algunaCorrecta = opcionesUI.Any(ui => ui.toggleCorrecta.isOn);

        btnGuardar.interactable = preguntaValida && todasOpcionesConTexto && algunaCorrecta;
    }

    private void MarcarSoloEstaCorrecta(OpcionUI seleccionada)
    {
        foreach (var ui in opcionesUI)
        {
            if (ui != seleccionada)
            {
                // Usar SetIsOnWithoutNotify para evitar cascadas de eventos
                ui.toggleCorrecta.SetIsOnWithoutNotify(false);
            }
        }
    }

    private void ActualizarAgregarInteractivo()
    {
        bool esMultiple = dropdownTipoPregunta.value == (int)TipoPregunta.OpcionMultiple;
        bool puedeAgregar = opcionesUI.Count < MAX_OPCIONES_MULTIPLE;
        btnAgregarOpcion.interactable = esMultiple && puedeAgregar;
    }

    public void PoblarUIDesdeModelo(PreguntaModelo modelo)
    {
        // Asegurarse de que los listeners estén listos
        SetupListeners();

        inputPregunta.text = modelo.TextoPregunta;

        // **LA SOLUCIÓN CLAVE (1):** Usar SetValueWithoutNotify para evitar disparar el evento.
        dropdownTipoPregunta.SetValueWithoutNotify((int)modelo.Tipo);

        // Ahora llamamos manualmente a OnTipoPreguntaChanged pero sin generar opciones por defecto.
        // Esto solo configura el estado del panel (ej. botón "agregar").
        OnTipoPreguntaChanged(dropdownTipoPregunta.value, false);

        // Limpiamos (aunque OnTipoPreguntaChanged ya lo hizo, es una doble seguridad)
        foreach (Transform t in contenedorOpciones) Destroy(t.gameObject);
        opcionesUI.Clear();

        // Y ahora poblamos con las opciones del modelo
        bool esVF = (modelo.Tipo == (int)TipoPregunta.VerdaderoFalso);
        foreach (var opc in modelo.Opciones)
        {
            AgregarOpcionUI(opc.Texto, opc.EsCorrecta, !esVF, !esVF, forzar: true);
        }

        // Finalmente, ajustamos el tiempo
        int tiempoIndex = System.Array.IndexOf(new int[] { 15, 30, 45, 60 }, modelo.TiempoSegundos);
        dropdownTiempo.SetValueWithoutNotify(tiempoIndex > -1 ? tiempoIndex : 0);
        tiempoRespuesta = modelo.TiempoSegundos;

        ValidarGuardar();
    }

    private void Guardar()
    {
        // La validación ahora ocurre en ValidarGuardar(), que activa/desactiva el botón.
        // Las validaciones aquí son una última barrera de seguridad.
        if (string.IsNullOrWhiteSpace(inputPregunta.text))
        {
            Debug.LogWarning("La pregunta no puede estar vacía.");
            return;
        }

        var modelo = new PreguntaModelo
        {
            TextoPregunta = inputPregunta.text,
            Tipo = dropdownTipoPregunta.value,
            TiempoSegundos = tiempoRespuesta,
            Opciones = new List<OpcionModelo>()
        };

        foreach (var ui in opcionesUI)
        {
            if (!string.IsNullOrWhiteSpace(ui.inputOpcion.text))
            {
                modelo.Opciones.Add(new OpcionModelo
                {
                    Texto = ui.inputOpcion.text,
                    EsCorrecta = ui.toggleCorrecta.isOn
                });
            }
        }

        if (modelo.Opciones.Count < 2)
        {
            Debug.LogWarning("Debes agregar al menos 2 opciones.");
            return;
        }
        if (!modelo.Opciones.Any(o => o.EsCorrecta)) // Cambiado a Linq.Any para consistencia
        {
            Debug.LogWarning("Debes marcar al menos una opción como correcta.");
            return;
        }

        encuestasManager.GuardarPregunta(modelo);
        CerrarPanelPregunta(); // Generalmente después de guardar se cierra el panel
    }

    public void LimpiarCampos()
    {
        // Asegurarse de que los listeners estén listos
        SetupListeners();

        inputPregunta.text = "";

        // **LA SOLUCIÓN CLAVE (2):** Usar SetValueWithoutNotify aquí también.
        dropdownTipoPregunta.SetValueWithoutNotify(0);
        dropdownTiempo.SetValueWithoutNotify(0);
        tiempoRespuesta = 15;

        // Limpia las opciones existentes y genera las de por defecto (V/F)
        OnTipoPreguntaChanged(dropdownTipoPregunta.value, true);
    }

    void CerrarPanelPregunta()
    {
        panelPregunta.SetActive(false);
        panelEncuesta.SetActive(true);
    }
}