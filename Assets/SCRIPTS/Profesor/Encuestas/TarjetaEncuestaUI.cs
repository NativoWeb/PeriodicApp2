using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events; // Necesario para UnityAction

public class TarjetaEncuestaUI : MonoBehaviour
{
    // Arrastra aquí los componentes desde el Inspector del Prefab
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoNumeroPreguntas;
    [SerializeField] private Button botonVerDetalles;

    /// <summary>
    /// Configura todos los elementos visuales de la tarjeta con los datos recibidos.
    /// </summary>
    /// <param name="titulo">El título de la encuesta.</param>
    /// <param name="numPreguntas">El número de preguntas.</param>
    /// <param name="onBotonClick">La acción que debe ejecutarse cuando se hace clic en el botón.</param>
    public void Configurar(string titulo, int numPreguntas, UnityAction onBotonClick)
    {
        // Asignamos los textos
        this.textoTitulo.text = titulo;
        this.textoNumeroPreguntas.text = $"{numPreguntas} Preguntas";

        // Configuramos la acción del botón
        // 1. Limpiamos cualquier listener anterior para evitar llamadas múltiples
        this.botonVerDetalles.onClick.RemoveAllListeners();
        // 2. Añadimos la nueva acción que nos pasaron desde ListarEncuestas
        this.botonVerDetalles.onClick.AddListener(onBotonClick);
    }
}