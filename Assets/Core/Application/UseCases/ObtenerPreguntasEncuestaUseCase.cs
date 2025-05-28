using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ObtenerPreguntasEncuestaUseCase
{
    private readonly IEncuestaConocimientoRepositorio _repositorio;

    public ObtenerPreguntasEncuestaUseCase(IEncuestaConocimientoRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<PreguntaEntity>> EjecutarAsync()
    {
        var preguntas = await _repositorio.ObtenerPreguntasAsync();
        // Aqu� puedes filtrar o preparar la l�gica del sorteo de 5 preguntas por categor�a si es necesario
        return preguntas;
    }
}
