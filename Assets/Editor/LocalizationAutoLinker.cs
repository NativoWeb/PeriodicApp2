//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using TMPro;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine.Localization;
//using UnityEngine.Localization.Components;
//using UnityEngine.Localization.Tables;
//using UnityEditor.Localization;

//public class LocalizationAutoLinker : EditorWindow
//{
//    // --- CONFIGURACIÓN ---
//    private const string TABLE_COLLECTION_NAME = "MasterLocalizationTable2";
//    private const string SOURCE_LOCALE_CODE = "es";
//    private const float MINIMUM_MATCH_SCORE = 0.8f;

//    // --- Variables de la ventana ---
//    private List<LocalizedStringSuggestion> suggestions = new List<LocalizedStringSuggestion>();
//    private Vector2 scrollPosition;

//    public class LocalizedStringSuggestion
//    {
//        public TMP_Text textComponent;
//        public string bestMatchKey;
//        public float matchScore;
//    }

//    [MenuItem("Tools/Localization/Asistente de Enlace de Textos")]
//    public static void ShowWindow()
//    {
//        GetWindow<LocalizationAutoLinker>("Asistente de Localización");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Asistente de Enlace de Textos", EditorStyles.boldLabel);
//        EditorGUILayout.HelpBox("Esta herramienta encuentra textos sin localizar y les asigna la clave más probable. Deberás conectar el evento 'OnUpdateString' manualmente.", MessageType.Info);

//        if (GUILayout.Button("1. Buscar y Preparar Textos (Escena Actual)"))
//        {
//            FindAndPrepare();
//        }

//        EditorGUILayout.Space();

//        if (suggestions.Count > 0)
//        {
//            EditorGUILayout.HelpBox($"Se prepararon {suggestions.Count} componentes. Revisa cada uno y finaliza la conexión del evento.", MessageType.Warning);
//            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//            foreach (var suggestion in suggestions)
//            {
//                if (suggestion.textComponent == null) continue;

//                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//                EditorGUILayout.ObjectField("Componente Preparado", suggestion.textComponent, typeof(TMP_Text), true);
//                EditorGUILayout.LabelField("Clave Asignada:", suggestion.bestMatchKey);

//                if (GUILayout.Button("Seleccionar en Jerarquía"))
//                {
//                    Selection.activeGameObject = suggestion.textComponent.gameObject;
//                    EditorGUIUtility.PingObject(suggestion.textComponent.gameObject);
//                }

//                EditorGUILayout.EndVertical();
//                EditorGUILayout.Space();
//            }
//            EditorGUILayout.EndScrollView();
//        }
//        else
//        {
//            GUILayout.Label("No hay textos para preparar. Presiona el botón para buscar.");
//        }
//    }

//    private void FindAndPrepare()
//    {
//        if (EditorSceneManager.GetActiveScene().isDirty)
//        {
//            EditorUtility.DisplayDialog("Escena no guardada", "Guarda la escena actual (Ctrl+S) antes de continuar.", "OK");
//            return;
//        }

//        suggestions.Clear();

//        var tableCollection = LocalizationEditorSettings.GetStringTableCollection(TABLE_COLLECTION_NAME);
//        if (tableCollection == null) { /* ... error dialog ... */ return; }

//        var masterStringTable = tableCollection.GetTable(SOURCE_LOCALE_CODE) as StringTable;
//        if (masterStringTable == null) { /* ... error dialog ... */ return; }

//        List<string> allKeys = masterStringTable.SharedData.Entries.Select(entry => entry.Key).ToList();

//        TMP_Text[] sceneTexts = FindObjectsOfType<TMP_Text>();
//        int componentsPrepared = 0;

//        foreach (var text in sceneTexts)
//        {
//            if (text.GetComponent<LocalizeStringEvent>() != null) continue;

//            string objectName = text.gameObject.name.ToLower();
//            string parentPath = GetParentPath(text.transform);

//            string bestMatch = "";
//            float bestScore = 0f;

//            foreach (var key in allKeys)
//            {
//                float currentScore = CalculateMatchScore(key.ToLower(), objectName, parentPath);
//                if (currentScore > bestScore)
//                {
//                    bestScore = currentScore;
//                    bestMatch = key;
//                }
//            }

//            if (bestScore >= MINIMUM_MATCH_SCORE)
//            {
//                PrepareComponent(text, bestMatch);
//                suggestions.Add(new LocalizedStringSuggestion { textComponent = text, bestMatchKey = bestMatch });
//                componentsPrepared++;
//            }
//        }

//        EditorUtility.DisplayDialog("Proceso Completado", $"{componentsPrepared} componentes de texto han sido preparados para la localización.\n\nAhora, revisa la lista en la ventana y completa la conexión del evento para cada uno.", "OK");

//        // Guarda los cambios en la escena
//        if (componentsPrepared > 0)
//        {
//            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
//            Debug.Log("¡Escena marcada como modificada! Asegúrate de guardarla (Ctrl+S).");
//        }
//    }

//    private void PrepareComponent(TMP_Text textComponent, string key)
//    {
//        LocalizeStringEvent localizeEvent = textComponent.gameObject.AddComponent<LocalizeStringEvent>();

//        // Asigna la referencia de la tabla y la clave
//        localizeEvent.StringReference.SetReference(TABLE_COLLECTION_NAME, key);

//        // Limpia el texto del editor
//        textComponent.text = "";
//        EditorUtility.SetDirty(textComponent.gameObject);
//    }

//    // --- Métodos de Ayuda (sin cambios) ---
//    private float CalculateMatchScore(string key, string objectName, string parentPath)
//    {
//        // ... (código del método sin cambios) ...
//        float score = 0;
//        var keyParts = key.Split('_');
//        if (key.Contains(objectName)) score += 1.5f;
//        foreach (var part in keyParts)
//        {
//            if (string.IsNullOrWhiteSpace(part)) continue;
//            if (objectName.Contains(part)) score += 0.5f;
//            if (parentPath.Contains(part)) score += 0.2f;
//        }
//        return score;
//    }

//    private string GetFullPath(Transform t)
//    {
//        string path = t.name;
//        while (t.parent != null)
//        {
//            t = t.parent;
//            path = t.name + "/" + path;
//        }
//        return path;
//    }

//    private string GetParentPath(Transform t)
//    {
//        if (t.parent == null) return "";
//        return GetFullPath(t.parent).ToLower();
//    }
//}
//#endif