using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

[System.Serializable]
public class PreguntaJuego
{
    public int id;
    public string elemento;
    public string imagen;
    public string pregunta;
    public List<string> opciones;
    public string respuesta_correcta;
}

[System.Serializable]
public class PreguntaData
{
    public List<PreguntaJuego> niveles;
}

public class Game2 : MonoBehaviour
{
    public static Game2 Instancia;

    public TextMeshProUGUI txtPregunta;
    public Image imgElemento;
    public Button[] botonesRespuestas; // Los 4 botones de las respuestas
    public Text txtRacha;
    public Text txtTemporizador;

    private List<PreguntaJuego> preguntas;
    private int indiceActual = 0;
    private int racha = 0;
    private float tiempoRestante = 10f; // Tiempo en segundos
    private bool tiempoActivo = true;

    void Awake()
    {
        CargarPreguntas();
    }

    void Update()
    {
        if (tiempoActivo)
        {
            tiempoRestante -= Time.deltaTime;
            txtTemporizador.text = Mathf.Ceil(tiempoRestante).ToString();

            if (tiempoRestante <= 0f)
            {
                tiempoActivo = false; // ⛔ Evita múltiples llamadas
                PerderRacha();
                SiguientePregunta(); // 👈 También pasamos a la siguiente
            }
        }
    }

    void CargarPreguntas()
    {
        // Cargar desde Resources (archivo sin extensión)
        TextAsset json = Resources.Load<TextAsset>("juego_tabla_periodica_preguntas");
        if (json != null)
        {
            PreguntaData data = JsonUtility.FromJson<PreguntaData>(json.text);
            preguntas = data.niveles.OrderBy(x => Random.value).ToList(); // Aleatorizar preguntas
            MostrarPregunta();
        }
        else
        {
            Debug.LogError("No se pudo cargar el archivo JSON de preguntas.");
        }
    }

    public void MostrarPregunta()
    {
        if (preguntas == null || preguntas.Count == 0) return;

        PreguntaJuego preguntaActual = preguntas[indiceActual];

        // Mostrar pregunta y opciones
        txtPregunta.text = preguntaActual.pregunta;
        //imgElemento.sprite = Resources.Load<Sprite>(preguntaActual.imagen); // Cargar la imagen

        // Aleatorizar las opciones
        List<string> opciones = preguntaActual.opciones.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < botonesRespuestas.Length; i++)
        {
            if (i < opciones.Count)
            {
                string opcion = opciones[i]; // ✅ Captura local para evitar bug de closure
                botonesRespuestas[i].GetComponentInChildren<TextMeshProUGUI>().text = opcion;
                botonesRespuestas[i].onClick.RemoveAllListeners();
                botonesRespuestas[i].onClick.AddListener(() => ComprobarRespuesta(opcion));
                botonesRespuestas[i].gameObject.SetActive(true); // Asegúrate de que esté visible
            }
            else
            {
                botonesRespuestas[i].gameObject.SetActive(false); // Oculta botones extra si hay menos de 4 opciones
            }
        }

        // Resetear el temporizador
        tiempoRestante = 10f;
        tiempoActivo = true;
    }

    public void ComprobarRespuesta(string respuestaUsuario)
    {
        if (tiempoActivo)
        {
            tiempoActivo = false; // Detener el temporizador

            if (VerificarRespuesta(respuestaUsuario))
            {
                AumentarRacha();
            }
            else
            {
                PerderRacha();
            }

            // Avanzar a la siguiente pregunta
            SiguientePregunta();
        }
    }

    public void AumentarRacha()
    {
        racha++;
        txtRacha.text = racha.ToString();
    }

    public void PerderRacha()
    {
        racha = 0;
        txtRacha.text = racha.ToString();
    }

    public bool VerificarRespuesta(string respuestaUsuario)
    {
        return preguntas[indiceActual].respuesta_correcta == respuestaUsuario;
    }

    public void SiguientePregunta()
    {
        if (indiceActual < preguntas.Count - 1)
        {
            indiceActual++;
            MostrarPregunta();
        }
        else
        {
            indiceActual = 0; // Reinicia las preguntas
            MostrarPregunta();
        }
    }

    public void FinalizarJuego()
    {
        // Aquí puedes agregar lógica para cuando el jugador termine todas las preguntas.
        Debug.Log("Juego finalizado!");
    }
}
