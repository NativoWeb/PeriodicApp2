using UnityEngine;

public class OrbitAnimation : MonoBehaviour
{
    // Propiedades públicas
    public int level { get; set; }
    public float baseRotationSpeed { get; set; }
    public float maxTiltAngle { get; set; }

    // Control de animación
    private bool animationEnabled = false;
    private float rotationProgress;
    private float wobbleProgress;

    public void Configure(int lvl, float speed, float tilt)
    {
        level = lvl;
        baseRotationSpeed = speed;
        maxTiltAngle = tilt;

        // Inicializar valores
        rotationProgress = Random.Range(0f, 360f);
        wobbleProgress = Random.Range(0f, 2f * Mathf.PI);
    }

    public void EnableAnimation()
    {
        animationEnabled = true;
    }

    void Update()
    {
        if (!animationEnabled) return;

        // Rotación continua en Y
        rotationProgress += baseRotationSpeed * Time.deltaTime;
        rotationProgress %= 360f;

        // Balanceo en X
        wobbleProgress += 1f * Time.deltaTime;
        float currentTilt = Mathf.Sin(wobbleProgress) * maxTiltAngle;

        transform.localRotation = Quaternion.Euler(
            currentTilt,
            rotationProgress,
            0
        );
    }
}