using System.Threading.Tasks;

public interface IServicioAutenticacion
{
    Task<Usuario> LoginAsync(string email, string password);
    Task ResetPasswordAsync(string email);

    Task<bool> ActualizarPerfil(string displayName);
}
