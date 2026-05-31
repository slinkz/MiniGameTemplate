using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸热数据（运动 + 碰撞 + 生命周期 + 视觉动画）。
    /// 每帧必遍历，sizeof = 60 bytes（Flags ushort + PierceHitMask ulong）。
    /// DEC-005=C：Mover 每帧写入 AnimScale/AnimAlpha/AnimColor，Renderer 直接读取，零查表。
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

        /// <summary>位标记（16 bits）</summary>
        public ushort Flags;           // offset 32, size 2

        /// <summary>阵营：0=Enemy, 1=Player, 2=Neutral</summary>
        public byte Faction;           // offset 34, size 1

        // 1 byte padding (alignment for uint)

        /// <summary>
        /// 发射者 Entity 的唯一 ID（EntityId.Value）。
        /// 0 = 无发射者（场景脚本/Spawner 直接发射的弹幕）。
        /// 碰撞时通过 EntityManager 反查发射者 Entity，读取 CritRate 等战斗属性。
        /// </summary>
        public uint OwnerEntityId;     // offset 36, size 4

        /// <summary>Pierce 碰撞冷却：位掩码，每 bit 对应 TargetRegistry 的一个槽位 (0-63)</summary>
        public ulong PierceHitMask;    // offset 40, size 8  (ulong 需 8 对齐，跳过 offset 40 刚好对齐)

        // ──── 视觉动画值（DEC-005=C：Mover 写入，Renderer 读取） ────

        /// <summary>动画缩放倍率（默认 1 = 无缩放变化）</summary>
        public float AnimScale;        // offset 48, size 4

        /// <summary>动画透明度倍率（默认 1 = 不透明）</summary>
        public float AnimAlpha;        // offset 52, size 4

        /// <summary>动画颜色叠加（默认白色 = 无变化）</summary>
        public Color32 AnimColor;      // offset 56, size 4
                                       // Total: 60 bytes

        // ──── Flags 位定义（ushort，16 bits） ────

        /// <summary>弹丸激活中</summary>
        public const ushort FLAG_ACTIVE = 1 << 0;

        /// <summary>飞行中追踪玩家</summary>
        public const ushort FLAG_HOMING = 1 << 1;

        /// <summary>速度随生命周期曲线变化</summary>
        public const ushort FLAG_SPEED_CURVE = 1 << 2;

        /// <summary>朝飞行方向旋转（米粒弹等非圆弹丸）</summary>
        public const ushort FLAG_ROTATE_TO_DIR = 1 << 3;

        /// <summary>使用 TrailPool 重量拖尾（而非 Mesh 内残影）</summary>
        public const ushort FLAG_HEAVY_TRAIL = 1 << 4;

        /// <summary>消亡时触发子弹幕</summary>
        public const ushort FLAG_HAS_CHILD = 1 << 5;

        /// <summary>有冷数据 BulletModifier（延迟变速/追踪延迟）</summary>
        public const ushort FLAG_HAS_MODIFIER = 1 << 6;

        /// <summary>正在穿透冷却中（防多帧重复伤害）</summary>
        public const ushort FLAG_PIERCE_COOLDOWN = 1 << 7;

        /// <summary>Buff 运行时穿透覆盖（被动 PA-01：OnHitTarget=Die → Pierce）</summary>
        public const ushort FLAG_PIERCE_OVERRIDE = 1 << 8;
    }
}
