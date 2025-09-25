namespace PeriodicApp.Core.Application.DTOs
{
    /// <summary>
    /// Representa la entrada básica de movimiento para un personaje en el plano horizontal.
    /// </summary>
    public readonly struct MovementInput
    {
        public MovementInput(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public float Horizontal { get; }

        public float Vertical { get; }

        public bool IsZero => Horizontal == 0f && Vertical == 0f;
    }
}
