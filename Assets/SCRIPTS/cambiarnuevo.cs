using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class cambiarnuevo : MonoBehaviour
{

    [SerializeField] private GameObject m_loginUI = null;
    [SerializeField] private GameObject m_registroUI = null;
    [SerializeField] private GameObject m_errorUI = null;


    // funcion ver login 
    public void showlogin()
    {
        m_loginUI.SetActive(true);
        m_registroUI.SetActive(false);
    }
    //funcion ver registro
    public void showregistro()
    {
        m_registroUI.SetActive(true);
        m_loginUI.SetActive(false);

    }

}
