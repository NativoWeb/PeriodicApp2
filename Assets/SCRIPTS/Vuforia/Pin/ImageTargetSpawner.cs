using UnityEngine;
using UnityEngine.UI;
using Vuforia;

public class ImageTargetSpawner : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el bot�n desde el Inspector

    void Start()
    {

        botonCompletarMision.interactable = false;
    }
}
