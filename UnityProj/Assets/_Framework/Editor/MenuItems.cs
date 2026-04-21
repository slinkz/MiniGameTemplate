#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Unified editor menu entry: Tools → MiniGame Template → ...
    /// Centralizes non-window editor actions (validation, utility).
    ///
    /// Note: SO creation is handled by SOCreationWizard (Tools → MiniGame Template → SO Creation Wizard).
    /// </summary>
    public static class MenuItems
    {
        private const string MENU_ROOT = "Tools/MiniGame Template/";

        // === Validation ===

        [MenuItem(MENU_ROOT + "Validate/Architecture Check", false, 200)]
        private static void ValidateArchitecture()
        {
            ArchitectureValidator.RunValidation();
        }

        // === Utility ===

        [MenuItem(MENU_ROOT + "Open Docs Folder", false, 300)]
        private static void OpenDocsFolder()
        {
            // Docs/ is at repo root, one level above the Unity project
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../Docs"));
            if (System.IO.Directory.Exists(path))
                EditorUtility.RevealInFinder(path);
            else
                UnityEngine.Debug.LogWarning("[MenuItems] Docs folder not found.");
        }
    }
}
#endif
