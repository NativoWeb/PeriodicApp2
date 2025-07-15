using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PanelDetallePregunta : MonoBehaviour
{
    [Header("UI Pregunta")]
    public TMP_Text Titulo;
    public TMP_InputField inputPregunta;
    public TMP_Dropdown dropdownTiempo;
    public Button btnAgregarOpcion;
    public Transform contenedorOpciones;
    public GameObject opcionPrefab;
    public Button btnGuardar;
    public Button btnCerrar;

    private List<OpcionUI> opcionesUI = new List<OpcionUI>();
    private TipoPregunta tipoActual;
    private int tiempoRespuesta = 15;
    private const int MIN_OPCIONES_MULTIPLE = 2;
    private const int MAX_OPCIONES_MULTIPLE = 4;

    void Awake()
    {
        SetupListeners();
    }

    public void InicializarParaCrear(TipoPregunta tipo)
    {
        LimpiarCampos();
        Titulo.text = "Nueva Pregunta";
        tipoActual = tipo;
        // Genera las opciones por defecto seg�n el tipo recibido
        ConfigurarPanelParaTipo(tipo, generarOpcionesPorDefecto: true);
    }

    public void InicializarParaEditar(PreguntaModelo modelo)
    {
        LimpiarCampos(); // Limpia por si acaso hab�a algo antes
        Titulo.text = "Editando Pregunta";
        tipoActual = (TipoPregunta)modelo.Tipo;

        // 1. Configurar el panel para el tipo SIN generar opciones por defecto
        ConfigurarPanelParaTipo(tipoActual, generarOpcionesPorDefecto: false);

        // 2. Poblar los campos con los datos del modelo
        inputPregunta.text = modelo.TextoPregunta;

        // 3. Poblar las opciones desde el modelo
        foreach (var opc in modelo.Opciones)
        {
            bool esVF = (tipoActual == TipoPregunta.VerdaderoFalso);
            AgregarOpcionUI(opc.Texto, opc.EsCorrecta, !esVF, !esVF, forzar: true);
        }

        // 4. Ajustar el tiempo
        int tiempoIndex = System.Array.IndexOf(new int[] { 15, 30, 45, 60 }, modelo.TiempoSegundos);
        dropdownTiempo.SetValueWithoutNotify(tiempoIndex > -1 ? tiempoIndex : 0);
        OnTiempoChanged(dropdownTiempo.value); // Actualizar variable interna

        ValidarGuardar();
    }

    private void SetupListeners()
    {
        // Configurar dropdown de tiempo
        dropdownTiempo.ClearOptions(); // <--- Limpia opciones viejas
        dropdownTiempo.AddOptions(new List<string> { "15", "30", "45", "60" }); // <--- A�ADE LAS NUEVAS
        dropdownTiempo.onValueChanged.AddListener(OnTiempoChanged);

        btnAgregarOpcion.onClick.AddListener(() => {
            AgregarOpcionUI("", false, true, true);
            ActualizarEstadoBotonAgregar();
        });
        btnGuardar.onClick.AddListener(Guardar);
        btnCerrar.onClick.AddListener(CerrarPanel);
    }

    private void ConfigurarPanelParaTipo(TipoPregunta tipo, bool generarOpcionesPorDefecto)
    {
        // Limpiamos las opciones UI VIEJAS, pero respetamos el bot�n "Agregar Opci�n".
        for (int i = contenedorOpciones.childCount - 1; i >= 0; i--)
        {
            Transform child = contenedorOpciones.GetChild(i);
            // La condici�n clave: comparamos GameObjects.
            if (child.gameObject != btnAgregarOpcion.gameObject) // <-- Cambio aqu�
            {
                Destroy(child.gameObject);
            }
        }
        opcionesUI.Clear();

        bool esVF = (tipo == TipoPregunta.VerdaderoFalso);

        // Controlamos la visibilidad del GameObject del bot�n.
        btnAgregarOpcion.gameObject.SetActive(!esVF); // <-- Cambio aqu�

        ActualizarEstadoBotonAgregar();

        if (generarOpcionesPorDefecto)
        {
            if (esVF)
            {
                AgregarOpcionUI("Verdadero", true, false, false);
                AgregarOpcionUI("Falso", false, false, false);
            }
            else
            {
                for (int i = 0; i < MIN_OPCIONES_MULTIPLE; i++)
                    AgregarOpcionUI("", i == 0, true, false);
            }
        }

        // Aseguramos que el bot�n se coloque al final del layout.
        ActualizarPosicionBotonAgregar();
        ValidarGuardar();
    }

    private void ActualizarPosicionBotonAgregar()
    {
        if (btnAgregarOpcion != null)
        {
            // Esto asegura que el bot�n siempre est� al final del Vertical Layout Group.
            btnAgregarOpcion.transform.SetAsLastSibling();
        }
    }

    private void OnTiempoChanged(int idx)
    {
        int[] tiempos = { 15, 30, 45, 60 };
        tiempoRespuesta = (idx >= 0 && idx < tiempos.Length) ? tiempos[idx] : 15;
    }

    // --- La l�gica de opciones, validaci�n y guardado permanece CASI id�ntica ---
    // (Solo he cambiado los nombres para que encajen y la llamada al manager)

    public void AgregarOpcionUI(string texto, bool esCorrecta, bool editable, bool eliminable, bool forzar = false)
    {
        if (!forzar && tipoActual == TipoPregunta.OpcionMultiple && opcionesUI.Count >= MAX_OPCIONES_MULTIPLE) return;

        GameObject go = Instantiate(opcionPrefab, contenedorOpciones);
        OpcionUI ui = go.GetComponent<OpcionUI>();
        if (ui == null) { Destroy(go); Debug.LogError("El prefab de opci�n no tiene el script OpcionUI."); return; }
        opcionesUI.Add(ui);

        ui.inputOpcion.text = texto;
        ui.inputOpcion.interactable = editable;
        ui.inputOpcion.onValueChanged.RemoveAllListeners();
        ui.BtnEliminar.onClick.RemoveAllListeners();

        // Primero, quita listeners viejos para evitar duplicados
        ui.toggleCorrecta.onValueChanged.RemoveAllListeners();

        // Asigna el valor del toggle. Esto no disparar� el evento.
        ui.toggleCorrecta.isOn = esCorrecta;

        // Luego, a�ade el listener nuevo.
        ui.toggleCorrecta.onValueChanged.AddListener((isOn) => {
            // Solo actuamos cuando el toggle se *activa* (se pone en true)
            if (isOn)
            {
                Debug.Log($"Toggle '{ui.inputOpcion.text}' activado. Desmarcando los dem�s.");
                MarcarSoloEstaCorrecta(ui);
            }
            ValidarGuardar();
        });
        ui.BtnEliminar.gameObject.SetActive(eliminable);

        if (eliminable)
        {
            ui.BtnEliminar.onClick.AddListener(() => {
                opcionesUI.Remove(ui);
                Destroy(go);
                ValidarGuardar();
                ActualizarEstadoBotonAgregar(); // <-- Llamar a la nueva funci�n
            });
        }
    }

    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(inputPregunta.text) || opcionesUI.Count < 2 || !opcionesUI.Any(o => o.toggleCorrecta.isOn))
        {
            Debug.LogWarning("Validaci�n fallida. Revisa los datos.");
            return;
        }

        var modelo = new PreguntaModelo
        {
            TextoPregunta = inputPregunta.text,
            Tipo = (int)tipoActual,
            TiempoSegundos = tiempoRespuesta,
            Opciones = new List<OpcionModelo>()
        };

        foreach (var ui in opcionesUI)
        {
            if (!string.IsNullOrWhiteSpace(ui.inputOpcion.text))
            {
                modelo.Opciones.Add(new OpcionModelo { Texto = ui.inputOpcion.text, EsCorrecta = ui.toggleCorrecta.isOn });
            }
        }

        // El manager se encarga de guardar y cerrar
        EditorPreguntaManager.Instance.GuardarPregunta(modelo); 
    }

    public void LimpiarCampos()
    {
        Titulo.text = "";
        inputPregunta.text = "";
        dropdownTiempo.SetValueWithoutNotify(0);
        OnTiempoChanged(0);

        // Limpiamos las opciones UI VIEJAS de forma segura, ignorando el bot�n.
        for (int i = contenedorOpciones.childCount - 1; i >= 0; i--)
        {
            Transform child = contenedorOpciones.GetChild(i);
            if (child.gameObject != btnAgregarOpcion.gameObject) // <-- Cambio aqu�
            {
                Destroy(child.gameObject);
            }
        }
        opcionesUI.Clear();
    }

    void CerrarPanel()
    {
        // Le decimos al manager que nos cierre
        EditorPreguntaManager.Instance.CerrarEditor();
    }

    // Funciones de utilidad (sin cambios)
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
                ui.toggleCorrecta.SetIsOnWithoutNotify(false);
            }
        }
    }

    private void ActualizarEstadoBotonAgregar()
    {
        // Si el bot�n no existe, no hacemos nada.
        if (btnAgregarOpcion == null) return;

        bool esTipoMultiple = (tipoActual == TipoPregunta.OpcionMultiple);

        // Si no es de opci�n m�ltiple, el bot�n siempre est� oculto.
        if (!esTipoMultiple)
        {
            btnAgregarOpcion.gameObject.SetActive(false);
            return;
        }

        // Si es de opci�n m�ltiple, decidimos si debe ser visible.
        // Condici�n para ser visible: tener MENOS de 4 opciones.
        bool puedeAgregarMas = (opcionesUI.Count < MAX_OPCIONES_MULTIPLE);

        // LA L�GICA CLAVE: El bot�n se activa solo si se pueden agregar m�s opciones.
        btnAgregarOpcion.gameObject.SetActive(puedeAgregarMas);

        // La interactividad es redundante si el objeto no est� activo,
        // pero es buena pr�ctica mantenerla por si decides cambiar a `interactable` en el futuro.
        btnAgregarOpcion.interactable = puedeAgregarMas;

        // Finalmente, aseguramos que est� al final del layout si est� visible.
        ActualizarPosicionBotonAgregar();
    }
}