using System.Threading.Tasks;
using UnityEngine;

public class ActualizarRangoUsuario
{
    private readonly IServicioFirestore firestore;
    private readonly IServicioLocalStorage localStorage;

    public ActualizarRangoUsuario(IServicioFirestore firestore, IServicioLocalStorage localStorage)
    {
        this.firestore = firestore;
        this.localStorage = localStorage;
    }

    public async Task Ejecutar()
    {
        string userId = localStorage.Obtener("userId");
        int xp = int.Parse(localStorage.Obtener("TempXp", "0"));

        await firestore.ActualizarRango(userId, xp);

    }

}
