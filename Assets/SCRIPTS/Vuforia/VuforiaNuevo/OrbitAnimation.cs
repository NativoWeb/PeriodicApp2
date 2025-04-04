using UnityEngine;

public class OrbitAnimation : MonoBehaviour
{
    private int level;
    private int electronCount;

    public float rotationSpeedY;
    public float rotationSpeedX;
    public float sharedAngle; // Para sincronizaci�n con ElectronOrbit

    private float currentRotationY;
    private float currentRotationX;
    private bool animationEnabled = false;

    // Configuraci�n extendida (con nivel y cantidad de electrones)
    public void Configure(int lvl, float speedY, float speedX, int eCount)
    {
        level = lvl;
        rotationSpeedY = speedY;
        rotationSpeedX = speedX;
        electronCount = eCount;

        sharedAngle = Random.Range(0f, 360f);
        currentRotationY = sharedAngle;
        currentRotationX = Random.Range(0f, 360f);
    }

    // Configuraci�n simple
    public void Configure(float speedY, float speedX)
    {
        rotationSpeedY = speedY;
        rotationSpeedX = speedX;

        sharedAngle = Random.Range(0f, 360f);
        currentRotationY = sharedAngle;
        currentRotationX = Random.Range(0f, 360f);
    }

    public void EnableAnimation()
    {
        animationEnabled = true;
    }

    public Quaternion GetCurrentRotation()
    {
        return Quaternion.Euler(currentRotationX, currentRotationY, 0);
    }

    void Update()
    {
        if (!animationEnabled) return;

        // Actualizaci�n de �ngulos
        sharedAngle += rotationSpeedY * Time.deltaTime;
        sharedAngle %= 360f;

        currentRotationY = sharedAngle;
        currentRotationX += rotationSpeedX * Time.deltaTime;
        currentRotationX %= 360f;

        // Aplicar rotaci�n en ambos ejes
        transform.localRotation = Quaternion.Euler(
            currentRotationX,
            currentRotationY,
            0
        );
    }
}
