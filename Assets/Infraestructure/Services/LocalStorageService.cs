using UnityEngine;

public class LocalStorageService : IServicioLocalStorage
{
    public void Guardar(string clave, string valor)
    {
        PlayerPrefs.SetString(clave, valor);
        PlayerPrefs.Save();
    }

    public string Obtener(string clave, string valor)
    {
        return PlayerPrefs.GetString(clave, valor);
    }

    public void Eliminar(string clave)
    {
        PlayerPrefs.DeleteKey(clave);
    }
}
