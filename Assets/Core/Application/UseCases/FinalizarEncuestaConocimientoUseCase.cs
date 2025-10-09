using System.Threading.Tasks;
using PeriodicApp.Core.Application.Interfaces;

//using UnityEngine;
//using UnityEngine.SceneManagement;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public class FinalizarEncuestaConocimientoUseCase
    {
        private readonly IServicioFirestore _firestore;
        private readonly IAuthenticationService _authenticationService;
        private readonly IPlayerPrefsService _playerPrefs;
        private readonly INetworkService _networkService;
        private readonly ILoggingService _logger;
        private readonly ISceneService _sceneService;

        public FinalizarEncuestaConocimientoUseCase(
            IServicioFirestore firestore, 
            IAuthenticationService authenticationService,
            IPlayerPrefsService playerPrefs,
            INetworkService networkService,
            ILoggingService logger,
            ISceneService sceneService)
        {
            _firestore = firestore;
            _authenticationService = authenticationService;
            _playerPrefs = playerPrefs;
            _networkService = networkService;
            _logger = logger;
            _sceneService = sceneService;
        }

        public async Task EjecutarAsync()
        {
            _playerPrefs.SetInt("EstadoEncuestaConocimiento", 1);
            _playerPrefs.Save();

            bool hayInternet = _networkService.IsConnected();
            bool estadoAprendizaje = _playerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoConocimiento = true;

            string? userId = _authenticationService.CurrentUserId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("No hay usuario autenticado.");
                return;
            }

            if (hayInternet)
            {
                await _firestore.GuardarEstadoEncuestaConocimientoAsync(userId, true).ConfigureAwait(false);
                var userData = await _firestore.ObtenerUsuarioAsync(userId).ConfigureAwait(false);

                estadoAprendizaje = userData.ContainsKey("EstadoEncuestaAprendizaje") && (bool)userData["EstadoEncuestaAprendizaje"];
                estadoConocimiento = userData.ContainsKey("EstadoEncuestaConocimiento") && (bool)userData["EstadoEncuestaConocimiento"];
            }

            if (estadoAprendizaje && estadoConocimiento)
            {
                _sceneService.LoadScene("Inicio");
            }
            else
            {
                _sceneService.LoadScene("SeleccionarEncuesta");
            }
        }
    }
}
