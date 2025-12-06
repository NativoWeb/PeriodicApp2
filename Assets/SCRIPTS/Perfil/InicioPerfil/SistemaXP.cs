//using System.Drawing.Text;
using UnityEngine;
using QuantumAI.Core;

public class SistemaXP : MonoBehaviour
{
    public static SistemaXP Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AgregarXP(int cantidad)
    {

        Debug.Log($"🟢 XP agregado: {cantidad}");

        // Aquí iría la lógica para sumar XP al jugador.
       int  xptempactual = PlayerPrefs.GetInt("TempXP", 0);
        int nuevoXP = cantidad + xptempactual;
        PlayerPrefs.SetInt("TempXP", nuevoXP);

        // Guardar cambios en PlayerPrefs
        PlayerPrefs.Save();

        Debug.Log($"✅ XP Total ahora: {nuevoXP}");

        // 🤖 Notificar a Quantum AI sobre la ganancia de XP
        if (QuantumAICore.Instance != null)
        {
            string razon = "Completar misión"; // Puedes hacer esto más específico si tienes el contexto
            QuantumAICore.Instance.NotifyXPGained(cantidad, razon, nuevoXP);
        }
    }

    // ✨ Nuevo método para crear una instancia si no existe
    public static void CrearInstancia()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("SistemaXP");
            Instance = obj.AddComponent<SistemaXP>();
            DontDestroyOnLoad(obj);
            Debug.Log("✅ Se ha creado una nueva instancia de SistemaXP.");
        }
    }
}
