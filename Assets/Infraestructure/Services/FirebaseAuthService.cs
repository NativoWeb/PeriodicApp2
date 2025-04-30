using System.Threading.Tasks;
using Firebase.Auth;

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

    public async Task<bool> ActualizarPerfil(string displayName)
    {
        if (auth.CurrentUser == null)
        return false;
        

        UserProfile profile = new UserProfile { DisplayName = displayName };
        await auth.CurrentUser.UpdateUserProfileAsync(profile);
        return true;
    }

}
