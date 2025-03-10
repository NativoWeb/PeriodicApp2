using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class GestorOraciones : MonoBehaviour
{
    public TextMeshProUGUI txtOracion;
    public Transform contenedorOpciones;
    public GameObject botonPrefab;
    public Text txtTiempo;
    public Text txtRacha;
    public GameObject panelFinal;
    public TextMeshProUGUI txtResultado;

    public BarraProgreso barraProgreso;

    private List<OracionConPalabras> preguntas;
    private int indicePreguntaActual = 0;
    private int racha = 0;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private int nivelActual = 3;
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        GenerarPreguntas();
        StartCoroutine(Temporizador());
    }

    void GenerarPreguntas()
    {
        preguntas = new List<OracionConPalabras>
        {
            new OracionConPalabras("El agua está compuesta por _____ y oxígeno.", new string[] { "hidrógeno", "carbono", "helio", "nitrógeno" }, 0),
            new OracionConPalabras("La célula es la _____ fundamental de los seres vivos.", new string[] { "unidad", "molécula", "estructura", "fuerza" }, 0),
            new OracionConPalabras("El sol es una _____.", new string[] { "estrella", "planeta", "galaxia", "luna" }, 0),
            new OracionConPalabras("El ADN contiene la información _____ de los seres vivos.", new string[] { "genética", "química", "celular", "solar" }, 0),
            new OracionConPalabras("La fuerza de _____ mantiene a los planetas en órbita.", new string[] { "gravedad", "electricidad", "magnetismo", "presión" }, 0),
            new OracionConPalabras("El oxígeno es esencial para la _____ celular.", new string[] { "respiración", "digestión", "fotosíntesis", "oxidación" }, 0),
            new OracionConPalabras("El ser humano tiene _____ sentidos básicos.", new string[] { "cinco", "tres", "siete", "cuatro" }, 0),
            new OracionConPalabras("La ecuación de Einstein es E=mc², donde 'E' representa la _____.", new string[] { "energía", "masa", "velocidad", "gravedad" }, 0),
            new OracionConPalabras("Los peces respiran a través de sus _____.", new string[] { "branquias", "pulmones", "aletas", "escamas" }, 0),
            new OracionConPalabras("Los metales son buenos _____ de electricidad.", new string[] { "conductores", "aislantes", "generadores", "bloqueadores" }, 0)
        };

        MostrarPregunta();
    }

    public void MostrarPregunta()
    {
        if (indicePreguntaActual >= preguntas.Count)
        {
            MostrarResultadosFinales();
            return;
        }

        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];
        txtOracion.text = preguntaActual.oracion;

        foreach (Transform child in contenedorOpciones)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < preguntaActual.opciones.Length; i++)
        {
            GameObject btn = Instantiate(botonPrefab, contenedorOpciones);
            TextMeshProUGUI txtBtn = btn.GetComponentInChildren<TextMeshProUGUI>();
            txtBtn.text = preguntaActual.opciones[i];

            int index = i;
            btn.GetComponent<Button>().onClick.AddListener(() => SeleccionarPalabra(index, btn));
        }

        preguntaEnCurso = true;
        StopCoroutine("ActualizarTimer");
        StartCoroutine("Temporizador");
    }

    public void SeleccionarPalabra(int indiceSeleccionado, GameObject boton)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("ActualizarTimer");

        OracionConPalabras preguntaActual = preguntas[indicePreguntaActual];

        bool esCorrecto = (indiceSeleccionado == preguntaActual.indiceCorrecto);

        // Colores en formato Rich Text de TMP
        string colorCorrecto = "<color=#A2C94D>"; // Verde
        string colorIncorrecto = "<color=#C43E3B>"; // Rojo
        string colorFin = "</color>";

        string palabraSeleccionada = preguntaActual.opciones[indiceSeleccionado];
        string palabraColoreada = esCorrecto ? $"{colorCorrecto}{palabraSeleccionada}{colorFin}" : $"{colorIncorrecto}{palabraSeleccionada}{colorFin}";

        txtOracion.text = preguntaActual.oracion.Replace("_____", palabraColoreada);

        // Cambiar color del botón
        boton.GetComponent<Image>().color = esCorrecto ? new Color(0.64f, 0.79f, 0.30f) : new Color(0.77f, 0.24f, 0.23f);

        if (esCorrecto)
        {
            racha++;
            respuestasCorrectas++;
        }
        else
        {
            racha = 0;
        }

        txtRacha.text = "" + racha;
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

[System.Serializable]
public class OracionConPalabras
{
    public string oracion;
    public string[] opciones;
    public int indiceCorrecto;

    public OracionConPalabras(string oracion, string[] opciones, int indiceCorrecto)
    {
        this.oracion = oracion;
        this.opciones = opciones;
        this.indiceCorrecto = indiceCorrecto;
    }
}
