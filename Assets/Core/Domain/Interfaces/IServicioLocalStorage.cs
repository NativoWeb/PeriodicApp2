using UnityEngine;

public interface IServicioLocalStorage
{
    void Guardar(string clave, string valor);
    string Obtener(string clave, string valorPorDefecto = "");
    void Eliminar(string clave);
}
