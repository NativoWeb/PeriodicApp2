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
using Firebase.Auth;

using UnityEngine.SceneManagement;


public class ControladorEncuesta : MonoBehaviour
{

    // instancion variables firebase 
    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // instanciar conecxion
    private bool hayInternet = false;

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
        panelFeedback.SetActive(false);

        firestore = FirebaseFirestore.DefaultInstance;
        
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
            FinalizarEncuesta();

        }
        EnviarDatosAPrediccion();
        Debug.Log("siguientePregunta() finalizado.");
        
    }

    public void FinalizarEncuesta()
    {
        PlayerPrefs.SetInt("EstadoEncuestaConocimiento", 1);
        PlayerPrefs.Save();
        hayInternet = Application.internetReachability != NetworkReachability.NotReachable;

        bool estadoencuestaaprendizaje = false;
        bool estadoencuestaconocimiento = false;

        if (hayInternet)
        {
            firestore = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            FirebaseUser currentUser = auth.CurrentUser;

            if (currentUser == null)
            {
                Debug.LogError("❌ No hay un usuario autenticado.");
            }

            string userId = currentUser.UserId;

            estadoencuestaconocimiento= PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;
            ActualizarEstadoEncuestaConocimiento(userId, estadoencuestaconocimiento);

            DocumentReference docRef = firestore.Collection("users").Document(userId);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("❌ Error al obtener los datos del usuario.");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    Debug.LogError("❌ No se encontraron datos para este usuario.");
                    return;
                }


                // Obtener valores de Firestore
                estadoencuestaaprendizaje = snapshot.ContainsField("EstadoEncuestaAprendizaje") ? snapshot.GetValue<bool>("EstadoEncuestaAprendizaje") : false;
                estadoencuestaconocimiento = snapshot.ContainsField("EstadoEncuestaConocimiento") ? snapshot.GetValue<bool>("EstadoEncuestaConocimiento") : false;

                // Verificar si se deben cargar las categorías
                if (estadoencuestaaprendizaje && estadoencuestaconocimiento)
                {
                    SceneManager.LoadScene("Categorías");
                }
                else
                {
                    SceneManager.LoadScene("SeleccionarEncuesta");
                }
            });
        }
        else
        {
            estadoencuestaaprendizaje = PlayerPrefs.GetInt("EstadoEncuestaAprendizaje", 0) == 1;
            estadoencuestaconocimiento = PlayerPrefs.GetInt("EstadoEncuestaConocimiento", 0) == 1;


            // Validar el estado de ambas encuestas para pasar a scena 
            if (estadoencuestaaprendizaje == true && estadoencuestaconocimiento == true)
            {
                SceneManager.LoadScene("Categorías");
            }
            else
            {
                SceneManager.LoadScene("SeleccionarEncuesta");
            }
        }
    }


    private async void ActualizarEstadoEncuestaConocimiento(string userId, bool estadoencuesta) // ------------------------------------------------
    {
        DocumentReference userRef = firestore.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaConocimiento", estadoencuesta);
        Debug.Log($"✅ Estado de la encuesta Conocimiento... {userId}: {estadoencuesta} desde Encuesta Conocimiento");
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
            FinalizarEncuesta();
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

        bool esCorrecta = (indiceOpcionSeleccionada == preguntaActual.indiceRespuestaCorrecta);

        // Visual
        if (esCorrecta)
        {
            // Mostrar solo la correcta
            for (int i = 0; i < opcionesToggleUI.Length; i++)
            {
                if (i == preguntaActual.indiceRespuestaCorrecta)
                    opcionesToggleUI[i].image.color = colorCorrecto;
                else
                    opcionesToggleUI[i].image.color = colorNormal;
            }

            // Activar feedback visual
            panelFeedback.SetActive(true);
            panelFeedback.GetComponent<Image>().color = colorFondoCorrecto;
            textoFeedback.text = "Correcto";
        }
        else
        {
            // Mostrar todas las opciones y resaltar correcta/incorrecta
            for (int i = 0; i < opcionesToggleUI.Length; i++)
            {
                opcionesToggleUI[i].gameObject.SetActive(true);

                if (i == indiceOpcionSeleccionada)
                    opcionesToggleUI[i].image.color = colorIncorrecto;
                else if (i == preguntaActual.indiceRespuestaCorrecta)
                    opcionesToggleUI[i].image.color = colorCorrecto;
                else
                    opcionesToggleUI[i].image.color = colorNormal;
            }

            // Activar feedback visual
            panelFeedback.SetActive(true);
            panelFeedback.GetComponent<Image>().color = colorFondoIncorrecto;
            textoFeedback.text = "Incorrecto";
        }

        Debug.Log($"Respuesta seleccionada: {preguntaActual.respuestaUsuario} | Correcta: {preguntaActual.opcionesRespuesta[preguntaActual.indiceRespuestaCorrecta]}");

        // Desactivar interactividad
        DesactivarInteractividadOpciones();

        // Actualizar lógica de racha, aciertos e indicadores
        if (esCorrecta)
        {
            racha++;
            respuestasCorrectas++;
            txtRacha.text = racha.ToString();

            string categoriaPregunta = preguntaActual.grupoPregunta.grupo;

            switch (categoriaPregunta)
            {
                case "Metales Alcalinos (Grupo 1)": correctasAlcalinos++; break;
                case "Metales Alcalinotérreos": correctasMetalesAlcalinotérreos++; break;
                case "Metales de Transición": correctasTransicion++; break;
                case "Lantánidos": correctasLantanidos++; break;
                case "Actinoides": correctasActinoides++; break;
                case "Metaloides": correctasMetaloides++; break;
                case "Metales postransicionales": correctasMetalesPostansicionales++; break;
                case "No Metales": correctasNoMetales++; break;
                case "Propiedades desconocidas": correctasPropiedadesDesconocidas++; break;
                case "Gases Nobles": correctasGasesNobles++; break;
                default: Debug.LogWarning($"Categoría desconocida: {categoriaPregunta}"); break;
            }
        }
        else
        {
            racha = 0;
            txtRacha.text = "0";
            incorrectasTotales++;
        }

        dificultadTotalPreguntas += preguntaActual.dificultadPregunta;
        cantidadPreguntasRespondidas++;

        StartCoroutine(MostrarFeedbackYCambiarPregunta());
    }


    IEnumerator MostrarFeedbackYCambiarPregunta()
    {
        yield return new WaitForSeconds(1.5f); // Tiempo visible del feedback
        panelFeedback.SetActive(false);

        // Restaurar visibilidad de todas las opciones
        foreach (Toggle toggle in opcionesToggleUI)
        {
            toggle.gameObject.SetActive(true);
            toggle.image.color = colorNormal;
        }

        // Aquí puedes avanzar a la siguiente pregunta
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
        dificultadMedia
    };

        // Guardar las categorías ordenadas en PlayerPrefs antes de enviar los datos a predicción
        GuardarCategoriasOrdenadas();

        // 2. Llamar al modelo de Barracuda
        ModeloAI modeloAI = GetComponent<ModeloAI>(); // Asumiendo que ModeloAI está en el mismo GameObject
        if (modeloAI != null)
        {
            float[] predictionResult = modeloAI.RunInference(features);
            int prediction = (predictionResult[0] > 0.5f) ? 1 : 0;
            Debug.Log("Predicción de Barracuda: " + prediction);
            ProcesarPrediccionDeConocimiento(predictionResult);
        }
        else
        {
            Debug.LogError("No se encontró el componente ModeloAI");
        }
    }

    private List<Categoria> categorias = new List<Categoria>
    {
            new Categoria("Metales Alcalinos", "¡Prepárate para la reactividad extrema! ¿Podrás dominar estos metales explosivos?"),
            new Categoria("Metales Alcalinotérreos", "¡Más estables, pero igual de sorprendentes! Descubre su papel esencial en la química."),
            new Categoria("Metales de Transición", "¡Los maestros del cambio! Explora los metales que forman los colores más vibrantes."),
            new Categoria("Metales Postransicionales", "¡Menos famosos, pero igual de útiles! ¿Cuánto sabes de estos metales versátiles?"),
            new Categoria("Metaloides", "¡Ni metal ni no metal! Atrévete a jugar con los elementos más enigmáticos."),
            new Categoria("No Metales Reactivos", "¡Elementos esenciales para la vida! Descubre su impacto en nuestro mundo."),
            new Categoria("Gases Nobles", "¡Silenciosos pero poderosos! ¿Podrás jugar con los elementos más estables?"),
            new Categoria("Lantánidos", "¡Los metales raros que hacen posible la tecnología moderna! ¿Aceptas el reto?"),
            new Categoria("Actínoides", "¡La energía del futuro! Juega con los elementos más radioactivos y misteriosos."),
            new Categoria("Propiedades Desconocidas", "¡Aventúrate en lo desconocido! ¿Cuánto sabes de estos elementos misteriosos?")
    };

    // Funci�n para procesar la predicci�n de conocimiento (0 o 1) recibida de la API �A�ADIDO!
    public void ProcesarPrediccionDeConocimiento(float[] predictions)
    {
        // Definir un umbral para considerar que el usuario conoce la categoría
        float umbral = 0.5f;

        for (int i = 0; i < predictions.Length; i++)
        {
            // Convertir el valor a porcentaje
            float porcentaje = predictions[i] * 100f;
            categorias[i].Porcentaje = porcentaje; // Guardar el porcentaje en la categoría

            if (predictions[i] > umbral)
            {
                Debug.Log($"El usuario CONOCE el concepto del Grupo {i + 1} ({porcentaje:F2}%).");
            }
            else
            {
                Debug.Log($"El usuario NO CONOCE el concepto del Grupo {i + 1} ({porcentaje:F2}%).");
            }
        }

        GuardarCategoriasOrdenadas();
    }

    private void GuardarCategoriasOrdenadas()
    {
        // Ordenar de menor a mayor porcentaje
        categorias = categorias.OrderBy(c => c.Porcentaje).ToList();

        // Convertir a JSON y guardar en PlayerPrefs
        CategoriasData data = new CategoriasData { categorias = categorias };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("CategoriasOrdenadas", json);
        PlayerPrefs.Save();

        Debug.Log("📌 Categorías ordenadas guardadas en JSON: " + json);
    }


    [Header("Referencias UI")]
    public TextMeshProUGUI textoPreguntaUI;
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;

    [Header("Feedback UI")]
    public GameObject panelFeedback; 
    public TextMeshProUGUI textoFeedback;
    public Color colorFondoCorrecto = new Color(0.66f, 0.81f, 0.30f); // Verde claro
    public Color colorFondoIncorrecto = new Color(0.89f, 0.31f, 0.31f); // Rojo claro


    [Header("Colores de Respuesta")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    public Color colorNormal = Color.white; // Color por defecto




    [System.Serializable]
    public class Categoria
    {
        public string Titulo;
        public string Descripcion;
        public float Porcentaje;

        public Categoria(string nombre, string descripcion)
        {
            Titulo = nombre;
            Descripcion = descripcion;
            Porcentaje = 0f;
        }
    }

    [System.Serializable]
    public class CategoriasData
    {
        public List<Categoria> categorias;
    }

}