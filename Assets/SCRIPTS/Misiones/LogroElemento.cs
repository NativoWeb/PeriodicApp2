using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LogroElemento : MonoBehaviour
{
    public TMP_Text SimboloElemento; // TMP para el símbolo del elemento
    public TMP_Text logroElemento;   // TMP para el logro del elemento
    public GameObject Fondo;         // Objeto de fondo (debe tener componente Image)
    public GameObject PanelElemento; // Panel del logro (también debe tener componente Image)

    // Diccionario de colores por categoría
    private Dictionary<string, string> coloresPorCategoria = new Dictionary<string, string>
    {
        { "Metales Alcalinos", "#41B9DE" },
        { "Metales Alcalinotérreos", "#F0812F" },
        { "Metales de Transición", "#ED6D9D" },
        { "Metales Postransicionales", "#7265AA" },
        { "Metaloides", "#CDCBCB" },
        { "No Metales Reactivos", "#79BB51" },
        { "Gases Nobles", "#00A293" },
        { "Lantánidos", "#C0203C" },
        { "Actínoides", "#33378E" },
        { "Propiedades Desconocidas", "#C28958" }
    };

    // Función para actualizar el estado del logro
    public void ActualizarLogro(string simbolo, string logro, bool completado, string categoria)
    {
        SimboloElemento.text = simbolo;
        logroElemento.text = logro;

        // Cambiar color del fondo según la categoría
        Image fondoImage = Fondo.GetComponent<Image>();
        if (fondoImage != null)
        {
            if (completado && coloresPorCategoria.ContainsKey(categoria))
            {
                Color colorCategoria;
                ColorUtility.TryParseHtmlString(coloresPorCategoria[categoria], out colorCategoria);
                fondoImage.color = colorCategoria;
            }
            else
            {
                Color grisClaro;
                ColorUtility.TryParseHtmlString("#DBDBDB", out grisClaro);
                fondoImage.color = grisClaro;
            }
        }
        else
        {
            Debug.LogWarning("El GameObject Fondo no tiene un componente Image.");
        }

        // Cambiar color del panel (efecto bloqueado si no está completado)
        Image panelImage = PanelElemento.GetComponent<Image>();
        if (panelImage != null)
        {
            if (completado)
            {
                panelImage.color = Color.white;
            }
            else
            {
                Color grisOscuro;
                ColorUtility.TryParseHtmlString("#E7E7E7", out grisOscuro);
                panelImage.color = grisOscuro;
            }
        }
        else
        {
            Debug.LogWarning("El GameObject PanelElemento no tiene un componente Image.");
        }
    }
}
