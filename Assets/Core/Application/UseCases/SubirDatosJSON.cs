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
        string misionesJson = localStorage.Obtener("Json_Misiones.json", "{}");
        string logrosJson = localStorage.Obtener("Json_Logros.json", "{}");
        string categoriasJson = PlayerPrefs.GetString("categorias_encuesta_firebase_json");

        await firestore.SubirJson(userId, misionesJson, logrosJson, categoriasJson);
    }
}
