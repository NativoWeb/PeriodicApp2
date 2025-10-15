using System.IO;
using System.Threading.Tasks;
using PeriodicApp.Core.Application.Interfaces;

//using UnityEngine;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class SubirDatosJSON
    {
        private readonly IServicioFirestore firestore;
        private readonly IServicioLocalStorage localStorage;
        private readonly IPersistenceService _persistenceService;


        public SubirDatosJSON(IServicioFirestore firestore, IServicioLocalStorage localStorage, IPersistenceService persistenceService)
        {
            this.firestore = firestore;
            this.localStorage = localStorage;
            _persistenceService = persistenceService;
        }

        public async Task Ejecutar()
        {
            string userId = localStorage.Obtener("userId");
            string basePath = _persistenceService.GetPersistentDataPath();

            string pathMisiones = Path.Combine(basePath, "Json_Misiones.json");
            string pathCategorias = Path.Combine(basePath, "categorias_encuesta_firebase.json");
            string pathLogros = Path.Combine(basePath, "Json_Logros.json");

            string misionesJson = File.Exists(pathMisiones) ? File.ReadAllText(pathMisiones) : "{}";
            string categoriasJson = File.Exists(pathCategorias) ? File.ReadAllText(pathCategorias) : "{}";
            string logrosJson = File.Exists(pathLogros) ? File.ReadAllText(pathLogros) : "{}";

            await firestore.SubirJson(userId, misionesJson, categoriasJson, logrosJson);
        }
    }
}
