using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 条件-动作表策略——BC-07.4 默认 AI 策略。
    /// 每帧按优先级评估 AIBehaviorSO.Entries，匹配第一个满足条件的 Entry，
    /// 然后执行对应 IAIAction。
    /// 
    /// 关键机制：
    /// - Action 切换时调用 Exit/Enter 保证状态正确
    /// - v2.6 安全网（WF-005）：所有条件未匹配时 fallback 到 IdleAction
    /// </summary>
    public sealed class ConditionActionTableStrategy : IDecisionStrategy
    {
        private Entity _owner;
        private AIBehaviorSO _behaviorSO;

        // Action 实例池（按 AIActionType 索引，避免每帧 new）
        private readonly IAIAction[] _actionPool;
        private IAIAction _currentAction;
        private AIActionType _currentActionType;
        private bool _hasCurrentAction;

        // 安全网 Idle（WF-005）
        private readonly IdleAction _fallbackIdleAction = new IdleAction();

        public ConditionActionTableStrategy()
        {
            // 预分配 Action 池（5 种内置 Action）
            _actionPool = new IAIAction[5];
            _actionPool[(int)AIActionType.Idle] = new IdleAction();
            _actionPool[(int)AIActionType.MoveToTarget] = new MoveToTargetAction();
            _actionPool[(int)AIActionType.Attack] = new AttackAction();
            _actionPool[(int)AIActionType.Flee] = new FleeAction();
            _actionPool[(int)AIActionType.Patrol] = new PatrolAction();
        }

        public void Init(Entity owner)
        {
            _owner = owner;

            // 从 EntityConfigSO 获取 AI 行为配置
            _behaviorSO = owner.ConfigSO != null ? owner.ConfigSO.AIBehavior : null;

            _currentAction = null;
            _currentActionType = AIActionType.Idle;
            _hasCurrentAction = false;
        }

        public DecisionCommand Evaluate(float dt)
        {
            if (_owner == null) return DecisionCommand.Idle;

            // 评估条件-动作表
            AIActionType matchedType = AIActionType.Idle;
            bool matched = false;

            if (_behaviorSO != null && _behaviorSO.Entries != null)
            {
                for (int i = 0; i < _behaviorSO.Entries.Length; i++)
                {
                    ref var entry = ref _behaviorSO.Entries[i];
                    if (EvaluateCondition(entry.Condition, entry.ConditionParam))
                    {
                        matchedType = entry.Action;
                        matched = true;
                        break;
                    }
                }
            }

            // WF-005 安全网
            IAIAction targetAction;
            if (matched)
            {
                int typeIndex = (int)matchedType;
                targetAction = typeIndex < _actionPool.Length ? _actionPool[typeIndex] : _fallbackIdleAction;
            }
            else
            {
                targetAction = _fallbackIdleAction;
                matchedType = AIActionType.Idle;
            }

            // Action 切换
            if (!_hasCurrentAction || matchedType != _currentActionType)
            {
                if (_hasCurrentAction && _currentAction != null)
                {
                    _currentAction.Exit(_owner);
                }
                _currentAction = targetAction;
                _currentActionType = matchedType;
                _hasCurrentAction = true;
                _currentAction.Enter(_owner);
            }

            // 执行当前 Action
            return _currentAction.Execute(_owner, dt);
        }

        public void Reset()
        {
            if (_hasCurrentAction && _currentAction != null)
            {
                _currentAction.Exit(_owner);
            }
            _currentAction = null;
            _hasCurrentAction = false;
            _currentActionType = AIActionType.Idle;
            _owner = null;
            _behaviorSO = null;
        }

        // ──────────────── 条件评估 ────────────────

        private bool EvaluateCondition(AIConditionType condition, float param)
        {
            switch (condition)
            {
                case AIConditionType.Always:
                    return true;

                case AIConditionType.HpBelow:
                    var health = _owner.GetComponent(ComponentType.Health) as HealthComponent;
                    if (health == null) return false;
                    return health.HpRatio < param;

                case AIConditionType.TargetInRange:
                    var aim = _owner.GetComponent(ComponentType.AutoAim);
                    if (aim == null || !(aim is ITargetProvider tp1) || !tp1.HasTarget) return false;
                    return tp1.DistanceToTarget <= param;

                case AIConditionType.TargetLost:
                    var aim2 = _owner.GetComponent(ComponentType.AutoAim);
                    if (aim2 == null || !(aim2 is ITargetProvider tp2)) return true; // 无 AutoAim 视为无目标
                    return !tp2.HasTarget;

                default:
                    return false;
            }
        }
    }
}
