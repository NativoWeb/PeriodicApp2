using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Juego de emparejamiento de compuestos químicos (sin Firebase).
/// Versión refactorizada de ControllerGame2.cs que hereda de DragAndMatchBase.
/// </summary>
public class CompoundMatchingGame : DragAndMatchBase
{
    [Header("Chemical Compounds Configuration")]
    [Tooltip("Define los emparejamientos válidos para este nivel")]
    [SerializeField] private List<ChemicalMatch> chemicalMatches = new List<ChemicalMatch>()
    {
        new ChemicalMatch("Na", "Cl"),  // Na + Cl → NaCl
        new ChemicalMatch("K", "OH"),   // K + OH → KOH
        new ChemicalMatch("Li", "O")    // Li + O → Li₂O
    };

    // Diccionario construido a partir de chemicalMatches
    private Dictionary<string, string> matchDictionary = new Dictionary<string, string>();

    protected override void Awake()
    {
        base.Awake();

        // Construir diccionario desde la lista serializada
        matchDictionary.Clear();
        foreach (var match in chemicalMatches)
        {
            if (!string.IsNullOrEmpty(match.element1) && !string.IsNullOrEmpty(match.element2))
            {
                matchDictionary[match.element1] = match.element2;
            }
        }
    }

    protected override void OnGameStart()
    {
        // Configurar el número total de emparejamientos basado en los definidos
        totalMatches = chemicalMatches.Count;

        if (enableDebugLogs)
        {
            Debug.Log($"🧪 Juego de compuestos iniciado. Total de emparejamientos: {totalMatches}");
        }
    }

    protected override bool CheckMatch(string draggedName, string droppedName)
    {
        return matchDictionary.ContainsKey(draggedName) &&
               matchDictionary[draggedName] == droppedName;
    }

    protected override void OnAllMatchesComplete()
    {
        ShowContinueButton();

        if (enableDebugLogs)
        {
            Debug.Log("🎉 ¡Todos los compuestos formados correctamente!");
        }
    }

    protected override void OnContinueButtonClick()
    {
        if (enableDebugLogs)
        {
            Debug.Log("➡️ Nivel completado, continuando...");
        }

        // Resetear para el próximo nivel
        ResetMatchCount();

        // Aquí puedes cargar la siguiente escena si lo deseas
        // SceneManager.LoadScene("NextScene");
    }

    /// <summary>
    /// Agrega un nuevo emparejamiento químico en tiempo de ejecución.
    /// </summary>
    public void AddChemicalMatch(string element1, string element2)
    {
        chemicalMatches.Add(new ChemicalMatch(element1, element2));
        matchDictionary[element1] = element2;
        totalMatches = chemicalMatches.Count;
    }

    /// <summary>
    /// Remueve un emparejamiento químico.
    /// </summary>
    public void RemoveChemicalMatch(string element1)
    {
        chemicalMatches.RemoveAll(m => m.element1 == element1);
        matchDictionary.Remove(element1);
        totalMatches = chemicalMatches.Count;
    }
}

/// <summary>
/// Estructura serializable para definir emparejamientos químicos.
/// </summary>
[System.Serializable]
public class ChemicalMatch
{
    public string element1;
    public string element2;

    public ChemicalMatch(string e1, string e2)
    {
        element1 = e1;
        element2 = e2;
    }
}
