using UnityEngine;
using PeriodicApp.Core.Application.DTOs;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Infrastructure.Input
{
    public abstract class PlayerInputReaderAsset : ScriptableObject, IPlayerInputReader
    {
        public abstract MovementInput ReadMovement();

        public abstract bool IsJumpPressed();
    }
}
