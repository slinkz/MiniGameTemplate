#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// Optional Spine integration controls.
    /// Keeps Spine integration opt-in via scripting define symbols.
    /// </summary>
    public static class SpineIntegrationTools
    {
        private const string MenuRoot = "Tools/MiniGame Template/Integrations/Spine/";
        private const string FairyGuiSpineDefine = "FAIRYGUI_SPINE";
        private const string ProjectSpineDefine = "ENABLE_SPINE";

        private const string SpineUnityLinkPath = "Assets/Spine";
        private const string SpineCSharpLinkPath = "Assets/SpineCSharp";

        [MenuItem(MenuRoot + "Enable Spine (Current Target)", false, 500)]
        private static void EnableSpineForCurrentTarget()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown)
            {
                Debug.LogError("[SpineIntegration] Unknown build target group. Switch to a valid target and retry.");
                return;
            }

            var defines = GetDefines(group);
            bool changed = false;
            changed |= AddDefine(defines, FairyGuiSpineDefine);
            changed |= AddDefine(defines, ProjectSpineDefine);

            if (!changed)
            {
                Debug.Log($"[SpineIntegration] Defines already enabled for {group}: {FairyGuiSpineDefine}, {ProjectSpineDefine}");
                return;
            }

            SetDefines(group, defines);
            Debug.Log($"[SpineIntegration] Enabled Spine defines for {group}: {string.Join(",", defines.OrderBy(x => x))}");
            ValidateIntegration();
        }

        [MenuItem(MenuRoot + "Enable Spine (Current Target)", true)]
        private static bool ValidateEnableSpineForCurrentTarget()
        {
            return EditorUserBuildSettings.selectedBuildTargetGroup != BuildTargetGroup.Unknown;
        }

        [MenuItem(MenuRoot + "Disable Spine (Current Target)", false, 501)]
        private static void DisableSpineForCurrentTarget()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown)
            {
                Debug.LogError("[SpineIntegration] Unknown build target group. Switch to a valid target and retry.");
                return;
            }

            var defines = GetDefines(group);
            bool changed = false;
            changed |= RemoveDefine(defines, FairyGuiSpineDefine);
            changed |= RemoveDefine(defines, ProjectSpineDefine);

            if (!changed)
            {
                Debug.Log($"[SpineIntegration] Spine defines already disabled for {group}.");
                return;
            }

            SetDefines(group, defines);
            Debug.Log($"[SpineIntegration] Disabled Spine defines for {group}: {string.Join(",", defines.OrderBy(x => x))}");
        }

        [MenuItem(MenuRoot + "Validate Integration", false, 510)]
        public static void ValidateIntegration()
        {
            bool hasSpineLinks = AssetDatabase.IsValidFolder(SpineUnityLinkPath) && AssetDatabase.IsValidFolder(SpineCSharpLinkPath);
            bool hasSpineAsmdef = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Spine/Runtime/spine-unity.asmdef") != null
                                 && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/SpineCSharp/spine-csharp.asmdef") != null;
            bool runtimeAssemblyLoaded = Type.GetType("Spine.Unity.SkeletonDataAsset, spine-unity") != null;

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            bool fairyGuiSpineEnabled = group != BuildTargetGroup.Unknown && HasDefine(group, FairyGuiSpineDefine);
            bool projectSpineEnabled = group != BuildTargetGroup.Unknown && HasDefine(group, ProjectSpineDefine);
            bool anyDefineEnabled = fairyGuiSpineEnabled || projectSpineEnabled;

            if (!hasSpineLinks)
            {
                Debug.LogWarning("[SpineIntegration] Spine source links not found. Run UnityProj/Tools/setup_spine.bat or setup_spine.sh first.");
            }

            if (!hasSpineAsmdef)
            {
                Debug.LogWarning("[SpineIntegration] Spine asmdef files not found under Assets/Spine or Assets/SpineCSharp.");
            }

            if (fairyGuiSpineEnabled != projectSpineEnabled)
            {
                Debug.LogWarning($"[SpineIntegration] Define mismatch on {group}: keep {FairyGuiSpineDefine} and {ProjectSpineDefine} enabled/disabled together.");
            }

            if (anyDefineEnabled && (!hasSpineLinks || !hasSpineAsmdef))
            {
                Debug.LogError($"[SpineIntegration] Spine define(s) enabled, but source is incomplete. Disable {FairyGuiSpineDefine}/{ProjectSpineDefine} or fix setup_spine links.");
            }
            else if (anyDefineEnabled && !runtimeAssemblyLoaded)
            {
                Debug.LogWarning("[SpineIntegration] Spine define(s) enabled, but spine-unity assembly is not loaded yet. Wait for recompile/import.");
            }
            else if (anyDefineEnabled)
            {
                Debug.Log("[SpineIntegration] Validation OK: Spine runtime detected and integration is enabled.");
            }
            else
            {
                Debug.Log("[SpineIntegration] Validation OK: Spine integration is currently optional/disabled.");
            }

        }

        private static bool HasDefine(BuildTargetGroup group, string define)
        {
            var defines = GetDefines(group);
            return defines.Contains(define);
        }

        private static HashSet<string> GetDefines(BuildTargetGroup group)
        {
#if UNITY_2021_2_OR_NEWER
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            var raw = PlayerSettings.GetScriptingDefineSymbols(named);
#else
            var raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(raw))
                return set;

            var parts = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var token = p.Trim();
                if (!string.IsNullOrEmpty(token))
                    set.Add(token);
            }

            return set;
        }

        private static void SetDefines(BuildTargetGroup group, HashSet<string> defines)
        {
            var raw = string.Join(";", defines.OrderBy(x => x));
#if UNITY_2021_2_OR_NEWER
            var named = NamedBuildTarget.FromBuildTargetGroup(group);
            PlayerSettings.SetScriptingDefineSymbols(named, raw);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, raw);
#endif
        }

        private static bool AddDefine(HashSet<string> defines, string define)
        {
            return defines.Add(define);
        }

        private static bool RemoveDefine(HashSet<string> defines, string define)
        {
            return defines.Remove(define);
        }
    }
}
#endif
