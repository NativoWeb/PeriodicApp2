using UnityEngine;

public class AlienRotator : MonoBehaviour
{
    [Tooltip("Qué tan rápido rota")]
    public float velocidadRotacion = 60f;

    private bool girar = false;       // → solo rota si esto es true

    void Update()
    {
        if (girar)
            transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime);
    }

    // Se llaman desde el controlador de swipe
    public void ComenzarRotacion() => girar = true;
    public void DetenerRotacion() => girar = false;

    
}
