//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using TMPro;
//using System.IO;
//using System.Text;
//using UnityEngine.Localization.Components; // Importante para detectar los ya localizados

//public class ProjectTextExtractor
//{
//    [MenuItem("Tools/Localization/Generate Localization To-Do List (CSV)")]
//    private static void GenerateLocalizationToDoList()
//    {
//        // Guardamos la escena actual para poder volver a ella
//        string originalScenePath = EditorSceneManager.GetActiveScene().path;
//        if (EditorSceneManager.isDirty)
//        {
//            EditorUtility.DisplayDialog("Escena no guardada", "Por favor, guarda la escena actual antes de continuar.", "OK");
//            return;
//        }

//        string outputPath = "Localization_ToDo_List.csv";
//        StringBuilder csv = new StringBuilder();
//        // <<< NUEVO: Cabecera del CSV más útil
//        csv.AppendLine("AssetPath,GameObjectPath,OriginalText,IsInitiallyEmpty,SuggestedKey");

//        Debug.Log("Iniciando inventario de textos... Esto puede tardar.");

//        // --- 1. Extraer de todas las escenas en Build Settings ---
//        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
//        {
//            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
//            {
//                try
//                {
//                    EditorSceneManager.OpenScene(scene.path);
//                    ExtractTextsFromCurrentScene(csv);
//                }
//                catch (System.Exception e)
//                {
//                    Debug.LogWarning($"No se pudo abrir o procesar la escena '{scene.path}'. Error: {e.Message}");
//                }
//            }
//        }

//        // --- 2. Extraer de todos los Prefabs en el proyecto ---
//        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
//        foreach (string prefabGUID in allPrefabs)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(prefabGUID);
//            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
//            if (prefab != null)
//            {
//                ExtractTextsFromPrefab(prefab, path, csv);
//            }
//        }

//        // Volver a la escena original
//        if (!string.IsNullOrEmpty(originalScenePath))
//        {
//            EditorSceneManager.OpenScene(originalScenePath);
//        }

//        File.WriteAllText(outputPath, csv.ToString());
//        Debug.Log($"¡Inventario COMPLETO! Se ha generado tu lista de tareas en '{outputPath}'.");
//        EditorUtility.RevealInFinder(outputPath); // Muestra el archivo en el explorador
//    }

//    private static void ExtractTextsFromCurrentScene(StringBuilder csv)
//    {
//        TMP_Text[] texts = Object.FindObjectsOfType<TMP_Text>();
//        string scenePath = EditorSceneManager.GetActiveScene().path;

//        foreach (var textComponent in texts)
//        {
//            // IGNORAR si ya tiene un componente de localización de Unity
//            if (textComponent.GetComponent<LocalizeStringEvent>() != null) continue;

//            ProcessTextComponent(textComponent, scenePath, csv);
//        }
//    }

//    private static void ExtractTextsFromPrefab(GameObject prefab, string prefabPath, StringBuilder csv)
//    {
//        TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true); // true para incluir inactivos
//        foreach (var textComponent in texts)
//        {
//            if (textComponent.GetComponent<LocalizeStringEvent>() != null) continue;

//            ProcessTextComponent(textComponent, prefabPath, csv);
//        }
//    }

//    private static void ProcessTextComponent(TMP_Text textComponent, string assetPath, StringBuilder csv)
//    {
//        string gameObjectPath = GetGameObjectPath(textComponent.transform);
//        string originalText = textComponent.text.Trim().Replace("\n", "\\n").Replace("\"", "\"\"");

//        // <<< CLAVE DEL PROBLEMA: Detectar si el texto está vacío
//        bool isInitiallyEmpty = string.IsNullOrWhiteSpace(originalText);

//        // La clave sugerida se basa en el nombre del objeto, no en su contenido
//        string suggestedKey = SanitizeKey($"{textComponent.gameObject.name}_{gameObjectPath}");

//        csv.AppendLine($"\"{assetPath}\",\"{gameObjectPath}\",\"{originalText}\",\"{isInitiallyEmpty.ToString().ToUpper()}\",\"{suggestedKey}\"");
//    }

//    // --- Métodos de Ayuda (Helpers) ---
//    private static string GetGameObjectPath(Transform transform)
//    {
//        string path = transform.name;
//        while (transform.parent != null)
//        {
//            transform = transform.parent;
//            path = transform.name + "/" + path;
//        }
//        return path;
//    }

//    private static string SanitizeKey(string key)
//    {
//        // Reemplaza caracteres no válidos por guiones bajos
//        return System.Text.RegularExpressions.Regex.Replace(key, @"[^a-zA-Z0-9_]", "_").ToLower();
//    }
//}
//#endif