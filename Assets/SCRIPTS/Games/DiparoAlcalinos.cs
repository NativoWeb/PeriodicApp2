using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.SceneManagement;

public class DisparoAlcalinos : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI preguntaText;
    public Button[] botonesRespuestas;
    public GameObject panelRespuesta;
    public TextMeshProUGUI textoRespuesta;
    public GameObject imagenSeleccion;
    public GameObject panelReporte;
    public TextMeshProUGUI textoReporte;

    private Dictionary<string, string> preguntasRespuestas = new Dictionary<string, string>();
    private List<string> preguntasPendientes = new List<string>();
    private string respuestaCorrecta;
    private int preguntasRespondidas = 0;
    private int xpGanadoPorNivel = 1200; // Ajustable desde el Inspector


    private Dictionary<string, string> preguntasMezcladas;
    private int indicePregunta = 0;
    private int respuestasCorrectas = 0;
    private int totalPreguntas = 6;
    private List<string> listaPreguntas;


    private FirebaseAuth firebaseAuth;
    private FirebaseFirestore firebaseDB;

    void Start()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
        firebaseDB = FirebaseFirestore.DefaultInstance;

        Debug.Log("Inicializando juego...");
        panelRespuesta.SetActive(false);
        imagenSeleccion.SetActive(false);
        panelReporte.SetActive(false);
        InicializarPreguntas();

        // Mezclar preguntas y convertir las claves en una lista
        preguntasMezcladas = MezclarDiccionario(preguntasRespuestas);
        listaPreguntas = new List<string>(preguntasMezcladas.Keys);

        GenerarPregunta();
    }

    void InicializarPreguntas()
    {
        preguntasRespuestas.Add("¿Cuál es el símbolo del Litio?", "Li");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Sodio?", "Na");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Potasio?", "K");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Rubidio?", "Rb");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Cesio?", "Cs");
        preguntasRespuestas.Add("¿Cuál es el símbolo del Francio?", "Fr");

        ReiniciarPreguntas();
    }

    void ReiniciarPreguntas()
    {
        preguntasPendientes = new List<string>(preguntasRespuestas.Keys);
        preguntasRespondidas = 0;
        respuestasCorrectas = 0;
        Debug.Log("Preguntas reiniciadas.");
    }


    void GenerarPregunta()
    {
        if (indicePregunta >= listaPreguntas.Count)
        {
            MostrarReporte();
            return;
        }

        string preguntaSeleccionada = listaPreguntas[indicePregunta];
        preguntaText.text = preguntaSeleccionada;
        respuestaCorrecta = preguntasMezcladas[preguntaSeleccionada];

        Debug.Log($"Pregunta {indicePregunta + 1}: {preguntaSeleccionada} (Correcta: {respuestaCorrecta})");

        // Generar opciones aleatorias, incluyendo la correcta
        List<string> opciones = new List<string>(preguntasRespuestas.Values);

        // Asegurar que la respuesta correcta esté presente en las opciones
        if (!opciones.Contains(respuestaCorrecta))
        {
            opciones.Add(respuestaCorrecta);
        }

        for (int i = 0; i < botonesRespuestas.Length; i++)
        {
            Button botonTemp = botonesRespuestas[i];
            TextMeshProUGUI textoBoton = botonTemp.GetComponentInChildren<TextMeshProUGUI>();

            if (textoBoton == null)
            {
                Debug.LogError($"Error: El botón {i} no tiene un TextMeshProUGUI.");
                continue;
            }

            string textoRespuesta = opciones[i];
            textoBoton.text = textoRespuesta;
            Debug.Log($"Botón {i}: {textoRespuesta}");

            botonTemp.onClick.RemoveAllListeners();
            botonTemp.onClick.AddListener(() => VerificarRespuesta(textoRespuesta, botonTemp.transform.position));
        }

        indicePregunta++; // Aseguramos que avance a la siguiente pregunta
    }

    public void VerificarRespuesta(string respuestaSeleccionada, Vector3 posicionBoton)
    {
        Debug.Log($"Respuesta seleccionada: {respuestaSeleccionada} (Correcta: {respuestaCorrecta})");
        preguntasRespondidas++;

        imagenSeleccion.SetActive(true);
        imagenSeleccion.transform.position = posicionBoton;

        if (respuestaSeleccionada == respuestaCorrecta)
        {
            textoRespuesta.text = "✅ ¡Correcto!";
            respuestasCorrectas++;
            Debug.Log("✅ Respuesta correcta");
        }
        else
        {
            textoRespuesta.text = "❌ Incorrecto";
            Debug.Log("❌ Respuesta incorrecta");
        }

        panelRespuesta.SetActive(true);
        StartCoroutine(OcultarImagenSeleccion());

        if (preguntasRespondidas >= 6)
        {
            StartCoroutine(MostrarReporteConRetraso());
        }
        else
        {
            StartCoroutine(DesactivarPanelRespuesta());
        }
    }

    IEnumerator OcultarImagenSeleccion()
    {
        yield return new WaitForSeconds(2);
        imagenSeleccion.SetActive(false);
    }

    IEnumerator DesactivarPanelRespuesta()
    {
        yield return new WaitForSeconds(2);
        panelRespuesta.SetActive(false);
        GenerarPregunta();
    }

    IEnumerator MostrarReporteConRetraso()
    {
        yield return new WaitForSeconds(2);
        MostrarReporte();
    }

    void MostrarReporte()
    {
        panelRespuesta.SetActive(false);
        panelReporte.SetActive(true);
        textoReporte.text = $"Respondiste correctamente {respuestasCorrectas} de 6 preguntas.";
        Debug.Log($"Juego terminado. Respuestas correctas: {respuestasCorrectas} de 6.");
        GuardarProgresoEnFirebase(respuestasCorrectas);
        SceneManager.LoadScene("Grupo1");

    }

    Dictionary<string, string> MezclarDiccionario(Dictionary<string, string> diccionario)
    {
        System.Random rng = new System.Random();
        List<string> clavesMezcladas = new List<string>(diccionario.Keys);

        // Algoritmo Fisher-Yates para mezclar la lista
        int n = clavesMezcladas.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (clavesMezcladas[n], clavesMezcladas[k]) = (clavesMezcladas[k], clavesMezcladas[n]);
        }

        // Crear un nuevo diccionario con las claves mezcladas pero conservando los valores originales
        Dictionary<string, string> diccionarioMezclado = new Dictionary<string, string>();
        foreach (string clave in clavesMezcladas)
        {
            diccionarioMezclado.Add(clave, diccionario[clave]);
        }

        return diccionarioMezclado;
    }

    private async void GuardarProgresoEnFirebase(int correctas)
    {
        if (firebaseAuth.CurrentUser != null)
        {
            string userId = firebaseAuth.CurrentUser.UserId;
            DocumentReference grupoRef = firebaseDB.Collection("users").Document(userId)
                                                   .Collection("grupos").Document("grupo 1");

            DocumentReference userRef = firebaseDB.Collection("users").Document(userId);

            try
            {
                // Obtener el XP actual del usuario
                DocumentSnapshot userSnapshot = await userRef.GetSnapshotAsync();
                int xpActual = userSnapshot.Exists && userSnapshot.TryGetValue<int>("xp", out int xp) ? xp : 0;

                // Obtener el nivel actual
                DocumentSnapshot grupoSnapshot = await grupoRef.GetSnapshotAsync();
                int nivelActual = grupoSnapshot.Exists && grupoSnapshot.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;

                // Sumar XP y subir de nivel
                xpGanadoPorNivel = correctas * 200;

                int nuevoXp = xpActual + xpGanadoPorNivel;
                int nuevoNivel = nivelActual + 1;

                Dictionary<string, object> datosGrupo = new Dictionary<string, object>
                {
                    { "nivel", nuevoNivel }
                };

                Dictionary<string, object> datosUsuario = new Dictionary<string, object>
                {
                    { "xp", nuevoXp }
                };

                // Guardar nivel en grupos/grupo1
                await grupoRef.SetAsync(datosGrupo, SetOptions.MergeAll);

                // Guardar XP en users/userId
                await userRef.SetAsync(datosUsuario, SetOptions.MergeAll);

                Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");

                // Guardar en PlayerPrefs por compatibilidad
                PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
                PlayerPrefs.SetInt("xp", nuevoXp);
                PlayerPrefs.Save();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("❌ Usuario no autenticado.");
        }
    }
}
