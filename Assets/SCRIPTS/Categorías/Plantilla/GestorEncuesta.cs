using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class GestorEncuesta : MonoBehaviour
{
    [System.Serializable]
    public class Pregunta
    {
        public string textoPregunta;
        public List<string> opcionesRespuesta;
        public int indiceRespuestaCorrecta;
    }

    [System.Serializable]
    public class PreguntasData
    {
        public List<Pregunta> preguntas;
    }

    public TextMeshProUGUI txtPregunta;
    public Toggle[] toggles;
    public TextMeshProUGUI[] txtOpciones;

    private List<Pregunta> preguntasDisponibles;
    private Pregunta preguntaActual;
    private List<string> opcionesMezcladas;
    private int indiceCorrecto;

    void Start()
    {
        CargarPreguntasDesdeJSON();
        MostrarNuevaPregunta();
    }

    void CargarPreguntasDesdeJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("metales_alcalinos");
        if (jsonFile == null)
        {
            Debug.LogError("❌ No se encontró el archivo preguntas.json en Resources.");
            return;
        }

        PreguntasData preguntasData = JsonUtility.FromJson<PreguntasData>(jsonFile.text);
        if (preguntasData == null || preguntasData.preguntas.Count == 0)
        {
            Debug.LogError("❌ Error al deserializar el JSON o no hay preguntas.");
            return;
        }

        preguntasDisponibles = new List<Pregunta>(preguntasData.preguntas);
        RandomizarLista(preguntasDisponibles); // Mezcla las preguntas
    }

    void MostrarNuevaPregunta()
    {
        if (preguntasDisponibles.Count == 0)
        {
            Debug.Log("🏁 No hay más preguntas disponibles.");
            return;
        }

        preguntaActual = preguntasDisponibles[0]; // Tomamos la primera pregunta
        preguntasDisponibles.RemoveAt(0); // Eliminamos para no repetir

        txtPregunta.text = preguntaActual.textoPregunta;
        opcionesMezcladas = new List<string>(preguntaActual.opcionesRespuesta);
        RandomizarLista(opcionesMezcladas); // Mezcla las opciones

        // Asignamos las respuestas mezcladas a los Toggle
        for (int i = 0; i < toggles.Length; i++)
        {
            txtOpciones[i].text = opcionesMezcladas[i];
            toggles[i].isOn = false;

            if (preguntaActual.opcionesRespuesta[preguntaActual.indiceRespuestaCorrecta] == opcionesMezcladas[i])
            {
                indiceCorrecto = i; // Guardamos cuál es la respuesta correcta después de mezclar
            }
        }
    }

    void RandomizarLista<T>(List<T> lista)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            T temp = lista[i];
            int randomIndex = Random.Range(i, lista.Count);
            lista[i] = lista[randomIndex];
            lista[randomIndex] = temp;
        }
    }
}
