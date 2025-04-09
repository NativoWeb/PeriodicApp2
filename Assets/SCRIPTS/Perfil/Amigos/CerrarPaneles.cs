using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CerrarPaneles : MonoBehaviour
{

    // instaciamos los paneles que vamos a activar/desactivar
    [SerializeField] public GameObject m_PanelAmigosUI = null;
    [SerializeField] public GameObject m_PanelSolicitudesAmistadUI = null;
    [SerializeField] public GameObject m_PanelAgregarAmigosUI = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void VolverAmigos()
    {
        m_PanelSolicitudesAmistadUI.SetActive(true);
        m_PanelAmigosUI.SetActive(false);
        m_PanelAgregarAmigosUI.SetActive(false);
    }

    public void DesactivarPanelAgregarAmigos()
    {
        m_PanelAgregarAmigosUI.SetActive(false);
        m_PanelAmigosUI.SetActive(false);
        m_PanelSolicitudesAmistadUI.SetActive(false);
        
    }
    // Update is called once per frame
    
}
