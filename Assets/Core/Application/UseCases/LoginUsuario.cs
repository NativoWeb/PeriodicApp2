using System;
using System.Threading;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class LoginUsuario
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IServicioLocalStorage _localStorage;

        public LoginUsuario(IAuthenticationService authenticationService, IServicioLocalStorage localStorage)
        {
            _authenticationService = authenticationService;
            _localStorage = localStorage;
        }

        public async Task<ResultadoLogin> EjecutarAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var usuario = await _authenticationService.LoginAsync(email, password, cancellationToken).ConfigureAwait(false);

                _localStorage.Guardar("userId", usuario.UserId);
                _localStorage.Guardar("DisplayName", usuario.DisplayName);
                _localStorage.Guardar("Estadouser", "nube");

                return ResultadoLogin.Exito(usuario.UserId);
            }
            catch (Exception ex)
            {
                return ResultadoLogin.Fallo(ex.Message);
            }
        }

        public readonly struct ResultadoLogin
        {
            private ResultadoLogin(bool exito, string usuarioId, string mensajeError)
            {
                EsExitoso = exito;
                UsuarioId = usuarioId;
                MensajeError = mensajeError;
            }

            public bool EsExitoso { get; }

            public string UsuarioId { get; }

            public string MensajeError { get; }

            public static ResultadoLogin Exito(string usuarioId) => new ResultadoLogin(true, usuarioId, string.Empty);

            public static ResultadoLogin Fallo(string mensajeError) => new ResultadoLogin(false, string.Empty, mensajeError);
        }
    }
}
