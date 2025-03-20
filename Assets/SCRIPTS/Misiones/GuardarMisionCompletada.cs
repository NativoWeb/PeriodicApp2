using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using TMPro;
using System.Collections.Generic;

public class GuardarMisionCompletada : MonoBehaviour
{
    public Button botonCompletarMision; // Asigna el botón desde el Inspector
    public Transform contenedorMisiones; // Asigna el contenedor de misiones en el Inspector

    void Start()
    {
        if (botonCompletarMision != null)
        {
            botonCompletarMision.onClick.AddListener(MarcarMisionComoCompletada);
        }
        else
        {
            Debug.LogError("❌ botonCompletarMision no está asignado en el Inspector.");
        }
    }

    public void MarcarMisionComoCompletada()
    {
        // Obtener valores desde PlayerPrefs
        string elemento = PlayerPrefs.GetString("ElementoSeleccionado", "");
        int idMision = PlayerPrefs.GetInt("MisionActual", -1);

        // Verificar que los valores sean válidos
        if (string.IsNullOrEmpty(elemento) || idMision == -1)
        {
            Debug.LogError("❌ No se encontraron datos válidos en PlayerPrefs.");
            return;
        }

        // Guardar la misión como completada en PlayerPrefs
        string claveMision = $"Mision_{elemento}_{idMision}";
        PlayerPrefs.SetInt(claveMision, 1);
        PlayerPrefs.Save();

        Debug.Log($"✅ Misión {idMision} del elemento {elemento} marcada como completada.");

        // Actualizar el JSON de misiones en PlayerPrefs
        ActualizarMisionEnJSON(elemento, idMision);
    }

    void ActualizarMisionEnJSON(string elemento, int idMision)
    {
        // Cargar el JSON desde PlayerPrefs
        string jsonString = PlayerPrefs.GetString("misiones", "");
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("❌ No se encontró el JSON en PlayerPrefs.");
            return;
        }

        var json = JSON.Parse(jsonString);
        if (!json.HasKey("misiones") || !json["misiones"].HasKey(elemento))
        {
            Debug.LogError($"❌ No se encontró el elemento '{elemento}' en el JSON.");
            return;
        }

        // Obtener el array de niveles
        var niveles = json["misiones"][elemento]["niveles"].AsArray;

        bool cambioRealizado = false;

        // Recorremos los niveles asegurándonos de modificar directamente el JSON
        for (int i = 0; i < niveles.Count; i++)
        {
            var nivel = niveles[i];

            if (nivel["id"].AsInt == idMision)
            {
                nivel["completada"] = true; // Marcar como completada
                cambioRealizado = true;
                break;
            }
        }

        if (cambioRealizado)
        {
            // Guardar el JSON actualizado en PlayerPrefs
            PlayerPrefs.SetString("misiones", json.ToString());
            PlayerPrefs.Save();
            Debug.Log($"✅ JSON actualizado para la misión {idMision} del elemento {elemento}: {json}");
        }
        else
        {
            Debug.LogError($"❌ No se encontró la misión con ID {idMision} dentro de '{elemento}'.");
        }
    }
}
