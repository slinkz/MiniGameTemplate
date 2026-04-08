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

        public byte _pad1;
        public ushort _pad2;
    }
}
