using UnityEngine;
using UnityEngine.UI;

public class IA : MonoBehaviour
{
    [Header("Paneles y botones")]
    public Button BtnDatos;
    public GameObject PanelDatos;
    public GameObject PanelIA;

    void Start()
    {
        BtnDatos.onClick.AddListener(AbrirPanelDatos);
    }
    private void AbrirPanelDatos()
    {
        PanelDatos.SetActive(true);
        PanelIA.SetActive(false);
    }
}
