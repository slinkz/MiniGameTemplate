using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// 资源引用反查工具：基于 GUID 扫描 YAML 文本资产，查找谁引用了目标资源。
    /// 支持 .unity / .prefab / .asset / .mat / .controller / .anim / .overrideController / .playable / .sbn
    /// 
    /// 用法：
    ///   1. 在 Project 视图中选中资源
    ///   2. 菜单 Tools/MiniGameTemplate/Find References Of Selected Asset
    ///   3. 结果输出到 Console，每条可点击跳转
    /// 
    /// 也可代码调用：AssetReferenceFinder.FindReferencers(target)
    /// 
    /// Changelog:
    ///   v1.0 (2026-04-21) — 最小可用版本
    ///   v1.1 (2026-04-22) — 加进度条、菜单验证、可点击输出、扩展搜索格式
    /// </summary>
    public static class AssetReferenceFinder
    {
        private static readonly string[] SearchPatterns =
        {
            "*.unity",
            "*.prefab",
            "*.asset",
            "*.mat",
            "*.controller",
            "*.overrideController",
            "*.anim",
            "*.playable",
            "*.sbn",
        };

        /// <summary>
        /// 查找引用了目标资产的所有文本序列化资产路径。
        /// </summary>
        public static List<string> FindReferencers(Object target)
        {
            if (target == null)
                return new List<string>();

            string assetPath = AssetDatabase.GetAssetPath(target);
            return FindReferencers(assetPath);
        }

        /// <summary>
        /// 查找引用了目标资产（按路径）的所有文本序列化资产路径。
        /// </summary>
        public static List<string> FindReferencers(string assetPath, bool showProgress = false)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(assetPath))
                return results;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return results;

            // 收集所有待扫描文件
            var filesToScan = new List<string>();
            var visited = new HashSet<string>();

            foreach (string pattern in SearchPatterns)
            {
                string[] files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);
                foreach (string fullPath in files)
                {
                    string normalized = fullPath.Replace('\\', '/');
                    string relativePath = "Assets" + normalized.Substring(Application.dataPath.Length);
                    if (relativePath != assetPath && visited.Add(relativePath))
                        filesToScan.Add(fullPath);
                }
            }

            // 扫描并匹配 GUID
            int total = filesToScan.Count;
            for (int i = 0; i < total; i++)
            {
                string fullPath = filesToScan[i];

                if (showProgress && i % 50 == 0)
                {
                    string normalized = fullPath.Replace('\\', '/');
                    string relativePath = "Assets" + normalized.Substring(Application.dataPath.Length);
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "查找引用中...",
                            $"[{i + 1}/{total}] {relativePath}",
                            (float)i / total))
                    {
                        EditorUtility.ClearProgressBar();
                        Debug.LogWarning("[AssetReferenceFinder] 用户取消了搜索。");
                        return results;
                    }
                }

                string text = File.ReadAllText(fullPath);
                if (text.Contains(guid))
                {
                    string norm = fullPath.Replace('\\', '/');
                    results.Add("Assets" + norm.Substring(Application.dataPath.Length));
                }
            }

            if (showProgress)
                EditorUtility.ClearProgressBar();

            results.Sort();
            return results;
        }

        [MenuItem("Tools/MiniGameTemplate/Find References Of Selected Asset")]
        private static void FindSelectedAssetReferences()
        {
            Object target = Selection.activeObject;
            if (target == null)
            {
                Debug.LogWarning("[AssetReferenceFinder] 请先在 Project 视图中选中一个资源。");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(target);
            List<string> referencers = FindReferencers(assetPath, showProgress: true);

            if (referencers.Count == 0)
            {
                Debug.Log($"[AssetReferenceFinder] 未找到引用：{assetPath}");
                return;
            }

            // 汇总日志
            Debug.Log($"[AssetReferenceFinder] 目标资源：{assetPath}\n引用者数量：{referencers.Count}");

            // 每条引用单独输出，点击可跳转到对应资产
            foreach (string refPath in referencers)
            {
                Object refAsset = AssetDatabase.LoadAssetAtPath<Object>(refPath);
                if (refAsset != null)
                    Debug.Log($"  → {refPath}", refAsset);
                else
                    Debug.Log($"  → {refPath}");
            }
        }

        [MenuItem("Tools/MiniGameTemplate/Find References Of Selected Asset", true)]
        private static bool ValidateFindSelectedAssetReferences()
        {
            return Selection.activeObject != null
                && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(Selection.activeObject));
        }
    }
}
