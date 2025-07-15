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

    // El panel de confirmación debe ser gestionado por el manager, no por cada item.
    // public GameObject panelConfirmarEliminar; // <-- COMENTAMOS O ELIMINAMOS ESTO

    // --- Variables privadas (sin cambios) ---
    private int indice;
    private PreguntaModelo modelo;

    // --- FUNCIÓN CONFIGURAR MODIFICADA ---
    public void Configurar(PreguntaModelo m, int i, bool esEditable)
    {
        modelo = m;
        indice = i;

        // --- Llenado de textos (sin cambios) ---
        txtNumero.text = $"{i + 1}";
        txtTexto.text = modelo.TextoPregunta;

        // El texto del tipo y la respuesta correcta también se pueden mostrar en ambos modos.
        txtTipo.text = (TipoPregunta)modelo.Tipo == TipoPregunta.VerdaderoFalso ? "Verdadero/Falso" : "Selección Múltiple";

        // Podemos decidir si mostrar o no la respuesta correcta en el modo de "solo lectura".
        // Por ahora la mostramos.
        txtCorrecta.text = ObtenerRespuestaCorrecta();

        // --- LÓGICA DE MODO ---
        // Aquí está la magia. Activamos o desactivamos los botones según el modo.
        if (contenedorAcciones != null)
        {
            contenedorAcciones.SetActive(esEditable);
        }
        else // Fallback si no se asignó el contenedor
        {
            btnEditar.gameObject.SetActive(esEditable);
            btnEliminar.gameObject.SetActive(esEditable);
        }

        // Solo añadimos los listeners si estamos en modo editable.
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
        // El item le dice al manager que quiere eliminar la pregunta en 'su' índice.
        // El manager se encargará de mostrar el panel de confirmación.
        // Esto centraliza la lógica y evita que cada item tenga una referencia al panel.
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
                // Devolvemos solo el texto de la respuesta. El prefijo puede ir en el diseño.
                return op.Texto;
            }
        }
        return "Sin respuesta correcta";
    }
}