using UnityEngine;

public class Rotador : MonoBehaviour
{
    public Vector3 velocidadRotacion = new Vector3(0, 15f, 0);

    void Update()
    {
        transform.Rotate(velocidadRotacion * Time.deltaTime);
    }
}
