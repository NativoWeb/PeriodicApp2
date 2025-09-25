using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using PeriodicApp.Core.Domain.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public sealed class SubirDatosJSON
    {
        private readonly IServicioFirestore firestore;
        private readonly IServicioLocalStorage localStorage;

        public SubirDatosJSON(IServicioFirestore firestore, IServicioLocalStorage localStorage)
        {
            this.firestore = firestore;
            this.localStorage = localStorage;
        }

        public async Task Ejecutar()
        {
            string userId = localStorage.Obtener("userId");

            string pathMisiones = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");
            string pathCategorias = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
            string pathLogros = Path.Combine(Application.persistentDataPath, "Json_Logros.json");

            string misionesJson = File.Exists(pathMisiones) ? File.ReadAllText(pathMisiones) : "{}";
            string categoriasJson = File.Exists(pathCategorias) ? File.ReadAllText(pathCategorias) : "{}";
            string logrosJson = File.Exists(pathLogros) ? File.ReadAllText(pathLogros) : "{}";

            await firestore.SubirJson(userId, misionesJson, categoriasJson, logrosJson);
        }
    }
}
