using System.Collections.Generic;

[System.Serializable]
public class EncuestaData
{
    public string id;
    public string titulo;
    public string codigoAcceso;
    public List<Dictionary<string, object>> preguntas;
    public bool activo;

    public EncuestaData(string id, string titulo, string codigoAcceso, List<Dictionary<string, object>> preguntas, bool activo)
    {
        this.id = id;
        this.titulo = titulo;
        this.codigoAcceso = codigoAcceso;
        this.preguntas = preguntas;
        this.activo = activo;
    }
}
