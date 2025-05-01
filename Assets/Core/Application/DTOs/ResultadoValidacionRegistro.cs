using UnityEngine;

public class ResultadoValidacionRegistro
{
    public bool EsValido { get; private set; }
    public string Mensaje { get; private set; }

    private ResultadoValidacionRegistro(bool esValido, string mensaje)
    {
        EsValido = esValido;
        Mensaje = mensaje;
    }
    public static ResultadoValidacionRegistro Exito()
    {
        return new ResultadoValidacionRegistro(true, null);
    }

    public static ResultadoValidacionRegistro Error(string mensaje)
    {
        return new ResultadoValidacionRegistro(false, mensaje);
    }

}
