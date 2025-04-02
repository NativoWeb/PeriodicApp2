using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    private Transform center;
    private float radius;
    private float speed;
    private float currentAngle;

    public void Initialize(Transform center, float radius, float speed)
    {
        this.center = center;
        this.radius = radius;
        this.speed = speed;
        this.currentAngle = Random.Range(0, 2 * Mathf.PI); // Ángulo inicial aleatorio
        UpdatePosition();
    }

    void Update()
    {
        currentAngle += speed * Time.deltaTime;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        Vector3 newPos = new Vector3(
            radius * Mathf.Cos(currentAngle),
            0,
            radius * Mathf.Sin(currentAngle)
        );
        transform.localPosition = newPos;
    }
}