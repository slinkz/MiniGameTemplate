using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action: 触发攻击意图。无状态。
    /// 返回 WantsAttack=true 的 DecisionCommand，由 SkillComponent 消费。
    /// </summary>
    public sealed class AttackAction : IAIAction
    {
        public void Enter(Entity owner) { }

        public DecisionCommand Execute(Entity owner, float dt)
        {
            // 瞄准方向：优先从 AutoAim 获取目标方向
            Vector2 aimDir;
            var autoAim = owner.GetComponent(ComponentType.AutoAim);
            if (autoAim != null && autoAim is ITargetProvider tp && tp.HasTarget)
            {
                Vector2 toTarget = tp.TargetPosition - owner.Position;
                float dist = toTarget.magnitude;
                aimDir = dist > 0.01f ? toTarget / dist : Vector2.right;
            }
            else
            {
                float rad = owner.Rotation * Mathf.Deg2Rad;
                aimDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }

            return new DecisionCommand
            {
                MoveDirection = Vector2.zero, // 攻击时站定
                WantsAttack = true,
                AimDirection = aimDir
            };
        }

        public void Exit(Entity owner) { }
    }
}
