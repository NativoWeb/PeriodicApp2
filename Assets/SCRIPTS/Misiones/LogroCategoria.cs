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

        public Categoria(string nombre, CategoriaData categoriaData)
        {
            Nombre = nombre;
            TituloMisionFinal = string.IsNullOrEmpty(categoriaData.TituloMisionFinal) ? nombre : categoriaData.TituloMisionFinal;
            ElementosData = categoriaData.Elementos ?? new Dictionary<string, ElementoData>();
        }
        public bool EstaCompletada()
        {
            return ElementosData.Values.All(e => e.misiones != null && e.misiones.Count > 0 && e.misiones.All(m => m.completada));
        }
    }
}

