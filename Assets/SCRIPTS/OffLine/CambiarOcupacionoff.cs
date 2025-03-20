using UnityEngine;
using UnityEngine.UI;

public class BotonOcupacion : MonoBehaviour
{
    // Referencias públicas a los botones
    public Button btnEstudiante;
    public Button btnProfesor;


    void Start()
    {
        // Asignar métodos a los botones
        btnEstudiante.onClick.AddListener(() => SeleccionarOcupacion("Estudiante"));
        btnProfesor.onClick.AddListener(() => SeleccionarOcupacion("Profesor"));
       
    }

    // Método que guarda la ocupación
    void SeleccionarOcupacion(string ocupacion)
    {
        PlayerPrefs.SetString("TempOcupacion", ocupacion);
        PlayerPrefs.Save();
        Debug.Log("Ocupación seleccionada: " + ocupacion);
    }
}
