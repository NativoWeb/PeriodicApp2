using System;
using PeriodicApp.Core.Application.DTOs;

namespace PeriodicApp.Presentation.ViewModels.Gameplay
{
    /// <summary>
    /// Contiene la lógica de movimiento del jugador desacoplada del ciclo de vida de Unity.
    /// </summary>
    public sealed class PlayerMovementViewModel
    {
        private float _verticalVelocity;
        private readonly float _playerSpeed;
        private readonly float _jumpHeight;
        private readonly float _gravityValue;

        public PlayerMovementViewModel(float playerSpeed, float jumpHeight, float gravityValue)
        {
            _playerSpeed = playerSpeed;
            _jumpHeight = jumpHeight;
            _gravityValue = gravityValue;
        }

        public MovementUpdate Tick(MovementInput input, bool jumpPressed, bool isGrounded, float deltaTime)
        {
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
            }

            if (jumpPressed && isGrounded)
            {
                _verticalVelocity = (float)Math.Sqrt(_jumpHeight * -2.0f * _gravityValue);
            }

            _verticalVelocity += _gravityValue * deltaTime;

            return new MovementUpdate(
                input.Horizontal * _playerSpeed,
                input.Vertical * _playerSpeed,
                _verticalVelocity,
                !input.IsZero);
        }
    }

    public readonly struct MovementUpdate
    {
        public MovementUpdate(float horizontalSpeed, float verticalSpeed, float verticalVelocity, bool hasMovement)
        {
            HorizontalSpeed = horizontalSpeed;
            VerticalSpeed = verticalSpeed;
            VerticalVelocity = verticalVelocity;
            HasMovement = hasMovement;
        }

        public float HorizontalSpeed { get; }

        public float VerticalSpeed { get; }

        public float VerticalVelocity { get; }

        public bool HasMovement { get; }
    }
}
