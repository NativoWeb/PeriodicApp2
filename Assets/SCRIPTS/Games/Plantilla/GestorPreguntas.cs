using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class GestorPreguntas : MonoBehaviour
{
    public TextMeshProUGUI txtPregunta;
    public Toggle[] opciones;
    public Text txtTiempo;
    public Text txtRacha;
    public BarraProgreso barraProgreso;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;

    private List<PreguntaConOpciones> preguntas;
    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

    int nivelActual = 2;

    void Start()
    {

        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        GenerarPreguntasDesdeIA();
        StartCoroutine(Temporizador());
    }

    void GenerarPreguntasDesdeIA()
    {
        preguntas = new List<PreguntaConOpciones>
        {
            new PreguntaConOpciones("¿Capital de Francia?", new string[] { "Madrid", "París", "Londres", "Berlín" }, 1),
            new PreguntaConOpciones("¿Cuánto es 5 + 5?", new string[] { "8", "10", "12", "15" }, 1),
            new PreguntaConOpciones("¿Cuál es el planeta más grande del sistema solar?", new string[] { "Tierra", "Marte", "Júpiter", "Saturno" }, 2)
        };

        barraProgreso.InicializarBarra(preguntas.Count);
        MostrarPregunta();
    }

    public void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntas.Count)
        {
            MostrarResultadosFinales();
            return;
        }

        PreguntaConOpciones preguntaActual = preguntas[indicePreguntaActual];
        txtPregunta.text = preguntaActual.pregunta;

        for (int i = 0; i < opciones.Length; i++)
        {
            opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = preguntaActual.opciones[i];
            opciones[i].isOn = false;
            opciones[i].GetComponentInChildren<Image>().color = Color.white;

            int index = i;
            opciones[i].onValueChanged.RemoveAllListeners();
            opciones[i].onValueChanged.AddListener(delegate { ValidarRespuesta(index); });
        }

        preguntaEnCurso = true;
        StopCoroutine("ActualizarTimer");
        StartCoroutine("Temporizador");
    }

    public void ValidarRespuesta(int indiceSeleccionado)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("ActualizarTimer");

        PreguntaConOpciones preguntaActual = preguntas[indicePreguntaActual];
        Color verdeCorrecto = new Color(0xAA / 255f, 0xC4 / 255f, 0x3D / 255f);
        Color rojoIncorrecto = new Color(0xC4 / 255f, 0x3E / 255f, 0x3B / 255f);

        Color color = (indiceSeleccionado == preguntaActual.indiceCorrecto) ? verdeCorrecto : rojoIncorrecto;
        opciones[indiceSeleccionado].GetComponentInChildren<Image>().color = color;

        if (indiceSeleccionado == preguntaActual.indiceCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
        }

        txtRacha.text = "" + racha;

        if (indicePreguntaActual == preguntas.Count - 1)
        {
            MostrarResultadosFinales();
            return;
        }

        StartCoroutine(EsperarYSiguientePregunta());
    }

    IEnumerator Temporizador()
    {
        tiempoRestante = tiempoPorPregunta;

        while (tiempoRestante > 0)
        {
            if (!preguntaEnCurso) yield break;
            tiempoRestante -= Time.deltaTime;
            txtTiempo.text = tiempoRestante.ToString("F1");
            yield return null;
        }

        if (preguntaEnCurso)
        {
            preguntaEnCurso = false;
            racha = 0;
            txtRacha.text = "" + racha;
            yield return new WaitForSeconds(1.5f);
            if (indicePreguntaActual == preguntas.Count - 1) yield break;
            StartCoroutine(EsperarYSiguientePregunta());
        }
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        indicePreguntaActual++;
        barraProgreso.InicializarBarra(preguntas.Count);
        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        panelFinal.SetActive(true);
        int experiencia = (respuestasCorrectas * 100) / preguntas.Count;
        txtResultado.text = $"Respuestas correctas: {respuestasCorrectas}/{preguntas.Count}\nExperiencia ganada: {experiencia}XP\nBonificación de racha: {racha * 10}";
    }

    public void GuardarYSalir()
    {
        SceneManager.LoadScene("Grupos");
        GuardarProgreso(nivelActual, respuestasCorrectas);
    }

    public async void GuardarProgreso(int nivelActualJugado, int correctas)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Usuario no autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        DocumentReference docGrupo = db.Collection("users").Document(userId).Collection("grupos").Document("grupo 1");
        DocumentReference docUsuario = db.Collection("users").Document(userId);

        try
        {
            // Obtener datos actuales
            DocumentSnapshot snapshotGrupo = await docGrupo.GetSnapshotAsync();
            DocumentSnapshot snapshotUsuario = await docUsuario.GetSnapshotAsync();

            int nivelAlmacenado = snapshotGrupo.Exists && snapshotGrupo.TryGetValue<int>("nivel", out int nivel) ? nivel : 1;
            int xpActual = snapshotUsuario.Exists && snapshotUsuario.TryGetValue<int>("xp", out int xp) ? xp : 0;

            int xpGanado = correctas * 200;
            bool subirNivel = nivelActualJugado > nivelAlmacenado;

            int nuevoNivel = subirNivel ? nivelActualJugado : nivelAlmacenado;
            int nuevoXp = xpActual + xpGanado;

            // Guardar XP
            await docUsuario.SetAsync(new Dictionary<string, object> { { "xp", nuevoXp } }, SetOptions.MergeAll);

            // Guardar Nivel si sube
            if (subirNivel)
            {
                await docGrupo.SetAsync(new Dictionary<string, object> { { "nivel", nuevoNivel } }, SetOptions.MergeAll);
            }

            Debug.Log($"✅ Progreso guardado: Nivel {nuevoNivel}, XP Total {nuevoXp}");

            // Guardar localmente en PlayerPrefs
            PlayerPrefs.SetInt("nivelCompletado", nuevoNivel);
            PlayerPrefs.SetInt("xp", nuevoXp);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al guardar el progreso: {e.Message}");
        }
    }
}



//void GenerarPreguntasDesdeIA()
//{
//    FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
//    db.Collection("preguntas").GetSnapshotAsync().ContinueWithOnMainThread(task =>
//    {
//        if (task.IsCompleted)
//        {
//            preguntas = new List<string>();
//            foreach (DocumentSnapshot doc in task.Result.Documents)
//            {
//                preguntas.Add(doc.GetString("pregunta"));
//            }
//            barraProgreso.InicializarBarra(preguntas.Count);
//            MostrarPregunta();
//        }
//    });
//}
