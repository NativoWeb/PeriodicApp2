using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;


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
