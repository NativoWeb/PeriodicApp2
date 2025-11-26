using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;

/// <summary>
/// Juego de emparejamiento de elementos químicos con Firebase.
/// Versión refactorizada de ControllerGame.cs que hereda de DragAndMatchBase.
/// </summary>
public class ElementMatchingGame : DragAndMatchBase
{
    [Header("Firebase Configuration")]
    [SerializeField] private int xpPerLevel = 100;
    [SerializeField] private int selectedLevel = 3;
    [SerializeField] private string nextSceneName = "Grupo1";

    [Header("Position Configuration")]
    [SerializeField] private bool randomizeInitialPositions = true;

    // Posiciones iniciales para los elementos
    private static List<Vector3> initialPositions = new List<Vector3>();

    // Firebase
    private FirebaseFirestore db;
    private FirebaseAuth auth;

    // Diccionario de emparejamientos válidos
    private Dictionary<string, string> validMatches = new Dictionary<string, string>()
    {
        {"K", "Potasio"},
        {"Rb", "Rubidio"},
        {"Na", "Sodio"},
        {"Li", "Litio"}
    };

    protected override void Start()
    {
        // Inicializar Firebase
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        // Configurar posiciones aleatorias
        if (randomizeInitialPositions && initialPositions.Count == 0)
        {
            GenerateRandomPositions();
        }

        // Asignar posición aleatoria a este objeto
        if (initialPositions.Count > 0)
        {
            int index = Random.Range(0, initialPositions.Count);
            initialPosition = initialPositions[index];
            initialPositions.RemoveAt(index);
            rectTransform.position = initialPosition;
        }

        base.Start();
    }

    protected override void OnGameStart()
    {
        // Configuración adicional al inicio del juego si es necesario
        totalMatches = 4; // Este juego requiere 4 emparejamientos
    }

    protected override bool CheckMatch(string draggedName, string droppedName)
    {
        return validMatches.ContainsKey(draggedName) &&
               validMatches[draggedName] == droppedName;
    }

    protected override void OnAllMatchesComplete()
    {
        ShowContinueButton();

        // Lógica adicional de Firebase si es necesario
        GameObject gestorProgreso = GameObject.Find("GestorProgreso");
        if (gestorProgreso != null && auth != null && auth.CurrentUser != null)
        {
            // Aquí puedes agregar lógica para guardar progreso en Firebase
            if (enableDebugLogs)
            {
                Debug.Log($"💾 Guardando progreso para usuario: {auth.CurrentUser.UserId}");
            }
        }
    }

    protected override void OnContinueButtonClick()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"➡️ Cargando escena: {nextSceneName}");
        }

        ResetMatchCount();
        initialPositions.Clear();
        SceneManager.LoadScene(nextSceneName);
    }

    private void GenerateRandomPositions()
    {
        initialPositions.Clear();
        initialPositions.Add(new Vector3(250, 1930, 0));
        initialPositions.Add(new Vector3(250, 1500, 0));
        initialPositions.Add(new Vector3(250, 1050, 0));
        initialPositions.Add(new Vector3(250, 580, 0));

        // Barajar posiciones usando Fisher-Yates shuffle
        for (int i = 0; i < initialPositions.Count; i++)
        {
            Vector3 temp = initialPositions[i];
            int randomIndex = Random.Range(i, initialPositions.Count);
            initialPositions[i] = initialPositions[randomIndex];
            initialPositions[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Método público para agregar XP al usuario (llamar desde otro script si es necesario).
    /// </summary>
    public async void AwardXP()
    {
        if (auth == null || auth.CurrentUser == null)
        {
            Debug.LogError("Usuario no autenticado, no se puede otorgar XP");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        try
        {
            // Aquí puedes implementar la lógica de XP usando FirebaseServiceLocator
            // o tu repositorio de usuarios
            if (enableDebugLogs)
            {
                Debug.Log($"✨ Otorgando {xpPerLevel} XP al usuario {userId}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error al otorgar XP: {ex.Message}");
        }
    }
}
