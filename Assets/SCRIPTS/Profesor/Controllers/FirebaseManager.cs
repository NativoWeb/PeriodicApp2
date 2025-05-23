using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using static ControladorEncuesta;

public class FirebaseManager : MonoBehaviour
{
    private FirebaseFirestore db;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("✅ Firestore Inicializado Correctamente");
    }

    //public void GuardarEncuesta(string encuestaID, string titulo, List<Pregunta> preguntas) 
    //{
    //    if (db == null)
    //    {
    //        Debug.LogError("❌ Firestore no está inicializado.");
    //        return;
    //    }

    //    // 📌 Verifica si la lista de preguntas está vacía o es nula
    //    if (preguntas == null || preguntas.Count == 0)
    //    {
    //        Debug.LogError("❌ La encuesta no tiene preguntas. No se guardará.");
    //        return;
    //    }

    //    // Convierte las preguntas a un formato de diccionario compatible con Firestore
    //    List<Dictionary<string, object>> preguntasData = new List<Dictionary<string, object>>();
    //    foreach (Pregunta pregunta in preguntas)
    //    {
    //        preguntasData.Add(new Dictionary<string, object>
    //    {
    //        { "textoPregunta", pregunta.textoPregunta },
    //        { "opciones", pregunta.opciones }
    //    });
    //    }

    //    // Estructura de la encuesta
    //    Dictionary<string, object> encuesta = new Dictionary<string, object>
    //{
    //    { "titulo", titulo },
    //    { "preguntas", preguntasData }
    //};

    //    // Guardar en Firestore
    //    db.Collection("encuestas").Document(encuestaID).SetAsync(encuesta).ContinueWithOnMainThread(task =>
    //    {
    //        if (task.IsCompletedSuccessfully)
    //        {
    //            Debug.Log($"✅ Encuesta {encuestaID} guardada en Firestore.");
    //        }
    //        else
    //        {
    //            Debug.LogError("❌ Error al guardar la encuesta: " + task.Exception);
    //        }
    //    });
    //}


    public void GuardarEncuesta(string encuestaID, string titulo, List<Pregunta> preguntas)
    {
        if (db == null)
        {
            Debug.LogError("❌ Firestore no está inicializado.");
            return;
        }

        // Convierte las preguntas a un formato de diccionario compatible con Firestore
        List<Dictionary<string, object>> preguntasData = new List<Dictionary<string, object>>();
        foreach (Pregunta pregunta in preguntas)
        {
            preguntasData.Add(new Dictionary<string, object>
            {
                { "textoPregunta", pregunta.textoPregunta },
                { "opciones", pregunta.opciones }
            });
        }

        // Estructura de la encuesta
        Dictionary<string, object> encuesta = new Dictionary<string, object>
        {
            { "titulo", titulo },
            { "preguntas", preguntasData }
        };

        // Guardar en Firestore
        db.Collection("encuestas").Document(encuestaID).SetAsync(encuesta).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log($"✅ Encuesta {encuestaID} guardada en Firestore.");
            }
            else
            {
                Debug.LogError("❌ Error al guardar la encuesta: " + task.Exception);
            }
        });
    }
}
