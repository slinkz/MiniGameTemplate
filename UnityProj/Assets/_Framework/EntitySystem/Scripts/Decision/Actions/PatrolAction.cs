using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// AI Action: 巡逻——随机选巡逻点→移向→到达后等待→再选新点。
    /// 有状态：维护当前目标点和等待计时器。
    /// </summary>
    public sealed class PatrolAction : IAIAction
    {
        private enum PatrolState : byte
        {
            Moving,
            Waiting
        }

        private PatrolState _state;
        private Vector2 _patrolTarget;
        private float _waitTimer;
        private float _patrolRadius;
        private float _waitDuration;
        private Vector2 _spawnPosition;

        // 配置常量（Phase 2 可从 ActionParam 读取）
        private const float DefaultPatrolRadius = 3f;
        private const float DefaultWaitDuration = 1f;
        private const float ArrivalThreshold = 0.2f;

        public void Enter(Entity owner)
        {
            _spawnPosition = owner.Position;
            _patrolRadius = DefaultPatrolRadius;
            _waitDuration = DefaultWaitDuration;
            _state = PatrolState.Moving;
            PickNewTarget();
        }

        public DecisionCommand Execute(Entity owner, float dt)
        {
            switch (_state)
            {
                case PatrolState.Moving:
                    Vector2 toTarget = _patrolTarget - owner.Position;
                    float dist = toTarget.magnitude;
                    if (dist <= ArrivalThreshold)
                    {
                        // 到达，进入等待
                        _state = PatrolState.Waiting;
                        _waitTimer = _waitDuration;
                        return DecisionCommand.Idle;
                    }
                    Vector2 dir = toTarget / dist;
                    return new DecisionCommand
                    {
                        MoveDirection = dir,
                        WantsAttack = false,
                        AimDirection = dir
                    };

                case PatrolState.Waiting:
                    _waitTimer -= dt;
                    if (_waitTimer <= 0f)
                    {
                        PickNewTarget();
                        _state = PatrolState.Moving;
                    }
                    return DecisionCommand.Idle;

                default:
                    return DecisionCommand.Idle;
            }
        }

        public void Exit(Entity owner)
        {
            _state = PatrolState.Moving;
            _waitTimer = 0f;
        }

        private void PickNewTarget()
        {
            // 在 spawnPosition 周围随机选一个点
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0.5f, _patrolRadius);
            _patrolTarget = _spawnPosition + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }
    }
}
