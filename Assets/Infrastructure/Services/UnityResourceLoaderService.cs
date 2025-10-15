using PeriodicApp.Core.Application.Interfaces;
using UnityEngine;

namespace PeriodicApp.Infrastructure.Services
{
    /// Implementación de IResourceLoaderService usando Unity Resources
    public class UnityResourceLoaderService : IResourceLoaderService
    {
        public string LoadTextAsset(string path)
        {
            var textAsset = Resources.Load<TextAsset>(path);
            return textAsset?.text;
        }

        public object Load(string path)
        {
            return Resources.Load(path);
        }

        public bool Exists(string path)
        {
            var resource = Resources.Load(path);
            bool exists = resource != null;

            // Liberar el recurso inmediatamente si solo estamos verificando existencia
            if (exists)
            {
                Resources.UnloadAsset(resource);
            }

            return exists;
        }
    }
}
