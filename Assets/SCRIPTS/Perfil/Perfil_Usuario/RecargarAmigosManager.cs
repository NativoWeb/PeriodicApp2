using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class RecargarAmigosManager : MonoBehaviour
{
    // instanciamos script que hace la recarga de amigos
    [SerializeField] private ListarAmigosManager listaramigosmanager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnEnable()
    {
        if (listaramigosmanager != null)
        {
            // declaramos el script para poder tomar el m�todo de recarga
            listaramigosmanager = FindFirstObjectByType<ListarAmigosManager>();
        }

        if (listaramigosmanager != null)
        {
            Debug.Log("instancia correctaaaa ListarAmigosManager");

            listaramigosmanager.LimpiarPaneles();
            listaramigosmanager.CargarAmigos();
        }
        else
        {
            Debug.LogWarning("No se encontr� ListarAmigosManager al activar el panel.");
        }
    }

}
