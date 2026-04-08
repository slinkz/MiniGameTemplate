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
    }
}
