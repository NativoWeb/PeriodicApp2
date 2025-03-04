using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using static ControladorEncuesta;

public class PreguntaController : MonoBehaviour
{
    public TMP_InputField inputPregunta;
    public Transform contenedorOpciones;
    public GameObject opcionPrefab;
    public Button btnAgregarOpcion;  // Botón de agregar opción
    private int maxOpciones = 4;  // Límite de opciones

    private List<string> opciones = new List<string>();

    public void AgregarOpcion()
    {
        GameObject nuevaOpcion = Instantiate(opcionPrefab, contenedorOpciones);
        TMP_InputField inputOpcion = nuevaOpcion.GetComponentInChildren<TMP_InputField>();

        if (inputOpcion == null)
        {
            Debug.LogError("❌ ERROR: No se encontró un TMP_InputField en la opción instanciada.");
            return;
        }


        inputOpcion.onEndEdit.AddListener(delegate { GuardarOpcion(inputOpcion.text); });

        // Verifica cuántas opciones hay en el contenedor
        if (contenedorOpciones.childCount >= maxOpciones)
        {
            Debug.LogWarning("⚠️ No puedes agregar más de 4 opciones.");
            btnAgregarOpcion.interactable = false;
            return;
        }
    }

    private void GuardarOpcion(string opcionTexto)
    {
        if (!string.IsNullOrEmpty(opcionTexto) && !opciones.Contains(opcionTexto))
        {
            opciones.Add(opcionTexto);
        }
    }

    public Pregunta ObtenerPregunta()
    {
        return new Pregunta(inputPregunta.text, new List<string>(opciones));
    }
}
