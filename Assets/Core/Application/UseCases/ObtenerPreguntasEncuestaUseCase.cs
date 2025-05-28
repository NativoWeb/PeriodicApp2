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
        // Aquí puedes filtrar o preparar la lógica del sorteo de 5 preguntas por categoría si es necesario
        return preguntas;
    }
}
