using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸冷数据（残影拖尾）。
    /// 与 BulletCore 同索引对齐，仅渲染时读取。sizeof = 30 bytes。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BulletTrail
    {
        /// <summary>最近一次采样位置（残影 1，最近）</summary>
        public Vector2 PrevPos1;

        /// <summary>倒数第 2 次采样位置</summary>
        public Vector2 PrevPos2;

        /// <summary>倒数第 3 次采样位置</summary>
        public Vector2 PrevPos3;

        /// <summary>残影数量：0=无, 1-3</summary>
        public byte TrailLength;

        /// <summary>受伤闪烁剩余帧数（0=不闪烁）。由 CollisionSolver 写入，BulletRenderer 读取并递减。</summary>
        public byte FlashTimer;

        /// <summary>TrailPool 句柄（FLAG_HEAVY_TRAIL 弹丸用）。-1 = 未分配。</summary>
        public short HeavyTrailHandle;

        /// <summary>Ghost 采样帧计数器。每帧 +1，达到 GhostInterval 时移位并归零。</summary>
        public byte GhostFrameCounter;

        /// <summary>已填充的采样位置数量（0~3），避免首次采样前显示初始位置的 ghost。</summary>
        public byte GhostFilledCount;
    }
}
