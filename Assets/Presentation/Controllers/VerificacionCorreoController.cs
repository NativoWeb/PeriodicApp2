using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VerificacionCorreoController : MonoBehaviour
{
    [Header("Referencias")]
    public TMP_InputField inputCodigoUsuario;
    public TMP_Text mensajeFeedback;
    public GameObject panelVerificacion;
    public GameObject panelError;
    public TMP_Text mensajeError;
    public GameObject panelSinInternet;

    [Header("Botones")]
    public Button btnVerificar;
    public Button btnCerrarError;

    private string codigoEsperado;
    private VerificarCodigoVerificacion useCase;
    public RegistroFlowController flowController;


    void Start()
    {
        useCase = new VerificarCodigoVerificacion();
        btnVerificar.onClick.AddListener(VerificarCodigo);
        btnCerrarError.onClick.AddListener(() => panelError.SetActive(false));
        panelError.SetActive(false);
    }

    public void SetCodigoEsperado(string codigo)
    {
        codigoEsperado = codigo;
    }

    private void VerificarCodigo()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            panelSinInternet.SetActive(true);
            return;
        }

        string codigoIngresado = inputCodigoUsuario.text.Trim();

        if (string.IsNullOrEmpty(codigoIngresado))
        {
            MostrarError("Debes ingresar el código de verificación.");
            return;
        }

        if (useCase.Ejecutar(codigoIngresado, codigoEsperado))
        {
            Debug.Log("Código correcto. Continuando...");
            flowController.FinalizarRegistro();

        }
        else
        {
            MostrarError("Código incorrecto. Intenta nuevamente.");
        }
    }

    private void MostrarError(string mensaje)
    {
        panelError.SetActive(true);
        mensajeError.text = mensaje;
        mensajeError.color = Color.red;
    }
}
