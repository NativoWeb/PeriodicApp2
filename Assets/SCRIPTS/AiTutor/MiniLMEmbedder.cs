using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MiniLMEmbedder : MonoBehaviour
{
    private InferenceSession session;
    private Dictionary<string, int> vocab;
    private int maxTokens = 32;

    void Start()
    {
        string modeloPath = Path.Combine(Application.streamingAssetsPath, "MiniLM_L6_v2.onnx");
        session = new InferenceSession(modeloPath);
        Debug.Log("✅ Modelo MiniLM cargado: " + modeloPath);

        string vocabPath = Path.Combine(Application.streamingAssetsPath, "vocab.txt");

        if (!File.Exists(vocabPath))
        {
            Debug.LogError("❌ No se encontró vocab.txt en: " + vocabPath);
            return;
        }

        vocab = File.ReadAllLines(vocabPath)
            .Select((palabra, i) => new { palabra, i })
            .ToDictionary(x => x.palabra, x => x.i);

        if (!vocab.ContainsKey("[UNK]"))
        {
            Debug.LogError("❌ El vocabulario no contiene la clave [UNK]");
        }
        else
        {
            Debug.Log("✅ Vocabulario cargado con " + vocab.Count + " tokens.");
        }
    }

    public float[] ObtenerEmbedding(string texto)
    {
        long[] tokens = Tokenizar(texto);
        var inputTensor = new DenseTensor<long>(new[] { 1, tokens.Length });

        for (int i = 0; i < tokens.Length; i++)
            inputTensor[0, i] = tokens[i];

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
        };

        using var resultados = session.Run(inputs);
        var salida = resultados.First().AsTensor<float>();

        // Promedio de los embeddings de salida (CLS + cada token)
        var vector = new float[salida.Dimensions[2]];
        for (int i = 0; i < salida.Dimensions[1]; i++)  // tokens
            for (int j = 0; j < salida.Dimensions[2]; j++)  // embedding
                vector[j] += salida[0, i, j];

        for (int j = 0; j < vector.Length; j++)
            vector[j] /= salida.Dimensions[1];

        return vector;
    }

    private long[] Tokenizar(string texto)
    {
        var tokens = texto.ToLower()
            .Replace(",", "")
            .Replace(".", "")
            .Split(' ')
            .Select(w => vocab.ContainsKey(w) ? vocab[w] : vocab["[UNK]"])
            .Take(maxTokens)
            .ToList();

        while (tokens.Count < maxTokens)
            tokens.Add(0);  // padding con [PAD] = 0

        return tokens.Select(t => (long)t).ToArray();

    }

    void OnDestroy()
    {
        session?.Dispose();
    }
}
