using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreguntaItemUI : MonoBehaviour
{
    // --- Referencias a la UI ---
    public TextMeshProUGUI txtNumero;
    public TextMeshProUGUI txtTexto;
    public TextMeshProUGUI txtTipo;
    public TextMeshProUGUI txtCorrecta;
    public Button btnEditar;
    public Button btnEliminar;
    public GameObject contenedorAcciones;

    // La referencia al manager que controla este item.
    private EncuestasManager manager;

    private int indice;
    private PreguntaModelo modelo;

    // --- FUNCI�N CONFIGURAR MODIFICADA ---
    // �A�adimos el par�metro EncuestasManager!
    public void Configurar(PreguntaModelo m, int i, bool esEditable, EncuestasManager ownerManager)
    {
        modelo = m;
        indice = i;
        // Asignamos la referencia del manager que nos ha creado.
        this.manager = ownerManager;

        // --- Llenado de textos ---
        txtNumero.text = $"{i + 1}";
        txtTexto.text = modelo.TextoPregunta;
        txtTipo.text = (TipoPregunta)modelo.Tipo == TipoPregunta.VerdaderoFalso ? "Verdadero/Falso" : "Selecci�n M�ltiple";
        txtCorrecta.text = ObtenerRespuestaCorrecta();

        // --- L�GICA DE MODO ---
        if (contenedorAcciones != null)
        {
            contenedorAcciones.SetActive(esEditable);
        }
        else
        {
            btnEditar.gameObject.SetActive(esEditable);
            btnEliminar.gameObject.SetActive(esEditable);
        }

        // Limpiamos siempre los listeners para evitar duplicados.
        btnEditar.onClick.RemoveAllListeners();
        btnEliminar.onClick.RemoveAllListeners();

        // Solo a�adimos los listeners si estamos en modo editable.
        if (esEditable)
        {
            btnEditar.onClick.AddListener(OnEditarClick);
            btnEliminar.onClick.AddListener(OnEliminarClick);
        }
    }

    private void OnEditarClick()
    {
        // Para depurar, puedes a�adir un log:
        // Debug.Log($"Click en Editar en el item {indice}. Manager es {(manager == null ? "NULL" : "ASIGNADO")}");
        if (manager != null)
        {
            manager.AbrirPanelEditarPregunta(indice);
        }
    }

    private void OnEliminarClick()
    {
        // Debug.Log($"Click en Eliminar en el item {indice}. Manager es {(manager == null ? "NULL" : "ASIGNADO")}");
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
                return op.Texto;
            }
        }
        return "Sin respuesta correcta";
    }
}