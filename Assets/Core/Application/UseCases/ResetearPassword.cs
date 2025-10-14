using System;
using System.Threading;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class ResetearPassword
    {
        private readonly IAuthenticationService _authenticationService;

        public ResetearPassword(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task<bool> EjecutarAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                // Removemos ConfigureAwait(false) para asegurar que volvemos al hilo principal de Unity
                await _authenticationService.ResetPasswordAsync(email, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
