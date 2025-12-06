using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class cambiarescena : MonoBehaviour
{
    // Start is called before the first frame update
    public void siguienteEscena(string nombre)
    {
        // Usar SceneTransition para transición suave sin flash
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(nombre);
        }
        else
        {
            SceneManager.LoadScene(nombre);
        }
    }

    public void VuforiaDesdeInicio()
    {
        PlayerPrefs.SetString("CargarVuforia", "Inicio");
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("VuforiaNuevo");
        }
        else
        {
            SceneManager.LoadScene("VuforiaNuevo");
        }
    }

    public void VuforiaDesdeProfesor()
    {
        PlayerPrefs.SetString("CargarVuforia", "Profesor");
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("VuforiaNuevo");
        }
        else
        {
            SceneManager.LoadScene("VuforiaNuevo");
        }
    }

    public void volverEntrePerfiles()
    {
        string navegacionCuenta = PlayerPrefs.GetString("navegacionCuenta", "estudiante");

        string targetScene = (navegacionCuenta == "estudiante") ? "Perfil_Usuario" : "InicioProfesor1";

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    public void DevolverComunidades()
    {
        string Ocupacion = PlayerPrefs.GetString("TempOcupacion", "");
        string vuforia = PlayerPrefs.GetString("CargarVuforia", "");

        string targetScene = (Ocupacion == "Estudiante" || vuforia == "inicio") ? "Perfil_Usuario" : "InicioProfesor1";

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(targetScene);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }
}
