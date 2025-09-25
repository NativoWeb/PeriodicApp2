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
                await _authenticationService.ResetPasswordAsync(email, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
