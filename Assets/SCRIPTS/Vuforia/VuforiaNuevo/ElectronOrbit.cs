using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    private float baseRadius;
    private float currentAngle;
    private float orbitSpeed;
    private Transform orbitTransform;
    private OrbitAnimation orbitAnimation; // acceso al �ngulo compartido
    private bool orbitEnabled = false;

    public void Configure(int level)
    {
        orbitSpeed = 50f + (level * 10f);
        orbitTransform = transform.parent;
        baseRadius = transform.localPosition.magnitude;

        orbitAnimation = orbitTransform.GetComponent<OrbitAnimation>();

        currentAngle = Mathf.Atan2(
            transform.localPosition.z,
            transform.localPosition.x
        ) * Mathf.Rad2Deg;
    }

    public void EnableOrbit()
    {
        orbitEnabled = true;
    }

    void Update()
    {
        if (!orbitEnabled || orbitTransform == null || orbitAnimation == null) return;

        // Actualizar �ngulo
        currentAngle += orbitSpeed * Time.deltaTime;
        currentAngle %= 360f;

        // Usar la rotaci�n Y sincronizada desde OrbitAnimation
        Quaternion rotation = Quaternion.Euler(0, orbitAnimation.sharedAngle, 0);

        // Calcular nueva posici�n
        Vector3 newPos = rotation * new Vector3(
            baseRadius * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            0,
            baseRadius * Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        );

        transform.localPosition = newPos;

        // Rotaci�n del electr�n
        transform.localRotation = Quaternion.LookRotation(transform.localPosition.normalized);
    }
}
