using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action: 朝当前锁定目标移动。无状态。
    /// 目标来源：AutoAimComponent 或 CollisionComponent 缓存的 TargetPosition。
    /// Phase 1 简化：从 Entity.Position 出发，朝目标方向移动。
    /// </summary>
    public sealed class MoveToTargetAction : IAIAction
    {
        public void Enter(Entity owner) { }

        public DecisionCommand Execute(Entity owner, float dt)
        {
            // Phase 1：目标位置从 AutoAim 获取（如有），否则朝前方移动
            var autoAim = owner.GetComponent(ComponentType.AutoAim);
            Vector2 targetDir;

            if (autoAim != null && autoAim is ITargetProvider targetProvider && targetProvider.HasTarget)
            {
                Vector2 toTarget = targetProvider.TargetPosition - owner.Position;
                float dist = toTarget.magnitude;
                targetDir = dist > 0.01f ? toTarget / dist : Vector2.zero;
            }
            else
            {
                // 无目标时朝当前朝向前进
                float rad = owner.Rotation * Mathf.Deg2Rad;
                targetDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }

            return new DecisionCommand
            {
                MoveDirection = targetDir,
                WantsAttack = false,
                AimDirection = targetDir
            };
        }

        public void Exit(Entity owner) { }
    }
}
