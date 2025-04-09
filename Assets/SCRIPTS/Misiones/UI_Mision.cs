using UnityEngine;
using TMPro;
using UnityEngine.UI;


[System.Serializable]
public class Mision
{
    public int id;
    public string titulo;
    public string descripcion;
    public string tipo;
    public string colorBoton;
    public string logoMision;
    public bool completada;
    public int xp;
    public string mensajeCompletada;
    public string rutaEscena;
}

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

        // Cambia el color del botón según `mision.colorBoton`
        Color color;
        if (ColorUtility.TryParseHtmlString(mision.colorBoton, out color))
        {
            botonMision.GetComponent<Image>().color = color;
        }

        // Carga el logo de la misión (debe estar en `Resources`)
        Sprite logo = Resources.Load<Sprite>(mision.logoMision);
        if (logo != null)
        {
            logoImage.sprite = logo;
        }
    }
}
