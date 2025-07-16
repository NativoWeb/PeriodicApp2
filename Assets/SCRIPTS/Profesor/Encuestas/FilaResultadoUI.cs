using UnityEngine;
using TMPro;

public class FilaResultadoUI : MonoBehaviour
{
    // Asigna estos campos en el inspector del Prefab
    [SerializeField] private TextMeshProUGUI textoEstudiante;
    [SerializeField] private TextMeshProUGUI textoResultado;

    public void Configurar(string nombreEstudiante, string resultado)
    {
        textoEstudiante.text = nombreEstudiante;
        textoResultado.text = resultado;

        // Opcional: Cambiar el color según el resultado
        if (resultado.ToLower() == "aprobado")
        {
            textoResultado.color = Color.green;
        }
        else
        {
            textoResultado.color = Color.red;
        }
    }
}