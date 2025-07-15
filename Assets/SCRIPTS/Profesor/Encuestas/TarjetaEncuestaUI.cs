using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events; // Necesario para UnityAction

public class TarjetaEncuestaUI : MonoBehaviour
{
    // Arrastra aqu� los componentes desde el Inspector del Prefab
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoNumeroPreguntas;
    [SerializeField] private Button botonVerDetalles;

    /// <summary>
    /// Configura todos los elementos visuales de la tarjeta con los datos recibidos.
    /// </summary>
    /// <param name="titulo">El t�tulo de la encuesta.</param>
    /// <param name="numPreguntas">El n�mero de preguntas.</param>
    /// <param name="onBotonClick">La acci�n que debe ejecutarse cuando se hace clic en el bot�n.</param>
    public void Configurar(string titulo, int numPreguntas, UnityAction onBotonClick)
    {
        // Asignamos los textos
        this.textoTitulo.text = titulo;
        this.textoNumeroPreguntas.text = $"{numPreguntas} Preguntas";

        // Configuramos la acci�n del bot�n
        // 1. Limpiamos cualquier listener anterior para evitar llamadas m�ltiples
        this.botonVerDetalles.onClick.RemoveAllListeners();
        // 2. A�adimos la nueva acci�n que nos pasaron desde ListarEncuestas
        this.botonVerDetalles.onClick.AddListener(onBotonClick);
    }
}