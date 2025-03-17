using UnityEngine;

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

    // Método para agregar XP
    public void AgregarXP(int cantidadXP)
    {
        int xpActual = PlayerPrefs.GetInt("xp", 0);
        xpActual += cantidadXP;
        PlayerPrefs.SetInt("TempXP", xpActual);
        PlayerPrefs.Save();
        Debug.Log("✅ XP local sumada: " + xpActual);
    }
}
