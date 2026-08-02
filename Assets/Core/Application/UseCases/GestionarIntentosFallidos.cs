using PeriodicApp.Core.Application.Interfaces;
using PeriodicApp.Core.Domain.Interfaces;
using System;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class GestionarIntentosFallidos
    {
        private readonly IServicioLocalStorage _localStorage;
        private readonly IPlayerPrefsService _playerPrefs;
        private const int MaxIntentos = 5;
        private const int TiempoBloqueoSegundos = 180;

        public GestionarIntentosFallidos(
            IServicioLocalStorage localStorage,
            IPlayerPrefsService playerPrefs)
        {
            _localStorage = localStorage;
            _playerPrefs = playerPrefs;
        }

        public void RegistrarIntentoFallido()
        {
            int intentos = _playerPrefs.GetInt("FailedAttempts", 0) + 1;
            _playerPrefs.SetInt("FailedAttempts", intentos);
            _playerPrefs.Save();

            if (intentos >= MaxIntentos)
            {
                BloquearUsuario();
            }
        }

        public bool EstaBloqueado()
        {
            if (!_playerPrefs.HasKey("LockoutTime"))
            {
                return false;
            }

            int tiempoBloqueo = _playerPrefs.GetInt("LockoutTime");
            int tiempoActual = GetUnixTimestamp();
            return tiempoActual < tiempoBloqueo;
        }

        public int TiempoRestante()
        {
            if (!_playerPrefs.HasKey("LockoutTime"))
            {
                return 0;
            }

            int tiempoBloqueo = _playerPrefs.GetInt("LockoutTime");
            int tiempoActual = GetUnixTimestamp();

            // Reemplazamos Mathf.Max con Math.Max (de System)
            return Math.Max(0, tiempoBloqueo - tiempoActual);
        }

        public void ResetearIntentos()
        {
            _playerPrefs.DeleteKey("FailedAttempts");
            _playerPrefs.DeleteKey("LockoutTime");
        }

        private void BloquearUsuario()
        {
            int tiempoActual = GetUnixTimestamp();
            int tiempoDesbloqueo = tiempoActual + TiempoBloqueoSegundos;
            _playerPrefs.SetInt("LockoutTime", tiempoDesbloqueo);
            _playerPrefs.Save();
        }

        private int GetUnixTimestamp()
        {
            return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}