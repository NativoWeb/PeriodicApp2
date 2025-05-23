using System.Threading.Tasks;

public class ResetearPassword
{
    private readonly IServicioAutenticacion servicioAutenticacion;

    public ResetearPassword(IServicioAutenticacion servicioAutenticacion)
    {
        this.servicioAutenticacion = servicioAutenticacion;
    }

    public async Task<bool> Ejecutar(string email)
    {
        try
        {
            await servicioAutenticacion.ResetPasswordAsync(email);
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error al enviar email de recuperación: {ex.Message}");
            return false;
        }
    }
}
