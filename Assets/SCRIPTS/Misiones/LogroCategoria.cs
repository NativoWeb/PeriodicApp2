using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LogroCategoria : MonoBehaviour
{
    public TMP_Text tituloMisionFinal; // TMP para el título
    public Image imagenCompletada;     // Imagen que mostrará el sprite correcto

    // Función para actualizar el estado del logro
    public void ActualizarDesdeCategoria(string titulo, bool completado)
    {
        // Establecer el título
        tituloMisionFinal.text = titulo;

        // Ruta completa dentro de Resources
        string nombreSprite = completado ? "Logros/LogroCatCom" : "Logros/LogroCatInc";
        Sprite sprite = Resources.Load<Sprite>(nombreSprite);

        if (sprite != null)
        {
            imagenCompletada.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("No se encontró el sprite en Resources: " + nombreSprite);
        }
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
