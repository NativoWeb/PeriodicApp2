using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; 
using System.Linq;
using System;
using Newtonsoft.Json;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine.SceneManagement;


public class ControladorEncuesta : MonoBehaviour
{
    private FirebaseFirestore db;

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
    private int prediccionGlobal;

    // Variables para registrar el conteo de respuestas correctas por categor�a �A�ADIDO!
    private int correctasAlcalinos = 0;
    private int correctasAlcalinoterreos = 0;
    private int correctasFamiliaEscandio = 0;
    private int correctasFamiliaTitanio = 0;
    private int correctasFamiliaVanadio = 0;
    private int correctasFamiliaCromo = 0;
    private int correctasFamiliaManganeso = 0;
    private int correctasFamiliaHierro = 0;
    private int correctasFamiliaCobalto = 0;
    private int correctasFamiliaNiquel = 0;
    private int correctasFamiliaCobre = 0;
    private int correctasFamiliaZinc = 0;
    private int correctasGrupoBoro = 0;
    private int correctasGrupoCarbono = 0;
    private int correctasNitrogenoides = 0;
    private int correctasCalcogenos = 0;
    private int correctasHalogenos = 0;
    private int correctasGasesNobles = 0;
    private int incorrectasTotales = 0;
    private float dificultadTotalPreguntas = 0f; // Para calcular la dificultad media
    private int cantidadPreguntasRespondidas = 0; // Contador de preguntas respondidas

    private int totalPreguntasAlcalinos = 0;
    private int totalPreguntasAlcalinoterreos = 0;
    private int totalPreguntasFamiliaEscandio = 0;
    private int totalPreguntasFamiliaTitanio = 0;
    private int totalPreguntasFamiliaVanadio = 0;
    private int totalPreguntasFamiliaCromo = 0;
    private int totalPreguntasFamiliaManganeso = 0;
    private int totalPreguntasFamiliaHierro = 0;
    private int totalPreguntasFamiliaCobalto = 0;
    private int totalPreguntasFamiliaNiquel = 0;
    private int totalPreguntasFamiliaCobre = 0;
    private int totalPreguntasFamiliaZinc = 0;
    private int totalPreguntasGrupoBoro = 0;
    private int totalPreguntasGrupoCarbono = 0;
    private int totalPreguntasNitrogenoides = 0;
    private int totalPreguntasCalcogenos = 0;
    private int totalPreguntasHalogenos = 0;
    private int totalPreguntasGasesNobles = 0;

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
        db = FirebaseFirestore.DefaultInstance;
        CargarPreguntasDesdeJSON();
        AleatorizarPreguntas();
        MostrarPreguntaActual();
        desmarcarToggle();
        ConfigurarToggleListeners(); 

        ActualizarTextoTiempo();

        eventosToggleHabilitados = true;

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
            textoPreguntaUI.text = "�Encuesta Finalizada!";

            SceneManager.LoadScene("EncuestaAprendizaje");
            //grupoOpcionesUI.enabled = false;
            // Ocultar panel de preguntas y mostrar resultados
            //EnviarDatosAPrediccion(); // Llamar a EnviarDatosAPrediccion al finalizar la encuesta!

            //panelPreguntas.SetActive(false);
            //panelResultados.SetActive(true);

            //CanvasGroup cg = panelResultados.GetComponent<CanvasGroup>();
            //if (cg != null)
            //{
            //    cg.alpha = 1;
            //    cg.interactable = true;
            //    cg.blocksRaycasts = true;
            //}

            //// Mostrar resultados finales
            //MostrarResultadosFinales();

