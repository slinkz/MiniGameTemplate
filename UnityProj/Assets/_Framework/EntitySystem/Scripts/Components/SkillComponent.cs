using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 技能组件——CD 管理的主动/被动技能（前摇 → 效果触发 → 后摇 → CD）。
    /// 
    /// ComponentType.Skill = 6
    /// TickOrder = 160（Attack 之后）
    /// 
    /// 与 AttackComponent 的关系：共存不替代。
    /// - AttackComponent = 持续自动射击（定时器 + BulletPattern）
    /// - SkillComponent = CD 管理的技能循环
    /// 
    /// 状态转换矩阵（SA-005）：
    /// Idle → Casting（CastTime>0）或 Recovery（瞬发）
    /// Casting → Recovery（前摇结束 → 执行效果 → 进入后摇）
    /// Recovery → Cooldown
    /// Cooldown → Idle
    /// 死亡 → 立即中断回 Idle（ATK-014）
    /// </summary>
    public sealed class SkillComponent : IEntityComponent, ITickable
    {
        public ComponentType Type => ComponentType.Skill;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;
        public int TickOrder => TickOrders.Skill; // 160

        /// <summary>当前技能状态</summary>
        public SkillState CurrentState { get; private set; }

        /// <summary>CD 剩余时间</summary>
        public float CooldownRemaining { get; private set; }

        private Entity _owner;
        private SkillConfigSO _config;
        private float _stateTimer;

        // v0.4（ATK-005/ATK-010）：Init 后固定，不支持运行时切换控制源。
        private IDecisionMaker _cachedDecisionMaker;

        public void Init(Entity owner)
        {
            _owner = owner;
            _config = owner.ConfigSO.SkillConfig;
            CurrentState = SkillState.Idle;
            CooldownRemaining = 0f;
            _stateTimer = 0f;
            IsActive = _config != null;

            _cachedDecisionMaker = (owner.GetComponent(ComponentType.Control) as IDecisionMaker)
                                 ?? (owner.GetComponent(ComponentType.AI) as IDecisionMaker);
        }

        public void Reset()
        {
            _owner = null;
            _config = null;
            _cachedDecisionMaker = null;
            CurrentState = SkillState.Idle;
            CooldownRemaining = 0f;
            _stateTimer = 0f;
            IsActive = false;
        }

        public void Tick(float dt)
        {
            if (_config == null) return;

            // v0.4（ATK-014）：死亡/待回收时中断技能
            if (!_owner.IsAlive || _owner.IsPendingDespawn)
            {
                if (CurrentState != SkillState.Idle)
                {
                    CurrentState = SkillState.Idle;
                    _stateTimer = 0f;
                }
                return;
            }

            switch (CurrentState)
            {
                case SkillState.Idle:
                    if (ShouldTrigger())
                    {
                        if (_config.CastTime > 0)
                        {
                            CurrentState = SkillState.Casting;
                            _stateTimer = _config.CastTime;
                        }
                        else
                        {
                            ExecuteEffects(dt);
                            EnterRecovery();
                        }
                    }
                    break;

                case SkillState.Casting:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        ExecuteEffects(dt);
                        EnterRecovery();
                    }
                    break;

                case SkillState.Recovery:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0)
                    {
                        CooldownRemaining = _config.CooldownTime;
                        CurrentState = SkillState.Cooldown;
                    }
                    break;

                case SkillState.Cooldown:
                    CooldownRemaining -= dt;
                    if (CooldownRemaining <= 0)
                    {
                        CooldownRemaining = 0;
                        CurrentState = SkillState.Idle;
                    }
                    break;
            }
        }

        private bool ShouldTrigger()
        {
            if (CooldownRemaining > 0) return false;
            return _config.TriggerMode switch
            {
                SkillTriggerMode.Auto => true,
                SkillTriggerMode.Manual => _cachedDecisionMaker?.GetDecision().WantsAttack ?? false,
                _ => false
            };
        }

        private void ExecuteEffects(float dt)
        {
            var ctx = new SkillContext
            {
                Caster = _owner,
                CastPosition = _owner.Position,
                AimDirection = GetAimDirection(),
                DeltaTime = dt,
                SkillConfig = _config,
            };

            for (int i = 0; i < _config.Effects.Length; i++)
            {
                _config.Effects[i]?.Execute(ctx);
            }
        }

        // v0.4（GD-005）：CD=0 + Recovery=0 安全网
        private void EnterRecovery()
        {
            if (_config.RecoveryTime > 0)
            {
                CurrentState = SkillState.Recovery;
                _stateTimer = _config.RecoveryTime;
            }
            else if (_config.CooldownTime > 0)
            {
                CooldownRemaining = _config.CooldownTime;
                CurrentState = SkillState.Cooldown;
            }
            else
            {
                // 安全网：CD=0 + Recovery=0 → 强制最短 Cooldown（下帧再触发）
                CooldownRemaining = 0.001f;
                CurrentState = SkillState.Cooldown;
                Debug.LogWarning($"[SkillComponent] {_config.DisplayName} CD=0 + Recovery=0，已强制最小间隔。");
            }
        }

        private Vector2 GetAimDirection()
        {
            var autoAim = _owner.GetComponent(ComponentType.AutoAim) as ITargetProvider;
            if (autoAim != null && autoAim.HasTarget)
                return (autoAim.TargetPosition - _owner.Position).normalized;

            float rad = _owner.Rotation * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
