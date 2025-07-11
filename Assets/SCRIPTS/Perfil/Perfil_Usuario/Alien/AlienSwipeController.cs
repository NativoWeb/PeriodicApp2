using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static AlienDataManager;
using System.Xml.Linq;
using TMPro;

public class AlienSwipeController : MonoBehaviour,
                                    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Botones Atrás/Siguiente")]
    public Button BtnSiguiente;
    public Button BtnAtras;

    [Header("IU — mismo orden que los aliens")]
    public RawImage[] rawImages;           // RawImages dentro del Canvas
    [Header("Modelos")]
    public AlienRotator[] alienRotators;   // Los scripts del §1
    [Header("Cámaras (opcional)")]
    public Camera[] alienCams;             // Desactiva la que no uses para ahorrar

   
    private bool[] puedeUsar;          // guardamos la máscara

    private int indiceActual = 0;
    private Vector2 startTouch;
    public float distanciaMinimaSwipe = 50f;

    [Header("Candado único para avatares bloqueados")]
    [SerializeField] private GameObject candadoIcon;

    private int usuarioXP;
    private RangoXP[] rangos;

    [Header("XP")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TMP_Text xpTexto; // opcional para mostrar "1234 / 2000"
    [SerializeField] private TMP_Text rangoNombreTexto;
    [SerializeField] private TMP_Text textoDesbloqueoSiguiente;

    void Start() => ActualizarVista();

   
    /* ───────── Gestión de Swipe ───────── */
    public void OnBeginDrag(PointerEventData e) => startTouch = e.position;

    public void OnDrag(PointerEventData e) { /* no lo necesitamos */ }

    public void OnEndDrag(PointerEventData e)
    {
        float dx = e.position.x - startTouch.x;
        if (Mathf.Abs(dx) < distanciaMinimaSwipe) return;      // swipe muy corto

        indiceActual += dx < 0 ? +1 : -1;                      // izq→siguiente, der→anterior
        indiceActual = Mathf.Clamp(indiceActual, 0, rawImages.Length - 1);

        ActualizarVista();
    }

    /* ───────── Muestra/oculta y enciende/apaga lo necesario ───────── */
    void ActualizarVista()
    {
        BtnSiguiente.onClick.RemoveAllListeners();
        BtnAtras.onClick.RemoveAllListeners();

        BtnSiguiente.onClick.AddListener(MostrarAlienSiguiente);
        BtnAtras.onClick.AddListener(MostrarAlienAnterior);

        for (int i = 0; i < rawImages.Length; i++)
        {
            bool esActivo = (i == indiceActual);

            rawImages[i].gameObject.SetActive(esActivo);
            alienRotators[i].enabled = true;

            if (esActivo) alienRotators[i].ComenzarRotacion();
            else alienRotators[i].DetenerRotacion();

            if (alienCams != null && alienCams.Length > i && alienCams[i] != null)
                alienCams[i].enabled = esActivo;
        }

        // Mostrar candado si está bloqueado
        if (puedeUsar != null && indiceActual < puedeUsar.Length)
        {
            bool bloqueado = !puedeUsar[indiceActual];
            candadoIcon.SetActive(bloqueado);

            if (bloqueado)
            {
                RectTransform rawRect = rawImages[indiceActual].GetComponent<RectTransform>();
                RectTransform candadoRect = candadoIcon.GetComponent<RectTransform>();
                // Mostrar cuántos puntos faltan para desbloquear el siguiente rango
                if (textoDesbloqueoSiguiente != null && indiceActual < rangos.Length)
                {
                    RangoXP rango = rangos[indiceActual];
                    int puntosFaltantes = Mathf.Max(rango.xpMinimo - usuarioXP, 0);

                    textoDesbloqueoSiguiente.text = $"{puntosFaltantes} puntos para desbloquear {rango.nombre.ToUpper()}";
                }
            }

            // Actualizar Slider y nombre del rango
            if (rangos != null && indiceActual < rangos.Length)
            {
                RangoXP rango = rangos[indiceActual];

                // Mostrar el nombre del rango
                if (rangoNombreTexto != null)
                    rangoNombreTexto.text = rango.nombre;

                if (!bloqueado && xpSlider != null)
                {
                    float xpRelativo = Mathf.Clamp(usuarioXP - rango.xpMinimo, 0, rango.xpMaximo - rango.xpMinimo);
                    float xpTotal = rango.xpMaximo - rango.xpMinimo;

                    float porcentaje = (xpRelativo / xpTotal) * 100f;
                    xpSlider.value = xpRelativo / xpTotal;

                    if (xpTexto != null)
                        xpTexto.text = $"{porcentaje:F0}%";

                    if (textoDesbloqueoSiguiente != null)
                        textoDesbloqueoSiguiente.text = ""; // Limpia el texto si no está bloqueado
                }
                else if (xpSlider != null)
                {
                    xpSlider.value = 0f;

                    if (xpTexto != null)
                        xpTexto.text = "0%";
                }
                

            }
        }
    }



    /* ───────── Uso de botones para pasar de alien ───────── */
    public void MostrarAlienAnterior()
    {
        if (indiceActual > 0)
        {
            indiceActual--;
            ActualizarVista();
        }
    }

    public void MostrarAlienSiguiente()
    {
        if (indiceActual < rawImages.Length - 1)
        {
            indiceActual++;
            ActualizarVista();
        }
    }

    /* ─────────────────────────────────────────────────── */
  

    public void ActualizarSliderXP(int xp, RangoXP[] dataRangos)
    {
        usuarioXP = xp;
        rangos = dataRangos;
        ActualizarVista(); // refresca el slider también
    }

    public void SetUnlockMask(bool[] mask, Material lockedMat)
    {
        puedeUsar = mask;

        for (int i = 0; i < alienRotators.Length; i++)
        {
            Renderer r = alienRotators[i].GetComponentInChildren<Renderer>();

            // Cambia material según esté desbloqueado
            if (mask != null && i < mask.Length && !mask[i])
                r.material = lockedMat;        // bloqueado → gris
            
        }

        ActualizarVista();   // refresca lo que se muestra
    }
    public void IrAlAlien(int indice)// Empezar con el alien de su rango
    {
        indiceActual = Mathf.Clamp(indice, 0, rawImages.Length - 1);
        ActualizarVista();
    }

}
