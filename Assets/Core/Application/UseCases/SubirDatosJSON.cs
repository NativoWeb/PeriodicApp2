using System.Threading.Tasks;
using UnityEngine;

public class SubirDatosJSON
{
    private readonly IServicioFirestore firestore;
    private readonly IServicioLocalStorage localStorage;


    public SubirDatosJSON(IServicioFirestore firestore, IServicioLocalStorage localStorage)
    {
        this.firestore = firestore;
        this.localStorage = localStorage;
    }

    public async Task Ejecutar()
    {
        string userId = localStorage.Obtener("userId");
        string misionesJson = localStorage.Obtener("misionesCategoriasJSON", "{}");
        string categoriasJson = localStorage.Obtener("CategoriasOrdenadas", "{}");

        await firestore.SubirJson(userId, misionesJson, categoriasJson);
    }
}
