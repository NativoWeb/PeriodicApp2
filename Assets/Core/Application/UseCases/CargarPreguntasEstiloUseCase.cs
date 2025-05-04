using System.Collections.Generic;
using UnityEngine;
using static ControladorEncuestaApre;

public class CargarPreguntasEstiloUseCase
{
    public List<PreguntaEstilo> Ejecutar(string json)
    {
        var contenedor = JsonUtility.FromJson<ContenedorPreguntas>(json);
        var preguntas = new List<PreguntaEstilo>();

        void Agregar(List<ControladorEncuestaApre.Pregunta> lista, string categoria)
        {
            foreach (var p in lista)
                preguntas.Add(new PreguntaEstilo { Texto = p.textoAfirmacion, Categoria = categoria });
        }

        var estilos = contenedor.preguntasEstiloBinario;
        Agregar(estilos.Gamificacion, "Gamificacion");
        Agregar(estilos.Metodologia_Tradicional, "Metodologia_Tradicional");
        Agregar(estilos.Aprendizaje_Basado_en_Proyectos, "Aprendizaje_Basado_en_Proyectos");
        Agregar(estilos.Aprendizaje_Basado_en_Problemas, "Aprendizaje_Basado_en_Problemas");
        Agregar(estilos.Aprendizaje_Cooperativo, "Aprendizaje_Cooperativo");

        return preguntas;
    }
}
