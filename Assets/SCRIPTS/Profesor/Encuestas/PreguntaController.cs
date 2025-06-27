using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using static ControladorEncuesta;
using System.Collections;

public class PreguntaController : MonoBehaviour
{
    public TMP_InputField inputPregunta;
    public Transform contenedorOpciones;
    public GameObject opcionPrefab;
    public Button btnAgregarOpcion;
    private int maxOpciones = 4;
    private List<Opcion> opciones = new List<Opcion>();
    public TMP_Dropdown dropdownTiempo; // Asigna desde el inspector
    private int tiempoRespuesta = 15; // Valor por defecto

    [Header("Referencias panel eliminar pregunta")]
    public Button btnEliminar;
    public GameObject panelConfirmacionPrefab;

    public EncuestasManager encuestasManager; // ¡Asegúrate de tener esta línea!

    private void Start()
    {
        // Desactivar el botón inicialmente
        btnAgregarOpcion.interactable = false;

        dropdownTiempo.onValueChanged.AddListener(ActualizarTiempoRespuesta);

        // Agregar la primera opción al crear la pregunta
        AgregarOpcion();
        btnEliminar.onClick.AddListener(MostrarConfirmacion);
    }
    private void MostrarConfirmacion()
    {
        // Crea el panel de confirmación
        GameObject panel = Instantiate(panelConfirmacionPrefab, FindFirstObjectByType<Canvas>().transform);
        panel.SetActive(true);

        // Configura botones TMP - Versión segura
        Button btnSi = panel.transform.Find("BtnSi")?.GetComponent<Button>();
        Button btnNo = panel.transform.Find("BtnNo")?.GetComponent<Button>();

        if (btnSi != null)
        {
            btnSi.onClick.AddListener(() => {
                Destroy(panel);
                EliminarPregunta();
            });
        }
        else
        {
            Debug.LogError("No se encontró el botón 'BtnSi'", panel);
        }

        if (btnNo != null)
        {
            btnNo.onClick.AddListener(() => {
                Destroy(panel);
            });
        }
        else
        {
            Debug.LogError("No se encontró el botón 'BtnNo'", panel);
        }
    }

    private void EliminarPregunta()
    {
        // Notifica al EncuestaManager antes de destruir
        EncuestasManager manager = FindFirstObjectByType<EncuestasManager>();
        if (manager != null) manager.PreguntaEliminada(this);

        Destroy(gameObject);
    }

    public void AgregarOpcion()
    {
        if (contenedorOpciones.childCount >= maxOpciones)
        {
            Debug.LogWarning("⚠️ No puedes agregar más de 4 opciones.");
            btnAgregarOpcion.interactable = false;
            return;
        }

        // Instanciar una nueva opción
        GameObject nuevaOpcion = Instantiate(opcionPrefab, contenedorOpciones);
        OpcionUI opcionUI = nuevaOpcion.GetComponent<OpcionUI>();

        if (opcionUI == null)
        {
            Debug.LogError("❌ ERROR: No se encontró el script OpcionUI en la opción instanciada.");
            return;
        }


        // Crear una nueva opción y agregarla a la lista de opciones de esta pregunta
        Opcion nuevaOpcionData = new Opcion("", false);
        opciones.Add(nuevaOpcionData);

        // Asociar eventos
        opcionUI.inputOpcion.onEndEdit.AddListener(valor =>
        {
            nuevaOpcionData.textoOpcion = valor;
            ValidarBotonAgregarOpcion(); // Validar cada vez que se edita el texto
        });

        opcionUI.toggleCorrecta.onValueChanged.AddListener(valor =>
        {
            if (valor)
            {
                MarcarOpcionCorrecta(nuevaOpcionData);
            }
        });

        // Si ya se alcanzaron las 4 opciones, desactivar el botón
        if (contenedorOpciones.childCount >= maxOpciones)
        {
            btnAgregarOpcion.interactable = false;
        }
        else
        {
            // Desactivar el botón hasta que la última opción tenga texto
            btnAgregarOpcion.interactable = false;
        }
    }
    // Añade este método a tu clase PreguntaController
    public void AgregarOpcionUI(string textoOpcion, bool esCorrecta)
    {
        if (contenedorOpciones.childCount >= maxOpciones)
        {
            Debug.LogWarning("No se pueden agregar más opciones, se alcanzó el límite");
            return;
        }

        // Instanciar nueva opción
        GameObject nuevaOpcion = Instantiate(opcionPrefab, contenedorOpciones);
        OpcionUI opcionUI = nuevaOpcion.GetComponent<OpcionUI>();

        if (opcionUI == null)
        {
            Debug.LogError("No se encontró el componente OpcionUI");
            return;
        }

        // Configurar la opción
        opcionUI.inputOpcion.text = textoOpcion;
        opcionUI.toggleCorrecta.isOn = esCorrecta;

        // Crear y agregar la nueva opción a la lista
        Opcion nuevaOpcionData = new Opcion(textoOpcion, esCorrecta);
        opciones.Add(nuevaOpcionData);

        // Configurar eventos
        opcionUI.inputOpcion.onEndEdit.AddListener(valor =>
        {
            nuevaOpcionData.textoOpcion = valor;
            ValidarBotonAgregarOpcion();
        });

        opcionUI.toggleCorrecta.onValueChanged.AddListener(valor =>
        {
            if (valor)
            {
                MarcarOpcionCorrecta(nuevaOpcionData);
            }
        });

        // Actualizar estado del botón de agregar
        ValidarBotonAgregarOpcion();
    }
    // Corrutina para asegurar que la nueva opción sea visible

