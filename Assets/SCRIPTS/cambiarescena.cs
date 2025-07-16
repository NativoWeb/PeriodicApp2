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

    public void volverEntrePerfiles()
    {
        string navegacionCuenta = PlayerPrefs.GetString("navegacionCuenta", "estudiante");

        if (navegacionCuenta == "estudiante")
        {
            SceneManager.LoadScene("Perfil_Usuario");
        }
        else
        {
            SceneManager.LoadScene("InicioProfesor1");
        }
    }

    public void DevolverComunidades()
    {
        string Ocupacion = PlayerPrefs.GetString("TempOcupacion", "");
        string vuforia = PlayerPrefs.GetString("CargarVuforia", "");
        if(Ocupacion == "Estudiante" || vuforia == "inicio")
        {
            SceneManager.LoadScene("Perfil_Usuario");
        }
        else
        {
            SceneManager.LoadScene("InicioProfesor1");
        }
    }
}
