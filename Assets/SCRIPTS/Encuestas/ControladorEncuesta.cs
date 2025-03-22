using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // �A�ADIDO: Necesario para UnityWebRequest!
using System.Linq;
using System;
using Newtonsoft.Json;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase;
using UnityEngine.SceneManagement;


public class ControladorEncuesta : MonoBehaviour
{
    private List<Pregunta> preguntas;
    private int preguntaActualIndex = 0;        // �ndice de la pregunta actual en la lista aleatoria
    private Pregunta preguntaActual;             // Para guardar la pregunta que se est� mostrando actualmente
    private List<string> opcionesAleatorias;
    private List<Pregunta> preguntasAleatorias;
    private bool eventosToggleHabilitados = false;
    public ToggleGroup grupoOpciones;


    public BarraProgreso barraProgreso;

    // Temporizador variables
    public Text TextTimer;  // Referencia al componente Text de la UI
    public Text txtRacha;
    public float tiempoRestante = 10f;  // Tiempo inicial del temporizador en segundos (10 segundos)
    private bool preguntaFinalizada = false;  // Flag para saber si la pregunta ha sido finalizada (cuando se pasa a la siguiente pregunta)

    // Variables para registrar el conteo de respuestas correctas por categor�a �A�ADIDO!
    private int correctasAlcalinos = 0;
    private int correctasMetalesAlcalinotérreos = 0;
    private int correctasLantanidos = 0;
    private int correctasActinoides = 0;
    private int correctasMetalesPostansicionales = 0;
    private int correctasMetaloides = 0;
    private int correctasTransicion = 0;
    private int correctasNoMetales = 0;
    private int correctasGasesNobles = 0;
    private int correctasPropiedadesDesconocidas = 0;
    private int incorrectasTotales = 0;
    private float dificultadTotalPreguntas = 0f; // Para calcular la dificultad media
    private int cantidadPreguntasRespondidas = 0; // Contador de preguntas respondidas

    private int racha = 0;
    private int respuestasCorrectas = 0;


    private FirebaseFirestore firestore;

    [System.Serializable]
    public class Pregunta
    {
        public string textoPregunta;
        public List<string> opcionesRespuesta;
        public int indiceRespuestaCorrecta;
        public string respuestaUsuario;
        public ControladorEncuesta.ElementoPreguntas.GrupoPreguntasData grupoPregunta; //  �RUTA CORRECTA para GrupoPreguntasData!
        public float dificultadPregunta; // �A�ADIDO: Para acceder a la dificultad!

    }


    [System.Serializable]
    public class ElementoPreguntas
    {
        public string elemento;
        public List<Pregunta> preguntas;

        [System.Serializable] // �Aseg�rate de que esta clase tambi�n sea serializable!
        public class GrupoPreguntasData
        {
            public string grupo; // �A�ADIDO: Para la categor�a de grupo!
        }
    }

    [System.Serializable]
    public class GrupoPreguntas
    {
        public string grupo;
        public List<ElementoPreguntas> elementos;
    }

    [System.Serializable]
    public class GrupoPreguntasWrapper
    {
        public List<GrupoPreguntas> gruposPreguntas;
    }

    void Start()
    {
        CargarPreguntasDesdeJSON();
        AleatorizarPreguntas();
        MostrarPreguntaActual();
        desmarcarToggle();
        ConfigurarToggleListeners(); // DEJA ESTA LLAMADA AL INICIO DE START

        ActualizarTextoTiempo();

        eventosToggleHabilitados = true;

        firestore = FirebaseFirestore.DefaultInstance;
        string userId = PlayerPrefs.GetString("userId", ""); // Obtener el ID del usuario
    }

    // M�todo para manejar el temporizador
    void Update()
    {
        ActualizarTextoTiempo();

        // Solo actualizar el temporizador si la pregunta no ha sido finalizada
        if (!preguntaFinalizada)
        {

            if (tiempoRestante > 0)
            {
                tiempoRestante -= Time.deltaTime; // Reduce el tiempo
            }
            else  // Verifica que la pregunta a�n no se ha respondido
            {
                preguntaFinalizada = true; // Evita que el c�digo se ejecute varias veces en un solo frame
                StartCoroutine(MostrarFeedbackYCambiarPregunta());
            }
        }

    }

    void ActualizarTextoTiempo()
    {
        TextTimer.text = tiempoRestante.ToString("00") + " Segundos";
    }

