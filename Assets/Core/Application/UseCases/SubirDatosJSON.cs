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

        // Cargar archivos desde Resources/Plantillas_Json
        TextAsset misionesAsset = Resources.Load<TextAsset>("Plantillas_Json/Json_Misiones");
        TextAsset logrosAsset = Resources.Load<TextAsset>("Plantillas_Json/Json_Logros");

        if (misionesAsset == null || logrosAsset == null)
        {
            Debug.LogError("No se pudieron cargar los archivos JSON desde Resources.");
            return;
        }

        string misionesJson = misionesAsset.text;
        string logrosJson = logrosAsset.text;

        // Obtener categorías desde PlayerPrefs (si ya fue cargado antes)
        string categoriasJson = PlayerPrefs.GetString("categorias_encuesta_firebase_json", "{}");

        await firestore.SubirJson(userId, misionesJson, logrosJson, categoriasJson);
    }

}
