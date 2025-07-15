using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeslizarParaCerrarManager : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    
    public RectTransform panel;           // El panel que se contrae
    public float distanciaMinima = 900f;  // Mínimo de movimiento para cerrar
    public float velocidadCierre = 5000f; // Velocidad de contracción

    private Vector2 inicioTouch;
    private bool cerrando = false;
    private float alturaInicial;

    
    

    private AlienRotator alienRotator;
    private PortalRotator portalRotator;

    private void Start()
    {
        if (panel != null)
            alturaInicial = panel.sizeDelta.y;

        alienRotator = FindAnyObjectByType<AlienRotator>();
        portalRotator = FindAnyObjectByType<PortalRotator>(); 
    }

    public void AbrirPanelAlien()
    {
        panel.gameObject.SetActive(true);
        //portalRotator.IniciarRotacion();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        inicioTouch = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (cerrando) return;

        Vector2 delta = eventData.position - inicioTouch;

        if (delta.y < 0)
        {
            float nuevaAltura = Mathf.Clamp(alturaInicial + delta.y, 0, alturaInicial);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, nuevaAltura);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (cerrando) return;

        Vector2 delta = eventData.position - inicioTouch;

        if (Mathf.Abs(delta.y) > distanciaMinima)
        {
            StartCoroutine(ContraerPanel());
        }
        else
        {
            // Volver a la altura original
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, alturaInicial);
        }
    }

    private System.Collections.IEnumerator ContraerPanel()
    {
        cerrando = true;

        float alturaActual = panel.sizeDelta.y;

        while (panel.sizeDelta.y > 10f)
        {
            alturaActual -= Time.deltaTime * velocidadCierre;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, Mathf.Max(alturaActual, 0));
            yield return null;
        }

        panel.gameObject.SetActive(false);
        alienRotator.DetenerRotacion();
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, alturaInicial); // Restaurar para próxima vez
        cerrando = false;
    }

}
