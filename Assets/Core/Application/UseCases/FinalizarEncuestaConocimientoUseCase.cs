using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public class FinalizarEncuestaConocimientoUseCase
    {
        private readonly IServicioFirestore _firestore;
        private readonly IAuthenticationService _authenticationService;

        public FinalizarEncuestaConocimientoUseCase(IServicioFirestore firestore, IAuthenticationService authenticationService)
        {
            _firestore = firestore;
            _authenticationService = authenticationService;
        }

        public async Task EjecutarAsync()
        {
            PlayerPrefs.SetInt("EstadoEncuestaConocimiento", 1);
            PlayerPrefs.Save();

            bool hayInternet = Application.internetReachability != NetworkReachability.NotReachable;
            bool estadoAprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            bool estadoConocimiento = true;

            string? userId = _authenticationService.CurrentUserId;

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("No hay usuario autenticado.");
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
                SceneManager.LoadScene("Inicio");
            }
            else
            {
                SceneManager.LoadScene("SeleccionarEncuesta");
            }
        }
    }
}
