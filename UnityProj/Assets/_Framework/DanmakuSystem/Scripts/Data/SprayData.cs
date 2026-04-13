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

        /// <summary>
        /// AttachSourceRegistry 中的挂载源 ID。
        /// 0 = 未挂载（Detached），喷雾发射后固定不动。
        /// &gt;0 = 挂载（Attached），每帧自动同步 Origin 和 Direction。
        /// </summary>
        public byte AttachId;

        /// <summary>喷雾阵营（从 SprayTypeSO.Faction 拷贝）</summary>
        public byte Faction;

        /// <summary>附着 VFX 的 slot index（-1=无）</summary>
        public int VfxSlot;
    }
}
