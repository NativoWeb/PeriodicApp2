using System.Threading.Tasks;
using UnityEngine;

public interface IUsuarioRepositorio
{
    Task ActualizarEstadoEncuestaAprendizajeAsync(string userId, bool estado);
    Task<(bool aprendizaje, bool conocimiento)> ObtenerEstadosEncuestasAsync(string userId);
}
