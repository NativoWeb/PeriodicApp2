using UnityEngine;

public class ElectronOrbit : MonoBehaviour
{
    private float baseRadius;
    private float currentAngle;
    private float orbitSpeed;
    private Transform orbitTransform;
    private bool orbitEnabled = false;

    /// <summary>Angulo actual sobre la orbita, en grados. Lo usa la estela.</summary>
    public float AnguloActual { get { return currentAngle; } }

    /// <summary>Radio de la orbita de este electron.</summary>
    public float Radio { get { return baseRadius; } }

    /// <summary>
    /// Prepara la orbita. El radio y el angulo llegan explicitos porque durante
    /// la animacion de formacion el electron aun esta en camino a su posicion,
    /// y leerlos del transform daria valores equivocados.
    /// </summary>
    public void Configure(int level, float radio, float anguloGrados)
    {
        // Las capas internas orbitan mas rapido, como en el modelo de Bohr
        orbitSpeed = 50f + (level * 10f);
        orbitTransform = transform.parent;
        baseRadius = radio;
        currentAngle = anguloGrados;
    }

    public void EnableOrbit()
    {
        orbitEnabled = true;
    }

    void Update()
    {
        if (!orbitEnabled || orbitTransform == null) return;

        // Avanzar sobre la orbita
        currentAngle += orbitSpeed * Time.deltaTime;
        currentAngle %= 360f;

        // La posicion se calcula en el espacio LOCAL del anillo, que ya viene
        // rotado por OrbitAnimation. Antes se volvia a aplicar aqui el sharedAngle
        // del padre, asi que el electron giraba en Y al doble de velocidad que su
        // propio anillo y se despegaba visualmente de la linea.
        transform.localPosition = new Vector3(
            baseRadius * Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            0,
            baseRadius * Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        );

        // Orientar el electron hacia su direccion de avance (tangente a la orbita)
        Vector3 tangente = new Vector3(
            -Mathf.Sin(currentAngle * Mathf.Deg2Rad),
            0,
            Mathf.Cos(currentAngle * Mathf.Deg2Rad)
        );

        if (tangente.sqrMagnitude > 0.0001f)
        {
            transform.localRotation = Quaternion.LookRotation(tangente);
        }
    }
}
