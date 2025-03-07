using System.Collections.Generic;

public class Pregunta
{
    public string textoPregunta;
    public List<Opcion> opciones;

    public Pregunta(string texto, List<Opcion> opciones)
    {
        this.textoPregunta = texto;
        this.opciones = opciones;
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
