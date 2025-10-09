using System.Threading.Tasks;
using PeriodicApp.Core.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeriodicApp.Infrastructure.Services
{
    /// Implementación de ISceneService usando Unity SceneManager
    public class UnitySceneService : ISceneService
    {

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }

        public async Task LoadSceneAsync(string sceneName)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }
}
