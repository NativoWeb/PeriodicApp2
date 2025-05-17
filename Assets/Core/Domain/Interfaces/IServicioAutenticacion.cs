using System.Threading.Tasks;
using Firebase.Auth;

public interface IServicioAutenticacion
{
    FirebaseUser CurrentUser { get; }
    Task<Usuario> LoginAsync(string email, string password);
    Task ResetPasswordAsync(string email);

    Task<bool> ActualizarPerfil(string displayName);
    Task<Usuario> CrearUsuarioAsync(string email, string password);
}
