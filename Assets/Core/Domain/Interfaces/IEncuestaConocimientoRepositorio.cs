using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IEncuestaConocimientoRepositorio
{
    Task<List<PreguntaEntity>> ObtenerPreguntasAsync();
    Task GuardarEstadoEncuestaConocimientoAsync(string userId, bool estado);
}
