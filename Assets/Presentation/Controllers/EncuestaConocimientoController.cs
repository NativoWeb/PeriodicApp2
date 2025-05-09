using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EncuestaConocimientoController : MonoBehaviour
{
    public TextMeshProUGUI textoPregunta;
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;
    public Text txtRacha;
    public Text txtTimer;

    [Header("Feedback UI")]
    public GameObject panelFeedback;
    public TextMeshProUGUI textoFeedback;
    public Color colorFondoCorrecto = new Color(0.66f, 0.81f, 0.30f); // Verde claro
    public Color colorFondoIncorrecto = new Color(0.89f, 0.31f, 0.31f); // Rojo claro


    [Header("Colores de Respuesta")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    public Color colorNormal = Color.white; // Color por defecto

    private List<PreguntaEntity> preguntas;
    private int indiceActual = 0;
    private float tiempoRestante = 10f;
    private bool preguntaRespondida = false;
    private int racha = 0;

    private async void Start()
    {
        panelFeedback.SetActive(false);
        var useCase = new ObtenerPreguntasEncuestaUseCase(new EncuestaConocimientoFirebase());
        preguntas = await useCase.EjecutarAsync();
        MostrarPregunta();
    }

    void Update()
    {
        if (preguntaRespondida) return;

        tiempoRestante -= Time.deltaTime;
        txtTimer.text = $"{(int)tiempoRestante} Segundos";

        if (tiempoRestante <= 0)
        {
            preguntaRespondida = true;
            MostrarResultado(false);
        }
    }

    private void MostrarPregunta()
    {
        if (indiceActual >= preguntas.Count)
        {
            FinalizarEncuesta();
            return;
        }

        var pregunta = preguntas[indiceActual];
        textoPregunta.text = pregunta.Texto;
        var opciones = pregunta.Opciones;


        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            if (i < opciones.Count)
            {
                opcionesToggleUI[i].gameObject.SetActive(true);
                opcionesToggleUI[i].GetComponentInChildren<TextMeshProUGUI>().text = opciones[i];
                opcionesToggleUI[i].isOn = false;
                opcionesToggleUI[i].image.color = colorNormal; 
            }
            else
            {
                opcionesToggleUI[i].gameObject.SetActive(false);
            }
        }
        ActivarInteractividadOpciones();
        preguntaRespondida = false;
        tiempoRestante = 10f;
    }

    void ActivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = true; // Reactiva la interactividad de cada Toggle de opci�n
        }
    }

    void DesactivarInteractividadOpciones()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.interactable = false; // Desactiva la interactividad de cada Toggle de opci�n
        }
    }

    public void OnRespuestaSeleccionada(int indice)
    {
        if (preguntaRespondida) return;

        var esCorrecta = indice == preguntas[indiceActual].IndiceCorrecto;
        preguntaRespondida = true;
        DesactivarInteractividadOpciones();
        MostrarResultado(esCorrecta);
    }

    private void MostrarResultado(bool correcta)
    {
        

        if (correcta)
        {
            racha++;
            txtRacha.text = racha.ToString();
        }
        else
        {
            racha = 0;
            txtRacha.text = "0";
        }

        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            if (!opcionesToggleUI[i].gameObject.activeSelf) continue;

            if (i == preguntas[indiceActual].IndiceCorrecto)
                opcionesToggleUI[i].image.color = colorCorrecto;
            else if (opcionesToggleUI[i].isOn)
                opcionesToggleUI[i].image.color = colorIncorrecto;
            else
                opcionesToggleUI[i].image.color = colorNormal;
        }

        // Mostrar panel de feedback
        panelFeedback.SetActive(true);
        textoFeedback.text = correcta ? "Correcto" : "Incorrecto";
        panelFeedback.GetComponent<Image>().color = correcta ? colorFondoCorrecto : colorFondoIncorrecto;

        Invoke(nameof(OcultarFeedbackYContinuar), 1.5f); // Separa esto para ocultar feedback
    }

    private void OcultarFeedbackYContinuar()
    {
        panelFeedback.SetActive(false);
        indiceActual++;
        MostrarPregunta();
    }

    private void FinalizarEncuesta()
    {
        Debug.Log("Encuesta finalizada");
        // Guardar estado y navegar
    }
}
