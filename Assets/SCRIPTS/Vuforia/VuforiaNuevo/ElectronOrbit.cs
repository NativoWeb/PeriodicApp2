using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    private float baseRadius;
    private float currentAngle;
    private float orbitSpeed = 180f; // Grados/segundo

    void Start()
    {
        // Captura la posici�n inicial como radio
        baseRadius = transform.localPosition.magnitude;
        currentAngle = Mathf.Atan2(
            transform.localPosition.z,
            transform.localPosition.x
        ) * Mathf.Rad2Deg;
    }

    void Update()
    {
        // Movimiento orbital independiente
        currentAngle += orbitSpeed * Time.deltaTime;
        currentAngle %= 360f;

        // Mantiene posici�n relativa a la �rbita padre
        transform.localPosition = new Vector3(
            baseRadius * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            0f,
            baseRadius * Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        );
    }
}
//public class ElectronOrbit : MonoBehaviour
//{
//    private Transform center;
//    private float radius;
//    private float speed;
//    private float angle;
//    private Vector3 originalPosition;

//    public void Initialize(Transform center, float radius, float speed)
//    {
//        this.center = center;
//        this.radius = radius;
//        this.speed = speed;
//        this.angle = Random.Range(0, 2 * Mathf.PI);
//        this.originalPosition = center.localPosition;
//    }

//    void Update()
//    {
//        // Movimiento orbital independiente de la rotaci�n global
//        angle += speed * Time.deltaTime;
//        Vector3 orbitPos = originalPosition + new Vector3(
//            radius * Mathf.Cos(angle),
//            0,
//            radius * Mathf.Sin(angle)
//        );

//        // Mantener posici�n relativa considerando la rotaci�n padre
//        transform.localPosition = orbitPos;
//    }
//}