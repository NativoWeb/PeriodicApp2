using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class ImageTargetSpawner : MonoBehaviour
{
    public GameObject imageTargetPrefab; // Prefab del ImageTarget
    public string[] elementos; // Lista de nombres de im�genes en la BD de Vuforia
    public Button botonCompletarMision; // Asigna el bot�n desde el Inspector

    void Start()
    {

        botonCompletarMision.interactable = false;

        foreach (string elemento in elementos)
        {
            GameObject newTarget = Instantiate(imageTargetPrefab, Vector3.zero, Quaternion.identity);
            var trackable = newTarget.GetComponent<ObserverBehaviour>();

            if (trackable != null)
            {
                newTarget.name = elemento; // Renombra el objeto en la jerarqu�a
                newTarget.GetComponent<ImageRecognition>().elementoQuimico = elemento;

                newTarget.gameObject.SetActive(true);
            }
        }
    }
}
