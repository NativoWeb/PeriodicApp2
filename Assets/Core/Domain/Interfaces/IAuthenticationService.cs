using System.Threading;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Entities;

namespace PeriodicApp.Core.Domain.Interfaces
{
    /// <summary>
    /// Servicio que abstrae la autenticación de usuarios sin exponer detalles del proveedor.
    /// </summary>
    public interface IAuthenticationService
    {
        string? CurrentUserId { get; }

        Task<Usuario> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default);

        Task<Usuario> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
