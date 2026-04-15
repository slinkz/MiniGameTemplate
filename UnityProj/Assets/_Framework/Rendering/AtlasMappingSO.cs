using UnityEngine;

namespace MiniGameTemplate.Rendering
{
    /// <summary>
    /// Atlas 映射资产——记录打包后的图集贴图与每张源贴图的 UV 映射。
    /// <para>
    /// ADR-017：Atlas 为可逆派生产物，输出 AtlasTexture + AtlasMapping，不作为源数据真相。
    /// 删除此资产即可回退到独立 SourceTexture + UVRect 模式。
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/Rendering/Atlas Mapping")]
    public class AtlasMappingSO : ScriptableObject
    {
        [Header("图集")]
        [Tooltip("打包生成的图集贴图")]
        public Texture2D AtlasTexture;

        [Header("打包参数")]
        [Tooltip("像素间距")]
        [Min(0)]
        public int Padding = 2;

        [Header("子图映射")]
        public AtlasEntry[] Entries = System.Array.Empty<AtlasEntry>();

        [HideInInspector]
        public int SchemaVersion = 1;

        /// <summary>
        /// 在 Entries 中查找源贴图对应的映射条目。
        /// 优先按 SourceTexture 引用匹配，失败则按 SourceGUID 匹配。
        /// </summary>
        public bool TryFindEntry(Texture2D source, out AtlasEntry entry)
        {
            if (Entries == null)
            {
                entry = default;
                return false;
            }

            // 第一轮：按引用匹配（使用 Unity Object == 重载，正确处理 fake null）
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].SourceTexture != null && Entries[i].SourceTexture == source)
                {
                    entry = Entries[i];
                    return true;
                }
            }

            // 第二轮：按 GUID 匹配（引用丢失时的兜底）
            if (source != null)
            {
#if UNITY_EDITOR
                string sourceGuid = UnityEditor.AssetDatabase.AssetPathToGUID(
                    UnityEditor.AssetDatabase.GetAssetPath(source));
                if (!string.IsNullOrEmpty(sourceGuid))
                {
                    for (int i = 0; i < Entries.Length; i++)
                    {
                        if (Entries[i].SourceGUID == sourceGuid)
                        {
                            entry = Entries[i];
                            return true;
                        }
                    }
                }
#endif
            }

            entry = default;
            return false;
        }

        /// <summary>
        /// 根据 SourceTexture 查找对应的 Atlas 子区域 UV。
        /// 找不到时返回 (0,0,1,1) 全贴图区域。
        /// </summary>
        public Rect GetUVRectForSource(Texture2D source)
        {
            if (TryFindEntry(source, out var entry))
                return entry.UVRect;
            return new Rect(0f, 0f, 1f, 1f);
        }
    }

    /// <summary>
    /// Atlas 中单张源贴图的映射条目。
    /// </summary>
    [System.Serializable]
    public struct AtlasEntry
    {
        [Tooltip("原始独立贴图引用（保持可逆）")]
        public Texture2D SourceTexture;

        [Tooltip("原始贴图的 GUID（引用丢失时的兜底匹配）")]
        public string SourceGUID;

        [Tooltip("在图集中的归一化 UV 区域")]
        public Rect UVRect;

        [Tooltip("在图集中的像素区域（调试/预览用）")]
        public RectInt PixelRect;
    }
}
