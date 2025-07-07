using System.Collections.Generic;

public class Preguntas
{
    public string textoPregunta;
    public List<Opcion> opciones;
    public int tiempoRespuesta;

    public Preguntas(string texto, List<Opcion> opciones, int tiempoRespuesta)
    {
        this.textoPregunta = texto;
        this.opciones = opciones;
        this.tiempoRespuesta = tiempoRespuesta;
    }
}
public class Opcion
{
    public string textoOpcion;
    public bool esCorrecta;  // Nuevo campo para identificar si es la correcta

    public Opcion(string texto, bool correcta)
    {
        textoOpcion = texto;
        esCorrecta = correcta;
    }
}
