using UnityEngine;
using UnityEngine.UI;

public class NavegacionAmigosSolicitudesManager : MonoBehaviour
{

    // acá tengo que instanciar los 4 paneles el seleccionar x y Y y el de listar amigos y listar solicitudes para dependiendo el btn se activa o desactiva los paneles con el btn
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
        
        panelAmigos.SetActive(true);
        panelseleccionarX.SetActive(true);

        // desactivamos los paneles anteriores
        if (panelSolicitudes != null)
            panelSolicitudes.SetActive(false);

        if(panelseleccionarY != null)
            panelseleccionarY.SetActive(false);
    }
    void ActivarPanelSolicitudes()
    {
        panelSolicitudes.SetActive(true);
        panelseleccionarY.SetActive(true);

        // desactivamos los paneles anteriores
        if (panelAmigos != null)
            panelAmigos.SetActive(false);

        if (panelseleccionarX != null)
            panelseleccionarX.SetActive(false);

    }

   
}
