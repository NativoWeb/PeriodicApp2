using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LogroCategoria : MonoBehaviour
{
    public TMP_Text tituloMisionFinal; // TMP para el título
    public GameObject imagenCompletada; // Imagen que se activa al completar

    // Función para actualizar el estado del logro
    public void ActualizarLogro(string titulo, bool completado)
    {
        tituloMisionFinal.text = titulo;
        imagenCompletada.SetActive(completado); // Activar la imagen si está completado
    }
}

namespace UI
{
    public class Categoria
    {
        public string Nombre { get; private set; }
        public string TituloMisionFinal { get; private set; }
        public Dictionary<string, ElementoData> ElementosData { get; private set; }

        // ✅ Nuevo campo: indica si la misión final está desbloqueada
        private bool misionFinalDesbloqueada;

        // ✅ Constructor modificado para recibir esa info
        public Categoria(string nombre, CategoriaData categoriaData, bool misionFinalDesbloqueada)
        {
            Nombre = nombre;
            TituloMisionFinal = string.IsNullOrEmpty(categoriaData.TituloMisionFinal) ? nombre : categoriaData.TituloMisionFinal;
            ElementosData = categoriaData.Elementos ?? new Dictionary<string, ElementoData>();
            this.misionFinalDesbloqueada = misionFinalDesbloqueada;
        }

        // ✅ Ahora devuelve true solo si la misión final está desbloqueada
        public bool EstaCompletada()
        {
            return misionFinalDesbloqueada;
        }
    }
}
