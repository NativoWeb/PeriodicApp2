using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TarjetaSolicitudUI : MonoBehaviour
{
    public TMP_Text textoNombre;
    public TMP_Text textoRango;
    public TMP_Text textoFecha;
    public Button botonAceptar;
    public Button botonRechazar;

    private string solicitudId;
    private string comunidadId;
    private string usuarioId;

    public void Configurar(
        string solicitudId,
        string comunidadId,
        string usuarioId,
        string nombre,
        string rango,
        string fecha,
        System.Action<string, string> onAceptar,
        System.Action<string, string> onRechazar
    )
    {
        this.solicitudId = solicitudId;
        this.comunidadId = comunidadId;
        this.usuarioId = usuarioId;

        textoNombre.text = nombre;
        textoRango.text = rango;
        textoFecha.text = fecha;

        botonAceptar.onClick.AddListener(() => onAceptar(solicitudId, comunidadId));
        botonRechazar.onClick.AddListener(() => onRechazar(solicitudId, comunidadId));
    }
}