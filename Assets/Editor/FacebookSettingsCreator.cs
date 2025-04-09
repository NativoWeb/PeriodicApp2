//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using Facebook.Unity.Settings;

//public class FacebookSettingsCreator
//{
//    [MenuItem("Facebook/Create Settings")]
//    public static void Create()
//    {
//        var instance = FacebookSettings.Instance;
//        EditorUtility.SetDirty(instance);
//        AssetDatabase.SaveAssets();
//        Debug.Log("FacebookSettings.asset creado en Resources/");
//    }
//}
//#endif