using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 伤害飘字运行时数据。由 DamageNumberSystem 环形缓冲区管理。
    /// </summary>
    public struct DamageNumberData
    {
        /// <summary>当前位置</summary>
        public Vector2 Position;

        /// <summary>向上飘动速度</summary>
        public Vector2 Velocity;

        /// <summary>总生命周期</summary>
        public float Lifetime;

        /// <summary>已过时间</summary>
        public float Elapsed;

        /// <summary>伤害数值</summary>
        public int Damage;

        /// <summary>位数（预计算，运行时不 ToString）</summary>
        public byte DigitCount;

        /// <summary>标记：bit0=暴击, bit1=元素, bit2=治疗</summary>
        public byte Flags;

        /// <summary>缩放（暴击放大）</summary>
        public float Scale;

        /// <summary>颜色</summary>
        public Color32 Color;
    }
}
