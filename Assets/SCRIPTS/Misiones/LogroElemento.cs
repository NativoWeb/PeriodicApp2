using UnityEngine;
using TMPro;

public class LogroElemento : MonoBehaviour
{
    public TMP_Text nombreElemento; // TMP para el nombre del elemento
    public TMP_Text logroElemento; // TMP para el logro del elemento
    public GameObject imagenCompletada; // Imagen que se activa cuando se completan todas las misiones

    // Función para actualizar el estado del logro
    public void ActualizarLogro(string nombre, string logro, bool completado)
    {
        nombreElemento.text = nombre;
        logroElemento.text = logro;
        imagenCompletada.SetActive(completado); // Activar la imagen si está completado
    }
}
