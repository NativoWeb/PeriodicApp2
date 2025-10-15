

namespace PeriodicApp.Core.Application.Interfaces
{
    /// Servicio para cargar recursos desde la carpeta Resources de Unity
    public interface IResourceLoaderService
    {
        /// Carga un TextAsset desde Resources y retorna su contenido como string
        /// <param name="path">Ruta relativa dentro de Resources (sin extensión)</param>
        /// <returns>Contenido del archivo o null si no existe</returns>
        string LoadTextAsset(string path);

        /// Carga un objeto genérico desde Resources
        object Load(string path);

        /// Verifica si un recurso existe
        bool Exists(string path);
    }
}
