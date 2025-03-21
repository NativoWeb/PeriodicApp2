using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class ImageTargetSpawner : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el botón desde el Inspector

    void Start()
    {

        botonCompletarMision.interactable = false;
    }
}
