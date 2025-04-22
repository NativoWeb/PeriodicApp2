using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class EmbeddingEntry
{
    public string id;
    public float[] embedding;
}

public class EmbeddingsLoader : MonoBehaviour
{
    public List<string> ids = new List<string>();
    public List<float[]> embeddings = new List<float[]>();

    public void CargarEmbeddings()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "chem_embeddings.json");
        string json = File.ReadAllText(path);
        EmbeddingEntry[] items = JsonHelper.FromJsonArray<EmbeddingEntry>(json);

        foreach (var item in items)
        {
            ids.Add(item.id.ToLower()); // se usa para buscar en el diccionario de elementos
            embeddings.Add(item.embedding);
        }

        Debug.Log("Embeddings cargados: " + embeddings.Count);
    }
}
