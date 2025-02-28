using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class controllerinicio : MonoBehaviour
{
    [SerializeField] private GameObject m_blancoUI = null;
    [SerializeField] private GameObject m_estudiaropcionesUI = null;
    [SerializeField] private GameObject m_trabajaropcionesUI = null;

    // funcion ver login 
    public void showpanelblanco()
    {
        m_blancoUI.SetActive(true);
        m_estudiaropcionesUI.SetActive(false);
        m_trabajaropcionesUI.SetActive(false);
    }
    //funcion ver registro

    public void showestudiaropciones()
    {
        m_blancoUI.SetActive(true);
        m_estudiaropcionesUI.SetActive(true);
        m_trabajaropcionesUI.SetActive(false);
    }

    public void showtrabajaropcioines()
    {
        m_blancoUI.SetActive(true);
        m_estudiaropcionesUI.SetActive(false);
        m_trabajaropcionesUI.SetActive(true);
    }
}
