using System.Collections.Generic;

public class CalcularEstiloDominanteUseCase
{
    private static readonly string[] _prioridad =
    {
        "Gamificacion",
        "Aprendizaje_Basado_en_Proyectos",
        "Aprendizaje_Cooperativo",
        "Aprendizaje_Basado_en_Problemas",
        "Metodologia_Tradicional"
    };

    public string Ejecutar(Dictionary<string, int> respuestas)
    {
        if (respuestas == null || respuestas.Count == 0)
            return "Mixto";

        string mejor = null;
        int mejorPuntaje = -1;
        int mejorPrioridad = int.MaxValue;

        foreach (var kvp in respuestas)
        {
            int prioridad = ObtenerPrioridad(kvp.Key);
            if (kvp.Value > mejorPuntaje ||
                (kvp.Value == mejorPuntaje && prioridad < mejorPrioridad))
            {
                mejor = kvp.Key;
                mejorPuntaje = kvp.Value;
                mejorPrioridad = prioridad;
            }
        }

        return mejor ?? "Mixto";
    }

    public List<(string estilo, int puntaje)> EjecutarRanking(Dictionary<string, int> respuestas)
    {
        if (respuestas == null || respuestas.Count == 0)
            return new List<(string, int)>();

        var ranking = new List<(string estilo, int puntaje, int prioridad)>();
        foreach (var kvp in respuestas)
            ranking.Add((kvp.Key, kvp.Value, ObtenerPrioridad(kvp.Key)));

        ranking.Sort((a, b) =>
        {
            int cmp = b.puntaje.CompareTo(a.puntaje);
            return cmp != 0 ? cmp : a.prioridad.CompareTo(b.prioridad);
        });

        var resultado = new List<(string estilo, int puntaje)>(ranking.Count);
        for (int i = 0; i < ranking.Count; i++)
            resultado.Add((ranking[i].estilo, ranking[i].puntaje));

        return resultado;
    }

    private int ObtenerPrioridad(string estilo)
    {
        for (int i = 0; i < _prioridad.Length; i++)
        {
            if (_prioridad[i] == estilo)
                return i;
        }
        return _prioridad.Length;
    }
}