    // M�todo para cambiar a la siguiente pregunta
    void siguientePregunta()
    {
        Debug.Log($"siguientePregunta() llamado. preguntaActualIndex ANTES de incrementar: {preguntaActualIndex}");
        preguntaFinalizada = true;
        preguntaActualIndex++; // Incrementamos el �ndice para la siguiente pregunta

        Debug.Log($"siguientePregunta() preguntaActualIndex DESPUÉS de incrementar: {preguntaActualIndex}, preguntasAleatorias.Count: {preguntasAleatorias.Count}"); // DEBUG LOG


        if (preguntaActualIndex < preguntasAleatorias.Count)
        {
            //preguntaActualIndex++;
            MostrarPreguntaActual();  // Mostrar la siguiente pregunta
            ActivarInteractividadOpciones();
            tiempoRestante = 10f;  // Reiniciar el temporizador para la nueva pregunta
            Debug.Log($"siguientePregunta(): Temporizador reiniciado a {tiempoRestante} segundos.");
            preguntaFinalizada = false;  // Permitir que el temporizador funcione de nuevo
            eventosToggleHabilitados = true;
        }
        else
        {
            Debug.Log("siguientePregunta(): ¡Encuesta Finalizada! (No hay más preguntas).");
            Debug.Log("Encuesta Finalizada");
            textoPreguntaUI.text = "�Encuesta Finalizada!";
            grupoOpcionesUI.enabled = false;
            SceneManager.LoadScene("EncuestaAprendizaje");
            EnviarDatosAPrediccion(); // �A�ADIDO: Llamar a EnviarDatosAPrediccion al finalizar la encuesta!
        }
        Debug.Log("siguientePregunta() finalizado.");
    }

    void CargarPreguntasDesdeJSON()
    {
        TextAsset archivoJSON = Resources.Load<TextAsset>("preguntas_tabla_periodica_categorias1");
        if (archivoJSON != null)
        {
            string jsonString = archivoJSON.text;
            GrupoPreguntasWrapper wrapper = JsonUtility.FromJson<GrupoPreguntasWrapper>(jsonString); // Paso 1: Deserializar JSON

            if (wrapper != null && wrapper.gruposPreguntas != null) // Validar estructura del JSON
            {
                preguntas = new List<Pregunta>(); // Inicializar lista de preguntas
                System.Random rnd = new System.Random(); // Generador de números aleatorios

                foreach (var grupo in wrapper.gruposPreguntas) // Paso 2: Recorrer cada grupo
                {
                    List<Pregunta> preguntasGrupo = new List<Pregunta>();

                    // Paso 3: Recopilar todas las preguntas de los elementos del grupo
                    foreach (var elemento in grupo.elementos)
                    {
                        preguntasGrupo.AddRange(elemento.preguntas);
                    }

                    // Paso 4: Tomar 2 preguntas aleatorias del grupo
                    if (preguntasGrupo.Count >= 5)
                    {
                        List<Pregunta> preguntasAleatoriasGrupo = preguntasGrupo
                            .OrderBy(x => rnd.Next()) // Aleatorizar
                            .Take(5) // Tomar 2
                            .ToList();

                        preguntas.AddRange(preguntasAleatoriasGrupo); // Agregar a la lista global
                    }
                    else
                    {
                        preguntas.AddRange(preguntasGrupo); // Si hay menos de 2, agregar todas
                    }

                    // Paso 5: Detener si ya hay 10 preguntas
                    if (preguntas.Count >= 54)
                    {
                        preguntas = preguntas.Take(54).ToList();
                        break; // Salir del bucle
                    }
                }

                // Paso 6: Advertencia si no hay suficientes preguntas
                if (preguntas.Count < 10)
                {
                    Debug.LogWarning($"Solo se cargaron {preguntas.Count} preguntas.");
                }
            }
            else
            {
                Debug.LogError("El JSON no tiene la estructura esperada.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el archivo JSON.");
        }
    }


    // M�todo para aleatorizar el orden de las opciones de respuesta y asegurar que la correcta est� entre ellas
    List<string> AleatorizarOpciones(List<string> opciones, int indiceCorrecto)
    {
        List<string> opcionesAleatorias = new List<string>(opciones); // Copia de la lista original

        // Verificar que indiceCorrecto sea válido
        if (indiceCorrecto < 0 || indiceCorrecto >= opcionesAleatorias.Count)
        {
            Debug.LogError("Índice de respuesta correcta fuera de rango: " + indiceCorrecto + ". Se asignará el índice 0 por defecto.");
            indiceCorrecto = 0; // O manejar el error de otra forma
        }

        string respuestaCorrecta = opcionesAleatorias[indiceCorrecto]; // Guarda la respuesta correcta

        // Algoritmo de Fisher-Yates para aleatorizar la lista
        for (int i = 0; i < opcionesAleatorias.Count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, opcionesAleatorias.Count);
            string temp = opcionesAleatorias[randomIndex];
            opcionesAleatorias[randomIndex] = opcionesAleatorias[i];
            opcionesAleatorias[i] = temp;
        }

        // Asegurar que la respuesta correcta está presente en las opciones aleatorizadas
        if (!opcionesAleatorias.Contains(respuestaCorrecta))
        {
            opcionesAleatorias[0] = respuestaCorrecta;
        }
        return opcionesAleatorias;
    }


