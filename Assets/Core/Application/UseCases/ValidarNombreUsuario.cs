using System.Threading.Tasks;
using UnityEngine;

public class ValidarNombreUsuario
{
    private readonly IServicioFirestore firestore;

    public ValidarNombreUsuario(IServicioFirestore firestore)
    {
        this.firestore = firestore;
    }

    public async Task<bool> EstaDisponible(string nombre)
    {
        return await firestore.NombreUsuarioDisponible(nombre);
    }
}
