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
    public ToggleGroup grupoOpciones    ;

    // Temporizador variables
    public Text TextTimer;  // Referencia al componente Text de la UI
    public float tiempoRestante = 10f;  // Tiempo inicial del temporizador en segundos (10 segundos)
    private bool preguntaFinalizada = false;  // Flag para saber si la pregunta ha sido finalizada (cuando se pasa a la siguiente pregunta)

    // Variables para registrar el conteo de respuestas correctas por categor�a �A�ADIDO!
    private int correctasAlcalinos = 0;
    private int correctasGasesNobles = 0;
    private int correctasMetaloides = 0;
    private int correctasTransicion = 0;
    private int correctasNoMetales = 0;
    private int incorrectasTotales = 0;
    private float dificultadTotalPreguntas = 0f; // Para calcular la dificultad media
    private int cantidadPreguntasRespondidas = 0; // Contador de preguntas respondidas


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
                StartCoroutine(MostrarMismaPreguntaPor5Segundos());
            }
        }

    }

    // Corutina para mostrar la misma pregunta por 5 segundos
    IEnumerator MostrarMismaPreguntaPor5Segundos()
    {
        preguntaFinalizada = true;
        tiempoRestante = 2f;
        ActualizarTextoTiempo();
        // Mostrar la misma pregunta y desactivar toggles
        //textoPreguntaUI.text = preguntaActual.textoPregunta;
        DesactivarInteractividadOpciones();

        // Desactivar el temporizador por 5 segundos
        yield return new WaitForSeconds(2f);

        // Pasar a la siguiente pregunta  activar toggles
        //ActivarInteractividadOpciones();
        tiempoRestante = 10f;
        siguientePregunta();
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
            //EnviarDatosAPrediccion(); // �A�ADIDO: Llamar a EnviarDatosAPrediccion al finalizar la encuesta!
        }
        Debug.Log("siguientePregunta() finalizado.");
    }

    void CargarPreguntasDesdeJSON()
    {
        TextAsset archivoJSON = Resources.Load<TextAsset>("preguntas_tabla_periodica");
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
                    if (preguntasGrupo.Count >= 2)
                    {
                        List<Pregunta> preguntasAleatoriasGrupo = preguntasGrupo
                            .OrderBy(x => rnd.Next()) // Aleatorizar
                            .Take(2) // Tomar 2
                            .ToList();

                        preguntas.AddRange(preguntasAleatoriasGrupo); // Agregar a la lista global
                    }
                    else
                    {
                        preguntas.AddRange(preguntasGrupo); // Si hay menos de 2, agregar todas
                    }

                    // Paso 5: Detener si ya hay 10 preguntas
                    if (preguntas.Count >= 10)
                    {
                        preguntas = preguntas.Take(10).ToList();
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
        List<string> opcionesAleatorias = new List<string>(opciones); // Copia las opciones originales
        string respuestaCorrecta = opcionesAleatorias[indiceCorrecto]; // Guarda la respuesta correcta

        // Algoritmo de Fisher-Yates para aleatorizar la lista
        for (int i = 0; i < opcionesAleatorias.Count - 1; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, opcionesAleatorias.Count);
            string temp = opcionesAleatorias[randomIndex];
            opcionesAleatorias[randomIndex] = opcionesAleatorias[i];
            opcionesAleatorias[i] = temp;
        }

        // Asegurar que la respuesta correcta est� siempre presente en las opciones aleatorizadas (esto es importante!)
        if (!opcionesAleatorias.Contains(respuestaCorrecta))
        {
            opcionesAleatorias[0] = respuestaCorrecta; // Si por alguna raz�n no est�, la coloca en la primera posici�n (puedes cambiar la posici�n si quieres)
        }
        return opcionesAleatorias;
    }

    void desmarcarToggle()
    {
        Debug.Log("desmarcarToggle() llamado"); // AÑADIDO DEBUG LOG
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.isOn = false;
            toggle.interactable = true;
            Debug.Log($"Toggle '{toggle.name}' isOn: {toggle.isOn}"); // AÑADIDO DEBUG LOG
        }
        Debug.Log("desmarcarToggle() finalizado"); // AÑADIDO DEBUG LOG
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

        // 7. Reiniciar temporizador
        tiempoRestante = 10f;
        preguntaFinalizada = false;
        eventosToggleHabilitados = true;

        Debug.Log("Pregunta mostrada correctamente");

        // Actualizar texto del contador
        NumeroPreguntas.text = $"Pregunta {preguntaActualIndex + 1} de {preguntasAleatorias.Count}";
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

            // Determinar la categor�a de la pregunta actual (asumiendo que tienes una propiedad 'grupo' en tu objeto PreguntaConocimiento) �A�ADIDO!
            string categoriaPregunta = preguntaActual.grupoPregunta.grupo; // Ajusta esto seg�n la estructura de tu objeto PreguntaConocimiento

            // Incrementar el contador de respuestas correctas seg�n la categor�a �A�ADIDO!
            switch (categoriaPregunta)
            {
                case "Metales Alcalinos (Grupo 1)":
                    correctasAlcalinos++;
                    break;
                case "Gases Nobles (Grupo 18)":
                    correctasGasesNobles++;
                    break;
                case "Metaloides": // Ajusta este nombre si es diferente en tus datos
                    correctasMetaloides++;
                    break;
                case "Metales de Transici�n": // Ajusta este nombre si es diferente en tus datos
                    correctasTransicion++;
                    break;
                case "No Metales": // Ajusta este nombre si es diferente en tus datos
                    correctasNoMetales++;
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
        yield return new WaitForSeconds(2f); // Tiempo para ver feedback
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
        int totalCorrectas = correctasAlcalinos + correctasGasesNobles + correctasMetaloides + correctasTransicion + correctasNoMetales;
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

    // Funci�n para enviar los datos a la API de Flask y obtener la predicci�n �A�ADIDO!
    private void EnviarDatosAPrediccion()
    {
        // 1. Recopilar los valores de las caracter�sticas
        float porcentajeAciertos = CalcularPorcentajeAciertos();
        float dificultadMedia = CalcularDificultadMedia();

        // 2. Crear un objeto JSON con las caracter�sticas
        Dictionary<string, object> jsonData = new Dictionary<string, object>()
        {
            {"features", new float[] {
                correctasAlcalinos,
                correctasGasesNobles,
                correctasMetaloides,
                correctasTransicion,
                correctasNoMetales,
                incorrectasTotales,
                porcentajeAciertos,
                dificultadMedia
            }}
        };

        string jsonDataString = JsonConvert.SerializeObject(jsonData); // Usar Newtonsoft.Json para serializar a JSON

        // 3. Crear la solicitud UnityWebRequest
        string apiURL = "http://127.0.0.1:5000/predict"; // **�Aseg�rate de que la URL sea correcta!** (localhost:5000 es para pruebas locales)
        UnityWebRequest request = new UnityWebRequest(apiURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonDataString);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer(); // Descargar la respuesta en buffer
        request.SetRequestHeader("Content-Type", "application/json"); // Establecer el Content-Type en el header


        // 4. Enviar la solicitud y procesar la respuesta (usando Coroutine)
        StartCoroutine(EnviarYProcesarPrediccion(request));
    }

    // Coroutine para enviar la solicitud y procesar la respuesta de la API �A�ADIDO!
    IEnumerator EnviarYProcesarPrediccion(UnityWebRequest request)
    {
        yield return request.SendWebRequest(); // Enviar la solicitud y esperar la respuesta

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error de UnityWebRequest: " + request.error);
            // Manejar el error de conexi�n o protocolo (ej: mostrar un mensaje de error en la UI)
        }
        else
        {
            Debug.Log("Respuesta de la API Recibida: " + request.downloadHandler.text);

            // Procesar la respuesta JSON (usando Newtonsoft.Json)
            try
            {
                Dictionary<string, object> responseData = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                if (responseData.ContainsKey("prediction"))
                {
                    int prediction = Convert.ToInt32(responseData["prediction"]); // Obtener la predicci�n (0 o 1)

                    Debug.Log("Predicci�n de la API: " + prediction);

                    // **Aqu� puedes usar la 'prediction' (0 o 1) para adaptar tu aplicaci�n**
                    ProcesarPrediccionDeConocimiento(prediction); // Llama a una funci�n para manejar la predicci�n

                }
                else if (responseData.ContainsKey("error"))
                {
                    Debug.LogError("Error en la respuesta de la API: " + responseData["error"]);
                    // Manejar el error de la API (ej: mostrar un mensaje de error en la UI)
                }
                else
                {
                    Debug.LogWarning("Respuesta de la API en formato inesperado: " + request.downloadHandler.text);
                }
            }
            catch (JsonException e)
            {
                Debug.LogError("Error al parsear JSON de la respuesta de la API: " + e.Message);
                Debug.Log("Texto de respuesta completo: " + request.downloadHandler.text); // Imprimir el texto completo para depuraci�n
                // Manejar el error de parsing JSON (ej: mostrar un mensaje de error en la UI)
            }
        }

        request.Dispose(); // Liberar recursos de UnityWebRequest
    }

    // Funci�n para procesar la predicci�n de conocimiento (0 o 1) recibida de la API �A�ADIDO!
    private void ProcesarPrediccionDeConocimiento(int prediction)
    {
        if (prediction == 1)
        {
            Debug.Log("�El modelo predice que el usuario CONOCE el concepto!");
            // **Aqu� puedes implementar l�gica para usuarios que 'conocen' el concepto:**
            // - Aumentar la dificultad de las preguntas siguientes
            // - Ofrecer contenido m�s avanzado
            // - Desbloquear niveles o contenido adicional
        }
        else
        {
            Debug.Log("El modelo predice que el usuario NO CONOCE el concepto.");
            // **Aqu� puedes implementar l�gica para usuarios que 'no conocen' el concepto:**
            // - Reducir la dificultad de las preguntas siguientes
            // - Ofrecer recursos de repaso o explicaciones
            // - Recomendar recursos educativos usando ContentRecommendationSystem (�pr�ximo paso!)
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

    [Header("Referencias UI")]
    public TextMeshProUGUI NumeroPreguntas;

}