    void desmarcarToggle()
    {
        //Debug.Log("desmarcarToggle() llamado"); // AÑADIDO DEBUG LOG
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.isOn = false;
            toggle.interactable = true;
            //Debug.Log($"Toggle '{toggle.name}' isOn: {toggle.isOn}"); // AÑADIDO DEBUG LOG
        }
        //Debug.Log("desmarcarToggle() finalizado"); // AÑADIDO DEBUG LOG
    }


    void MostrarPreguntaActual()
    {
        Debug.Log($"MostrarPreguntaActual() llamado. preguntaActualIndex: {preguntaActualIndex}");

        // 1. Resetear estado de los Toggles
        grupoOpcionesUI.SetAllTogglesOff();
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.isOn = false;
            toggle.interactable = true;
            toggle.gameObject.SetActive(false); // Ocultar todos inicialmente
        }

        // 2. Validar preguntas disponibles
        if (preguntasAleatorias == null || preguntaActualIndex >= preguntasAleatorias.Count)
        {
            Debug.Log("¡Encuesta Finalizada!");
            textoPreguntaUI.text = "¡Encuesta Finalizada!";
            grupoOpcionesUI.enabled = false;
            SceneManager.LoadScene("EncuestaAprendizaje");
            return;
        }

        // 3. Obtener pregunta actual
        preguntaActual = preguntasAleatorias[preguntaActualIndex];
        textoPreguntaUI.text = preguntaActual.textoPregunta;

        // 4. Aleatorizar opciones
        opcionesAleatorias = AleatorizarOpciones(preguntaActual.opcionesRespuesta, preguntaActual.indiceRespuestaCorrecta);

        // 5. Actualizar índice correcto
        string respuestaCorrecta = preguntaActual.opcionesRespuesta[preguntaActual.indiceRespuestaCorrecta];
        preguntaActual.indiceRespuestaCorrecta = opcionesAleatorias.IndexOf(respuestaCorrecta);

        // 6. Configurar Toggles visibles
        for (int i = 0; i < opcionesAleatorias.Count; i++)
        {
            if (i < opcionesToggleUI.Length)
            {
                opcionesToggleUI[i].gameObject.SetActive(true);
                opcionesToggleUI[i].GetComponentInChildren<TextMeshProUGUI>().text = opcionesAleatorias[i];
                opcionesToggleUI[i].isOn = false;
                opcionesToggleUI[i].image.color = colorNormal;
            }
        }

        // 7. Reiniciar temporizado
        preguntaFinalizada = false;
        eventosToggleHabilitados = true;

        barraProgreso.InicializarBarra(preguntas.Count);
    }


    void AleatorizarPreguntas()
    {
        if (preguntas != null && preguntas.Count > 0)
        {
            preguntasAleatorias = new List<Pregunta>(preguntas); // Crea una copia de la lista original
            // Usar algoritmo de Fisher-Yates para aleatorizar la lista copiada 'preguntasAleatorias'
            for (int i = 0; i < preguntasAleatorias.Count - 1; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, preguntasAleatorias.Count);
                Pregunta temp = preguntasAleatorias[randomIndex];
                preguntasAleatorias[randomIndex] = preguntasAleatorias[i];
                preguntasAleatorias[i] = temp;
            }
            preguntaActualIndex = 0; // Reinicia el �ndice de la pregunta actual al empezar la encuesta aleatorizada
        }
        else
        {
            Debug.LogWarning("No hay preguntas para aleatorizar o la lista de preguntas es nula.");
        }
    }

    private void ConfigurarToggleListeners()
    {
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.onValueChanged.RemoveAllListeners(); // Limpiar listeners previos
        }

        for (int i = 0; i < opcionesToggleUI.Length; i++)
        {
            int index = i;
            opcionesToggleUI[index].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn && eventosToggleHabilitados)
                {
                    VerificarRespuesta(index);
                }
            });
        }
    }

    // M�todo para verificar la respuesta seleccionada por el usuario
    public void VerificarRespuesta(int indiceOpcionSeleccionada)
    {

        if (!eventosToggleHabilitados) return;

        eventosToggleHabilitados = false;

        if (preguntaActual == null)
        {
            Debug.LogError("preguntaActual es null");
            return;
        }

        // Guardar la respuesta seleccionada
        preguntaActual.respuestaUsuario = opcionesAleatorias[indiceOpcionSeleccionada];

        Debug.Log($"Respuesta seleccionada: {preguntaActual.respuestaUsuario} | Respuesta correcta: {preguntaActual.opcionesRespuesta[preguntaActual.indiceRespuestaCorrecta]}");


        // Resetear colores de todas las opciones
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.image.color = colorNormal;
        }

        // Cambiar color de la opción seleccionada
        if (indiceOpcionSeleccionada == preguntaActual.indiceRespuestaCorrecta)
        {
            opcionesToggleUI[indiceOpcionSeleccionada].image.color = colorCorrecto;
        }
        else
        {
            // Color para respuesta incorrecta
            opcionesToggleUI[indiceOpcionSeleccionada].image.color = colorIncorrecto;

            // Resaltar también la respuesta correcta
            opcionesToggleUI[preguntaActual.indiceRespuestaCorrecta].image.color = colorCorrecto;
        }

        Debug.Log($"Verificando respuesta. �ndice seleccionado: {indiceOpcionSeleccionada}, �ndice correcto: {preguntaActual.indiceRespuestaCorrecta}");
        // Desactivar la interactividad de las opciones y el bot�n "Siguiente Pregunta" una vez que se responde
        DesactivarInteractividadOpciones();

        if (indiceOpcionSeleccionada == preguntaActual.indiceRespuestaCorrecta)
        {
            // �Respuesta CORRECTA!
            Debug.Log("�Respuesta Correcta!");
            if (indiceOpcionSeleccionada == preguntaActual.indiceRespuestaCorrecta)
            {
                racha++;
                respuestasCorrectas++;
            }
            txtRacha.text = "" + racha;
            // Determinar la categor�a de la pregunta actual (asumiendo que tienes una propiedad 'grupo' en tu objeto PreguntaConocimiento) �A�ADIDO!
            string categoriaPregunta = preguntaActual.grupoPregunta.grupo; // Ajusta esto seg�n la estructura de tu objeto PreguntaConocimiento

            // Incrementar el contador de respuestas correctas seg�n la categor�a �A�ADIDO!
            switch (categoriaPregunta)
            {
                case "Metales Alcalinos (Grupo 1)":
                    correctasAlcalinos++;
                    break;
                case "Metales Alcalinotérreos":
                    correctasMetalesAlcalinotérreos++;
                    break;
                case "Metales de Transición":
                    correctasTransicion++;
                    break;
                case "Lantánidos":
                    correctasLantanidos++;
                    break;
                case "Actinoides":
                    correctasActinoides++;
                    break;
                case "Metaloides": // Ajusta este nombre si es diferente en tus datos
                    correctasMetaloides++;
                    break;
                case "Metales postransicionales": // Ajusta este nombre si es diferente en tus datos
                    correctasMetalesPostansicionales++;
                    break;
                case "No Metales": // Ajusta este nombre si es diferente en tus datos
                    correctasNoMetales++;
                    break;
                case "Propiedades desconocidas":
                    correctasPropiedadesDesconocidas++;
                    break;
                case "Gases Nobles": 
                    correctasGasesNobles++;
                    break;
                default:
                    Debug.LogWarning($"Categor�a de pregunta no reconocida: {categoriaPregunta}. Ajusta el switch en VerificarRespuesta.");
                    break;
            }
            // **IMPORTANTE:** Aseg�rate de que los nombres de las categor�as en este `switch` coincidan EXACTAMENTE con los nombres que usas en tus datos de preguntas JSON.

        }
        else
        {
            // �Respuesta INCORRECTA!
            Debug.Log("Respuesta Incorrecta");
            racha = 0;
            txtRacha.text = "" + racha;
            incorrectasTotales++; // �A�ADIDO: Incrementar contador de incorrectas!
        }

        // Registrar la dificultad de la pregunta actual (asumiendo que tienes una propiedad 'dificultadPregunta' en tu objeto PreguntaConocimiento) �A�ADIDO!
        float dificultadPregunta = preguntaActual.dificultadPregunta; // Ajusta esto seg�n la estructura de tu objeto PreguntaConocimiento
        dificultadTotalPreguntas += dificultadPregunta;
        cantidadPreguntasRespondidas++;


        // Preparar para la siguiente pregunta (puedes decidir cu�ndo avanzar a la siguiente pregunta, por ejemplo, con un bot�n)
        //preguntaActualIndex++; // Incrementar el �ndice para la siguiente pregunta
        StartCoroutine(MostrarFeedbackYCambiarPregunta());

    }

    IEnumerator MostrarFeedbackYCambiarPregunta()
    {
        tiempoRestante = 2f;
        yield return new WaitForSeconds(1.5f); // Tiempo para ver feedback
        siguientePregunta();
    }
    // M�todo para reactivar la interactividad de las opciones (Toggles) para la siguiente pregunta
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

    // Funci�n para calcular el porcentaje de aciertos �A�ADIDO!
    private float CalcularPorcentajeAciertos()
    {
        int totalCorrectas = correctasAlcalinos + correctasMetalesAlcalinotérreos + correctasLantanidos + correctasActinoides + correctasGasesNobles 
            + correctasMetaloides + correctasMetalesPostansicionales +  correctasTransicion + correctasNoMetales + correctasPropiedadesDesconocidas;
        int totalRespuestas = totalCorrectas + incorrectasTotales;
        if (totalRespuestas == 0) return 0f; // Evitar divisi�n por cero
        return (float)totalCorrectas / totalRespuestas * 100f;
    }

    // Funci�n para calcular la dificultad media �A�ADIDO!
    private float CalcularDificultadMedia()
    {
        if (cantidadPreguntasRespondidas == 0) return 0f; // Evitar divisi�n por cero
        return dificultadTotalPreguntas / cantidadPreguntasRespondidas;
    }

 
    // Coroutine para enviar la solicitud y procesar la respuesta de la API �A�ADIDO!
    private void EnviarDatosAPrediccion()
    {
        // 1. Recopilar los valores de las características
        float porcentajeAciertos = CalcularPorcentajeAciertos();
        float dificultadMedia = CalcularDificultadMedia();

        // Las características deben coincidir con el orden y cantidad usado durante el entrenamiento.
        float[] features = new float[] {
        correctasAlcalinos,
        correctasMetalesAlcalinotérreos,
        correctasTransicion,
        correctasLantanidos,
        correctasActinoides,
        correctasMetaloides,
        correctasMetalesPostansicionales,
        correctasNoMetales,
        correctasPropiedadesDesconocidas,
        correctasGasesNobles,
        incorrectasTotales,
        //porcentajeAciertos,
        dificultadMedia
    };

        // 2. Llamar al modelo de Barracuda
        ModeloAI modeloAI = GetComponent<ModeloAI>(); // Asumiendo que ModeloAI está en el mismo GameObject
        if (modeloAI != null)
        {
            float[] predictionResult = modeloAI.RunInference(features);
            // Por ejemplo, si el modelo devuelve un valor entre 0 y 1, puedes usar un umbral de 0.5
            int prediction = (predictionResult[0] > 0.5f) ? 1 : 0;
            Debug.Log("Predicción de Barracuda: " + prediction);
            ProcesarPrediccionDeConocimiento(predictionResult);
        }
        else
        {
            Debug.LogError("No se encontró el componente ModeloAI");
        }
    }


    // Funci�n para procesar la predicci�n de conocimiento (0 o 1) recibida de la API �A�ADIDO!
    public void ProcesarPrediccionDeConocimiento(float[] predictions)
    {
        // Definir un umbral para considerar que el usuario conoce la categoría
        float umbral = 0.5f;

        for (int i = 0; i < predictions.Length; i++)
        {
            // Convertir el valor a porcentaje
            float porcentaje = predictions[i] * 100f;
            if (predictions[i] > umbral)
            {
                Debug.Log($"El usuario CONOCE el concepto del Grupo {i + 1} ({porcentaje:F2}%).");
            }
            else
            {
                Debug.Log($"El usuario NO CONOCE el concepto del Grupo {i + 1} ({porcentaje:F2}%).");
            }
        }
    }



    [Header("Referencias UI")]
    public TextMeshProUGUI textoPreguntaUI;
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;

    [Header("Colores de Respuesta")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    public Color colorNormal = Color.white; // Color por defecto

}