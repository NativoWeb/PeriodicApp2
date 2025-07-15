using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreguntaItemUI : MonoBehaviour
{
    // --- Referencias a la UI (sin cambios) ---
    public TextMeshProUGUI txtNumero;
    public TextMeshProUGUI txtTexto;
    public TextMeshProUGUI txtTipo;
    public TextMeshProUGUI txtCorrecta;
    public Button btnEditar;
    public Button btnEliminar;
    public GameObject contenedorAcciones;
    public EncuestasManager manager;

    // El panel de confirmaci�n debe ser gestionado por el manager, no por cada item.
    // public GameObject panelConfirmarEliminar; // <-- COMENTAMOS O ELIMINAMOS ESTO

    // --- Variables privadas (sin cambios) ---
    private int indice;
    private PreguntaModelo modelo;

    // --- FUNCI�N CONFIGURAR MODIFICADA ---
    public void Configurar(PreguntaModelo m, int i, bool esEditable)
    {
        modelo = m;
        indice = i;

        // --- Llenado de textos (sin cambios) ---
        txtNumero.text = $"{i + 1}";
        txtTexto.text = modelo.TextoPregunta;

        // El texto del tipo y la respuesta correcta tambi�n se pueden mostrar en ambos modos.
        txtTipo.text = (TipoPregunta)modelo.Tipo == TipoPregunta.VerdaderoFalso ? "Verdadero/Falso" : "Selecci�n M�ltiple";

        // Podemos decidir si mostrar o no la respuesta correcta en el modo de "solo lectura".
        // Por ahora la mostramos.
        txtCorrecta.text = ObtenerRespuestaCorrecta();

        // --- L�GICA DE MODO ---
        // Aqu� est� la magia. Activamos o desactivamos los botones seg�n el modo.
        if (contenedorAcciones != null)
        {
            contenedorAcciones.SetActive(esEditable);
        }
        else // Fallback si no se asign� el contenedor
        {
            btnEditar.gameObject.SetActive(esEditable);
            btnEliminar.gameObject.SetActive(esEditable);
        }

        // Solo a�adimos los listeners si estamos en modo editable.
        if (esEditable)
        {
            // Limpiamos listeners para evitar llamadas duplicadas si se reutiliza el objeto.
            btnEditar.onClick.RemoveAllListeners();
            btnEliminar.onClick.RemoveAllListeners();

            btnEditar.onClick.AddListener(OnEditarClick);
            btnEliminar.onClick.AddListener(OnEliminarClick);
        }
    }
    private void OnEditarClick()
    {
        if (manager != null)
        {
            manager.AbrirPanelEditarPregunta(indice);
        }
    }

    private void OnEliminarClick()
    {
        // El item le dice al manager que quiere eliminar la pregunta en 'su' �ndice.
        // El manager se encargar� de mostrar el panel de confirmaci�n.
        // Esto centraliza la l�gica y evita que cada item tenga una referencia al panel.
        if (manager != null)
        {
            manager.MostrarConfirmacionEliminarPregunta(indice);
        }
    }

    private string ObtenerRespuestaCorrecta()
    {
        if (modelo == null || modelo.Opciones == null) return "N/A";

        foreach (var op in modelo.Opciones)
        {
            if (op.EsCorrecta)
            {
                // Devolvemos solo el texto de la respuesta. El prefijo puede ir en el dise�o.
                return op.Texto;
            }
        }
        return "Sin respuesta correcta";
    }
}