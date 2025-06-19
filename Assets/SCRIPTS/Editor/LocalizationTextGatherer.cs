//    using UnityEngine;
//using UnityEditor;
//using TMPro;
//using UnityEngine.Localization.Tables;
//using UnityEngine.Localization.Settings;
//using System.Linq;
//using UnityEditor.Localization;
//using UnityEngine.Localization;

//public class LocalizationTextGatherer : EditorWindow
//{
//    private StringTableCollection stringTableCollection;

//    // Crea la opción en el menú de Unity
//    [MenuItem("Tools/Localization/Gather Scene Texts")]
//    public static void ShowWindow()
//    {
//        GetWindow<LocalizationTextGatherer>("Gather Scene Texts");
//    }

//    void OnGUI()
//    {
//        GUILayout.Label("Assign your String Table Collection", EditorStyles.boldLabel);

//        // Campo para que arrastres tu Tabla de Strings
//        stringTableCollection = (StringTableCollection)EditorGUILayout.ObjectField(
//            "String Table",
//            stringTableCollection,
//            typeof(StringTableCollection),
//            false);

//        if (stringTableCollection == null)
//        {
//            EditorGUILayout.HelpBox("Please assign a String Table Collection to proceed.", MessageType.Warning);
//            return;
//        }

//        if (GUILayout.Button("Gather Texts from Active Scene"))
//        {
//            GatherTexts();
//        }
//    }

//    private void GatherTexts()
//    {
//        // Busca todos los componentes TextMeshProUGUI en la escena activa
//        var textComponents = FindObjectsOfType<TextMeshProUGUI>();
//        int newEntries = 0;

//        foreach (var textComponent in textComponents)
//        {
//            // Ignora los textos que ya están siendo localizados
//            if (textComponent.GetComponent<LocalizedTextUI>() != null)
//            {
//                continue;
//            }

//            string originalText = textComponent.text;
//            if (string.IsNullOrEmpty(originalText))
//            {
//                continue;
//            }

//            // Genera una clave única basada en el nombre del objeto y su texto
//            // Ejemplo: "MainMenu/PlayButton/Jugar" -> se convierte en "mainmenu_playbutton"
//            string key = $"{textComponent.gameObject.scene.name}_{textComponent.transform.name}".ToLower().Replace(" ", "_");

//            // Añade la entrada a la tabla si no existe
//            var table = stringTableCollection.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable;
//            if (table.GetEntry(key) == null)
//            {
//                table.AddEntry(key, originalText);
//                EditorUtility.SetDirty(table); // Marca la tabla como modificada para que se guarde
//                newEntries++;
//            }

//            // Añade el componente marcador y le asigna la clave
//            var localizedComponent = textComponent.gameObject.AddComponent<LocalizedTextUI>();
//            localizedComponent.localizationKey = key;
//        }

//        Debug.Log($"Localization Gatherer Finished. Added {newEntries} new entries to the string table.");
//    }
//}
