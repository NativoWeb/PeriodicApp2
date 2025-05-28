using System.Threading.Tasks;

public interface IEmailSender
{
    Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string contenidoHtml);
}
