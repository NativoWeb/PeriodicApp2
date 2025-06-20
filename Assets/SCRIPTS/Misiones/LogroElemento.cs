using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LogroElemento : MonoBehaviour
{
    public TMP_Text SimboloElemento;
    public TMP_Text logroElemento;
    public Image ImgCat;
    public Image Fondo;
    public GameObject PanelElemento;

    private static readonly Dictionary<string, Color32> ColoresPorCategoria = new Dictionary<string, Color32>
    {
        { "Metales Alcalinos",        new Color32(0x41, 0xB9, 0xDE, 0xFF) },
        { "Metales Alcalinotérreos",  new Color32(0xF0, 0x81, 0x2F, 0xFF) },
        { "Metales de Transición",     new Color32(0xED, 0x6D, 0x9D, 0xFF) },
        { "Metales postransicionales", new Color32(0x72, 0x65, 0xAA, 0xFF) },
        { "Metaloides",                new Color32(0xCD, 0xCB, 0xCC, 0xFF) },
        { "No Metales",                new Color32(0x79, 0xBB, 0x51, 0xFF) },
        { "Gases Nobles",              new Color32(0x00, 0xA2, 0x93, 0xFF) },
        { "Lantánidos",                new Color32(0xC0, 0x20, 0x3C, 0xFF) },
        { "Actinoides",                new Color32(0x33, 0x37, 0x8E, 0xFF) },
        { "Propiedades desconocidas",  new Color32(0xC2, 0x89, 0x58, 0xFF) },
    };

    public void ActualizarLogro(string simbolo, string logro, string categoria, bool desbloqueado)
    {
        SimboloElemento.text = simbolo;
        logroElemento.text = logro;

        Color grisClaro;
        ColorUtility.TryParseHtmlString("#DBDBDB", out grisClaro);

        if (desbloqueado)
        {
            // Fondo según categoría
            if (ColoresPorCategoria.TryGetValue(categoria, out Color32 colorCategoria))
            {
                Fondo.color = colorCategoria;
            }
            else
            {
                Fondo.color = grisClaro;
            }

            // Imagen correspondiente
            Sprite sprite = Resources.Load<Sprite>("ImagenesLogroElementos/" + categoria);
            if (sprite != null)
                ImgCat.sprite = sprite;
            else
                Debug.LogWarning("No se encontró la imagen para la categoría: " + categoria);
        }
        else
        {
            // Texto gris claro
            SimboloElemento.color = grisClaro;
            logroElemento.color = grisClaro;

            // Fondo gris claro
            Fondo.color = grisClaro;

            // Imagen correspondiente
            Sprite sprite = Resources.Load<Sprite>("ImagenesLogroElementos/" + categoria);
            if (sprite != null)
                ImgCat.sprite = sprite;
            else
                Debug.LogWarning("No se encontró la imagen para la categoría (bloqueado): " + categoria);
        }

        // Panel: blanco si desbloqueado, gris claro con alpha 150 si bloqueado
        Image panelImage = PanelElemento.GetComponent<Image>();
        if (panelImage != null)
        {
            if (desbloqueado)
            {
                panelImage.color = Color.white;
            }
            else
            {
                Color colorConTransparencia = grisClaro;
                colorConTransparencia.a = 150f / 255f; // Alpha de 150
                panelImage.color = colorConTransparencia;
            }
        }
        else
        {
            Debug.LogWarning("El GameObject PanelElemento no tiene un componente Image.");
        }
    }
}