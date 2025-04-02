using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ControladorEncuestaAprendizaje;

public class ControladorEncuestaApre : MonoBehaviour
{
    public TextMeshProUGUI textoPregunta;
    public Slider barraProgreso;
    public ContenedorPreguntas contenedor;
    private int indexActual = 0;

    private List<PreguntaEstilo> preguntasOrdenadas = new List<PreguntaEstilo>();
    private Dictionary<string, int> respuestasPositivas = new Dictionary<string, int>();


    [System.Serializable]
    public class Pregunta
    {
        public string textoAfirmacion;
    }

    [System.Serializable]
    public class PreguntasPorEstilo
    {
        public List<Pregunta> Gamificacion;
        public List<Pregunta> Metodologia_Tradicional;
        public List<Pregunta> Aprendizaje_Basado_en_Proyectos;
        public List<Pregunta> Aprendizaje_Basado_en_Problemas;
        public List<Pregunta> Aprendizaje_Cooperativo;
    }

    [System.Serializable]
    public class PreguntaEstilo
    {
        public string texto;
        public string categoria;
    }


    [System.Serializable]
    public class ContenedorPreguntas
    {
        public PreguntasPorEstilo preguntasEstiloBinario;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CargarPreguntaDesdeJson();
    }

    void CargarPreguntaDesdeJson()
    {
        TextAsset archivoJson = Resources.Load<TextAsset>("preguntas_estilo_aprendizaje_2"); // sin extensión
        if (archivoJson != null)
        {
            contenedor = JsonUtility.FromJson<ContenedorPreguntas>(archivoJson.text);
            Debug.Log("Preguntas cargadas correctamente ✅");

            // Llenar el diccionario de respuestas
            respuestasPositivas["Gamificacion"] = 0;
            respuestasPositivas["Metodologia_Tradicional"] = 0;
            respuestasPositivas["Aprendizaje_Basado_en_Proyectos"] = 0;
            respuestasPositivas["Aprendizaje_Basado_en_Problemas"] = 0;
            respuestasPositivas["Aprendizaje_Cooperativo"] = 0;

            // Unir todas las preguntas en una sola lista
            foreach (var p in contenedor.preguntasEstiloBinario.Gamificacion)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Gamificacion" });

            foreach (var p in contenedor.preguntasEstiloBinario.Metodologia_Tradicional)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Metodologia_Tradicional" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Basado_en_Proyectos)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Basado_en_Proyectos" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Basado_en_Problemas)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Basado_en_Problemas" });

            foreach (var p in contenedor.preguntasEstiloBinario.Aprendizaje_Cooperativo)
                preguntasOrdenadas.Add(new PreguntaEstilo { texto = p.textoAfirmacion, categoria = "Aprendizaje_Cooperativo" });

            MostrarPregunta();
        }
        else
        {
            Debug.LogError("❌ No se encontró el archivo JSON");
            textoPregunta.text = "Error al cargar preguntas.";
        }
    }


    void MostrarPregunta()
    {
        if (indexActual < preguntasOrdenadas.Count)
        {
            textoPregunta.text = preguntasOrdenadas[indexActual].texto;
            barraProgreso.value = (float)indexActual / preguntasOrdenadas.Count;
        }
        else
        {
            MostrarResultadoFinal();
        }
    }

    public void Responder(bool respuesta)
    {
        string categoria = preguntasOrdenadas[indexActual].categoria;

        if (respuesta)
        {
            respuestasPositivas[categoria]++;
        }

        indexActual++;
        MostrarPregunta();
    }


    void MostrarResultadoFinal()
    {
        string estiloDominante = "";
        int mayorValor = -1;

        foreach (var estilo in respuestasPositivas)
        {
            if (estilo.Value > mayorValor)
            {
                mayorValor = estilo.Value;
                estiloDominante = estilo.Key;
            }
        }

        textoPregunta.text = $"🧠 Tu estilo de aprendizaje dominante es:\n<b>{estiloDominante.Replace("_", " ")}</b>";
        barraProgreso.value = 1f;
    }







    // Update is called once per frame
    void Update()
    {
        
    }
}
