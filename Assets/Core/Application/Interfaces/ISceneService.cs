//using UnityEngine;

using System.Threading.Tasks;

namespace PeriodicApp.Core.Application
{
    public interface ISceneService
    {   
        //Carga una scena por nombre
        void LoadScene(string sceneName);

        //Carga una escena por indice
        void LoadScene(int sceneIndex);

        //Carga una escena de forma asincrona
        Task LoadSceneAsync(string sceneName);

        //Obtiene el nombre de la escena activa actual
        string GetActiveSceneName();


    }
}
