using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸冷数据（残影拖尾）。
    /// 与 BulletCore 同索引对齐，仅渲染时读取。sizeof = 28 bytes。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BulletTrail
    {
        /// <summary>上 1 帧位置（残影拖尾用）</summary>
        public Vector2 PrevPos1;

        /// <summary>上 2 帧位置</summary>
        public Vector2 PrevPos2;

        /// <summary>上 3 帧位置</summary>
        public Vector2 PrevPos3;

        /// <summary>残影数量：0=无, 1-3</summary>
        public byte TrailLength;

        /// <summary>受伤闪烁剩余帧数（0=不闪烁）。由 CollisionSolver 写入，BulletRenderer 读取并递减。</summary>
        public byte FlashTimer;

        /// <summary>TrailPool 句柄（FLAG_HEAVY_TRAIL 弹丸用）。-1 = 未分配。</summary>
        public short HeavyTrailHandle;
    }
}
