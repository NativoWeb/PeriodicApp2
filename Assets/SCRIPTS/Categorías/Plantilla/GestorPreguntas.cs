using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;
using UnityEngine.SceneManagement;
using SimpleJSON;
using System.Linq;

public class GestorPreguntas : MonoBehaviour
{
    public TextMeshProUGUI txtPregunta;
    public Toggle[] opciones;
    public Text txtTiempo;
    public Text txtRacha;
    public TextMeshProUGUI txtResultado;
    public GameObject PanelContinuar;

    public Slider barraProgresoSlider;
    private List<Pregunta> preguntasFiltradas;
    private int preguntaActual = 0;
    private int rachaActual;
    private int respuestasCorrectas = 0;
    private float tiempoPorPregunta = 10f;
    private float tiempoRestante;
    private bool preguntaEnCurso = true;
    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string elementoSeleccionado;
    private string simboloSeleccionado;
    private string elementoCompleto;
    private string categoriaSeleccionada;

    [System.Serializable]
    public class Pregunta
    {
        public string textoPregunta;
        public List<string> opcionesRespuesta;
        public int indiceRespuestaCorrecta;
    }

    void Start()
    {
        // FIREBASE
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Configurar el slider de progreso
        barraProgresoSlider.minValue = 0;
        barraProgresoSlider.value = preguntaActual;

        // Recuperar datos de PlayerPrefs
        elementoSeleccionado = PlayerPrefs.GetString("ElementoSeleccionado", "").Trim() + " ";
        simboloSeleccionado = "(" + PlayerPrefs.GetString("SimboloElemento", "").Trim() + ")";
        elementoCompleto = elementoSeleccionado + simboloSeleccionado;
        categoriaSeleccionada = PlayerPrefs.GetString("CategoriaSeleccionada", "").Trim();
        rachaActual = PlayerPrefs.GetInt("RachaActual");

        // Cargar progreso guardado para el elemento
        preguntaActual = PlayerPrefs.GetInt($"Progreso_{elementoCompleto}", 0);

        CargarPreguntasDesdeJSON(categoriaSeleccionada, elementoCompleto);
        MostrarPregunta();
        StartCoroutine(Temporizador());
        SistemaXP.CrearInstancia();
    }


    void CargarPreguntasDesdeJSON(string categoriaSeleccionada, string elementoSeleccionado)
    {

        TextAsset jsonFile = Resources.Load<TextAsset>("metales_alcalinos"); // Cargar JSON desde Resources
        if (jsonFile == null)
        {
            Debug.LogError("❌ No se encontró el archivo JSON en Resources.");
            return;
        }

        var json = JSON.Parse(jsonFile.text);

        if (json == null || !json.HasKey("grupo") || !json.HasKey("elementos") || !json["elementos"].IsArray)
        {
            Debug.LogError("❌ El JSON no tiene la estructura esperada.");
            return;
        }

        Debug.Log("✅ JSON cargado correctamente.");

        // 💡 Verifica si preguntasFiltradas ha sido inicializada
        if (preguntasFiltradas == null)
        {
            preguntasFiltradas = new List<Pregunta>();
        }
        preguntasFiltradas.Clear();

        bool categoriaEncontrada = json["grupo"].Value == categoriaSeleccionada;
        bool elementoEncontrado = false;

        if (!categoriaEncontrada)
        {
            Debug.LogError("⚠ No se encontró la categoría seleccionada en el JSON.");
            return;
        }

        foreach (JSONNode elementoJson in json["elementos"].AsArray)
        {
            if (elementoJson.HasKey("elemento") && elementoJson["elemento"].Value == elementoSeleccionado)
            {
                elementoEncontrado = true;

                if (elementoJson.HasKey("preguntas") && elementoJson["preguntas"].IsArray)
                {
                    foreach (JSONNode preguntaJson in elementoJson["preguntas"].AsArray)
                    {
                        // 💡 Verificación adicional para evitar null
                        if (!preguntaJson.HasKey("opcionesRespuesta") || !preguntaJson["opcionesRespuesta"].IsArray)
                        {
                            Debug.LogError("⚠ La pregunta no tiene opciones de respuesta.");
                            continue;
                        }

                        List<string> opciones = new List<string>();
                        foreach (JSONNode opcion in preguntaJson["opcionesRespuesta"].AsArray)
                        {
                            opciones.Add(opcion.Value);
                        }

                        Pregunta pregunta = new Pregunta
                        {
                            textoPregunta = preguntaJson["textoPregunta"].Value,
                            opcionesRespuesta = opciones,
                            indiceRespuestaCorrecta = preguntaJson["indiceRespuestaCorrecta"].AsInt
                        };

                        preguntasFiltradas.Add(pregunta);
                    }
                }
                else
                {
                    Debug.LogError("⚠ El elemento no tiene preguntas registradas.");
                }

                break; // Ya encontramos el elemento, salimos del loop
            }
        }

        if (!elementoEncontrado)
        {
            Debug.LogError("⚠ No se encontró el elemento seleccionado en la categoría.");
            return;
        }

        if (preguntasFiltradas.Count == 0)
        {
            Debug.LogError("⚠ No se encontraron preguntas para este elemento.");
            return;
        }
        //valor maximo del slider de progreso
        barraProgresoSlider.maxValue = preguntasFiltradas.Count;
    }


