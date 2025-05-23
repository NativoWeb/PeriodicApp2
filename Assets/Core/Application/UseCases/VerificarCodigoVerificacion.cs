using UnityEngine;

public class VerificarCodigoVerificacion
{
    public bool Ejecutar(string ingresado, string esperado)
    {
        return ingresado?.Trim() == esperado?.Trim();
    }
}
