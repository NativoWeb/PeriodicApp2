using UnityEngine;
using System.Collections.Generic;
using TMPro;
using static ControladorEncuesta;

public class EncuestaManager : MonoBehaviour
{
    public TMP_InputField inputTituloEncuesta;
    public Transform contenedorPreguntas;
    public GameObject preguntaPrefab;
    private List<PreguntaController> listaPreguntas = new List<PreguntaController>();

    private FirebaseManager firebaseManager;

    void Start()
    {
        firebaseManager = FindObjectOfType<FirebaseManager>();
    }

    public void AgregarPregunta()
    {
        GameObject nuevaPregunta = Instantiate(preguntaPrefab, contenedorPreguntas);
        PreguntaController controlador = nuevaPregunta.GetComponent<PreguntaController>();
        listaPreguntas.Add(controlador);
    }

    public void GuardarEncuesta()
    {
        string encuestaID = System.Guid.NewGuid().ToString();
        string titulo = inputTituloEncuesta.text;
        List<Pregunta> preguntas = new List<Pregunta>();

        foreach (PreguntaController preguntaController in listaPreguntas)
        {
            preguntas.Add(preguntaController.ObtenerPregunta());
        }

        firebaseManager.GuardarEncuesta(encuestaID, titulo, preguntas);
    }
}