    public void MostrarPregunta()
    {
        if (preguntasFiltradas == null || preguntasFiltradas.Count == 0)
        {
            Debug.LogError("❌ Error: No hay preguntas disponibles.");
            return;
        }

        if (preguntaActual >= preguntasFiltradas.Count)
        {
            Debug.Log("✅ Todas las preguntas han sido respondidas. Mostrando resultados finales...");
            MostrarResultadosFinales();
            return;
        }

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        txtPregunta.text = pregunta.textoPregunta;

        // Actualizar el progreso en la barra
        barraProgresoSlider.value = preguntaActual + 1;

        // Asignar opciones aleatorizadas
        List<(string opcion, int indice)> opcionesIndexadas = new List<(string, int)>();
        for (int i = 0; i < pregunta.opcionesRespuesta.Count; i++)
            opcionesIndexadas.Add((pregunta.opcionesRespuesta[i], i));

        opcionesIndexadas = opcionesIndexadas.OrderBy(x => Random.value).ToList();
        int nuevoIndiceCorrecto = opcionesIndexadas.FindIndex(x => x.indice == pregunta.indiceRespuestaCorrecta);
        pregunta.indiceRespuestaCorrecta = nuevoIndiceCorrecto;

        for (int i = 0; i < opciones.Length; i++)
        {
            if (i >= opcionesIndexadas.Count) continue;
            opciones[i].GetComponentInChildren<TextMeshProUGUI>().text = opcionesIndexadas[i].opcion;
            opciones[i].isOn = false;
            opciones[i].GetComponentInChildren<Image>().color = Color.white;

            int index = i;
            opciones[i].onValueChanged.RemoveAllListeners();
            opciones[i].onValueChanged.AddListener(delegate { ValidarRespuesta(index); });
        }

        Debug.Log($"✅ Pregunta {preguntaActual + 1} mostrada correctamente.");
        preguntaEnCurso = true;
        StopCoroutine("ActualizarTimer");
        StartCoroutine("Temporizador");
    }

    public void ValidarRespuesta(int indiceSeleccionado)
    {
        if (!preguntaEnCurso) return;
        preguntaEnCurso = false;
        StopCoroutine("ActualizarTimer");

        Pregunta pregunta = preguntasFiltradas[preguntaActual];
        Color verdeCorrecto = new Color(0xAA / 255f, 0xC4 / 255f, 0x3D / 255f);
        Color rojoIncorrecto = new Color(0xC4 / 255f, 0x3E / 255f, 0x3B / 255f);
        opciones[indiceSeleccionado].GetComponentInChildren<Image>().color =
            (indiceSeleccionado == pregunta.indiceRespuestaCorrecta) ? verdeCorrecto : rojoIncorrecto;

        if (indiceSeleccionado == pregunta.indiceRespuestaCorrecta)
        {
            rachaActual++;
            respuestasCorrectas++;
        }
        else
        {
            rachaActual = 0;
        }

        txtRacha.text = "" + rachaActual;
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

        preguntaEnCurso = false;
        rachaActual = 0;
        txtRacha.text = "" + rachaActual;
        StartCoroutine(EsperarYSiguientePregunta());
    }

    IEnumerator EsperarYSiguientePregunta()
    {
        yield return new WaitForSeconds(1.5f);
        preguntaActual++;

        // Guardar progreso por elemento
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntaActual);
        PlayerPrefs.Save();

        barraProgresoSlider.value = preguntaActual + 1;
        MostrarPregunta();
    }

    void MostrarResultadosFinales()
    {
        PanelContinuar.SetActive(true);

        int experiencia = (respuestasCorrectas * 100) / preguntasFiltradas.Count;
        txtResultado.text = $"Bonificación de racha: {rachaActual * 3}";

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            SumarXPFirebase(rachaActual);
        }
        else
        {
            SumarXPTemporario(rachaActual);
        }

        // Guardar que el elemento ha sido completado
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntasFiltradas.Count);
        PlayerPrefs.Save();

        SistemaXP.Instance?.AgregarXP(experiencia);
    }

    public void GuardarYSalir()
    {
        PlayerPrefs.SetInt($"Progreso_{elementoCompleto}", preguntaActual);
        PlayerPrefs.SetInt("RachaActual", rachaActual);
        PlayerPrefs.SetFloat("ProgresoBarra", barraProgresoSlider.value);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Categorías");
    }

    void SumarXPTemporario(int xp)
    {
        int xpTemporal = PlayerPrefs.GetInt("TempXP", 0);
        xpTemporal += xp;
        PlayerPrefs.SetInt("TempXP", xpTemporal);
        PlayerPrefs.Save();
        Debug.Log($"🔄 No hay conexión. XP {xp} guardado en TempXP. Total: {xpTemporal}");
    }

    async void SumarXPFirebase(int xp)
    {
        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("❌ No hay usuario autenticado.");
            return;
        }

        DocumentReference userRef = db.Collection("users").Document(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await userRef.GetSnapshotAsync();
            int xpActual = 0;

            if (snapshot.Exists && snapshot.TryGetValue<int>("xp", out int valorXP))
            {
                xpActual = valorXP;
            }

            int xpNuevo = xpActual + xp;


            await userRef.UpdateAsync("xp", xpNuevo);
            Debug.Log($"✅ XP actualizado en Firebase: {xpNuevo}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error al actualizar XP en Firebase: {e.Message}");
        }
    }
}


public static class ListExtensions
{
    public static void Shuffle<T>(this List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
