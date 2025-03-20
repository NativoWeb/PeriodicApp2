using UnityEngine;
using UnityEngine.UI;

public class BotonOcupacion : MonoBehaviour
{
    // Referencias p�blicas a los botones
    public Button btnEstudiante;
    public Button btnProfesor;


    void Start()
    {
        // Asignar m�todos a los botones
        btnEstudiante.onClick.AddListener(() => SeleccionarOcupacion("Estudiante"));
        btnProfesor.onClick.AddListener(() => SeleccionarOcupacion("Profesor"));
       
    }

    // M�todo que guarda la ocupaci�n
    void SeleccionarOcupacion(string ocupacion)
    {
        PlayerPrefs.SetString("TempOcupacion", ocupacion);
        PlayerPrefs.Save();
        Debug.Log("Ocupaci�n seleccionada: " + ocupacion);
    }
}
