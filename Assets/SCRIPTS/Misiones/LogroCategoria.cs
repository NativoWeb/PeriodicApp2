using UnityEngine;
using TMPro;

public class LogroCategoria : MonoBehaviour
{
    public TMP_Text tituloMisionFinal; // TMP para el título
    public GameObject imagenCompletada; // Imagen que se activa al completar

    // Función para actualizar el estado del logro
    public void ActualizarLogro(string titulo, bool completado)
    {
        tituloMisionFinal.text = titulo;
        imagenCompletada.SetActive(completado); // Activar la imagen si está completado
    }
}
