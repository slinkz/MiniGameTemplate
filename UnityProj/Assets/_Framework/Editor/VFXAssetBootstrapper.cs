#if UNITY_EDITOR
using System.IO;
using MiniGameTemplate.VFX;
using UnityEditor;
using UnityEngine;

namespace MiniGameTemplate.EditorTools
{
    /// <summary>
    /// 阶段2：一键生成 VFX Demo 所需的最小资产。
    /// 先用程序生成共享图集，验证系统闭环；后续可替换成 AI 贴图。
    /// </summary>
    public static class VFXAssetBootstrapper
    {
        private const string Root = "Assets/_Example/VFXDemo";
        private const string TextureFolder = Root + "/Textures";
        private const string MaterialFolder = Root + "/Materials";
        private const string ConfigFolder = Root + "/Config";
        private const string TypeFolder = Root + "/Type";

        [MenuItem("Tools/MiniGame Template/VFX/Create Stage2 Demo Assets", false, 150)]
        private static void CreateStage2DemoAssets()
        {
            EnsureFolders();

            Texture2D atlas = CreateExplosionAtlas();
            Material normalMat = CreateMaterial("Mat_VFX_Normal", "MiniGameTemplate/Danmaku/Bullet", atlas);
            VFXRenderConfig renderConfig = CreateRenderConfig(normalMat, atlas);
            VFXTypeSO type = CreateExplosionType();

            // ADR-030: VFXTypeRegistry 已降级为运行时类，不再需要创建 Registry .asset。
            // TypeSO 在运行时首次使用时自动注册。

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = type;
            EditorGUIUtility.PingObject(type);
            Debug.Log("[VFXAssetBootstrapper] Stage2 Demo assets created (ADR-030: Registry is now runtime-only).");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "_Example");
            EnsureFolder("Assets/_Example", "VFXDemo");
            EnsureFolder(Root, "Textures");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Config");
            EnsureFolder(Root, "Type");
            EnsureFolder(Root, "Registry");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static Texture2D CreateExplosionAtlas()
        {
            const int frameSize = 128;
            const int columns = 4;
            const int rows = 4;
            int width = frameSize * columns;
            int height = frameSize * rows;
            string assetPath = TextureFolder + "/Tex_VFX_ExplosionAtlas.png";
            string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Tex_VFX_ExplosionAtlas"
            };

            var pixels = new Color32[width * height];
            for (int frame = 0; frame < columns * rows; frame++)
            {
                int frameX = frame % columns;
                int frameY = rows - 1 - (frame / columns);
                PaintFrame(pixels, width, frameX * frameSize, frameY * frameSize, frameSize, frame);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes(absolutePath, png);
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void PaintFrame(Color32[] pixels, int texWidth, int startX, int startY, int size, int frame)
        {
            float t = frame / 15f;
            float innerRadius = Mathf.Lerp(size * 0.10f, size * 0.22f, t);
            float outerRadius = Mathf.Lerp(size * 0.18f, size * 0.48f, t);
            float smokeRadius = Mathf.Lerp(size * 0.20f, size * 0.42f, t);
            Color core = Color.Lerp(new Color(1f, 1f, 0.85f, 1f), new Color(1f, 0.5f, 0.1f, 0.35f), t);
            Color ring = Color.Lerp(new Color(1f, 0.8f, 0.2f, 0.9f), new Color(0.9f, 0.2f, 0.05f, 0.1f), t);
            Color smoke = new Color(0.25f, 0.25f, 0.25f, Mathf.Lerp(0.0f, 0.35f, t));
            Vector2 center = new Vector2(startX + size * 0.5f, startY + size * 0.5f);

            for (int y = startY; y < startY + size; y++)
            {
                for (int x = startX; x < startX + size; x++)
                {
                    float dx = x + 0.5f - center.x;
                    float dy = y + 0.5f - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float noise = Mathf.Sin(angle * 6f + t * 10f) * 0.06f * size;
                    float warpedOuter = outerRadius + noise;

                    Color color = Color.clear;
                    if (dist <= innerRadius)
                    {
                        float a = 1f - dist / Mathf.Max(1f, innerRadius);
                        color = core * a;
                    }
                    else if (dist <= warpedOuter)
                    {
                        float a = 1f - Mathf.InverseLerp(innerRadius, warpedOuter, dist);
                        color = ring * a;
                    }
                    else if (dist <= smokeRadius)
                    {
                        float a = 1f - Mathf.InverseLerp(warpedOuter, smokeRadius, dist);
                        color = smoke * a;
                    }

                    pixels[y * texWidth + x] = AlphaBlend(pixels[y * texWidth + x], color);
                }
            }
        }

        private static Color32 AlphaBlend(Color32 dst, Color src)
        {
            float srcA = src.a;
            float outA = srcA + dst.a / 255f * (1f - srcA);
            if (outA <= 0.0001f)
                return new Color32(0, 0, 0, 0);

            float dstA = dst.a / 255f;
            float r = (src.r * srcA + (dst.r / 255f) * dstA * (1f - srcA)) / outA;
            float g = (src.g * srcA + (dst.g / 255f) * dstA * (1f - srcA)) / outA;
            float b = (src.b * srcA + (dst.b / 255f) * dstA * (1f - srcA)) / outA;
            return new Color(r, g, b, outA);
        }

        private static Material CreateMaterial(string assetName, string shaderName, Texture2D atlas)
        {
            string path = MaterialFolder + "/" + assetName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError($"[VFXAssetBootstrapper] Shader not found: {shaderName}");
                    return null;
                }

                material = new Material(shader) { name = assetName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = atlas;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static VFXRenderConfig CreateRenderConfig(Material normalMat, Texture2D atlas)
        {
            string path = ConfigFolder + "/VFXRenderConfig_Demo.asset";
            VFXRenderConfig config = AssetDatabase.LoadAssetAtPath<VFXRenderConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<VFXRenderConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            config.NormalMaterial = normalMat;
            config.AtlasTexture = atlas;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static VFXTypeSO CreateExplosionType()
        {
            string path = TypeFolder + "/VFXType_Explosion_Test.asset";
            VFXTypeSO type = AssetDatabase.LoadAssetAtPath<VFXTypeSO>(path);
            if (type == null)
            {
                type = ScriptableObject.CreateInstance<VFXTypeSO>();
                AssetDatabase.CreateAsset(type, path);
            }

            type.UVRect = new Rect(0f, 0f, 1f, 1f);
            type.Columns = 4;
            type.Rows = 4;
            type.FramesPerSecond = 24f;
            type.Loop = false;
            type.Size = new Vector2(1.6f, 1.6f);
            type.Tint = Color.white;
            type.RotateWithInstance = false;
            EditorUtility.SetDirty(type);
            return type;
        }

    }
}
#endif
