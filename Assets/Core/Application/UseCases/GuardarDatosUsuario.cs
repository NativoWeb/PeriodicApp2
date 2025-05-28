using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GuardarDatosUsuario
{
    private readonly IServicioFirestore firestore;
    private readonly IServicioLocalStorage localStorage;


    public GuardarDatosUsuario(IServicioFirestore firestore, IServicioLocalStorage localStorage)
    {
        this.firestore = firestore;
        this.localStorage = localStorage;
    }

    public async Task Ejecutar(Dictionary<string, object> data)
    {
        string userId = localStorage.Obtener("userId");
        await firestore.GuardarDatosUsuario(userId, data);
    }
}
