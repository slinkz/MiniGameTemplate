using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 技能组件——6 槽位 CD 管理的全自动技能释放循环。
    /// 
    /// ComponentType.Skill = 6
    /// TickOrder = 160
    /// 
    /// TDD-06 改造：普攻收编为 Slot[0]（AimMode=FixedForward, IsNormalAttack=true）。
    /// 所有攻击行为统一走 SkillComponent。
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

        /// <summary>
        /// 运行时 CD 覆盖值（TDD-06 §2.7）。
        /// 外部可通过 OverrideSlotCooldown 注入，运行时优先于 SO 默认值。
        /// </summary>
        private readonly float[] _runtimeCooldownOverrides = new float[MAX_SLOTS];

        /// <summary>获取指定槽位状态（UI 显示 CD 用）</summary>
        public ref readonly SkillSlot GetSlot(int index) => ref _slots[index];

        /// <summary>
        /// 运行时覆盖指定槽位的 CooldownTime（TDD-06 §2.7）。
        /// 注意：不修改 SO 资产——只读 Config.CooldownTime 作为 fallback。
        /// </summary>
        public void OverrideSlotCooldown(int slotIndex, float cooldownTime)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return;
            if (_slots[slotIndex].Config == null) return;
            _runtimeCooldownOverrides[slotIndex] = cooldownTime;
        }

        /// <summary>
        /// 获取有效 CD（优先运行时覆盖值，否则读 Config.CooldownTime）。
        /// </summary>
        private float GetEffectiveCooldown(int slotIndex)
        {
            float over = _runtimeCooldownOverrides[slotIndex];
            return over > 0f ? over : _slots[slotIndex].Config.CooldownTime;
        }

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
        /// <param name="firstSlotInitialCD">Slot[0] 首发延迟（秒）。
        ///   >0 = Slot[0] 初始进入 Cooldown 状态等待此时间后首发。
        ///   不做内部 Clamp——调用者负责确保值合理。【CR-010】
        ///   ≤0 = 立即可用（无首发延迟）。</param>
        public void InitWithEquipment(SkillConfigSO[] equippedSkills, float staggerOffsetPerSlot = 0.5f,
                                      float firstSlotInitialCD = 0f)
        {
            // 清零运行时覆盖（对象池复用安全）
            System.Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS);

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

            // 【PK-ET-009】Slot[0] 首发延迟
            if (firstSlotInitialCD > 0f && _slots[0].Config != null)
            {
                _slots[0].CooldownTimer = firstSlotInitialCD;
                _slots[0].State = SkillState.Cooldown;
            }

            ActiveSlotCount = count;
            IsActive = true;
        }

        public void Reset()
        {
            _owner = null;
            _cachedDecisionMaker = null;
            System.Array.Clear(_runtimeCooldownOverrides, 0, MAX_SLOTS);
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
                TickSlot(i, dt); // 【CR-001】传 index，不传 ref
            }
        }

        /// <summary>
        /// 单槽位 Tick（TDD-06 §2.7 CR-001 改造）。
        /// 传 slotIndex 以便内部访问 _runtimeCooldownOverrides。
        /// </summary>
        private void TickSlot(int slotIndex, float dt)
        {
            ref var slot = ref _slots[slotIndex];
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
                            // 效果执行失败（如无目标）→ 留在 Idle，下帧重试，不进 CD
                            if (ExecuteEffects(slot.Config, dt))
                                EnterRecovery(slotIndex); // 【CR-001】传 index
                        }
                    }
                    break;

                case SkillState.Casting:
                    slot.CastTimer -= dt;
                    if (slot.CastTimer <= 0)
                    {
                        // 前摇结束但效果失败 → 退回 Idle 重试（不浪费 CD）
                        if (ExecuteEffects(slot.Config, dt))
                            EnterRecovery(slotIndex); // 【CR-001】传 index
                        else
                            slot.State = SkillState.Idle;
                    }
                    break;

                case SkillState.Recovery:
                    slot.CastTimer -= dt;
                    if (slot.CastTimer <= 0)
                    {
                        // 【CR-002 核心修正】使用 GetEffectiveCooldown 替代 Config.CooldownTime
                        slot.CooldownTimer = GetEffectiveCooldown(slotIndex);
                        slot.State = SkillState.Cooldown;
                    }
                    break;

                case SkillState.Cooldown:
                    float cdDt = dt;
                    // 普攻槽：Buff 攻速修正影响 CD 消耗速率
                    // AttackIntervalModifier < 1 → 攻速更快 → CD 消耗更快
                    if (slot.Config.IsNormalAttack)
                    {
                        // 【CR-006】不缓存 BuffComponent 的原因：
                        //   1. Entity.GetComponent(ComponentType.Buff) = _components[10]，O(1) 数组索引，零 GC
                        //   2. ComponentType.Buff=10 > Skill=6，Init 时 Buff 可能还未创建
                        //   3. 每帧仅 Slot[0] 执行此分支（≤1 次/帧），性能可忽略
                        var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
                        if (buff != null && buff.AttackIntervalModifier != 1f)
                        {
                            // 修正值 = 1/modifier（modifier=0.5 → cd消耗2倍速）
                            cdDt = dt / buff.AttackIntervalModifier;
                        }
                    }
                    slot.CooldownTimer -= cdDt;
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

        /// <summary>
        /// 执行技能效果。返回 true = 至少一个效果成功执行；false = 全部跳过（如无目标）。
        /// 返回 false 时技能不进 CD，下一帧重试。
        /// </summary>
        private bool ExecuteEffects(SkillConfigSO config, float dt)
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
                AimDirection = GetAimDirection(config, autoAim),
                DeltaTime = dt,
                SkillConfig = config,
                CasterTransform = casterTransform,
                HasTarget = hasTarget,
                SourceTagId = config.SourceTagId,
            };

            bool anySuccess = false;
            for (int i = 0; i < config.Effects.Length; i++)
            {
                if (config.Effects[i]?.Execute(ctx) == true)
                    anySuccess = true;
            }
            return anySuccess;
        }

        /// <summary>
        /// 进入 Recovery 或直接进入 Cooldown（TDD-06 §2.7 CR-001/002）。
        /// 使用 GetEffectiveCooldown 读取运行时覆盖的 CD 值。
        /// </summary>
        private void EnterRecovery(int slotIndex)
        {
            ref var slot = ref _slots[slotIndex];
            if (slot.Config.RecoveryTime > 0)
            {
                slot.State = SkillState.Recovery;
                slot.CastTimer = slot.Config.RecoveryTime;
            }
            else
            {
                float cd = GetEffectiveCooldown(slotIndex); // 使用运行时覆盖
                if (cd > 0)
                {
                    slot.CooldownTimer = cd;
                    slot.State = SkillState.Cooldown;
                }
                else
                {
                    // 安全网：CD=0 + Recovery=0 → 强制最短 Cooldown（下帧再触发）
                    slot.CooldownTimer = 0.001f;
                    slot.State = SkillState.Cooldown;
                    Debug.LogWarning($"[SkillComponent] {slot.Config.DisplayName} CD=0，已强制最小间隔。");
                }
            }
        }

        /// <summary>
        /// 根据 SkillConfigSO.AimMode 决定施法方向。
        /// - FixedForward：纵版射击固定向上（当前普攻行为等价）
        /// - AutoAim：有锁定目标→跟踪，无目标→Decision→Rotation
        /// - CommandDir：纯 Decision 方向（预留手动操控）
        /// </summary>
        private Vector2 GetAimDirection(SkillConfigSO config, ITargetProvider autoAim)
        {
            switch (config.AimMode)
            {
                case AimMode.FixedForward:
                    // 【PK-UA-009/013】纵版射击固定向上。
                    // 未来支持非纵版时可从 SkillConfigSO 新增 FixedDirection 字段扩展。
                    return Vector2.up;

                case AimMode.AutoAim:
                    // 有目标→跟踪，无目标→Decision→Rotation（原有逻辑）
                    if (autoAim != null && autoAim.HasTarget)
                        return (autoAim.TargetPosition - _owner.Position).normalized;
                    goto case AimMode.CommandDir;

                case AimMode.CommandDir:
                    if (_cachedDecisionMaker != null)
                    {
                        Vector2 aimDir = _cachedDecisionMaker.GetDecision().AimDirection;
                        if (aimDir.sqrMagnitude > 0.01f)
                            return aimDir.normalized;
                    }
                    // 兜底：Entity 朝向
                    float fallbackRad = _owner.Rotation * Mathf.Deg2Rad;
                    return new Vector2(Mathf.Cos(fallbackRad), Mathf.Sin(fallbackRad));

                default:
                    return Vector2.up;
            }
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
