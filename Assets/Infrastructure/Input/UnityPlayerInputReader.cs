using UnityEngine;
using PeriodicApp.Core.Application.DTOs;

namespace PeriodicApp.Infrastructure.Input
{
    [CreateAssetMenu(menuName = "PeriodicApp/Input/Unity Player Input Reader", fileName = "UnityPlayerInputReader")]
    public sealed class UnityPlayerInputReader : PlayerInputReaderAsset
    {
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";

        public override MovementInput ReadMovement()
        {
            float horizontal = UnityEngine.Input.GetAxisRaw(horizontalAxis);
            float vertical = UnityEngine.Input.GetAxisRaw(verticalAxis);
            return new MovementInput(horizontal, vertical);
        }

        public override bool IsJumpPressed()
        {
            return UnityEngine.Input.GetButtonDown(jumpButton);
        }
    }
}
