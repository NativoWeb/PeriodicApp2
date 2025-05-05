using System;
using System.Collections.Generic;

[Serializable]
public class Pregunta
{
    public string textoAfirmacion;
}

[Serializable]
public class PreguntasPorEstilo
{
    public List<Pregunta> Gamificacion;
    public List<Pregunta> Metodologia_Tradicional;
    public List<Pregunta> Aprendizaje_Basado_en_Proyectos;
    public List<Pregunta> Aprendizaje_Basado_en_Problemas;
    public List<Pregunta> Aprendizaje_Cooperativo;
}

[Serializable]
public class ContenedorPreguntas
{
    public PreguntasPorEstilo preguntasEstiloBinario;
}
