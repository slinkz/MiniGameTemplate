using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action: 朝远离目标方向移动。无状态。
    /// </summary>
    public sealed class FleeAction : IAIAction
    {
        public void Enter(Entity owner) { }

        public DecisionCommand Execute(Entity owner, float dt)
        {
            var autoAim = owner.GetComponent(ComponentType.AutoAim);
            Vector2 fleeDir;

            if (autoAim != null && autoAim is ITargetProvider tp && tp.HasTarget)
            {
                Vector2 fromTarget = owner.Position - tp.TargetPosition;
                float dist = fromTarget.magnitude;
                fleeDir = dist > 0.01f ? fromTarget / dist : Vector2.right;
            }
            else
            {
                // 无目标时朝反方向跑
                float rad = owner.Rotation * Mathf.Deg2Rad;
                fleeDir = new Vector2(-Mathf.Cos(rad), -Mathf.Sin(rad));
            }

            return new DecisionCommand
            {
                MoveDirection = fleeDir,
                WantsAttack = false,
                AimDirection = -fleeDir
            };
        }

        public void Exit(Entity owner) { }
    }
}
