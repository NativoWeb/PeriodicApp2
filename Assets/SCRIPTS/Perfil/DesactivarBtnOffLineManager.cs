using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DesactivarBtnOffLineManager : MonoBehaviour
{
    [Header("Configuraci�n de Botones")]
    [Tooltip("Activar para controlar botones por conexi�n")]
    [SerializeField] private bool desactivarBotonesSinInternet = true;

    [Header("Botones a Desactivar si no hay Wifi")]
    [Tooltip("Arrastra los 2 botones espec�ficos a controlar")]
    [SerializeField] private Button[] botonesControlados = new Button[2]; // Array fijo de 2 botones



    private void OnEnable()
    {
        if (!desactivarBotonesSinInternet || botonesControlados.Length != 2) return;

        bool hayInternet = VerificarConexionInternet();

        // Controlar solo los 2 botones asignados
        for (int i = 0; i < botonesControlados.Length; i++)
        {
            if (botonesControlados[i] != null)
            {
                botonesControlados[i].interactable = hayInternet;
            }
        }

      
    }

    // M�todo p�blico para forzar actualizaci�n
    public void ActualizarEstadoBotones()
    {
        OnEnable(); // Reutiliza la l�gica existente
    }

    private bool VerificarConexionInternet()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

 
 
}