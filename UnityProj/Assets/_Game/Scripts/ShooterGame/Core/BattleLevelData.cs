using System;
using MiniGameTemplate.Navigation;

namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗节点的导航数据 — 携带关卡索引。
    /// [Serializable] 支持热重载恢复 + 栈序列化（Phase 4）。
    /// </summary>
    [Serializable]
    public class BattleLevelData : IFlowData
    {
        /// <summary>关卡索引（0-based）。</summary>
        public int LevelIndex;

        public override string ToString() => $"BattleLevelData(Level={LevelIndex})";
    }
}
