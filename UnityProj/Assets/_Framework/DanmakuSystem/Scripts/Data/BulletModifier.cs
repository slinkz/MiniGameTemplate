using System.Runtime.InteropServices;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸修饰数据（延迟变速 + 追踪延迟）。
    /// 与 BulletCore 同索引对齐，仅带 FLAG_HAS_MODIFIER 的弹丸读取。sizeof = 16 bytes。
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

        /// <summary>追踪开始时刻（0=立即追踪）</summary>
        public float HomingStartTime;
    }
}
