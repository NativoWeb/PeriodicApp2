using UnityEngine;
using TMPro;

public class TarjetaMisComunidadesManager : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text textoNombre;
    [SerializeField] private TMP_Text textoDescripcion;
    [SerializeField] private TMP_Text textoFecha;
    [SerializeField] private TMP_Text textoMiembros;
    [SerializeField] private TMP_Text textoPrivacidad;

    [Header("Iconos")]
    [SerializeField] private GameObject iconoPrivado;
    [SerializeField] private GameObject iconoPublico;

    [Header("Formato")]
    [Tooltip("Formato para el texto de miembros. {0} será reemplazado por el número")]
    [SerializeField] private string formatoMiembros = "{0} Miembros";

    public void Configurar(string nombre, string descripcion, string fecha, string tipo, int cantidadMiembros)
    {
        // Validación básica de parámetros
        if (string.IsNullOrEmpty(nombre))
            nombre = "Sin nombre";

        if (string.IsNullOrEmpty(descripcion))
            descripcion = "Sin descripción disponible";

        if (string.IsNullOrEmpty(fecha))
            fecha = "Fecha desconocida";

        // Configurar textos
        textoNombre.text = nombre;
        textoDescripcion.text = descripcion;
        textoFecha.text = FormatearFecha(fecha);
        textoMiembros.text = string.Format(formatoMiembros, cantidadMiembros);

        // Manejo de tipos de privacidad más robusto
        string tipoNormalizado = tipo?.ToLower() ?? "publica";
        bool esPrivada = tipoNormalizado == "privada";

        textoPrivacidad.text = esPrivada ? "Privada" : "Publica";
        iconoPrivado.SetActive(esPrivada);
        iconoPublico.SetActive(!esPrivada);
    }

    private string FormatearFecha(string fechaOriginal)
    {
        return fechaOriginal;

    }
}