using System.Collections.Generic;

public static class EncuestaDataExtensions
{
    public static Dictionary<string, object> ToDictionary(this EncuestaData encuesta)
    {
        return new Dictionary<string, object>
        {
            { "id", encuesta.id },
            { "titulo", encuesta.titulo },
            { "descripcion", encuesta.descripcion },
            { "codigoAcceso", encuesta.codigoAcceso },
            { "preguntas", encuesta.preguntas },
            { "activo", encuesta.activo }
        };
    }
}