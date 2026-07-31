using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class DeslizarParaCerrarManager : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform panel;
    public float distanciaMinima = 900f;
    public float velocidadCierre = 5000f;

    private Vector2 _inicioTouch;
    private bool _cerrando;
    private float _alturaInicial;

    private AlienRotator _alienRotator;
    private PortalRotator _portalRotator;

    private void Start()
    {
        if (panel != null)
            _alturaInicial = panel.sizeDelta.y;

        _alienRotator = FindAnyObjectByType<AlienRotator>();
        _portalRotator = FindAnyObjectByType<PortalRotator>();
    }

    public void AbrirPanelAlien()
    {
        panel.gameObject.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _inicioTouch = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_cerrando) return;

        Vector2 delta = eventData.position - _inicioTouch;

        if (delta.y < 0)
        {
            float nuevaAltura = Mathf.Clamp(_alturaInicial + delta.y, 0, _alturaInicial);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, nuevaAltura);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_cerrando) return;

        Vector2 delta = eventData.position - _inicioTouch;

        if (Mathf.Abs(delta.y) > distanciaMinima)
        {
            ContraerPanel();
        }
        else
        {
            AnimarRestaurar();
        }
    }

    private void ContraerPanel()
    {
        _cerrando = true;
        DOTween.Kill(panel);

        float alturaActual = panel.sizeDelta.y;
        DOTween.To(
                () => alturaActual,
                v =>
                {
                    alturaActual = v;
                    panel.sizeDelta = new Vector2(panel.sizeDelta.x, v);
                },
                0f,
                0.3f)
            .SetEase(Ease.InCubic)
            .SetTarget(panel)
            .OnComplete(() =>
            {
                panel.gameObject.SetActive(false);
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, _alturaInicial);
                _cerrando = false;
            });
    }

    private void AnimarRestaurar()
    {
        DOTween.Kill(panel);

        float alturaActual = panel.sizeDelta.y;
        DOTween.To(
                () => alturaActual,
                v =>
                {
                    alturaActual = v;
                    panel.sizeDelta = new Vector2(panel.sizeDelta.x, v);
                },
                _alturaInicial,
                0.2f)
            .SetEase(Ease.OutCubic)
            .SetTarget(panel);
    }

    private void OnDestroy()
    {
        DOTween.Kill(panel);
    }
}
