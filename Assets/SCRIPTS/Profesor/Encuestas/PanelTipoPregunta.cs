using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelTipoPregunta : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown dropdownTipoPregunta;
    public Button btnSiguiente;

    void Awake()
    {
        // Configurar el dropdown una sola vez
        dropdownTipoPregunta.ClearOptions();
        dropdownTipoPregunta.AddOptions(new List<string> { "Verdadero/Falso", "Opci�n M�ltiple" });

        // Configurar el bot�n
        btnSiguiente.onClick.AddListener(OnSiguienteClick);
    }

    public void Inicializar()
    {
        // Resetea el dropdown a la primera opci�n cada vez que se abre para crear
        dropdownTipoPregunta.SetValueWithoutNotify(0);
    }

    private void OnSiguienteClick()
    {
        TipoPregunta tipoSeleccionado = (TipoPregunta)dropdownTipoPregunta.value;
        EditorPreguntaManager.Instance.AvanzarAPanelDetalles(tipoSeleccionado);
    }
}