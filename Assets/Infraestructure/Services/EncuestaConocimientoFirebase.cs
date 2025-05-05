using Firebase.Firestore;
using System.Collections.Generic;
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

        foreach (var grupo in wrapper.gruposPreguntas)
        {
            foreach (var elemento in grupo.elementos)
            {
                foreach (var pregunta in elemento.preguntas)
                {
                    preguntas.Add(new PreguntaEntity
                    {
                        Texto = pregunta.textoPregunta,
                        Opciones = pregunta.opcionesRespuesta,
                        IndiceCorrecto = pregunta.indiceRespuestaCorrecta,
                        Grupo = pregunta.grupoPregunta.grupo,
                        Dificultad = pregunta.dificultadPregunta
                    });
                }
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
