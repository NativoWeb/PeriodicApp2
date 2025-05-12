using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class cambiarescena : MonoBehaviour
{
    // Start is called before the first frame update
    public void siguienteEscena(string nombre)
    {
        SceneManager.LoadScene(nombre);
    }

    public void VuforiaDesdeInicio()
    {
        PlayerPrefs.SetString("CargarVuforia", "Inicio");
        SceneManager.LoadScene("VuforiaNuevo");
    }

    public void VuforiaDesdeProfesor()
    {
        PlayerPrefs.SetString("CargarVuforia", "Profesor");
        SceneManager.LoadScene("VuforiaNuevo");
    }

    public void DevolverComunidades()
    {
        string Ocupacion = PlayerPrefs.GetString("TempOcupacion", "");
        if(Ocupacion == "Estudiante")
        {
            SceneManager.LoadScene("Perfil_Usuario");
        }
        else
        {
            SceneManager.LoadScene("InicioProfesor");
        }
    }
}
