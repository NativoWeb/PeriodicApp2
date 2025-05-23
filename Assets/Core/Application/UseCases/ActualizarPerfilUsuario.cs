using System.Threading.Tasks;
using UnityEngine;

public class ActualizarPerfilUsuario
{
    private readonly IServicioAutenticacion auth;

    public ActualizarPerfilUsuario(IServicioAutenticacion auth)
    {
        this.auth = auth;
    }

    public async Task<bool> Ejecutar(string displayName)
    {
        return await auth.ActualizarPerfil(displayName);
    }
}
