using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coreografia de formacion del atomo.
///
/// Anima la entrada de cientos de particulas desde un unico Update en lugar de
/// crear un tween por objeto: el oganeson son 412 particulas, y 412 tweens (o
/// 412 corrutinas) cuestan mucho mas que un bucle sobre una lista de structs.
///
/// Las particulas no aparecen en su sitio: vuelan hacia el desde fuera, que es
/// lo que convierte el montaje en una formacion y no en un simple encendido.
/// </summary>
public class AtomoEnsamblador : MonoBehaviour
{
    /// <summary>Curva de entrada de cada particula.</summary>
    public enum Curva
    {
        /// <summary>Frena suave al llegar. Para el nucleo.</summary>
        Suave,
        /// <summary>Se pasa de largo y vuelve. Para los electrones.</summary>
        Rebote
    }

    private struct Entrada
    {
        public Transform objetivo;
        public Vector3 desde;
        public Vector3 hasta;
        public float escalaFinal;
        public float retardo;
        public float duracion;
        public float tiempo;
        public Curva curva;
    }

    private readonly List<Entrada> entradas = new List<Entrada>();

    /// <summary>Indica si ya no queda ninguna particula en vuelo.</summary>
    public bool Terminado { get { return entradas.Count == 0; } }

    /// <summary>
    /// Registra la entrada animada de una particula. El objeto se coloca de
    /// inmediato en <paramref name="desde"/> con escala cero, y viaja hasta
    /// <paramref name="hasta"/>.
    /// </summary>
    public void Registrar(Transform objetivo, Vector3 desde, Vector3 hasta,
                          float escalaFinal, float retardo, float duracion, Curva curva)
    {
        if (objetivo == null)
        {
            return;
        }

        objetivo.localPosition = desde;
        objetivo.localScale = Vector3.zero;

        Entrada entrada;
        entrada.objetivo = objetivo;
        entrada.desde = desde;
        entrada.hasta = hasta;
        entrada.escalaFinal = escalaFinal;
        entrada.retardo = retardo;
        entrada.duracion = Mathf.Max(0.01f, duracion);
        entrada.tiempo = 0f;
        entrada.curva = curva;

        entradas.Add(entrada);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Se recorre hacia atras para poder retirar las terminadas sin
        // desordenar los indices pendientes.
        for (int i = entradas.Count - 1; i >= 0; i--)
        {
            Entrada e = entradas[i];

            if (e.objetivo == null)
            {
                entradas.RemoveAt(i);
                continue;
            }

            e.tiempo += dt;

            if (e.tiempo < e.retardo)
            {
                entradas[i] = e;
                continue;
            }

            float avance = Mathf.Clamp01((e.tiempo - e.retardo) / e.duracion);
            float suavizado = e.curva == Curva.Rebote
                ? SalidaRebote(avance)
                : SalidaSuave(avance);

            e.objetivo.localPosition = Vector3.LerpUnclamped(e.desde, e.hasta, suavizado);
            e.objetivo.localScale = Vector3.one * (e.escalaFinal * SalidaSuave(avance));

            if (avance >= 1f)
            {
                e.objetivo.localPosition = e.hasta;
                e.objetivo.localScale = Vector3.one * e.escalaFinal;
                entradas.RemoveAt(i);
                continue;
            }

            entradas[i] = e;
        }
    }

    /// <summary>
    /// Late un objeto: crece y vuelve. Se usa al cerrarse el nucleo, para
    /// marcar que la masa ya esta completa.
    /// </summary>
    public IEnumerator Pulso(Transform objetivo, float intensidad, float duracion)
    {
        if (objetivo == null)
        {
            yield break;
        }

        Vector3 escalaBase = objetivo.localScale;
        Vector3 escalaPico = escalaBase * (1f + intensidad);
        float mitad = Mathf.Max(0.01f, duracion * 0.5f);

        float t = 0f;
        while (t < mitad)
        {
            if (objetivo == null)
            {
                yield break;
            }

            t += Time.deltaTime;
            objetivo.localScale = Vector3.Lerp(escalaBase, escalaPico, SalidaSuave(t / mitad));
            yield return null;
        }

        t = 0f;
        while (t < mitad)
        {
            if (objetivo == null)
            {
                yield break;
            }

            t += Time.deltaTime;
            objetivo.localScale = Vector3.Lerp(escalaPico, escalaBase, SalidaSuave(t / mitad));
            yield return null;
        }

        if (objetivo != null)
        {
            objetivo.localScale = escalaBase;
        }
    }

    /// <summary>
    /// Traza el anillo de una orbita: la linea se dibuja sola desde un punto
    /// hasta cerrar la circunferencia.
    /// </summary>
    public IEnumerator TrazarAnillo(LineRenderer linea, float radio, int puntos, float duracion)
    {
        if (linea == null)
        {
            yield break;
        }

        puntos = Mathf.Max(8, puntos);

        // Mientras se dibuja no puede estar cerrado, o Unity uniria el ultimo
        // punto con el primero desde el primer frame.
        linea.loop = false;
        linea.enabled = true;

        float t = 0f;
        duracion = Mathf.Max(0.01f, duracion);
        int dibujados = 0;

        while (t < duracion)
        {
            // En AR el marcador puede perderse a mitad de la formacion y el
            // atomo se destruye; sin esta guarda la corrutina seguiria viva
            // lanzando excepciones contra un LineRenderer ya inexistente.
            if (linea == null)
            {
                yield break;
            }

            t += Time.deltaTime;
            float suavizado = SalidaSuave(Mathf.Clamp01(t / duracion));
            int visibles = Mathf.Max(2, Mathf.RoundToInt(puntos * suavizado));

            linea.positionCount = visibles;

            // Se colocan solo los puntos nuevos y de uno en uno: SetPositions
            // exigiria un array del tamano exacto, que cambia cada frame y
            // generaria basura para el recolector durante toda la animacion.
            for (int i = dibujados; i < visibles; i++)
            {
                linea.SetPosition(i, PuntoDelAnillo(i, puntos, radio));
            }
            dibujados = visibles;

            yield return null;
        }

        if (linea == null)
        {
            yield break;
        }

        // Cerrar el anillo
        linea.positionCount = puntos;
        for (int i = dibujados; i < puntos; i++)
        {
            linea.SetPosition(i, PuntoDelAnillo(i, puntos, radio));
        }
        linea.loop = true;
    }

    private static Vector3 PuntoDelAnillo(int indice, int total, float radio)
    {
        float angulo = 2f * Mathf.PI * indice / total;
        return new Vector3(
            radio * Mathf.Cos(angulo),
            0f,
            radio * Mathf.Sin(angulo));
    }

    // ------------------------------------------------------------------ curvas

    /// <summary>Arranca rapido y frena al final (cubica).</summary>
    private static float SalidaSuave(float t)
    {
        t = Mathf.Clamp01(t);
        float inverso = 1f - t;
        return 1f - inverso * inverso * inverso;
    }

    /// <summary>Se pasa del destino y vuelve, como un iman al encajar.</summary>
    private static float SalidaRebote(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float inverso = t - 1f;
        return 1f + c3 * inverso * inverso * inverso + c1 * inverso * inverso;
    }
}