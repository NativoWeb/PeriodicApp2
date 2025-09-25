using System.Threading;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class ActualizarPerfilUsuario
    {
        private readonly IAuthenticationService _authenticationService;

        public ActualizarPerfilUsuario(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public Task<bool> EjecutarAsync(string displayName, CancellationToken cancellationToken = default)
        {
            return _authenticationService.UpdateProfileAsync(displayName, cancellationToken);
        }
    }
}
