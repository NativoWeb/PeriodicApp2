using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

// Clases auxiliares que coinciden con la estructura de tu JSON:
[System.Serializable]
public class GrupoPreguntasWrapper
{
    public List<GrupoPreguntas> gruposPreguntas;
}

[System.Serializable]
public class GrupoPreguntas
{
    public string grupo;
    public List<ElementoPreguntas> elementos;
}

[System.Serializable]
public class ElementoPreguntas
{
    public string elemento;
    public List<PreguntaJson> preguntas;
}

[System.Serializable]
public class PreguntaJson
{
    public string textoPregunta;
    public List<string> opcionesRespuesta;
    public int indiceRespuestaCorrecta;
    // El JSON original no tiene 'dificultad' ni 'grupo' dentro de cada pregunta,
    // por lo que aquí no los declaramos.
}

public class EncuestaConocimientoFirebase : IEncuestaConocimientoRepositorio
{
    public async Task<List<PreguntaEntity>> ObtenerPreguntasAsync()
    {
        // Cargar el TextAsset desde Resources (sin la extensión .json)
        TextAsset json = Resources.Load<TextAsset>("preguntas_tabla_periodica_categorias1");
        if (json == null) return new List<PreguntaEntity>();

        var preguntas = new List<PreguntaEntity>();
        var wrapper = JsonUtility.FromJson<GrupoPreguntasWrapper>(json.text);

        if (wrapper == null || wrapper.gruposPreguntas == null)
        {
            Debug.LogError("Error al deserializar JSON o estructura inesperada.");
            return preguntas;
        }

        System.Random rnd = new System.Random();

        foreach (var grupo in wrapper.gruposPreguntas)
        {
            // Para cada grupo (por ejemplo "Metales Alcalinos"), juntamos todas las preguntas
            // de sus elementos, las mezclamos y tomamos hasta 5.
            List<PreguntaJson> preguntasGrupo = new List<PreguntaJson>();

            foreach (var elemento in grupo.elementos)
            {
                preguntasGrupo.AddRange(elemento.preguntas);
            }

            var seleccionadas = preguntasGrupo
                .OrderBy(x => rnd.Next())
                .Take(5) // máximo 5 preguntas por grupo
                .ToList();

            foreach (var p in seleccionadas)
            {
                preguntas.Add(new PreguntaEntity
                {
                    Texto = p.textoPregunta,
                    Opciones = p.opcionesRespuesta,
                    IndiceCorrecto = p.indiceRespuestaCorrecta,
                    Grupo = grupo.grupo,        // asignamos el nombre del grupo desde el wrapper
                    Dificultad = 0f             // el JSON no tiene dificultad; ponemos 0 por defecto
                });
            }

            if (preguntas.Count >= 54)
            {
                preguntas = preguntas.Take(54).ToList();
                break;
            }
        }

        return preguntas;
    }

    public async Task GuardarEstadoEncuestaConocimientoAsync(string userId, bool estado)
    {
        var firestore = FirebaseFirestore.DefaultInstance;
        var userRef = firestore.Collection("users").Document(userId);
        await userRef.UpdateAsync("EstadoEncuestaConocimiento", estado);
    }
}