            Debug.Log("Encuesta Finalizada");
        }
        Debug.Log("siguientePregunta() finalizado.");
    }

    void CargarPreguntasDesdeJSON()
    {
        TextAsset archivoJSON = Resources.Load<TextAsset>("preguntas_tabla_periodica_mejores");
        if (archivoJSON != null)
        {
            string jsonString = archivoJSON.text;
            GrupoPreguntasWrapper wrapper = JsonUtility.FromJson<GrupoPreguntasWrapper>(jsonString); // Deserializar el JSON

            if (wrapper != null && wrapper.gruposPreguntas != null)
            {
                preguntas = new List<Pregunta>();
                System.Random rnd = new System.Random();

                foreach (var grupo in wrapper.gruposPreguntas)
                {
                    List<Pregunta> preguntasGrupo = new List<Pregunta>();

                    // Por cada elemento del grupo, seleccionar una pregunta aleatoria
                    foreach (var elemento in grupo.elementos)
                    {
                        if (elemento.preguntas != null && elemento.preguntas.Count > 0)
                        {
                            // Seleccionar una pregunta aleatoria de este elemento
                            var preguntaSeleccionada = elemento.preguntas.OrderBy(x => rnd.Next()).First();
                            preguntasGrupo.Add(preguntaSeleccionada);
                        }
                    }

                    // Si el grupo tiene más de 3 elementos, escoger aleatoriamente 3 preguntas
                    if (preguntasGrupo.Count > 3)
                    {
                        preguntasGrupo = preguntasGrupo.OrderBy(x => rnd.Next()).Take(3).ToList();
                    }

                    // Agregar las preguntas seleccionadas de este grupo a la lista global
                    preguntas.AddRange(preguntasGrupo);
                }

                // Opcional: Si deseas un total máximo de preguntas, por ejemplo 10, se puede limitar aquí
               // if (preguntas.Count > 10)
                //{
                //    preguntas = preguntas.Take(10).ToList();
                //}

                Debug.Log($"Se cargaron un total de {preguntas.Count} preguntas de todos los grupos.");
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

            // Incrementar el contador de respuestas correctas segun la categoria 
            switch (categoriaPregunta)
            {
                case "Metales Alcalinos (Grupo 1)":
                    correctasAlcalinos++;
                    totalPreguntasAlcalinos++;
                    break;
                case "Metales Alcalinotérreos (Grupo 2)":
                    correctasAlcalinoterreos++;
                    totalPreguntasAlcalinoterreos++;
                    break;
                case "Familia del Escandio (Grupo 3)": 
                    correctasFamiliaEscandio++;
                    totalPreguntasFamiliaEscandio++;
                    break;
                case "Familia del Titanio (Grupo 4)":
                    correctasFamiliaTitanio++;
                    totalPreguntasFamiliaTitanio++;
                    break;
                case "Familia del Vanadio (Grupo 5)":
                    correctasFamiliaVanadio++;
                    totalPreguntasFamiliaVanadio++;
                    break;
                case "Familia del Cromo (Grupo 6)":
                    correctasFamiliaCromo++;
                    totalPreguntasFamiliaCromo++;
                    break;
                case "Familia del Manganeso (Grupo 7)":
                    correctasFamiliaManganeso++;
                    totalPreguntasFamiliaManganeso++;
                    break;
                case "Familia del Hierro (Grupo 8)":
                    correctasFamiliaHierro++;
                    totalPreguntasFamiliaHierro++;
                    break;
                case "Familia del Cobalto (Grupo 9)":
                    correctasFamiliaCobalto++;
                    totalPreguntasFamiliaCobalto++;
                    break;
                case "Familia del Níquel (Grupo 10)":
                    correctasFamiliaNiquel++;
                    totalPreguntasFamiliaNiquel++;
                    break;
                case "Familia del Cobre (Grupo 11)":
                    correctasFamiliaCobre++;
                    totalPreguntasFamiliaCobre++;
                    break;
                case "Familia del Zinc (Grupo 12)":
                    correctasFamiliaZinc++;
                    totalPreguntasFamiliaZinc++;
                    break;
                case "Grupo del Boro (Grupo 13)":
                    correctasGrupoBoro++;
                    totalPreguntasGrupoBoro++;
                    break;
                case "Grupo del Carbono (Grupo 14)":
                    correctasGrupoCarbono++;
                    totalPreguntasGrupoCarbono++;
                    break;
                case "Nitrogenoides (Grupo 15)":
                    correctasNitrogenoides++;
                    totalPreguntasNitrogenoides++;
                    break;
                case "Calcógenos (Grupo 16)":
                    correctasCalcogenos++;
                    totalPreguntasCalcogenos++;
                    break;
                case "Halógenos (Grupo 17)":
                    correctasHalogenos++;
                    totalPreguntasHalogenos++;
                    break;
                case "Gases Nobles (Grupo 18)":
                    correctasGasesNobles++;
                    totalPreguntasGasesNobles++;
                    break;
                default:
                    Debug.LogWarning($"Categor�a de pregunta no reconocida: {categoriaPregunta}. Ajusta el switch en VerificarRespuesta.");
                    break;
            }
        }
        else
        {
            // �Respuesta INCORRECTA!
            Debug.Log("Respuesta Incorrecta");
            incorrectasTotales++; // Incrementar contador de incorrectas!
        }

        // Registrar la dificultad de la pregunta actual (asumiendo que tienes una propiedad 'dificultadPregunta' en tu objeto PreguntaConocimiento) �A�ADIDO!
        float dificultadPregunta = preguntaActual.dificultadPregunta; // Ajusta esto seg�n la estructura de tu objeto PreguntaConocimiento
        dificultadTotalPreguntas += dificultadPregunta;
        cantidadPreguntasRespondidas++;


        // Preparar para la siguiente pregunta (puedes decidir cu�ndo avanzar a la siguiente pregunta, por ejemplo, con un bot�n)
        //preguntaActualIndex++; // Incrementar el �ndice para la siguiente pregunta
        //StartCoroutine(MostrarFeedbackYCambiarPregunta());
        siguientePregunta();

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
        int totalCorrectas = correctasAlcalinos + correctasAlcalinoterreos +  correctasFamiliaEscandio + correctasFamiliaTitanio + correctasFamiliaVanadio + correctasFamiliaCromo
            + correctasFamiliaManganeso + correctasFamiliaHierro + correctasFamiliaCobalto + correctasFamiliaNiquel + correctasFamiliaCobre + correctasFamiliaZinc + correctasGrupoBoro
            + correctasGrupoCarbono + correctasNitrogenoides + correctasCalcogenos + correctasHalogenos + correctasGasesNobles;
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

    
    private float CalcularPorcentajePorGrupo(int correctasGrupo, int totalPreguntasGrupo)
    {
        if (totalPreguntasGrupo == 0)
            return 0f;
        return (float)correctasGrupo / totalPreguntasGrupo * 100f;
    }

    private Dictionary<string, float> CalcularPorcentajesPorGrupo()
    {
        Dictionary<string, float> porcentajes = new Dictionary<string, float>();

        porcentajes["Metales Alcalinos (Grupo 1)"] = CalcularPorcentajePorGrupo(correctasAlcalinos, totalPreguntasAlcalinos);
        porcentajes["Metales Alcalinotérreos (Grupo 2)"] = CalcularPorcentajePorGrupo(correctasAlcalinoterreos, totalPreguntasAlcalinoterreos);
        porcentajes["Familia del Escandio (Grupo 3)"] = CalcularPorcentajePorGrupo(correctasFamiliaEscandio, totalPreguntasFamiliaEscandio);
        porcentajes["Familia del Titanio (Grupo 4)"] = CalcularPorcentajePorGrupo(correctasFamiliaTitanio, totalPreguntasFamiliaTitanio);
        porcentajes["Familia del Vanadio (Grupo 5)"] = CalcularPorcentajePorGrupo(correctasFamiliaVanadio, totalPreguntasFamiliaVanadio);
        porcentajes["Familia del Cromo (Grupo 6)"] = CalcularPorcentajePorGrupo(correctasFamiliaCromo, totalPreguntasFamiliaCromo);
        porcentajes["Familia del Manganeso (Grupo 7)"] = CalcularPorcentajePorGrupo(correctasFamiliaManganeso, totalPreguntasFamiliaManganeso);
        porcentajes["Familia del Hierro (Grupo 8)"] = CalcularPorcentajePorGrupo(correctasFamiliaHierro, totalPreguntasFamiliaHierro);
        porcentajes["Familia del Cobalto (Grupo 9)"] = CalcularPorcentajePorGrupo(correctasFamiliaCobalto, totalPreguntasFamiliaCobalto);
        porcentajes["Familia del Níquel (Grupo 10)"] = CalcularPorcentajePorGrupo(correctasFamiliaNiquel, totalPreguntasFamiliaNiquel);
        porcentajes["Familia del Cobre (Grupo 11)"] = CalcularPorcentajePorGrupo(correctasFamiliaCobre, totalPreguntasFamiliaCobre);
        porcentajes["Familia del Zinc (Grupo 12)"] = CalcularPorcentajePorGrupo(correctasFamiliaZinc, totalPreguntasFamiliaZinc);
        porcentajes["Grupo del Boro (Grupo 13)"] = CalcularPorcentajePorGrupo(correctasGrupoBoro, totalPreguntasGrupoBoro);
        porcentajes["Grupo del Carbono (Grupo 14)"] = CalcularPorcentajePorGrupo(correctasGrupoCarbono, totalPreguntasGrupoCarbono);
        porcentajes["Nitrogenoides (Grupo 15)"] = CalcularPorcentajePorGrupo(correctasNitrogenoides, totalPreguntasNitrogenoides);
        porcentajes["Calcógenos (Grupo 16)"] = CalcularPorcentajePorGrupo(correctasCalcogenos, totalPreguntasCalcogenos);
        porcentajes["Halógenos (Grupo 17)"] = CalcularPorcentajePorGrupo(correctasHalogenos, totalPreguntasHalogenos);
        porcentajes["Gases Nobles (Grupo 18)"] = CalcularPorcentajePorGrupo(correctasGasesNobles, totalPreguntasGasesNobles);

        return porcentajes;
    }

    // Función para calcular el total de aciertos (sumando todos los grupos)
    private int CalcularTotalAciertos()
    {
        return correctasAlcalinos + correctasAlcalinoterreos + correctasFamiliaEscandio +
               correctasFamiliaTitanio + correctasFamiliaVanadio + correctasFamiliaCromo +
               correctasFamiliaManganeso + correctasFamiliaHierro + correctasFamiliaCobalto +
               correctasFamiliaNiquel + correctasFamiliaCobre + correctasFamiliaZinc +
               correctasGrupoBoro + correctasGrupoCarbono + correctasNitrogenoides +
               correctasCalcogenos + correctasHalogenos + correctasGasesNobles;
    }



    // Funci�n para enviar los datos a la API de Flask y obtener la predicci�n �A�ADIDO!
    private void EnviarDatosAPrediccion()
    {
        Debug.Log("Entrando a EnviarDatosAPrediccion()");
        // 1. Recopilar los valores de las caracter�sticas
        float porcentajeAciertos = CalcularPorcentajeAciertos();
        float dificultadMedia = CalcularDificultadMedia();

        // 2. Crear un objeto JSON con las caracter�sticas
        Dictionary<string, object> jsonData = new Dictionary<string, object>()
        {
            {"features", new float[] {
                correctasAlcalinos,
                correctasAlcalinoterreos,
                correctasFamiliaEscandio,
                correctasFamiliaTitanio,
                correctasFamiliaVanadio,
                correctasFamiliaCromo,
                correctasFamiliaManganeso,
                correctasFamiliaHierro,
                correctasFamiliaCobalto,
                correctasFamiliaNiquel,
                correctasFamiliaCobre,
                correctasFamiliaZinc,
                correctasGrupoBoro,
                correctasGrupoCarbono,
                correctasNitrogenoides,
                correctasCalcogenos,
                correctasHalogenos,
                correctasGasesNobles,
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
       // StartCoroutine(EnviarYProcesarPrediccion(request));
        Debug.Log("Corutina EnviarYProcesarPrediccion(request) llamada");
    }

    // Coroutine para enviar la solicitud y procesar la respuesta de la API �A�ADIDO!
    IEnumerator EnviarYProcesarPrediccion(UnityWebRequest request)
    {
        Debug.Log("Iniciando EnviarYProcesarPrediccion()");
        yield return request.SendWebRequest(); // Enviar la solicitud y esperar la respuesta
        Debug.Log($"request.result: {request.result}, error: {request.error}");

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
                    prediccionGlobal = Convert.ToInt32(responseData["prediction"]); // Obtener la predicci�n (0 o 1)

                    Debug.Log("Predicci�n de la API: " + prediccionGlobal);

                    // **Aqu� puedes usar la 'prediction' (0 o 1) para adaptar tu aplicaci�n**
                    ProcesarPrediccionDeConocimiento(prediccionGlobal); // Llama a una funci�n para manejar la predicci�n

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

    public void MostrarResultadosFinales()
    {
        // Calcular datos
        int totalAciertos = CalcularTotalAciertos();
        Dictionary<string, float> porcentajes = CalcularPorcentajesPorGrupo();

        // Construir texto
        string resultados = $"<b>RESULTADOS FINALES</b>\n\n";
        resultados += $"Aciertos totales: {totalAciertos}/54\n";
        resultados += $"Predicción del modelo: {(prediccionGlobal == 1 ? "✅ Dominio adecuado" : "❌ Necesita refuerzo")}\n\n";

        resultados += "<b>Detalle por grupos:</b>\n";
        foreach (var grupo in porcentajes)
        {
            resultados += $"{grupo.Key}: {grupo.Value:F1}%\n";
        }

        // Actualizar UI
        resultadosTextUI.text = resultados;

        //Guardar en FireStore
        GuardarResultadosEncuesta();
        
    }

    private void GuardarResultadosEncuesta()
    {
        // 1. Preparar datos a guardar
        float porcentajeAciertos = CalcularPorcentajeAciertos();
        Dictionary<string, float> porcentajesGrupo = CalcularPorcentajesPorGrupo();
        int totalAciertos = CalcularTotalAciertos();

        // Construir un diccionario con la información
        Dictionary<string, object> data = new Dictionary<string, object>();

        // Conversión a double porque Firestore espera double en lugar de float
        data["prediccionGlobal"] = prediccionGlobal;
        data["porcentajeAciertos"] = (double)porcentajeAciertos;
        data["totalAciertos"] = totalAciertos;
        data["timestamp"] = DateTime.UtcNow.ToString("o"); // o un FieldValue.serverTimestamp

        // 2. Guardar los porcentajes por grupo en un sub-diccionario
        Dictionary<string, object> porcentajesDic = new Dictionary<string, object>();
        foreach (var kvp in porcentajesGrupo)
        {
            porcentajesDic[kvp.Key] = (double)kvp.Value;
        }
        data["porcentajesGrupos"] = porcentajesDic;

        // 3. Escribir en Firestore
        CollectionReference coleccion = db.Collection("encuestasAprendizaje");
        coleccion.AddAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                Debug.Log("Resultados de la encuesta guardados con éxito en Firestore.");
            }
            else
            {
                Debug.LogError("Error al guardar resultados en Firestore: " + task.Exception);
            }
        });
    }




    [Header("Referencias UI")]
    public TextMeshProUGUI textoPreguntaUI;
    public ToggleGroup grupoOpcionesUI;
    public Toggle[] opcionesToggleUI;
    public TextMeshProUGUI NumeroPreguntas;
    public TextMeshProUGUI resultadosTextUI;

    [Header("Colores de Respuesta")]
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;
    public Color colorNormal = Color.white; // Color por defecto

    [Header("Paneles UI")]
    public GameObject panelPreguntas; 
    public GameObject panelResultados; 


}