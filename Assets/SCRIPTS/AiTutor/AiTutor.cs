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
    public GameObject panelChatTutor;
    public GameObject bubbleUserPrefab;
    public GameObject bubbleAiPrefab;
    public Transform contentChat;
    public EmbeddingsLoader loader;
    public MiniLMEmbedder embedder;
    public GuardarMisionCompletada gestorMisiones;

    private Dictionary<string, ElementoQuimico> elementos;
    


    void Start()
    {
        panelChatTutor.SetActive(false);
        CargarElementosDesdeJSONL();
        embedder = GetComponent<MiniLMEmbedder>();
        loader.CargarEmbeddings();

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

        // 1. Búsqueda directa por nombre o símbolo
        foreach (var par in elementos)
        {
            if (pregunta.ToLower().Contains(par.Value.simbolo.ToLower()) ||
                pregunta.ToLower().Contains(par.Value.nombre.ToLower()))
            {
                CrearBurbujaIA(par.Value.descripcion);
                return;
            }
        }

        // 2. Búsqueda por similitud semántica (embeddings)
        float[] embeddingPregunta = embedder.ObtenerEmbedding(pregunta);
        int index = BuscarElementoMasParecido(embeddingPregunta);
        string id = loader.ids[index];  // ids = lista de símbolos de los elementos

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
            DarRecomendacion(categoria, elemento, idMision);
        }
    }

    public void DarRecomendacion(string categoria, string elemento, int idMision)
    {
        string jsonString = PlayerPrefs.GetString("misionesCategoriasJSON", "");
        var json = JSON.Parse(jsonString);
        var categorias = json["Misiones_Categorias"]["Categorias"].AsObject;
        var elementoJson = categorias[categoria]["Elementos"][elemento];
        var misiones = elementoJson["misiones"].AsArray;

        JSONNode misionFallida = null;
        foreach (var m in misiones)
        {
            if (m.Value["id"].AsInt == idMision)
            {
                misionFallida = m.Value;
                break;
            }
        }


        if (misionFallida == null)
        {
            CrearBurbujaIA("😕 No encontré información suficiente para ayudarte.");
            return;
        }

        string tipo = misionFallida["tipo"];
        string descripcionElemento = elementoJson["descripcion"];
        string mensaje = "";

        switch (tipo)
        {
            case "QR":
                mensaje = $"📲 ¡Intenta escanear el código QR del elemento {elemento} nuevamente! Asegúrate de tener buena luz y enfocar correctamente. ¿Sabías esto?: {descripcionElemento}";
                break;
            case "AR":
                mensaje = $"🔍 ¿Ya exploraste el modelo 3D de {elemento}? Acércate y rota el objeto en realidad aumentada para ver detalles clave. Esto te ayudará a entender mejor la misión. 🧪\nDato: {descripcionElemento}";
                break;
            case "Juego":
                mensaje = $"🎮 ¡Reintenta el mini juego del elemento {elemento}! Concéntrate en las pistas y recuerda que puedes repetirlo las veces que necesites. ¿Sabías que: {descripcionElemento}";
                break;
            case "Quiz":
                mensaje = $"🧠 Si fallaste el quiz sobre {elemento}, revisa sus propiedades como número atómico, masa y electronegatividad. Aquí un dato útil: {descripcionElemento}";
                break;
            case "Evaluacion":
                mensaje = $"📋 La evaluación final requiere que recuerdes todo sobre {elemento}. Repasa las otras misiones y lee bien las preguntas. Aquí va un dato importante: {descripcionElemento}";
                break;
            default:
                mensaje = $"💡 ¿Sabías esto sobre {elemento}?: {descripcionElemento}";
                break;
        }

        CrearBurbujaIA(mensaje);
    }



    void CrearBurbujaUsuario(string texto)
    {
        GameObject burbuja = Instantiate(bubbleUserPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;
    }

    void CrearBurbujaIA(string texto)
    {
        GameObject burbuja = Instantiate(bubbleAiPrefab, contentChat);
        burbuja.GetComponentInChildren<TextMeshProUGUI>().text = texto;
    }

    public void ToggleChatPanel()
    {
        bool estadoActual = panelChatTutor.activeSelf;
        panelChatTutor.SetActive(!panelChatTutor.activeSelf);

        if (!estadoActual)
        {
            CrearBurbujaIA("👋 ¡Hola! Soy tu tutor virtual de química. Pregúntame sobre cualquier elemento de la tabla periódica.");
        }
    }


}
