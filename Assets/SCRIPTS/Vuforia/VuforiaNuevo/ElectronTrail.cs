using UnityEngine;

/// <summary>
/// Estela luminosa detras de un electron.
///
/// No usa TrailRenderer a proposito: ese componente trabaja en espacio de mundo
/// y, en AR, deja rastros flotando por la habitacion en cuanto el marcador se
/// mueve o se pierde el tracking. Aqui la estela se calcula como un arco de la
/// propia orbita, en espacio local, asi que queda pegada al atomo pase lo que
/// pase con el tracking.
///
/// Ademas es mas barato: no guarda historico de posiciones, solo evalua el arco
/// por detras del angulo actual.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ElectronTrail : MonoBehaviour
{
    private ElectronOrbit electron;
    private LineRenderer linea;
    private Vector3[] puntos;
    private float arcoGrados;

    /// <summary>
    /// Prepara la estela. Este componente debe vivir en un objeto hijo de la
    /// orbita, para compartir su espacio local con el electron.
    /// </summary>
    public void Configurar(ElectronOrbit electronOrbita, Material material,
                           float grados, int segmentos, float ancho)
    {
        electron = electronOrbita;
        arcoGrados = grados;

        segmentos = Mathf.Max(2, segmentos);
        puntos = new Vector3[segmentos];

        linea = GetComponent<LineRenderer>();
        linea.useWorldSpace = false;
        linea.loop = false;
        linea.positionCount = segmentos;
        linea.numCapVertices = 0;
        linea.numCornerVertices = 0;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        linea.alignment = LineAlignment.View;

        if (material != null)
        {
            linea.sharedMaterial = material;
        }

        // La estela nace fina en la cola y se ensancha junto al electron
        AnimationCurve grosor = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, ancho));
        linea.widthCurve = grosor;

        // Y se desvanece hacia la cola
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        linea.colorGradient = degradado;

        linea.enabled = false;
    }

    public void Activar()
    {
        if (linea != null)
        {
            linea.enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (electron == null || linea == null || puntos == null)
        {
            return;
        }

        float radio = electron.Radio;
        float angulo = electron.AnguloActual;
        int total = puntos.Length;

        // El ultimo punto coincide con el electron; los anteriores retroceden
        // sobre la circunferencia de la orbita.
        for (int i = 0; i < total; i++)
        {
            float avance = i / (float)(total - 1);          // 0 = cola, 1 = electron
            float grados = angulo - arcoGrados * (1f - avance);
            float radianes = grados * Mathf.Deg2Rad;

            puntos[i] = new Vector3(
                radio * Mathf.Cos(radianes),
                0f,
                radio * Mathf.Sin(radianes));
        }

        linea.SetPositions(puntos);
    }
}