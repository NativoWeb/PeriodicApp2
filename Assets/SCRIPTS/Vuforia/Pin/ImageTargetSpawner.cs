using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class ImageTargetSpawner : MonoBehaviour
{
    public GameObject imageTargetPrefab; // Prefab del ImageTarget
    public string[] elementos; // Lista de nombres de imágenes en la BD de Vuforia
    public Button botonCompletarMision; // Asigna el botón desde el Inspector

    void Start()
    {

        botonCompletarMision.interactable = false;

        foreach (string elemento in elementos)
        {
            GameObject newTarget = Instantiate(imageTargetPrefab, Vector3.zero, Quaternion.identity);
            var trackable = newTarget.GetComponent<ObserverBehaviour>();

            if (trackable != null)
            {
                newTarget.name = elemento; // Renombra el objeto en la jerarquía
                newTarget.GetComponent<ImageRecognition>().elementoQuimico = elemento;

                newTarget.gameObject.SetActive(true);
            }
        }
    }
}
