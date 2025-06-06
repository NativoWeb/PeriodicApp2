using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class RecargarAmigosManager : MonoBehaviour
{
    // instanciamos script que hace la recarga de amigos
    private ListarAmigosManager listaramigosmanager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // declaramos el script para poder tomar el método de recarga
        listaramigosmanager = FindFirstObjectByType<ListarAmigosManager>();
    }

    private void OnEnable()
    {
        listaramigosmanager.LimpiarPaneles();
        listaramigosmanager.CargarAmigos();
    }

}
