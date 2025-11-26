using UnityEngine;

/// <summary>
/// Script unificado para rotación de objetos.
/// Reemplaza: Rotar.cs, Rotate.cs, RotateAround.cs
/// Soporta múltiples modos de rotación con un solo componente.
/// </summary>
public class ObjectRotator : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [Tooltip("Tipo de rotación a aplicar")]
    public RotationType rotationType = RotationType.SelfRotate;

    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float rotationSpeed = 50f;

    [Header("Configuración de Ejes")]
    [Tooltip("Eje de rotación (solo para SelfRotate)")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Configuración de RotateAround")]
    [Tooltip("Objeto objetivo alrededor del cual rotar (solo para RotateAround)")]
    public GameObject targetObject;

    [Tooltip("Usar posición del objetivo como punto de pivote")]
    public bool useTargetPosition = true;

    [Tooltip("Usar forward del objetivo como eje de rotación")]
    public bool useTargetForward = true;

    [Tooltip("Punto de pivote personalizado (si no se usa targetObject)")]
    public Vector3 customPivotPoint = Vector3.zero;

    [Tooltip("Eje personalizado para RotateAround (si no se usa targetForward)")]
    public Vector3 customRotationAxis = Vector3.up;

    private void Update()
    {
        switch (rotationType)
        {
            case RotationType.SelfRotate:
                RotateSelf();
                break;

            case RotationType.RotateAroundSelf:
                RotateAroundSelf();
                break;

            case RotationType.RotateAroundTarget:
                RotateAroundTarget();
                break;
        }
    }

    /// <summary>
    /// Rota el objeto sobre sí mismo usando Transform.Rotate
    /// Equivalente a: Rotar.cs
    /// </summary>
    private void RotateSelf()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Rota el objeto alrededor de su propia posición
    /// Equivalente a: Rotate.cs
    /// </summary>
    private void RotateAroundSelf()
    {
        transform.RotateAround(transform.position, rotationAxis, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Rota el objeto alrededor de otro objeto o punto
    /// Equivalente a: RotateAround.cs
    /// </summary>
    private void RotateAroundTarget()
    {
        // Determinar el punto de pivote
        Vector3 pivotPoint = useTargetPosition && targetObject != null
            ? targetObject.transform.position
            : customPivotPoint;

        // Determinar el eje de rotación
        Vector3 axis = useTargetForward && targetObject != null
            ? targetObject.transform.forward
            : customRotationAxis;

        transform.RotateAround(pivotPoint, axis, rotationSpeed * Time.deltaTime);
    }

    #region Editor Helpers
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validar que targetObject esté asignado cuando se usa RotateAroundTarget
        if (rotationType == RotationType.RotateAroundTarget &&
            targetObject == null &&
            (useTargetPosition || useTargetForward))
        {
            UnityEngine.Debug.LogWarning($"[{gameObject.name}] ObjectRotator: Se requiere un Target Object para RotateAroundTarget con useTargetPosition/useTargetForward activado.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualizar el punto de pivote y eje de rotación en el editor
        if (rotationType == RotationType.RotateAroundTarget)
        {
            Vector3 pivotPoint = useTargetPosition && targetObject != null
                ? targetObject.transform.position
                : customPivotPoint;

            Vector3 axis = useTargetForward && targetObject != null
                ? targetObject.transform.forward
                : customRotationAxis;

            // Dibujar punto de pivote
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pivotPoint, 0.1f);

            // Dibujar eje de rotación
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(pivotPoint, axis.normalized * 0.5f);

            // Línea desde el objeto al pivote
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, pivotPoint);
        }
    }
#endif
    #endregion
}

/// <summary>
/// Tipos de rotación disponibles
/// </summary>
public enum RotationType
{
    /// <summary>
    /// Rota el objeto sobre sí mismo usando el eje especificado
    /// Equivalente a: transform.Rotate()
    /// </summary>
    SelfRotate,

    /// <summary>
    /// Rota el objeto alrededor de su propia posición
    /// Equivalente a: transform.RotateAround(transform.position, ...)
    /// </summary>
    RotateAroundSelf,

    /// <summary>
    /// Rota el objeto alrededor de otro objeto o punto en el espacio
    /// Equivalente a: transform.RotateAround(targetPosition, ...)
    /// </summary>
    RotateAroundTarget
}
