using UnityEngine;
using Unity.Barracuda;

public class ModeloAI : MonoBehaviour
{
    // Asigna el modelo ONNX importado en el Inspector
    public NNModel modelAsset;
    private Model runtimeModel;
    private IWorker worker;

    // Parámetros de normalización (deben ser los mismos que usaste en Python)
    public float[] mean = new float[] {8.8975f,8.6225f,49.175f,22.2025f,21.4925f,9.845f,11.7725f,14.5725f,8.715f,13.02f,4.4775f,3.76221415f};  
    public float[] std = new float[] { 5.66762682f,5.4735723f,30.45635196f,12.65924539f,12.73538157f,6.44018439f,7.28531013f,8.83768882f,5.30412811f,7.94415508f,2.83716298f,0.75520968f}; 

    void Start()
    {
        // Cargar el modelo y crear el trabajador
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
    }

    // Función para normalizar la entrada
    private float[] NormalizeInput(float[] input)
    {
        if (input.Length != mean.Length || input.Length != std.Length)
        {
            Debug.LogError("La longitud del array de entrada (" + input.Length +
                             ") no coincide con la longitud de 'mean' (" + mean.Length +
                             ") o 'std' (" + std.Length + ").");
            return input; // O bien, decide cómo manejar este error.
        }

        float[] normalized = new float[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            normalized[i] = (input[i] - mean[i]) / std[i];
        }
        return normalized;
    }


    // Función para ejecutar la inferencia
    // Se asume que la salida del modelo es un array de un valor, pero ajústalo según tu modelo
    public float[] RunInference(float[] inputData)
    {
        float[] normalizedInput = NormalizeInput(inputData);
        Tensor inputTensor = new Tensor(1, normalizedInput.Length, normalizedInput);
        worker.Execute(inputTensor);
        Tensor outputTensor = worker.PeekOutput();
        float[] results = outputTensor.ToReadOnlyArray();
        inputTensor.Dispose();
        outputTensor.Dispose();
        return results;
    }

    void OnDestroy()
    {
        worker.Dispose();
    }
}
