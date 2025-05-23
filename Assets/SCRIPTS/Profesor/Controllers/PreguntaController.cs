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
    public Button btnAgregarOpcion;
    private int maxOpciones = 4;
    private List<Opcion> opciones = new List<Opcion>();

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
        opcionUI.inputOpcion.onEndEdit.AddListener(valor => nuevaOpcionData.textoOpcion = valor);
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
    }

    // Asegurar que solo una opción sea correcta dentro de la misma pregunta
    public void MarcarOpcionCorrecta(Opcion opcionSeleccionada)
    {
        foreach (Opcion opcion in opciones)
        {
            opcion.esCorrecta = false;
        }

        opcionSeleccionada.esCorrecta = true;

        // 🔍 Verificar si realmente se está actualizando la lista de opciones
        Debug.Log("📋 Estado actual de las opciones:");
        foreach (Opcion opcion in opciones)
        {
            Debug.Log($"🔹 Opción: {opcion.textoOpcion} | Correcta: {opcion.esCorrecta}");
        }

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
        Preguntas pregunta = new Preguntas(inputPregunta.text, new List<Opcion>(opciones));

        // 🛠 Debug para ver si se está marcando la opción correcta
        foreach (Opcion opcion in pregunta.opciones)
        {
            Debug.Log($"📌 Opción: {opcion.textoOpcion}, Correcta: {opcion.esCorrecta}");
        }

        return pregunta;
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


}
