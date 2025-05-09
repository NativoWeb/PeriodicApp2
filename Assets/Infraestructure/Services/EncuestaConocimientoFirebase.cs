using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static ControladorEncuesta;

public class EncuestaConocimientoFirebase : IEncuestaConocimientoRepositorio
{
    public async Task<List<PreguntaEntity>> ObtenerPreguntasAsync()
    {
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
            List<ControladorEncuesta.Pregunta> preguntasGrupo = new List<ControladorEncuesta.Pregunta>();


            foreach (var elemento in grupo.elementos)
            {
                preguntasGrupo.AddRange(elemento.preguntas);
            }

            var seleccionadas = preguntasGrupo
                .OrderBy(x => rnd.Next())
                .Take(5) // máx. 5 por grupo
                .ToList();

            foreach (var p in seleccionadas)
            {
                preguntas.Add(new PreguntaEntity
                {
                    Texto = p.textoPregunta,
                    Opciones = p.opcionesRespuesta,
                    IndiceCorrecto = p.indiceRespuestaCorrecta,
                    Grupo = p.grupoPregunta.grupo,
                    Dificultad = p.dificultadPregunta
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
