#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Unified editor menu entry: Tools → MiniGame Template → ...
    /// Centralizes all template-related editor actions.
    /// </summary>
    public static class MenuItems
    {
        private const string MENU_ROOT = "Tools/MiniGame Template/";

        // === Quick Create ===

        [MenuItem(MENU_ROOT + "Create/Game Event", false, 100)]
        private static void CreateGameEvent()
        {
            CreateSOAsset<Events.GameEvent>("NewGameEvent");
        }

        [MenuItem(MENU_ROOT + "Create/Int Variable", false, 101)]
        private static void CreateIntVariable()
        {
            CreateSOAsset<Data.IntVariable>("NewIntVariable");
        }

        [MenuItem(MENU_ROOT + "Create/Float Variable", false, 102)]
        private static void CreateFloatVariable()
        {
            CreateSOAsset<Data.FloatVariable>("NewFloatVariable");
        }

        [MenuItem(MENU_ROOT + "Create/Bool Variable", false, 103)]
        private static void CreateBoolVariable()
        {
            CreateSOAsset<Data.BoolVariable>("NewBoolVariable");
        }

        [MenuItem(MENU_ROOT + "Create/String Variable", false, 104)]
        private static void CreateStringVariable()
        {
            CreateSOAsset<Data.StringVariable>("NewStringVariable");
        }

        [MenuItem(MENU_ROOT + "Create/Pool Definition", false, 120)]
        private static void CreatePoolDefinition()
        {
            CreateSOAsset<Pool.PoolDefinition>("NewPoolDefinition");
        }

        [MenuItem(MENU_ROOT + "Create/FSM State", false, 130)]
        private static void CreateState()
        {
            CreateSOAsset<FSM.State>("NewState");
        }

        // === Validation ===

        [MenuItem(MENU_ROOT + "Validate Architecture", false, 200)]
        private static void ValidateArchitecture()
        {
            ArchitectureValidator.RunValidation();
        }

        // === Utility ===

        [MenuItem(MENU_ROOT + "Open Docs Folder", false, 300)]
        private static void OpenDocsFolder()
        {
            var path = System.IO.Path.GetFullPath("Docs");
            if (System.IO.Directory.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                UnityEngine.Debug.LogWarning("[MenuItems] Docs folder not found.");
        }

        // === Helper ===

        private static void CreateSOAsset<T>(string defaultName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            var path = GetSelectedFolder() + "/" + defaultName + ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static string GetSelectedFolder()
        {
            var selected = Selection.activeObject;
            if (selected != null)
            {
                var path = AssetDatabase.GetAssetPath(selected);
                if (System.IO.Directory.Exists(path))
                    return path;
                if (System.IO.File.Exists(path))
                    return System.IO.Path.GetDirectoryName(path);
            }
            return "Assets";
        }
    }
}
#endif
