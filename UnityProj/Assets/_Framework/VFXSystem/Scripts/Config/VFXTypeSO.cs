using MiniGameTemplate.Rendering;
using UnityEngine;
using UnityEngine.Serialization;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// Sprite Sheet 特效类型配置。
    /// 共享运行时配置全部存在 SO 中，便于设计师直接调整。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/VFX/VFX Type")]
    public class VFXTypeSO : ScriptableObject
    {
        // ──── 统一资源描述（Phase 1.6） ────

        [Header("资源描述（统一）")]
        [Tooltip("源贴图——每个 VFX 类型可引用独立贴图")]
        public Texture2D SourceTexture;

        [Tooltip("在 SourceTexture 上的 UV 区域（归一化）")]
        [FormerlySerializedAs("AtlasUV")]
        public Rect UVRect = new Rect(0f, 0f, 1f, 1f);

        /// <summary>资源描述版本号，用于迁移器</summary>
        [HideInInspector] public int SchemaVersion = 1;

        [Header("Atlas 绑定（可选优化）")]
        [Tooltip("绑定的 Atlas 映射。null = 使用 SourceTexture 独立模式。ADR-017：Atlas 为可逆派生产物。")]
        public AtlasMappingSO AtlasBinding;

        // ──── Sprite Sheet 配置 ────

        [Header("Sprite Sheet")]
        [Min(1)]
        [Tooltip("横向帧数。")]
        public int Columns = 4;

        [Min(1)]
        [Tooltip("纵向帧数。")]
        public int Rows = 4;

        [Min(1)]
        [Tooltip("有效帧数。小于 Columns * Rows 时，只播放前 TotalFrames 帧。")]
        public int TotalFrames = 16;

        [Min(1f)]
        [Tooltip("每秒播放帧数。")]
        public float FramesPerSecond = 16f;

        [Header("播放")]
        [Tooltip("是否循环播放。关闭时播放完自动回收。")]
        public bool Loop;

        [Tooltip("播放时是否朝朝向旋转。")]
        public bool RotateWithInstance;

        [Header("渲染")]
        [Tooltip("特效尺寸（世界单位）。")]
        public Vector2 Size = new Vector2(1f, 1f);

        [Tooltip("默认颜色。")]
        public Color Tint = Color.white;

        [Header("附着模式（ADR-013）")]
        [Tooltip("World=世界空间固定位置, FollowTarget=跟随附着源")]
        public VFXAttachMode AttachMode = VFXAttachMode.World;

        public int MaxFrameCount => Mathf.Max(1, Mathf.Min(TotalFrames, Columns * Rows));
        public float Duration => MaxFrameCount / Mathf.Max(1f, FramesPerSecond);

        // ──── Atlas 解析辅助 ────

        /// <summary>
        /// 解析实际使用的贴图。优先级：AtlasBinding.AtlasTexture > SourceTexture。
        /// </summary>
        public Texture2D GetResolvedTexture()
        {
            if (AtlasBinding != null && AtlasBinding.AtlasTexture != null)
                return AtlasBinding.AtlasTexture;
            return SourceTexture;
        }

        /// <summary>
        /// 解析基础 UV 区域。
        /// Atlas 模式下优先从映射表查找 SourceTexture 的子区域；
        /// 若 SourceTexture 为空或映射中找不到，则 fallback 到手动配置的 UVRect。
        /// </summary>
        public Rect GetResolvedBaseUV()
        {
            if (AtlasBinding != null && AtlasBinding.AtlasTexture != null
                && SourceTexture != null
                && AtlasBinding.TryFindEntry(SourceTexture, out var entry))
            {
                return entry.UVRect;
            }
            return UVRect;
        }

        // ──── 序列帧辅助方法 ────

        /// <summary>
        /// 根据帧索引计算该帧在指定 baseUV 内的子区域。
        /// </summary>
        public Rect GetFrameUV(int frameIndex, Rect baseUV)
        {
            int cols = Mathf.Max(1, Columns);
            int rows = Mathf.Max(1, Rows);
            int clampedFrame = Mathf.Clamp(frameIndex, 0, MaxFrameCount - 1);
            int x = clampedFrame % cols;
            int y = clampedFrame / cols;
            float fw = baseUV.width / cols;
            float fh = baseUV.height / rows;
            return new Rect(baseUV.x + x * fw, baseUV.y + y * fh, fw, fh);
        }

        /// <summary>
        /// 根据帧索引计算该帧在 UVRect 内的子区域（无 Atlas 绑定的兼容方法）。
        /// </summary>
        public Rect GetFrameUV(int frameIndex)
        {
            return GetFrameUV(frameIndex, UVRect);
        }
    }
}