    // ✅ Validar si la última opción tiene texto para habilitar el botón
    private void ValidarBotonAgregarOpcion()
    {
        if (contenedorOpciones.childCount == 0)
        {
            btnAgregarOpcion.interactable = false;
            return;
        }

        // Obtener la última opción
        Transform ultimaOpcion = contenedorOpciones.GetChild(contenedorOpciones.childCount - 1);
        TMP_InputField inputOpcion = ultimaOpcion.GetComponentInChildren<TMP_InputField>();

        // Habilitar el botón solo si la última opción tiene texto y no se alcanzó el máximo
        bool puedeAgregarMas = (contenedorOpciones.childCount < maxOpciones);
        bool ultimaOpcionTieneTexto = !string.IsNullOrWhiteSpace(inputOpcion.text);

        btnAgregarOpcion.interactable = (puedeAgregarMas && ultimaOpcionTieneTexto);
    }

    // 🔄 Asegurar que solo una opción sea correcta
    public void MarcarOpcionCorrecta(Opcion opcionSeleccionada)
    {
        foreach (Opcion opcion in opciones)
        {
            opcion.esCorrecta = false;
        }

        opcionSeleccionada.esCorrecta = true;

        // Actualizar la UI
        foreach (Transform opcionTransform in contenedorOpciones)
        {
            OpcionUI opcionUI = opcionTransform.GetComponent<OpcionUI>();
            if (opcionUI != null)
            {
                opcionUI.toggleCorrecta.isOn = (opcionUI.inputOpcion.text == opcionSeleccionada.textoOpcion);
            }
        }
    }

    public Preguntas ObtenerPregunta()
    {
        return new Preguntas(inputPregunta.text, new List<Opcion>(opciones), tiempoRespuesta);
    }

    public List<string> ObtenerOpciones()
    {
        List<string> opcionesTexto = new List<string>();
        foreach (Transform opcion in contenedorOpciones)
        {
            TMP_InputField inputOpcion = opcion.GetComponentInChildren<TMP_InputField>();
            if (inputOpcion != null && !string.IsNullOrEmpty(inputOpcion.text))
            {
                opcionesTexto.Add(inputOpcion.text);
            }
        }
        return opcionesTexto;
    }

    private void ActualizarTiempoRespuesta(int index)
    {
        // Según el índice, asignamos el tiempo
        switch (index)
        {
            case 0: tiempoRespuesta = 15; break;
            case 1: tiempoRespuesta = 30; break;
            case 2: tiempoRespuesta = 45; break;
            case 3: tiempoRespuesta = 60; break;
            default: tiempoRespuesta = 15; break;
        }

        Debug.Log("⏱️ Tiempo de respuesta asignado: " + tiempoRespuesta + " segundos");
    }
    public PreguntaModelo ObtenerModeloDesdeUI()
    {
        var modelo = new PreguntaModelo();

        // Aquí usas los métodos que ya tienes
        modelo.TextoPregunta = ObtenerTextoPregunta();
        modelo.Opciones = ObtenerOpcionesUI();

        return modelo;
    }


    public string ObtenerTextoPregunta()
    {
        return inputPregunta.text;
    }

    public List<OpcionModelo> ObtenerOpcionesUI()
    {
        var listaOpciones = new List<OpcionModelo>();
        // Itera sobre tus GameObjects de opciones en la UI
        foreach (OpcionUI opcionController in contenedorOpciones.GetComponentsInChildren<OpcionUI>())
        {
            listaOpciones.Add(new OpcionModelo
            {
                Texto = opcionController.ObtenerTextoOpcion(),      // Método que devuelve el texto de la opción
                EsCorrecta = opcionController.EsCorrecta() // Método que devuelve si el toggle está activado
            });
        }
        return listaOpciones;
    }

    // MÉTODO 2: Para rellenar la UI desde un modelo de datos
    public void PoblarUIDesdeModelo(PreguntaModelo modelo)
    {
        inputPregunta.text = modelo.TextoPregunta;

        // Limpia las opciones viejas
        foreach (Transform child in contenedorOpciones)
        {
            Destroy(child.gameObject);
        }

        // Crea las nuevas opciones desde el modelo
        foreach (var opcionModelo in modelo.Opciones)
        {
            // Llama a tu método existente para añadir una opción a la UI
            AgregarOpcionUI(opcionModelo.Texto, opcionModelo.EsCorrecta);
        }
    }

}