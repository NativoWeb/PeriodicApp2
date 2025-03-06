using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DisparoAlcalinos : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI preguntaText;
    public Button[] botonesRespuestas;
    public GameObject panelRespuesta;
    public TextMeshProUGUI textoRespuesta;
    public GameObject imagenSeleccion; // Imagen que se moverá sobre el botón seleccionado

    private Dictionary<string, string> preguntasRespuestas = new Dictionary<string, string>();
    private string respuestaCorrecta;

    void Start()
    {
        panelRespuesta.SetActive(false); // Ocultar panel de respuesta al inicio
        imagenSeleccion.SetActive(false); // Ocultar imagen al inicio
        InicializarPreguntas();
        GenerarPregunta();
    }

    void InicializarPreguntas()
    {
        preguntasRespuestas.Add("¿Cuál es el símbolo del Litio?", "Li");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Sodio?", "Na");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Potasio?", "K");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Rubidio?", "Rb");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Cesio?", "Cs");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Francio?", "Fr");
    }

    void GenerarPregunta()
    {
        List<string> keys = new List<string>(preguntasRespuestas.Keys);
        string preguntaSeleccionada = keys[Random.Range(0, keys.Count)];

        preguntaText.text = preguntaSeleccionada;
        respuestaCorrecta = preguntasRespuestas[preguntaSeleccionada];

        List<string> opciones = new List<string>(preguntasRespuestas.Values);
        opciones = opciones.GetRange(0, botonesRespuestas.Length);

        for (int i = 0; i < botonesRespuestas.Length; i++)
        {
            Button botonTemp = botonesRespuestas[i];
            TextMeshProUGUI textoBoton = botonTemp.GetComponentInChildren<TextMeshProUGUI>();

            botonTemp.onClick.RemoveAllListeners();
            botonTemp.onClick.AddListener(() => VerificarRespuesta(textoBoton.text, botonTemp.transform.position));
        }
    }

    public void VerificarRespuesta(string respuestaSeleccionada, Vector3 posicionBoton)
    {
        Debug.Log("Respuesta seleccionada: " + respuestaSeleccionada);

        // Activar la imagen y posicionarla sobre el botón seleccionado
        imagenSeleccion.SetActive(true);
        imagenSeleccion.transform.position = posicionBoton;

        if (respuestaSeleccionada == respuestaCorrecta)
        {
            textoRespuesta.text = "✅ ¡Correcto!";
            Debug.Log("✅ Respuesta correcta");
        }
        else
        {
            textoRespuesta.text = "❌ Incorrecto";
            Debug.Log("❌ Respuesta incorrecta");
        }

        panelRespuesta.SetActive(true);
        StartCoroutine(OcultarImagenSeleccion());
        StartCoroutine(DesactivarPanelRespuesta());
    }

    IEnumerator OcultarImagenSeleccion()
    {
        yield return new WaitForSeconds(2);
        imagenSeleccion.SetActive(false);
    }

    IEnumerator DesactivarPanelRespuesta()
    {
        yield return new WaitForSeconds(2);
        panelRespuesta.SetActive(false);
        GenerarPregunta();
    }
}
