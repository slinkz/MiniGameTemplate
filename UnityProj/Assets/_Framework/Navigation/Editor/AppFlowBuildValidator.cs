using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MiniGameTemplate.Navigation;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// AppFlow 构建守护。IPreprocessBuildWithReport 确保构建前验证通过。
    /// MenuItem 支持手动验证。（PK ET-003/009）
    /// </summary>
    public class AppFlowBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 10;

        public void OnPreprocessBuild(BuildReport report)
        {
            var errors = Validate();
            if (errors.Count > 0)
            {
                throw new BuildFailedException(
                    $"[AppFlow] 构建验证失败（{errors.Count} 项）：\n" + string.Join("\n", errors));
            }
            Debug.Log("[AppFlow] 构建验证通过。");
        }

        [MenuItem("Tools/AppFlow/Validate Panel Registration")]
        private static void ValidateFromMenu()
        {
            var errors = Validate();
            if (errors.Count == 0)
            {
                Debug.Log("[AppFlow] ✅ 面板注册验证通过，所有 FlowNodeSO 配置正确。");
                EditorUtility.DisplayDialog("AppFlow Validation", "✅ 所有验证通过！", "OK");
            }
            else
            {
                foreach (var err in errors)
                    Debug.LogError($"[AppFlow] {err}");
                EditorUtility.DisplayDialog("AppFlow Validation",
                    $"❌ 发现 {errors.Count} 个问题，查看 Console。", "OK");
            }
        }

        private static List<string> Validate()
        {
            var errors = new List<string>();

            // 1. 收集所有 FlowNodeSO
            var guids = AssetDatabase.FindAssets("t:FlowNodeSO");
            var nodes = guids.Select(g => AssetDatabase.LoadAssetAtPath<FlowNodeSO>(
                AssetDatabase.GUIDToAssetPath(g))).Where(n => n != null).ToList();

            // 2. 收集项目源代码中的 RegisterPanelOpener 调用
            var registeredKeys = new HashSet<string>();
            var csFiles = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            foreach (var guid in csFiles)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs")) continue;
                var content = System.IO.File.ReadAllText(path);

                // 匹配直接字符串字面量: RegisterPanelOpener("XxxPanel", ...)
                var directMatches = Regex.Matches(content, @"RegisterPanelOpener\(\s*""([^""]+)""");
                foreach (Match m in directMatches)
                    registeredKeys.Add(m.Groups[1].Value);

                // 匹配常量引用: RegisterPanelOpener(PanelKey, ...) + const string PanelKey = "..."
                var constRefMatches = Regex.Matches(content, @"RegisterPanelOpener\(\s*([A-Za-z_]\w*)\s*[,)]");
                foreach (Match m in constRefMatches)
                {
                    var varName = m.Groups[1].Value;
                    if (varName.StartsWith("\"")) continue; // 已被上面处理
                    // 查找同文件中的 const 声明
                    var constMatch = Regex.Match(content, @"const\s+string\s+" + Regex.Escape(varName) + @"\s*=\s*""([^""]+)""");
                    if (constMatch.Success)
                        registeredKeys.Add(constMatch.Groups[1].Value);
                }
            }

            // 3. 验证每个 FlowNodeSO
            foreach (var node in nodes)
            {
                // 3a. PanelTypeName 有对应注册
                if (!string.IsNullOrEmpty(node.PanelTypeName) && !registeredKeys.Contains(node.PanelTypeName))
                {
                    errors.Add($"FlowNodeSO '{node.name}': PanelTypeName='{node.PanelTypeName}' 但项目中未找到对应 RegisterPanelOpener 调用。");
                }

                // 3b. RequiredScene 在 Build Settings 中
                if (node.RequiredScene != null)
                {
                    bool inBuild = EditorBuildSettings.scenes.Any(s =>
                        s.enabled && s.path.Contains(node.RequiredScene.SceneName));
                    if (!inBuild)
                    {
                        errors.Add($"FlowNodeSO '{node.name}': RequiredScene='{node.RequiredScene.SceneName}' 不在 Build Settings 中。");
                    }
                }

                // 3c. NodeId 不为空
                if (string.IsNullOrEmpty(node.NodeId))
                {
                    errors.Add($"FlowNodeSO '{node.name}': NodeId 为空，栈序列化将失败。");
                }
            }

            return errors;
        }
    }
}
