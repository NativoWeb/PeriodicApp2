using PeriodicApp.Core.Application.DTOs;

namespace PeriodicApp.Core.Application.Interfaces
{
    public interface IPlayerInputReader
    {
        MovementInput ReadMovement();

        bool IsJumpPressed();
    }
}
