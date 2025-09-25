using System.Threading;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Entities;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class RegistrarUsuario
    {
        private readonly IAuthenticationService _authenticationService;

        public RegistrarUsuario(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public Task<Usuario> EjecutarAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            return _authenticationService.CreateUserAsync(email, password, cancellationToken);
        }
    }
}
