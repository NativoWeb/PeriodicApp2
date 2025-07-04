using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PreguntaItemUI : MonoBehaviour
{
    public TextMeshProUGUI txtNumero;
    public TextMeshProUGUI txtTexto;
    public TextMeshProUGUI txtTipo;
    public TextMeshProUGUI txtCorrecta;
    public Button btnEditar;
    public Button btnEliminar;
    public GameObject panelConfirmarEliminar;

    private int indice;
    private EncuestasManager manager;
    private PreguntaModelo modelo;

    public void Configurar(PreguntaModelo m, int i, EncuestasManager man)
    {
        modelo = m;
        indice = i;
        manager = man;

        txtNumero.text = $"{i + 1}";
        txtTexto.text = modelo.TextoPregunta;
        txtTipo.text = (TipoPregunta)modelo.Tipo == TipoPregunta.VerdaderoFalso ? "Verdadero/Falso" : "Múltiple";
        txtCorrecta.text = ObtenerRespuestaCorrecta();

        btnEditar.onClick.AddListener(() => manager.AbrirPanelEditarPregunta(indice));
        btnEliminar.onClick.AddListener(() => panelConfirmarEliminar.SetActive(true));
        panelConfirmarEliminar.transform.Find("BtnSi").GetComponent<Button>().onClick.AddListener(() => {
            manager.EliminarPregunta(indice);
            Destroy(gameObject);
        });
        panelConfirmarEliminar.transform.Find("BtnNo").GetComponent<Button>().onClick.AddListener(() =>
            panelConfirmarEliminar.SetActive(false));
    }

    private string ObtenerRespuestaCorrecta()
    {
        foreach (var op in modelo.Opciones)
        {
            if (op.EsCorrecta) 
                return "Respuesta Correcta: " + op.Texto;
        }
        return "-";
    }
}
