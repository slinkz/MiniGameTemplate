using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光运行时数据。由 LaserPool 管理，LaserUpdater 每帧更新。
    /// </summary>
    public struct LaserData
    {
        /// <summary>发射点</summary>
        public Vector2 Origin;

        /// <summary>角度（弧度）</summary>
        public float Angle;

        /// <summary>长度</summary>
        public float Length;

        /// <summary>当前宽度（由 WidthOverLifetime 曲线驱动）</summary>
        public float Width;

        /// <summary>最大宽度（来自 LaserTypeSO.MaxWidth）</summary>
        public float MaxWidth;

        /// <summary>已过时间</summary>
        public float Elapsed;

        /// <summary>总生命周期（= ChargeDuration + FiringDuration + FadeDuration）</summary>
        public float Lifetime;

        /// <summary>DPS 计时器（达到 TickInterval 时触发伤害）</summary>
        public float TickTimer;

        /// <summary>伤害间隔（秒）</summary>
        public float TickInterval;

        /// <summary>每次 Tick 伤害量</summary>
        public float DamagePerTick;

        /// <summary>当前阶段：0=未激活, 1=Charging, 2=Firing, 3=Fading</summary>
        public byte Phase;

        /// <summary>LaserTypeSO 在 DanmakuTypeRegistry 中的索引</summary>
        public byte LaserTypeIndex;

        // ──── 挂载跟踪 ────

        /// <summary>
        /// AttachSourceRegistry 中的挂载源 ID。
        /// 0 = 未挂载（Detached），激光发射后固定不动。
        /// &gt;0 = 挂载（Attached），每帧自动同步 Origin 和 Angle。
        /// </summary>
        public byte AttachId;

        /// <summary>激光阵营（从 LaserTypeSO.Faction 拷贝）</summary>
        public byte Faction;

        // ──── 折射 ────

        /// <summary>最大反射次数（来自 LaserTypeSO.MaxReflections）</summary>
        public byte MaxReflections;

        /// <summary>当前帧实际线段数（= 反射次数 + 1，最大 MaxReflections + 1）</summary>
        public byte SegmentCount;

        /// <summary>折射后的线段数组。长度 = MaxReflections + 1，分配时一次性创建。</summary>
        public LaserSegment[] Segments;

        /// <summary>截断/折射后的实际总视觉长度（所有线段长度之和）</summary>
        public float VisualLength;
    }
}
