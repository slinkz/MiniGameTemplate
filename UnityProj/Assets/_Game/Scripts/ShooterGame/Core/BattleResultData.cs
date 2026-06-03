using System.Collections.Generic;

namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗结果值对象——传递给结算面板。
    /// 纯数据，无逻辑，无 Unity 依赖（方便单元测试）。
    /// TDD_04 §S4.4
    /// </summary>
    public sealed class BattleResultData
    {
        /// <summary>是否胜利</summary>
        public bool IsVictory;

        /// <summary>星级评价（0=失败, 1~3）</summary>
        public int Stars;

        /// <summary>关卡索引（0-based）</summary>
        public int LevelIndex;

        /// <summary>总击杀数</summary>
        public int TotalKills;

        /// <summary>战斗持续时间（秒）</summary>
        public float BattleTime;

        /// <summary>获得金币（V3 预留，V2 固定值 0）</summary>
        public int CoinsEarned;

        /// <summary>
        /// 伤害统计快照（冻结副本）。
        /// Key = sourceTag（0=基础攻击, 1~6=技能, 7=反击弹幕, 100+=DOT）。
        /// Value = 该来源累计总伤害。
        /// </summary>
        public Dictionary<int, int> DamageStats;

        /// <summary>战斗结束时基地剩余 HP</summary>
        public int BaseHpRemaining;

        /// <summary>基地最大 HP</summary>
        public int BaseHpMax;

        /// <summary>失败时停在第几波（1-based）</summary>
        public int CurrentWave;

        /// <summary>本关总波数</summary>
        public int TotalWaves;
    }
}
