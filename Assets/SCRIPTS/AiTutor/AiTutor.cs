using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;


[System.Serializable]
public class ElementoQuimico
{
    public string id;
    public string nombre;
    public string simbolo;
    public int numero_atomico;
    public int grupo;
    public int periodo;
    public float masa_atomica;
    public string estado;
    public string tipo;
    public string descripcion;
}

public class AiTutor : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_InputField inputPregunta;
    public GameObject bubbleUserPrefab;
    public GameObject bubbleAiPrefab;
    public Transform contentChat;
    public EmbeddingsLoader loader;
    public MiniLMEmbedder embedder;
    public GuardarMisionCompletada gestorMisiones;

    private Dictionary<string, ElementoQuimico> elementos;

    private string ultimoElementoActivo = "";
    private string ultimaIntencion = "";


    public class Intencion
    {
        public string nombre;
        public string ejemplo;
    }

    private List<Intencion> intenciones = new List<Intencion>
    {
        new Intencion { nombre = "uso", ejemplo = "¿para qué sirve este elemento?" },
        new Intencion { nombre = "ubicacion", ejemplo = "¿en qué grupo está este elemento?" },
        new Intencion { nombre = "masa", ejemplo = "¿cuál es la masa atómica del elemento?" },
        new Intencion { nombre = "estado", ejemplo = "¿es sólido, líquido o gas?" },
        new Intencion { nombre = "tipo", ejemplo = "¿es un metal o no metal?" },
        new Intencion { nombre = "general", ejemplo = "cuéntame del elemento" },
    };



    void Start()
    {
        CrearBurbujaIA("👋 ¡Hola! Soy tu tutor virtual de química. Pregúntame sobre cualquier elemento de la tabla periódica.");
        CargarElementosDesdeJSONL();
        loader.CargarEmbeddings();
        if (embedder == null)
        {
            Debug.LogError("❌ El embedder no está asignado en el Start().");
        }
        else
        {
            Debug.Log("✅ Embedder activo en tiempo de ejecución.");
        }


    }



    void CargarElementosDesdeJSONL()
    {
        elementos = new Dictionary<string, ElementoQuimico>();
        string ruta = Path.Combine(Application.streamingAssetsPath, "periodic_master_118.jsonl");

        foreach (string linea in File.ReadAllLines(ruta))
        {
            ElementoQuimico el = JsonUtility.FromJson<ElementoQuimico>(linea);

            if (!elementos.ContainsKey(el.simbolo.ToLower()))
                elementos[el.simbolo.ToLower()] = el;
        }

        Debug.Log("Elementos cargados: " + elementos.Count);
        Debug.Log("Ejemplo clave: " + elementos.Keys.First());

    }

    public void EnviarPregunta()
    {
        string pregunta = inputPregunta.text.ToLower();
        if (!string.IsNullOrEmpty(pregunta))
        {
            CrearBurbujaUsuario(pregunta);
            ProcesarPregunta(pregunta);
            inputPregunta.text = "";
        }
    }

    public void ProcesarPregunta(string pregunta)
    {
        Debug.Log("📍 Pregunta: " + pregunta);

        string preguntaNormalizada = pregunta.Trim().ToLower();

        if (ultimoElementoActivo != "" && (
            preguntaNormalizada == "sí" ||
            preguntaNormalizada.Contains("más") ||
            preguntaNormalizada.Contains("cuéntame") ||
            preguntaNormalizada.Contains("dime más") ||
            preguntaNormalizada.Contains("quiero saber más")))
        {
            Debug.Log("🔁 Usuario quiere saber más sobre " + ultimoElementoActivo);

            if (elementos.ContainsKey(ultimoElementoActivo))
            {
                string respuestaExtendida = GenerarRespuestaExtendida(elementos[ultimoElementoActivo], ultimaIntencion);
                CrearBurbujaIA(respuestaExtendida);
                return;
            }
        }



        // 1. Búsqueda directa por nombre o símbolo
        string[] palabras = pregunta.ToLower().Split(' ', ',', '.', '?', '¿', '!', '¡');

        foreach (var par in elementos)
        {
            string simbolo = par.Value.simbolo.ToLower();
            string nombre = par.Value.nombre.ToLower();

            if (palabras.Contains(simbolo) || palabras.Contains(nombre))
            {
                Debug.Log("🔁 Coincidencia exacta encontrada: " + simbolo + " o " + nombre);

                ElementoQuimico el = par.Value;
                string intencion = DetectarIntencionPorEmbedding(pregunta);
                string respuesta = GenerarRespuestaConversacional(el, intencion);
                ultimoElementoActivo = el.simbolo.ToLower(); // guarda el símbolo (ej. "o")
                ultimaIntencion = intencion;

                CrearBurbujaIA(respuesta);

                return;
            }

        }


        if (embedder == null)
        {
            Debug.LogError("❌ EL COMPONENTE embedder ESTÁ NULL en tiempo de ejecución.");
            return;
        }


        // 2. Búsqueda por similitud semántica (embeddings)
        float[] embeddingPregunta = embedder.ObtenerEmbedding(pregunta);
        int index = BuscarElementoMasParecido(embeddingPregunta);
        Debug.Log("🎯 Índice de mayor similitud: " + index);
        string id = loader.ids[index].ToLower();  // ids = lista de símbolos de los elementos

        if (elementos.ContainsKey(id))
        {
            CrearBurbujaIA(elementos[id].descripcion);
        }
        else
        {
            CrearBurbujaIA("😕 No entendí muy bien tu pregunta. ¿Podrías reformularla o mencionar un elemento químico?");
        }
    }

    int BuscarElementoMasParecido(float[] vector)
    {
        float maxSim = float.MinValue;
        int mejorIndex = 0;

        for (int i = 0; i < loader.embeddings.Count; i++)
        {
            float sim = CalcularSimilitudCoseno(vector, loader.embeddings[i]);
            if (sim > maxSim)
            {
                maxSim = sim;
                mejorIndex = i;
            }
        }


        return mejorIndex;
    }

    float CalcularSimilitudCoseno(float[] a, float[] b)
    {
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Mathf.Sqrt(magA) * Mathf.Sqrt(magB) + 1e-6f);
    }

    public void GuiarMisionDesdeTutor(string elemento, int idMision)
    {
        PlayerPrefs.SetString("ElementoSeleccionado", elemento);
        PlayerPrefs.SetInt("MisionActual", idMision);
        PlayerPrefs.Save();

        CrearBurbujaIA($"🧪 Misión del elemento {elemento}:\n¿Listo para completarla?");
    }

    public void EvaluarRespuesta(string respuesta)
    {
        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);
        string categoria = PlayerPrefs.GetString("CategoriaSeleccionada", "");

        if (elemento == "" || idMision == -1)
        {
            CrearBurbujaIA("🔍 No hay ninguna misión activa. Pídele al tutor que te asigne una.");
            return;
        }

        // Aquí puedes usar tu lógica para validar si la respuesta es correcta (ideal si tienes campo 'respuestaEsperada')
        if (respuesta.ToLower().Contains("metaloide"))  // ← temporal, puedes hacerlo dinámico
        {
            gestorMisiones.MarcarMisionComoCompletada(); // activa todo tu flujo: XP, JSON, Firebase
            CrearBurbujaIA("✅ ¡Excelente! Completaste la misión correctamente.");
        }
        else
        {
            CrearBurbujaIA("❌ Esa no es la respuesta esperada. Intenta de nuevo.");
        }
    }

    

    string DetectarIntencionPorEmbedding(string pregunta)
    {
        float[] embPregunta = embedder.ObtenerEmbedding(pregunta);
        float maxSim = float.MinValue;
        string mejor = "general";

        foreach (var intencion in intenciones)
        {
            float[] embEjemplo = embedder.ObtenerEmbedding(intencion.ejemplo);
            float sim = CalcularSimilitudCoseno(embPregunta, embEjemplo);
            if (sim > maxSim)
            {
                maxSim = sim;
                mejor = intencion.nombre;
            }
        }

        Debug.Log("🎯 Intención detectada: " + mejor);
        return mejor;
    }

    string GenerarRespuestaConversacional(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "uso":
                return $"🔬 ¡Gran pregunta! El {el.nombre} se utiliza frecuentemente en ciencia, industria o medicina. Por ejemplo: {el.descripcion.Split('.')[0]}. ¿Te interesa saber más usos?";

            case "ubicacion":
                return $"🧭 Claro, el {el.nombre} está ubicado en el grupo {el.grupo} y el periodo {el.periodo} de la tabla periódica. ¡Eso nos dice mucho sobre su comportamiento!";

            case "masa":
                return $"⚖️ La masa atómica del {el.nombre} es aproximadamente {el.masa_atomica} u. Es un dato útil cuando estudias reacciones químicas.";

            case "estado":
                return $"💧 En condiciones normales, el {el.nombre} se encuentra en estado {el.estado.ToLower()}. Esto influye en cómo lo puedes manejar o almacenar.";

            case "tipo":
                return $"🔎 El {el.nombre} es un {el.tipo.ToLower()}. Eso significa que comparte propiedades con otros elementos del mismo tipo.";

            default:
                return $"📘 El {el.nombre} ({el.simbolo}) tiene número atómico {el.numero_atomico}. Es un elemento fascinante. ¿Quieres saber para qué se usa o cómo se comporta?";
        }
    }

    string GenerarRespuestaExtendida(ElementoQuimico el, string intencion)
    {
        switch (intencion)
        {
            case "uso":
                return $"🧪 Además de su uso común, el {el.nombre} también juega un rol importante en muchos procesos industriales y naturales. Por ejemplo, {el.descripcion}";

            case "ubicacion":
                return $"📊 El {el.nombre} está en el grupo {el.grupo}, lo que indica su número de electrones de valencia, y en el periodo {el.periodo}, que indica cuántos niveles tiene su configuración electrónica.";

            case "masa":
                return $"📐 Su masa atómica precisa es de {el.masa_atomica}. Este valor se usa en cálculos estequiométricos para determinar proporciones en reacciones.";

            case "estado":
                return $"💡 Saber que el {el.nombre} es un {el.estado.ToLower()} nos ayuda a entender cómo almacenarlo y manipularlo, especialmente si trabajas en laboratorios.";

            case "tipo":
                return $"🧲 Como {el.tipo.ToLower()}, el {el.nombre} comparte propiedades con otros elementos similares, como su conductividad, brillo o reactividad química.";

            default:
                return $"📚 El {el.nombre} tiene muchas otras propiedades interesantes. Por ejemplo: {el.descripcion}";
        }
    }

    void CrearBurbujaUsuario(string texto)
    {
        GameObject burbuja = Instantiate(bubbleUserPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;

        RectTransform rt = burbuja.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1, rt.anchorMax.y);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-10, rt.anchoredPosition.y); // separarlo un poco del borde
    }

    void CrearBurbujaIA(string texto)
    {
        GameObject burbuja = Instantiate(bubbleAiPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;

        RectTransform rt = burbuja.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, rt.anchorMin.y);
        rt.anchorMax = new Vector2(0, rt.anchorMax.y);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(10, rt.anchoredPosition.y);
    }
}
