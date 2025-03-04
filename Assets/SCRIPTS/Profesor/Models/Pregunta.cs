using System.Collections.Generic;

public class Pregunta
{
    public string textoPregunta;
    public List<string> opciones;

    public Pregunta(string texto, List<string> opciones)
    {
        this.textoPregunta = texto;
        this.opciones = opciones ?? new List<string>(); // Evita que opciones sea null
    }
}
