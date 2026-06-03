using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.Editor.Entity
{
    /// <summary>
    /// P1.11: 通过代码创建 Debug View Prefab + 伤害数字 Prefab。
    /// 省去手工搭 Prefab 的步骤。
    /// </summary>
    public static class EntityDebugViewPrefabCreator
    {
        private const string PREFAB_DIR = "Assets/_Game/Prefabs/Debug";

        [MenuItem("MiniGameTemplate/Entity/Create Debug View Prefab", false, 201)]
        public static void CreateDebugViewPrefab()
        {
            EnsureDirectoryExists(PREFAB_DIR);

            string prefabPath = $"{PREFAB_DIR}/EntityDebugView.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[Prefab] {prefabPath} 已存在，跳过。");
                return;
            }

            // 创建根 GO
            var root = new GameObject("EntityDebugView");

            // 圆形 SpriteRenderer
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(root.transform);
            spriteGo.transform.localPosition = Vector3.zero;
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = Color.white;
            sr.sortingOrder = 10;

            // HP 文本
            var textGo = new GameObject("HPText");
            textGo.transform.SetParent(root.transform);
            textGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            textGo.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "100/100";
            tm.fontSize = 32;
            tm.characterSize = 0.5f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            // 保存 Prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Prefab] Debug View Prefab 创建完成：{prefabPath}");
            AssetDatabase.SaveAssets();
        }

        // ──────────── 工具方法 ────────────

        /// <summary>创建一个白色圆形 Sprite（代码生成，无外部依赖）</summary>
        private static Sprite CreateCircleSprite()
        {
            // 检查是否已创建
            string spritePath = $"{PREFAB_DIR}/DebugCircle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null) return existing;

            // 生成 32x32 白色圆形纹理
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;
            float radius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    if (dist <= radius)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();

            // 保存 PNG
            byte[] pngBytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(
                System.IO.Path.Combine(Application.dataPath, "../", spritePath), pngBytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

            // 设置 TextureImporter 为 Sprite
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectoryExists(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
