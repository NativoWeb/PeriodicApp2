using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using PeriodicApp.Core.Domain.Entities;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Infrastructure.Services
{
    public sealed class FirebaseAuthService : IAuthenticationService
    {
        private readonly FirebaseAuth _auth;

        public FirebaseAuthService(FirebaseAuth auth)
        {
            _auth = auth;
        }

        public string? CurrentUserId => _auth.CurrentUser?.UserId;

        public FirebaseUser CurrentUser => _auth.CurrentUser;

        public async Task<Usuario> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var result = await _auth.SignInWithEmailAndPasswordAsync(email, password).ConfigureAwait(false);
            var user = result.User;

            return new Usuario(user.Email, user.DisplayName, user.UserId);
        }

        public Task ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            return _auth.SendPasswordResetEmailAsync(email);
        }

        public Task<bool> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default)
        {
            if (_auth.CurrentUser == null)
            {
                return Task.FromResult(false);
            }

            var profile = new UserProfile { DisplayName = displayName };
            return UpdateProfileInternalAsync(profile);
        }

        public async Task<Usuario> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var result = await _auth.CreateUserWithEmailAndPasswordAsync(email, password).ConfigureAwait(false);
            var user = result.User;

            return new Usuario(user.Email, user.DisplayName, user.UserId);
        }

        private async Task<bool> UpdateProfileInternalAsync(UserProfile profile)
        {
            await _auth.CurrentUser.UpdateUserProfileAsync(profile).ConfigureAwait(false);
            return true;
        }
    }
}
