using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarraProgreso : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI progresoTexto; // Para mostrar "Pregunta X / Total"

    private int totalPreguntas = 1;
    private int preguntaActual = 0;

    public void InicializarBarra(int total)
    {
        totalPreguntas = total;
        slider.maxValue = totalPreguntas;
        slider.value = 0;
        AvanzarPregunta();
    }

    public void AvanzarPregunta()
    {
        if (preguntaActual < totalPreguntas)
        {
            preguntaActual++;
            slider.value = preguntaActual;
        }
    }
}
