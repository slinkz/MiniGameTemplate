using System.Runtime.InteropServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸修饰数据（延迟变速 + 追踪延迟 + 运动策略参数）。
    /// 与 BulletCore 同索引对齐，仅带 FLAG_HAS_MODIFIER 的弹丸读取。sizeof = 28 bytes。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BulletModifier
    {
        /// <summary>延迟变速结束时刻 = DelayBeforeAccel</summary>
        public float DelayEndTime;

        /// <summary>延迟期间速度倍率（0=完全静止）</summary>
        public float DelaySpeedScale;

        /// <summary>加速结束时刻 = DelayBeforeAccel + AccelDuration</summary>
        public float AccelEndTime;

        /// <summary>追踪开始时刻（0=立即追踪）。SineWave 复用为振幅。</summary>
        public float HomingStartTime;

        /// <summary>追踪转向速度（度/秒）。SineWave 复用为频率，Spiral 复用为角速度。</summary>
        public float HomingStrength;

        /// <summary>
        /// 初始飞行方向（单位向量）。SineWave/Spiral 用此作基准方向，
        /// 而非 core.Velocity，以免写回 Velocity 导致基准漂移。
        /// 非 Default 运动策略时由 BulletSpawner 初始化。
        /// </summary>
        public Vector2 InitialDirection;
    }
}
