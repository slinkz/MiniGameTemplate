using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// Sprite Sheet 特效类型配置。
    /// 共享运行时配置全部存在 SO 中，便于设计师直接调整。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGameTemplate/VFX/VFX Type")]
    public class VFXTypeSO : ScriptableObject
    {
        [Header("贴图")]
        [Tooltip("Sprite Sheet 在共享图集中的 UV 区域。默认整张图。")]
        public Rect AtlasUV = new Rect(0f, 0f, 1f, 1f);

        [Min(1)]
        [Tooltip("横向帧数。")]
        public int Columns = 4;

        [Min(1)]
        [Tooltip("纵向帧数。")]
        public int Rows = 4;

        [Min(1)]
        [Tooltip("有效帧数。小于 Columns × Rows 时，只播放前 TotalFrames 帧。")]
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
        public VFXRenderLayer Layer = VFXRenderLayer.Additive;

        [Header("运行时")]
        [System.NonSerialized]
        public ushort RuntimeIndex;

        public int MaxFrameCount => Mathf.Max(1, Mathf.Min(TotalFrames, Columns * Rows));
        public float Duration => MaxFrameCount / Mathf.Max(1f, FramesPerSecond);
    }
}
