using System.Collections.Generic;

public class PreguntaEntity
{
    public string Texto { get; set; }
    public List<string> Opciones { get; set; }
    public int IndiceCorrecto { get; set; }
    public string Grupo { get; set; }
    public float Dificultad { get; set; }
}