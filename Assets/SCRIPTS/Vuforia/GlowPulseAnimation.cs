using UnityEngine;

public class GlowPulseAnimation : MonoBehaviour
{
    public float minSize = 0.25f;
    public float maxSize = 0.45f;
    public float pulseSpeed = 0.5f;

    private float currentScale;
    private bool growing = true;

    void Update()
    {
        // Calcular el nuevo tama�o
        if (growing)
        {
            currentScale += Time.deltaTime * pulseSpeed;
            if (currentScale >= maxSize) growing = false;
        }
        else
        {
            currentScale -= Time.deltaTime * pulseSpeed;
            if (currentScale <= minSize) growing = true;
        }

        // Aplicar el tama�o
        transform.localScale = Vector3.one * currentScale;

        // Variar tambi�n la intensidad de la emisi�n
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            float emissionIntensity = Mathf.Lerp(1.0f, 1.5f, (currentScale - minSize) / (maxSize - minSize));
            Color baseColor = renderer.material.GetColor("_Color");
            renderer.material.SetColor("_EmissionColor", baseColor * emissionIntensity);
        }
    }
}