using UnityEngine;
using UnityEngine.SceneManagement;

public class RegistroFlowController : MonoBehaviour
{
    [Header("Paneles de UI")]
    public GameObject panelRegistro;
    public GameObject panelVerificacion;
    public GameObject panelCorreoInfo;

    [Header("Controladores")]
    public RegistroEmailController registroEmailController;
    public VerificacionCorreoController verificacionCorreoController;


    private void Start()
    {
        MostrarPanelRegistro();
    }

    public void MostrarPanelRegistro()
    {
        panelRegistro.SetActive(true);
        panelVerificacion.SetActive(false);
        panelCorreoInfo.SetActive(false);
    }

    public void MostrarPanelVerificacion(string codigoVerificacion)
    {
        panelRegistro.SetActive(false);
        panelVerificacion.SetActive(true);
        panelCorreoInfo.SetActive(true);

        verificacionCorreoController.SetCodigoEsperado(codigoVerificacion);
    }

    public void ReiniciarFlujo()
    {
        registroEmailController.ResetearFormulario();
        verificacionCorreoController.inputCodigoUsuario.text = "";
        MostrarPanelRegistro();
    }

    public void FinalizarRegistro()
    {
        Debug.Log("Registro completo. Cargando escena principal...");
        SceneManager.LoadScene("Registrar");
    }

}
