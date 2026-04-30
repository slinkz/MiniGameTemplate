using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI 行为配置资产。策划在 Inspector 中按优先级配置条件-动作表。
    /// 路径：Assets/_Game/Configs/AI/
    /// v2.4 新增（GD-R4-002/010）。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAIBehavior", menuName = "Entity/AIBehavior")]
    public class AIBehaviorSO : ScriptableObject
    {
        [Tooltip("按优先级排列的条件-动作表（索引越小优先级越高）")]
        public AIBehaviorEntry[] Entries;
    }

    [System.Serializable]
    public struct AIBehaviorEntry
    {
        [Tooltip("条件类型")]
        public AIConditionType Condition;

        [Tooltip("条件参数（如距离阈值、HP 百分比 0.0~1.0）")]
        public float ConditionParam;

        [Tooltip("匹配后执行的 Action")]
        public AIActionType Action;

        [Tooltip("动作参数（如巡逻半径、逃跑距离）")]
        public float ActionParam;
    }

    public enum AIConditionType : byte
    {
        Always = 0,             // 无条件匹配（兜底）
        HpBelow = 1,            // HP 百分比低于 ConditionParam（0.0~1.0）
        TargetInRange = 2,      // 目标在 ConditionParam 距离内
        TargetLost = 3,         // 无目标 / 目标超出检测范围
        // Phase 2 扩展：HpAbove, AllyCountBelow, WaveIndex, ...
    }

    public enum AIActionType : byte
    {
        Idle = 0,
        MoveToTarget = 1,
        Attack = 2,
        Flee = 3,
        Patrol = 4,
        // Phase 2 扩展：Guard, Retreat, ...
    }
}
