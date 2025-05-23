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
        Debug.Log("🔧 Generando embedding para: " + texto);

        if (vocab == null)
        {
            Debug.LogError("❌ Vocabulario no cargado.");
            return new float[384]; // evitar crash
        }

        long[] tokens = Tokenizar(texto);

        if (tokens.Length == 0)
        {
            Debug.LogError("❌ Tokenización fallida, no se generaron tokens.");
            return new float[384];
        }

        var inputTensor = new DenseTensor<long>(new[] { 1, tokens.Length });
        var tokenTypeTensor = new DenseTensor<long>(new[] { 1, tokens.Length });
        var attentionMask = new DenseTensor<long>(new[] { 1, tokens.Length });
        for (int i = 0; i < tokens.Length; i++)
        {
            tokenTypeTensor[0, i] = 0; // todos del mismo segmento
            attentionMask[0, i] = tokens[i] != 0 ? 1 : 0; // 1 para tokens válidos, 0 para padding
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
        };



        Debug.Log("📤 Enviando input al modelo...");

        using var resultados = session.Run(inputs);
        var salida = resultados.First().AsTensor<float>();

        Debug.Log($"📥 Modelo respondió. Shape: [{salida.Dimensions[0]}, {salida.Dimensions[1]}, {salida.Dimensions[2]}]");

        float[] vector = new float[salida.Dimensions[2]];
        for (int i = 0; i < salida.Dimensions[1]; i++)
            for (int j = 0; j < salida.Dimensions[2]; j++)
                vector[j] += salida[0, i, j];

        for (int j = 0; j < vector.Length; j++)
            vector[j] /= salida.Dimensions[1];

        Debug.Log("✅ Embedding generado correctamente.");
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
