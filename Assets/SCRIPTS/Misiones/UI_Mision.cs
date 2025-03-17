using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_Mision : MonoBehaviour
{
    public TMP_Text tituloText;
    public TMP_Text descripcionText;
    public Image logoImage;
    public Button botonMision;

    public void ConfigurarMision(Mision mision)
    {
        tituloText.text = mision.titulo;
        descripcionText.text = mision.descripcion;

        // Cambia el color del bot�n seg�n `mision.colorBoton`
        Color color;
        if (ColorUtility.TryParseHtmlString(mision.colorBoton, out color))
        {
            botonMision.GetComponent<Image>().color = color;
        }

        // Carga el logo de la misi�n (debe estar en `Resources`)
        Sprite logo = Resources.Load<Sprite>(mision.logoMision);
        if (logo != null)
        {
            logoImage.sprite = logo;
        }

        // Desactiva el bot�n si la misi�n est� completada
        botonMision.interactable = !mision.completada;
    }
}
