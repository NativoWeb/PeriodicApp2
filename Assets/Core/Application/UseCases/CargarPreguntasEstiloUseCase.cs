using System.Collections.Generic;
using PeriodicApp.Core.Application.Interfaces;

namespace PeriodicApp.Core.Application.UseCases
{
    public class CargarPreguntasEstiloUseCase
    {
        private readonly IJsonService _jsonService;

        public CargarPreguntasEstiloUseCase(IJsonService jsonService)
        {
            _jsonService = jsonService;
        }

        public List<PreguntaEstilo> Ejecutar(string json)
        {
            var contenedor = _jsonService.FromJson<ContenedorPreguntas>(json);
            var preguntas = new List<PreguntaEstilo>();

            void Agregar(List<Pregunta> lista, string categoria)
            {
                foreach (var p in lista)
                {
                    preguntas.Add(new PreguntaEstilo
                    {
                        Texto = p.textoAfirmacion,
                        Categoria = categoria
                    });
                }
            }

            var estilos = contenedor.preguntasEstiloBinario;
            Agregar(estilos.Gamificacion, "Gamificacion");
            Agregar(estilos.Metodologia_Tradicional, "Metodologia_Tradicional");
            Agregar(estilos.Aprendizaje_Basado_en_Proyectos, "Aprendizaje_Basado_en_Proyectos");
            Agregar(estilos.Aprendizaje_Basado_en_Problemas, "Aprendizaje_Basado_en_Problemas");
            Agregar(estilos.Aprendizaje_Cooperativo, "Aprendizaje_Cooperativo");

            return preguntas;
        }
    }
}