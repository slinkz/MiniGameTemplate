using System;
using MiniGameTemplate.Entity;
using MiniGameTemplate.Navigation;

namespace Game.ShooterGame
{
    /// <summary>
    /// 战斗节点的导航数据 — 携带关卡索引 + 出战装备（V2 Sprint 2）。
    /// [Serializable] 支持热重载恢复 + 栈序列化（Phase 4）。
    /// 
    /// 数据流：SortieBottomSheet → BattleLevelData → BattleController.Init
    /// </summary>
    [Serializable]
    public class BattleLevelData : IFlowData
    {
        /// <summary>关卡索引（0-based）。</summary>
        public int LevelIndex;

        // ── V2 Sprint 2：出战装备 ──

        /// <summary>
        /// 已装备的主动技能列表（最多 6 个，顺序=槽位顺序）。
        /// null 或空 = 使用 EntityConfigSO.SkillConfig 的单技能兜底。
        /// ⚠️ 不可序列化到 JSON（SO 引用），仅用于内存跨场景传参。
        /// </summary>
        [NonSerialized] public SkillConfigSO[] EquippedSkills;

        /// <summary>
        /// 已装备的被动技能列表（最多 3 个）。
        /// V2 Sprint 3：被动走 PassiveComponent CD 驱动。
        /// 内部通过 LinkedBuff 桥接 BuffComponent。
        /// </summary>
        [NonSerialized] public PassiveAbilitySO[] EquippedPassives;

        public override string ToString()
        {
            int skillCount = EquippedSkills?.Length ?? 0;
            int passiveCount = EquippedPassives?.Length ?? 0;
            return $"BattleLevelData(Level={LevelIndex}, Skills={skillCount}, Passives={passiveCount})";
        }
    }
}
