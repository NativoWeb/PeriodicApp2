using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class ActualizarRangoUsuario
    {
        private readonly IServicioFirestore firestore;
        private readonly IServicioLocalStorage localStorage;

        public ActualizarRangoUsuario(IServicioFirestore firestore, IServicioLocalStorage localStorage)
        {
            this.firestore = firestore;
            this.localStorage = localStorage;
        }

        public Task Ejecutar()
        {
            string userId = localStorage.Obtener("userId");
            int xp = int.Parse(localStorage.Obtener("TempXp", "0"));

            return firestore.ActualizarRango(userId, xp);
        }
    }
}
