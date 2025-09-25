using System.Collections.Generic;
using System.Threading.Tasks;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class GuardarDatosUsuario
    {
        private readonly IServicioFirestore firestore;
        private readonly IServicioLocalStorage localStorage;

        public GuardarDatosUsuario(IServicioFirestore firestore, IServicioLocalStorage localStorage)
        {
            this.firestore = firestore;
            this.localStorage = localStorage;
        }

        public Task Ejecutar(Dictionary<string, object> data)
        {
            string userId = localStorage.Obtener("userId");
            return firestore.GuardarDatosUsuario(userId, data);
        }
    }
}
