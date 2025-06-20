using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class LogroCategoria : MonoBehaviour
{
    public TMP_Text nombre; // TMP para el título
    public TMP_Text logroscompletados;
    public Image imagenCompletada;     // Imagen que mostrará el sprite correcto

    // Función para actualizar el estado del logro
    public void MostrarDesdeCategoria(string titulo, bool completado, int totalElementos, int elementosCompletados)
    {
        string rutaBase;
        // Establecer el título
        nombre.text = titulo;
        logroscompletados.text = elementosCompletados + "-" + totalElementos;

        // Ruta completa dentro de Resourcesstring
        rutaBase = "LogrosCategoriaDesbloqueada/" + titulo;
        Sprite sprite = Resources.Load<Sprite>(rutaBase);

        if (sprite != null)
        {
            imagenCompletada.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("No se encontró el sprite en Resources: " + rutaBase);
        }
    }

    public void MostrarDesdeElemento( string logro, int totalElementos, int elementosCompletados, bool desbloqueado)
    {
        string titulo = PlayerPrefs.GetString("CatSeleccionada");
        nombre.text = logro;
        logroscompletados.text = elementosCompletados + "-" + totalElementos;

        string carpeta = desbloqueado ? "LogrosCategoriaDesbloqueada/" : "LogrosCategoriaBloqueada/";
        string ruta = carpeta + titulo;

        Sprite sprite = Resources.Load<Sprite>(ruta);
        if (sprite != null)
        {
            imagenCompletada.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("No se encontró el sprite en Resources: " + ruta);
        }
    }

}

namespace UI
{
    public class Categoria
    {
        public string Nombre { get; private set; }

        public bool Desbloqueado;
        public Dictionary<string, ElementoData> ElementosData { get; private set; }


        // ✅ Constructor modificado para recibir esa info
        public Categoria(string nombre, CategoriaData categoriaData, bool misionFinalDesbloqueada)
        {
            Nombre = nombre;
            Desbloqueado = categoriaData.desbloqueado;
            ElementosData = categoriaData.Elementos ?? new Dictionary<string, ElementoData>();
        }
    }
}
