using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpcionUI : MonoBehaviour
{
    public TMP_InputField inputOpcion;  // Campo para el texto de la opción
    public Toggle toggleCorrecta;      // Toggle para marcar como correcta

    public string ObtenerTextoOpcion()
    {
        return inputOpcion.text;
    }

    public bool EsCorrecta()
    {
        return toggleCorrecta.isOn;
    }
}
