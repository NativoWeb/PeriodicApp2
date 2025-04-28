using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class NavegacionComunidades : MonoBehaviour
{
    [SerializeField] public GameObject m_MisComunidadesUI = null;
    [SerializeField] public GameObject m_CrearComunidadUI = null;
    [SerializeField] public GameObject m_ListaComunidadesUI = null;
    [SerializeField] public GameObject m_InvitacionesUI = null;
    [SerializeField] public GameObject m_InicioComunidadesUI = null;


    // declaracion de intancias de script para llamar metodos
    private ListaComunidadesManager listaComunidadesManager;
    private MisComunidadesManager misComunidadesManager;
    void Start()
    {
        listaComunidadesManager = FindFirstObjectByType<ListaComunidadesManager>();
        misComunidadesManager = FindFirstObjectByType<MisComunidadesManager>();
    }
    public void MostrarInicioComunidades()
    {
        m_InicioComunidadesUI.SetActive(true);
        m_MisComunidadesUI.SetActive(false);
        m_CrearComunidadUI.SetActive(false);
        m_ListaComunidadesUI.SetActive(false);
        m_InvitacionesUI.SetActive(false);
    }

    public void MostrarMisComunidades()
    {
        m_MisComunidadesUI.SetActive(true);
        // recargamos el metodo de cargar cada vez que se activa el panel
        misComunidadesManager.CargarComunidadesDelUsuario();

        m_InicioComunidadesUI.SetActive(false);
        m_CrearComunidadUI.SetActive(false);
        m_ListaComunidadesUI.SetActive(false);
        m_InvitacionesUI.SetActive(false);
    }
    public void MostrarCrearComunidad()
    {

        m_CrearComunidadUI.SetActive(true);
        m_InicioComunidadesUI.SetActive(false);
        m_MisComunidadesUI.SetActive(false);
        m_ListaComunidadesUI.SetActive(false);
        m_InvitacionesUI.SetActive(false);
    }
   
    public void MostrarListaComunidades()
    {
        m_ListaComunidadesUI.SetActive(true);
        // recargamos el metodo de cargar cada vez que se activa el panel 
        listaComunidadesManager.CargarComunidadesDelUsuario(); 

        m_InicioComunidadesUI.SetActive(false);
        m_MisComunidadesUI.SetActive(false);
        m_InvitacionesUI.SetActive(false);
        m_CrearComunidadUI.SetActive(false);
    }
    public void MostrarInvitaciones()
    {
        m_InvitacionesUI.SetActive(true);
        m_InicioComunidadesUI.SetActive(false);
        m_ListaComunidadesUI.SetActive(false);
        m_MisComunidadesUI.SetActive(false);
        m_CrearComunidadUI.SetActive(false);

    }


}
