using System;

//[Serializable] // Permite que Unity reconozca la clase en el Inspector
public class PreguntaConOpciones
{
    public string pregunta;
    public string[] opciones;
    public int indiceCorrecto; // �ndice de la respuesta correcta en el array

    public PreguntaConOpciones(string pregunta, string[] opciones, int indiceCorrecto)
    {
        this.pregunta = pregunta;
        this.opciones = opciones;
        this.indiceCorrecto = indiceCorrecto;
    }
}
