using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI/Control 决策输出——每帧由 IDecisionMaker 产生，
    /// 驱动 MovementComponent（方向）和 AttackComponent（攻击意图）。
    /// 纯值类型，零 GC。
    /// </summary>
    public struct DecisionCommand
    {
        /// <summary>移动方向（归一化或零向量=停止）</summary>
        public Vector2 MoveDirection;

        /// <summary>是否请求攻击</summary>
        public bool WantsAttack;

        /// <summary>瞄准方向（用于 AttackComponent 发射方向）</summary>
        public Vector2 AimDirection;

        /// <summary>静止不动的默认指令</summary>
        public static readonly DecisionCommand Idle = new DecisionCommand
        {
            MoveDirection = Vector2.zero,
            WantsAttack = false,
            AimDirection = Vector2.zero
        };
    }
}
