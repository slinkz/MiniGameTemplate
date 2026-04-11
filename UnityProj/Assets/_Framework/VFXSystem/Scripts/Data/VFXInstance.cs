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

        public const byte FLAG_ACTIVE = 1 << 0;
        public const byte FLAG_PLAY_ONCE = 1 << 1;

        public bool IsActive => (Flags & FLAG_ACTIVE) != 0;
    }
}
