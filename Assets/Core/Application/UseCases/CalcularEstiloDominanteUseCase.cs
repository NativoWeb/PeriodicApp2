using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CalcularEstiloDominanteUseCase
{
    public string Ejecutar(Dictionary<string, int> respuestas)
    {
        return respuestas
            .OrderByDescending(p => p.Value)
            .First().Key;
    }
}
