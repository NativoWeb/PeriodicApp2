using UnityEngine;
using UnityEngine.UI;

public class Notificaciones : MonoBehaviour
{
    [Header("Paneles y botones")]
    public Button BtnDatos;
    public GameObject PanelNotificaciones;
    public GameObject PanelDatos;

    void Start()
    {
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
    }
    private void AbrirPanelDatos()
    {
        PanelDatos.SetActive(true);
        PanelNotificaciones.SetActive(false);
    }
}
