using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUsuarioRepositorio
{
    Task ActualizarEstadoEncuestaAprendizajeAsync(string userId, bool estado);
    Task<(bool aprendizaje, bool conocimiento)> ObtenerEstadosEncuestasAsync(string userId);
    Task GuardarEstiloAprendizajeAsync(string userId, string estiloDominante, string rankingJson);
}
