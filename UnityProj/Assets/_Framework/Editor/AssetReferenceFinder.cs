using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// 最小可用的资源引用反查工具：基于 GUID 扫描 YAML 文本，查找谁引用了目标资源。
    /// 适用于 .unity / .prefab / .asset / .mat / .controller 等文本序列化资产。
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
        };

        public static List<string> FindReferencers(Object target)
        {
            if (target == null)
                return new List<string>();

            string assetPath = AssetDatabase.GetAssetPath(target);
            return FindReferencers(assetPath);
        }

        public static List<string> FindReferencers(string assetPath)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(assetPath))
                return results;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                return results;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
                return results;

            var visited = new HashSet<string>();
            foreach (string pattern in SearchPatterns)
            {
                string[] files = Directory.GetFiles(Application.dataPath, pattern, SearchOption.AllDirectories);
                foreach (string fullPath in files)
                {
                    string normalized = fullPath.Replace('\\', '/');
                    string relativePath = "Assets" + normalized.Substring(Application.dataPath.Length);
                    if (relativePath == assetPath || !visited.Add(relativePath))
                        continue;

                    string text = File.ReadAllText(fullPath);
                    if (text.Contains(guid))
                        results.Add(relativePath);
                }
            }

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
            List<string> referencers = FindReferencers(assetPath);

            if (referencers.Count == 0)
            {
                Debug.Log($"[AssetReferenceFinder] 未找到引用：{assetPath}");
                return;
            }

            Debug.Log($"[AssetReferenceFinder] 目标资源：{assetPath}\n引用者数量：{referencers.Count}\n- " + string.Join("\n- ", referencers));
        }
    }
}
