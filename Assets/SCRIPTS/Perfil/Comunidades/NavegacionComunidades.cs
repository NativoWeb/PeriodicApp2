using Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class NavegacionComunidades : MonoBehaviour
{
    //[SerializeField] public GameObject m_MisComunidadesUI = null;
    [SerializeField] public GameObject m_CrearComunidadUI = null;
    //[SerializeField] public GameObject m_ListaComunidadesUI = null;
    [SerializeField] public GameObject m_InicioComunidadesUI = null;
    [SerializeField] public GameObject m_panelMisComunidadesUI = null;
    [SerializeField] public GameObject m_panelEncuentraComunidadesUI = null;



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
        // recargamos el metodo de cargar cada vez que se activa el panel
        misComunidadesManager.CargarComunidadesDelUsuario();
        m_CrearComunidadUI.SetActive(false);
    }

    public void MostrarMisComunidades()
    {
        m_panelMisComunidadesUI.SetActive(true);
        // recargamos el metodo de cargar cada vez que se activa el panel
        misComunidadesManager.CargarComunidadesDelUsuario();
        m_CrearComunidadUI.SetActive(false);
        m_panelEncuentraComunidadesUI.SetActive(false);

    }
    public void MostrarCrearComunidad()
    {
        m_CrearComunidadUI.SetActive(true);
        m_InicioComunidadesUI.SetActive(false);
    }
   
    public void MostrarEncuentraComunidades()
    {
        m_panelEncuentraComunidadesUI.SetActive(true);
        // recargamos el metodo de cargar cada vez que se activa el panel 
        listaComunidadesManager.CargarComunidades();
        m_CrearComunidadUI.SetActive(false);
        m_panelMisComunidadesUI.SetActive(false);
    }
    


}
