using UnityEngine;

public class GestionarIntentosFallidos
{
    private readonly IServicioLocalStorage localStorage;
    private const int MaxIntentos = 3;
    private const int TiempoBloqueoSegundos = 600;


    public GestionarIntentosFallidos(IServicioLocalStorage localStorage)
    {
        this.localStorage = localStorage;
    }

    public void RegistrarIntentoFallido()
    {
        int intentos = PlayerPrefs.GetInt("FailedAttempts", 0) + 1;
        PlayerPrefs.SetInt("FailedAttempts", intentos);
        PlayerPrefs.Save();

        if(intentos >= MaxIntentos)
        {
            BloquearUsuario();
        }
    }

    public bool EstaBloqueado()
    {
        if (!PlayerPrefs.HasKey("LockoutTime"))
            return false;

        int tiempoBloqueo = PlayerPrefs.GetInt("LockoutTime");
        int tiempoActual = GetUnixTimestamp();
        return tiempoActual < tiempoBloqueo;
    }

    public int TiempoRestante()
    {
        if (!PlayerPrefs.HasKey("LockoutTime")) 
        return 0;

        int tiempoBloqueo = PlayerPrefs.GetInt("LockoutTime");
        int tiempoActual = GetUnixTimestamp();

        return Mathf.Max(0, tiempoBloqueo - tiempoActual);
    }

    public void ResetearIntentos()
    {
        PlayerPrefs.DeleteKey("FailedAttempts");
        PlayerPrefs.DeleteKey("LockoutTime");
        PlayerPrefs.Save();
    }

    private void BloquearUsuario()
    {
        int tiempoBloqueo = GetUnixTimestamp() + TiempoBloqueoSegundos;
        PlayerPrefs.SetInt("LockoutTime", tiempoBloqueo);
        PlayerPrefs.Save();
    }

    private int GetUnixTimestamp()
    {
        return (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
    }
}
