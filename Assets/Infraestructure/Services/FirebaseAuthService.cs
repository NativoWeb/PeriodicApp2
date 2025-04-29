using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;

public class FirebaseAuthService : IServicioAutenticacion
{
    private readonly FirebaseAuth auth;

    public FirebaseAuthService(FirebaseAuth auth)
    {
        this.auth = auth;
    }

    public async Task<Usuario> LoginAsync(string email, string password)
    {
        var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
        var user = result.User;

        return new Usuario
        {
            Email = user.Email,
            UserId = user.UserId,
            DisplayName = user.DisplayName
        };
    }

    public async Task ResetPasswordAsync(string email)
    {
        await auth.SendPasswordResetEmailAsync(email);
    }
}
