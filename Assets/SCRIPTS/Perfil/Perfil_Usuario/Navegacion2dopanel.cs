using UnityEngine;
using UnityEngine.UI;

public class Navegacion2dopanel : MonoBehaviour
{
    [Header("Botones para seleccionar panel")]
    public Button BtnPanelAmigos;
    public Button BtnPanelSolicitudes;

    [Header("Panel seleccionar X - Y")]
    [SerializeField] public GameObject panelseleccionarX;
    [SerializeField] public GameObject panelseleccionarY;

    [Header("Paneles amigos y solicitudes")]
    [SerializeField] public GameObject panelAmigos;
    [SerializeField] public GameObject panelSolicitudes;

    void Start()
    {
        BtnPanelAmigos.onClick.AddListener(ActivarPanelAmigos);
        BtnPanelSolicitudes.onClick.AddListener(ActivarPanelSolicitudes);
    }

    void ActivarPanelAmigos()
    {
        PanelAnimator.Show(panelAmigos);
        panelseleccionarX.SetActive(true);

        if (panelSolicitudes != null)
            PanelAnimator.Hide(panelSolicitudes);

        if (panelseleccionarY != null)
            panelseleccionarY.SetActive(false);
    }

    void ActivarPanelSolicitudes()
    {
        PanelAnimator.Show(panelSolicitudes);
        panelseleccionarY.SetActive(true);

        if (panelAmigos != null)
            PanelAnimator.Hide(panelAmigos);

        if (panelseleccionarX != null)
            panelseleccionarX.SetActive(false);
    }
}
