namespace PeriodicApp.Core.Domain.Entities
{
    /// <summary>
    /// Representa a un usuario autenticado dentro del sistema.
    /// </summary>
    public sealed class Usuario
    {
        public Usuario(string email, string displayName, string userId)
        {
            Email = email;
            DisplayName = displayName;
            UserId = userId;
        }

        public string Email { get; }

        public string DisplayName { get; }

        public string UserId { get; }
    }
}
