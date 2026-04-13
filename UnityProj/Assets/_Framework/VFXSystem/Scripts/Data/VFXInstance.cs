using UnityEngine;

namespace MiniGameTemplate.VFX
{
    /// <summary>
    /// Sprite Sheet 特效运行时实例数据。
    /// 单槽位只负责一个特效实例。
    /// </summary>
    public struct VFXInstance
    {
        public Vector3 Position;
        public Color32 Color;
        public float RotationDegrees;
        public float Scale;
        public float Elapsed;
        public ushort TypeIndex;
        public byte CurrentFrame;
        public byte Flags;

        /// <summary>附着源 ID（0=世界空间/无附着，>0=跟随 AttachSourceRegistry 的源）</summary>
        public byte AttachSourceId;

        public const byte FLAG_ACTIVE = 1 << 0;
        public const byte FLAG_PLAY_ONCE = 1 << 1;
        /// <summary>源失效后冻结在最后有效位置，播放到完（ADR-021）</summary>
        public const byte FLAG_FROZEN = 1 << 2;

        public bool IsActive => (Flags & FLAG_ACTIVE) != 0;
    }
}
