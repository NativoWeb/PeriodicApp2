using System.Threading.Tasks;

namespace PeriodicApp.Core.Domain.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string contenidoHtml);
    }
}
