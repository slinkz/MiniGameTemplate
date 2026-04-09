using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸热数据（运动 + 碰撞 + 生命周期）。
    /// 每帧必遍历，sizeof = 36 bytes，2048 颗 × 36 = 72 KB，可完整放入 L2 缓存。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BulletCore
    {
        /// <summary>当前位置</summary>
        public Vector2 Position;       // offset  0, size 8

        /// <summary>速度向量</summary>
        public Vector2 Velocity;       // offset  8, size 8

        /// <summary>最大存活时间（超时即死）</summary>
        public float Lifetime;         // offset 16, size 4

        /// <summary>已过时间（速度曲线采样用）</summary>
        public float Elapsed;          // offset 20, size 4

        /// <summary>碰撞半径</summary>
        public float Radius;           // offset 24, size 4

        /// <summary>BulletTypeSO 在 DanmakuTypeRegistry 中的索引</summary>
        public ushort TypeIndex;       // offset 28, size 2

        /// <summary>生命阶段：Active / Exploding / Dead</summary>
        public byte Phase;             // offset 30, size 1

        /// <summary>剩余生命值（0=死亡，1=单次即死，255=几乎不可摧毁）</summary>
        public byte HitPoints;         // offset 31, size 1

        /// <summary>位标记（8 bits）</summary>
        public byte Flags;             // offset 32, size 1

        /// <summary>阵营：0=Enemy, 1=Player, 2=Neutral</summary>
        public byte Faction;           // offset 33, size 1

        /// <summary>Pierce 碰撞冷却：位掩码，每 bit 对应 TargetRegistry 的一个槽位 (0-15)</summary>
        public ushort PierceHitMask;   // offset 34, size 2

        // ──── Flags 位定义（byte，8 bits） ────

        /// <summary>弹丸激活中</summary>
        public const byte FLAG_ACTIVE = 1 << 0;

        /// <summary>飞行中追踪玩家</summary>
        public const byte FLAG_HOMING = 1 << 1;

        /// <summary>速度随生命周期曲线变化</summary>
        public const byte FLAG_SPEED_CURVE = 1 << 2;

        /// <summary>朝飞行方向旋转（米粒弹等非圆弹丸）</summary>
        public const byte FLAG_ROTATE_TO_DIR = 1 << 3;

        /// <summary>使用 TrailPool 重量拖尾（而非 Mesh 内残影）</summary>
        public const byte FLAG_HEAVY_TRAIL = 1 << 4;

        /// <summary>消亡时触发子弹幕</summary>
        public const byte FLAG_HAS_CHILD = 1 << 5;

        /// <summary>有冷数据 BulletModifier（延迟变速/追踪延迟）</summary>
        public const byte FLAG_HAS_MODIFIER = 1 << 6;

        /// <summary>正在穿透冷却中（防多帧重复伤害）</summary>
        public const byte FLAG_PIERCE_COOLDOWN = 1 << 7;
    }
}
