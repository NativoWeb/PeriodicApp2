using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface IServicioFirestore
{
    Task<bool> NombreUsuarioDisponible(string nombre);
    Task GuardarDatosUsuario(string userId, Dictionary<string, object> data);
    Task SubirJson(string userId, string misiones, string categorias);
    Task ActualizarRango(string userId, int xpActual);
    Task<Dictionary<string, object>> ObtenerUsuarioAsync(string userId);
    Task GuardarEstadoEncuestaConocimientoAsync(string userId, bool estado);


}
