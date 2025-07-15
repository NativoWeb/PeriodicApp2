using UnityEngine;

public class PortalRotator : MonoBehaviour
{
   
    public float velocidadRotacion = 100f; // Velocidad de rotación (grados por segundo)

    void Update()
    {
        // Rota el objeto en el eje Z
        transform.Rotate(0, 0, -velocidadRotacion * Time.deltaTime);
    }


    //public void IniciarRotacion()
    //{
    //    rotando = true;
    //}

    //public void DetenerRotacion()
    //{
    //    rotando = false;
    //}
}
