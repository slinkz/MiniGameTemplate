using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 技能组件——6 槽位 CD 管理的全自动技能释放循环（V2 Sprint 2 改造）。
    /// 
    /// ComponentType.Skill = 6
    /// TickOrder = 160（Attack 之后）
    /// 
    /// 与 AttackComponent 的关系：共存不替代。
    /// - AttackComponent = 持续自动射击（定时器 + BulletPattern）
    /// - SkillComponent = 6 技能槽各自独立 CD 全自动释放
    /// 
    /// 初始化模式：
    /// - 外部注入（V2）：BattleController 调用 InitWithEquipment(skills[])
    /// - 兜底单槽（V1）：从 EntityConfigSO.SkillConfig 读单个配置填入 Slot 0
    /// 
    /// 每槽状态转换矩阵（SA-005）：
    /// Idle → Casting（CastTime>0）或 Recovery（瞬发）
    /// Casting → Recovery（前摇结束 → 执行效果 → 进入后摇）
    /// Recovery → Cooldown
    /// Cooldown → Idle
    /// 死亡 → 立即中断所有槽回 Idle（ATK-014）
    /// </summary>
    public sealed class SkillComponent : IEntityComponent, ITickable
    {
        public ComponentType Type => ComponentType.Skill;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;
        public int TickOrder => TickOrders.Skill; // 160

        /// <summary>最大技能槽数量</summary>
        public const int MAX_SLOTS = 6;

        /// <summary>技能槽数组（固定 6 个，避免运行时分配）</summary>
        private readonly SkillSlot[] _slots = new SkillSlot[MAX_SLOTS];

        /// <summary>活跃槽位数量（非空槽位，缓存避免每帧遍历空槽）</summary>
        public int ActiveSlotCount { get; private set; }

        private Entity _owner;

        // v0.4（ATK-005/ATK-010）：Init 后固定，不支持运行时切换控制源。
        private IDecisionMaker _cachedDecisionMaker;

        /// <summary>获取指定槽位状态（UI 显示 CD 用）</summary>
        public ref readonly SkillSlot GetSlot(int index) => ref _slots[index];

        public void Init(Entity owner)
        {
            _owner = owner;
            _cachedDecisionMaker = (owner.GetComponent(ComponentType.Control) as IDecisionMaker)
                                 ?? (owner.GetComponent(ComponentType.AI) as IDecisionMaker);

            // 兜底模式：从 EntityConfigSO.SkillConfig 读取单技能
            var singleConfig = owner.ConfigSO.SkillConfig;
            if (singleConfig != null)
            {
                _slots[0].Config = singleConfig;
                _slots[0].State = SkillState.Cooldown;
                _slots[0].CooldownTimer = 0.5f; // 初始短 CD 避免开场瞬发
                ActiveSlotCount = 1;
            }
            else
            {
                ActiveSlotCount = 0;
            }

            IsActive = ActiveSlotCount > 0;
        }

        /// <summary>
        /// V2 外部注入装备技能（由 BattleController 调用）。
        /// 覆盖 Init 中的兜底配置。
        /// </summary>
        /// <param name="equippedSkills">已装备的技能列表（可少于 6 个）</param>
        /// <param name="staggerOffsetPerSlot">
        /// 初始 CD 错开间隔（秒）。避免 6 技能同时释放造成视觉爆炸。
        /// TDD 建议 0.5f。
        /// </param>
        public void InitWithEquipment(SkillConfigSO[] equippedSkills, float staggerOffsetPerSlot = 0.5f)
        {
            // 先清空所有槽位
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                _slots[i] = default;
            }

            if (equippedSkills == null || equippedSkills.Length == 0)
            {
                ActiveSlotCount = 0;
                IsActive = false;
                return;
            }

            int count = Mathf.Min(equippedSkills.Length, MAX_SLOTS);
            for (int i = 0; i < count; i++)
            {
                if (equippedSkills[i] == null) continue;
                _slots[i].Config = equippedSkills[i];
                _slots[i].State = SkillState.Cooldown;
                _slots[i].CooldownTimer = staggerOffsetPerSlot * i; // 错开释放
            }

            ActiveSlotCount = count;
            IsActive = true;
        }

        public void Reset()
        {
            _owner = null;
            _cachedDecisionMaker = null;
            for (int i = 0; i < MAX_SLOTS; i++)
            {
                _slots[i] = default;
            }
            ActiveSlotCount = 0;
            IsActive = false;
        }

        public void Tick(float dt)
        {
            if (ActiveSlotCount == 0) return;

            // v0.4（ATK-014）：死亡/待回收时中断所有技能
            if (!_owner.IsAlive || _owner.IsPendingDespawn)
            {
                for (int i = 0; i < MAX_SLOTS; i++)
                {
                    if (_slots[i].Config != null && _slots[i].State != SkillState.Idle)
                    {
                        _slots[i].State = SkillState.Idle;
                        _slots[i].CooldownTimer = 0f;
                        _slots[i].CastTimer = 0f;
                    }
                }
                return;
            }

            for (int i = 0; i < MAX_SLOTS; i++)
            {
                if (_slots[i].Config == null) continue;
                TickSlot(ref _slots[i], dt);
            }
        }

        private void TickSlot(ref SkillSlot slot, float dt)
        {
            switch (slot.State)
            {
                case SkillState.Idle:
                    if (ShouldTrigger(slot.Config))
                    {
                        if (slot.Config.CastTime > 0)
                        {
                            slot.State = SkillState.Casting;
                            slot.CastTimer = slot.Config.CastTime;
                        }
                        else
                        {
                            ExecuteEffects(slot.Config, dt);
                            EnterRecovery(ref slot);
                        }
                    }
                    break;

                case SkillState.Casting:
                    slot.CastTimer -= dt;
                    if (slot.CastTimer <= 0)
                    {
                        ExecuteEffects(slot.Config, dt);
                        EnterRecovery(ref slot);
                    }
                    break;

                case SkillState.Recovery:
                    slot.CastTimer -= dt;
                    if (slot.CastTimer <= 0)
                    {
                        slot.CooldownTimer = slot.Config.CooldownTime;
                        slot.State = SkillState.Cooldown;
                    }
                    break;

                case SkillState.Cooldown:
                    slot.CooldownTimer -= dt;
                    if (slot.CooldownTimer <= 0)
                    {
                        slot.CooldownTimer = 0;
                        slot.State = SkillState.Idle;
                    }
                    break;
            }
        }

        private bool ShouldTrigger(SkillConfigSO config)
        {
            return config.TriggerMode switch
            {
                SkillTriggerMode.Auto => true,
                SkillTriggerMode.Manual => _cachedDecisionMaker?.GetDecision().WantsAttack ?? false,
                _ => false
            };
        }

        private void ExecuteEffects(SkillConfigSO config, float dt)
        {
            // 查找施法者的 View Transform（激光/喷雾 Attached 模式需要）
            UnityEngine.Transform casterTransform = null;
            var viewBridge = EntityManagerAccessor.ViewBridge;
            if (viewBridge != null)
                casterTransform = viewBridge.GetViewTransform(_owner.Id.Value);

            // 单次查询 AutoAim，同时供 HasTarget 判断和 AimDirection 计算
            var autoAim = _owner.GetComponent(ComponentType.AutoAim) as ITargetProvider;
            bool hasTarget = autoAim != null && autoAim.HasTarget;

            var ctx = new SkillContext
            {
                Caster = _owner,
                CastPosition = _owner.Position,
                AimDirection = GetAimDirection(autoAim),
                DeltaTime = dt,
                SkillConfig = config,
                CasterTransform = casterTransform,
                HasTarget = hasTarget,
                SourceTagId = config.SourceTagId,
            };

            for (int i = 0; i < config.Effects.Length; i++)
            {
                config.Effects[i]?.Execute(ctx);
            }
        }

        private void EnterRecovery(ref SkillSlot slot)
        {
            if (slot.Config.RecoveryTime > 0)
            {
                slot.State = SkillState.Recovery;
                slot.CastTimer = slot.Config.RecoveryTime;
            }
            else if (slot.Config.CooldownTime > 0)
            {
                slot.CooldownTimer = slot.Config.CooldownTime;
                slot.State = SkillState.Cooldown;
            }
            else
            {
                // 安全网：CD=0 + Recovery=0 → 强制最短 Cooldown（下帧再触发）
                slot.CooldownTimer = 0.001f;
                slot.State = SkillState.Cooldown;
                Debug.LogWarning($"[SkillComponent] {slot.Config.DisplayName} CD=0 + Recovery=0，已强制最小间隔。");
            }
        }

        /// <param name="autoAim">已查询的 AutoAim 组件（可为 null）</param>
        private Vector2 GetAimDirection(ITargetProvider autoAim)
        {
            if (autoAim != null && autoAim.HasTarget)
                return (autoAim.TargetPosition - _owner.Position).normalized;

            float rad = _owner.Rotation * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    /// <summary>
    /// 技能槽结构体（值类型，存储于固定数组，零 GC）。
    /// </summary>
    public struct SkillSlot
    {
        /// <summary>技能配置（null = 空槽）</summary>
        public SkillConfigSO Config;

        /// <summary>当前状态</summary>
        public SkillState State;

        /// <summary>CD 剩余时间</summary>
        public float CooldownTimer;

        /// <summary>前摇/后摇计时器</summary>
        public float CastTimer;

        /// <summary>是否为空槽位</summary>
        public bool IsEmpty => Config == null;

        /// <summary>CD 进度（0~1，0=CD 好了，1=刚开始 CD）</summary>
        public float CooldownProgress => Config == null || Config.CooldownTime <= 0
            ? 0f
            : Mathf.Clamp01(CooldownTimer / Config.CooldownTime);
    }
}
