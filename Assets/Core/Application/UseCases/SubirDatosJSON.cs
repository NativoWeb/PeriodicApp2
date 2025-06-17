using System.Threading.Tasks;
using UnityEngine;
using System.IO;

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
        string userId = localStorage.Obtener("userId"); // o ajusta si también quieres cargarlo desde archivo

        // Reemplaza estas rutas con la ubicación real de tus archivos JSON
        string pathMisiones = Path.Combine(Application.persistentDataPath, "Json_Misiones.json");
        string pathCategorias = Path.Combine(Application.persistentDataPath, "categorias_encuesta_firebase.json");
        string pathLogros = Path.Combine(Application.persistentDataPath, "Json_Logros.json");

        // Lee el contenido de los archivos
        string misionesJson = File.Exists(pathMisiones) ? File.ReadAllText(pathMisiones) : "{}";
        string categoriasJson = File.Exists(pathCategorias) ? File.ReadAllText(pathCategorias) : "{}";
        string logrosJson = File.Exists(pathLogros) ? File.ReadAllText(pathLogros) : "{}";

        await firestore.SubirJson(userId, misionesJson, categoriasJson, logrosJson);
    }
}
