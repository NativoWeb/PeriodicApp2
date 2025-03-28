using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Importante para trabajar con botones
public class VerificarEncuestas : MonoBehaviour
{
    public Button botonAprendizaje; 
    public Button botonConocimiento;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VerificarEncuestass();
    }

    private void VerificarEncuestass()
    {
        bool estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
        bool estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;

        if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
        {
                SceneManager.LoadScene("Categorías");
        }


        // Desactivar botones si la encuesta ya fue realizada
        botonAprendizaje.interactable = !estadoencuestaaprendizaje;
        botonConocimiento.interactable = !estadoencuestaconocimiento;
        if (botonAprendizaje.interactable ==false)
        {
            botonAprendizaje.GetComponentInChildren <TMP_Text>().color = Color.white;
        }
        else if (botonConocimiento.interactable == false)
        {
            botonConocimiento.GetComponentInChildren<TMP_Text>().color = Color.white;
        }


    }

}
