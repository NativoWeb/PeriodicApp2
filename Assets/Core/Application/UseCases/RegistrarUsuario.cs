using UnityEngine;
using System.Threading.Tasks;

public class RegistrarUsuario
{
    private readonly IServicioAutenticacion authService;

    public RegistrarUsuario(IServicioAutenticacion authService)
    {
        this.authService = authService;
    }

    public async Task<Usuario> Ejecutar(string email, string password)
    {
        return await authService.CrearUsuarioAsync(email, password);
    }
}
