using UnityEngine;
using UnityEngine.UI;

public class NavegacionComunidades : MonoBehaviour
{
    [SerializeField] public GameObject m_CrearComunidadUI = null;
    [SerializeField] public GameObject m_InicioComunidadesUI = null;
    [SerializeField] public GameObject m_panelMisComunidadesUI = null;
    [SerializeField] public GameObject m_panelEncuentraComunidadesUI = null;

    private ListaComunidadesManager _listaComunidadesManager;
    private MisComunidadesManager _misComunidadesManager;
    private CrearComunidad _crearComunidad;

    void Start()
    {
        _listaComunidadesManager = FindFirstObjectByType<ListaComunidadesManager>();
        _misComunidadesManager = FindFirstObjectByType<MisComunidadesManager>();
        _crearComunidad = FindFirstObjectByType<CrearComunidad>();
    }

    public void MostrarInicioComunidades()
    {
        PanelAnimator.Show(m_InicioComunidadesUI);
        _misComunidadesManager.CargarComunidadesDelUsuario();
        _crearComunidad.LimpiarFormulario();
        PanelAnimator.Hide(m_CrearComunidadUI);
    }

    public void MostrarMisComunidades()
    {
        PanelAnimator.Show(m_panelMisComunidadesUI);
        _misComunidadesManager.CargarComunidadesDelUsuario();
        PanelAnimator.Hide(m_CrearComunidadUI);
        PanelAnimator.Hide(m_panelEncuentraComunidadesUI);
    }

    public void MostrarCrearComunidad()
    {
        PanelAnimator.Show(m_CrearComunidadUI);
        PanelAnimator.Hide(m_InicioComunidadesUI);
    }

    public void MostrarEncuentraComunidades()
    {
        PanelAnimator.Show(m_panelEncuentraComunidadesUI);
        _listaComunidadesManager.CargarComunidades();
        PanelAnimator.Hide(m_CrearComunidadUI);
        PanelAnimator.Hide(m_panelMisComunidadesUI);
    }
}
