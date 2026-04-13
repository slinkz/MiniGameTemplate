using UnityEngine;
using UnityEngine.Serialization;
using MiniGameTemplate.Rendering;

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

        [Tooltip("渲染层。")]
        public RenderLayer Layer = RenderLayer.Additive;

        [Header("附着模式（ADR-013）")]
        [Tooltip("World=世界空间固定位置, FollowTarget=跟随附着源")]
        public VFXAttachMode AttachMode = VFXAttachMode.World;

        [Header("运行时")]
        [System.NonSerialized]
        public ushort RuntimeIndex;

        public int MaxFrameCount => Mathf.Max(1, Mathf.Min(TotalFrames, Columns * Rows));
        public float Duration => MaxFrameCount / Mathf.Max(1f, FramesPerSecond);

        // ──── 序列帧辅助方法 ────

        /// <summary>
        /// 根据帧索引计算该帧在 UVRect 内的子区域。
        /// </summary>
        public Rect GetFrameUV(int frameIndex)
        {
            int cols = Mathf.Max(1, Columns);
            int rows = Mathf.Max(1, Rows);
            int clampedFrame = Mathf.Clamp(frameIndex, 0, MaxFrameCount - 1);
            int x = clampedFrame % cols;
            int y = clampedFrame / cols;
            float fw = UVRect.width / cols;
            float fh = UVRect.height / rows;
            return new Rect(UVRect.x + x * fw, UVRect.y + y * fh, fw, fh);
        }
    }
}
