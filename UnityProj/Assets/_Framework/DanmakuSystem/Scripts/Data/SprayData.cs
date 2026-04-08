using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 喷雾运行时数据。由 SprayPool 管理，SprayUpdater 每帧更新。
    /// </summary>
    public struct SprayData
    {
        /// <summary>喷射源</summary>
        public Vector2 Origin;

        /// <summary>朝向角度（弧度）</summary>
        public float Direction;

        /// <summary>扇形半角（弧度）</summary>
        public float ConeAngle;

        /// <summary>射程</summary>
        public float Range;

        /// <summary>已过时间</summary>
        public float Elapsed;

        /// <summary>总生命周期</summary>
        public float Lifetime;

        /// <summary>DPS 计时器</summary>
        public float TickTimer;

        /// <summary>伤害间隔（秒）</summary>
        public float TickInterval;

        /// <summary>每次 Tick 伤害量</summary>
        public float DamagePerTick;

        /// <summary>当前阶段：0=未激活, 1=Active, 2=Fading</summary>
        public byte Phase;

        /// <summary>SprayTypeSO 在 DanmakuTypeRegistry 中的索引</summary>
        public byte SprayTypeIndex;
    }
}
