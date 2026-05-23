using System;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 生命组件——管理 HP + 受伤/死亡流程 + 无敌帧 + IDamageModifier 链。
    /// 
    /// 设计要点（TDD §4.2 + P2.4 扩展）：
    /// - TakeDamage 接收 DamageContext（ref），先遍历 IDamageModifier 链修正伤害
    /// - 使用 context.FinalDamage 扣血（fallback 到 BaseDamage * CritMultiplier）
    /// - 无敌帧（IFrameCount）：受伤后 N 帧内不可再受伤
    /// - HitStop 顿帧：受伤后调用 Entity.PauseFor(HitStopFrames)
    /// - HP ≤ 0 时发布 OnDeath，并通过 StateComponent 强制添加 Dead 状态
    /// - 从 EntityConfigSO 读取 MaxHp / IFrameCount / HitStopFrames
    /// </summary>
    public class HealthComponent : IEntityComponent, ITickable
    {
        // ── IEntityComponent 实现 ──
        public bool IsActive { get; private set; }
        public ComponentType Type => ComponentType.Health;

        // ── ITickable 实现（用于无敌帧计时）──
        public int TickOrder => TickOrders.Health;

        private Entity _owner;

        // ── HP 数据 ──
        private int _maxHp;
        private int _currentHp;

        /// <summary>当前 HP</summary>
        public int CurrentHp => _currentHp;

        /// <summary>最大 HP</summary>
        public int MaxHp => _maxHp;

        /// <summary>是否死亡</summary>
        public bool IsDead => _currentHp <= 0;

        /// <summary>HP 百分比（0~1）</summary>
        public float HpRatio => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;

        /// <summary>
        /// HP 变化时触发（参数为归一化 HpRatio 0~1）。
        /// 所有修改 _currentHp 的路径（TakeDamage / SetHp / Heal / Init）均自动发布。
        /// 单一事件源——外部只需订阅此事件即可，无需关心 HP 是怎么变的。
        /// </summary>
        public event Action<float> OnHpChanged;

        /// <summary>统一的 HP 变化通知出口。</summary>
        private void NotifyHpChanged()
        {
            OnHpChanged?.Invoke(HpRatio);
        }

        // ── 无敌帧（P2.4 新增）──
        private int _iFrameMax;       // 配置的无敌帧数（从 EntityConfigSO 读取）
        private int _iFrameRemaining; // 当前剩余无敌帧

        /// <summary>是否处于无敌帧状态</summary>
        public bool IsInvincible => _iFrameRemaining > 0;

        // ── HitStop 顿帧（P2.4 新增）──
        private int _hitStopFrames;   // 配置的顿帧数（从 EntityConfigSO 读取）

        // ── IDamageModifier 链（P2.4 新增）──
        private const int MAX_MODIFIERS = 4;
        private readonly IDamageModifier[] _modifiers = new IDamageModifier[MAX_MODIFIERS];
        private int _modifierCount;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;

            // 从配置读取属性
            _maxHp = owner.ConfigSO != null ? owner.ConfigSO.MaxHp : 100;
            _currentHp = _maxHp;
            // 注：Init 时不触发 NotifyHpChanged——订阅方尚未就绪。
            // 由外部调用 SetHp() 或首次 TakeDamage 触发。

            _iFrameMax = owner.ConfigSO != null ? owner.ConfigSO.IFrameCount : 0;
            _hitStopFrames = owner.ConfigSO != null ? owner.ConfigSO.HitStopFrames : 0;
            _iFrameRemaining = 0;
            _modifierCount = 0;
        }

        public void Reset()
        {
            _currentHp = 0;
            _maxHp = 0;
            _iFrameRemaining = 0;
            _modifierCount = 0;
            _owner = null;
            OnHpChanged = null; // 清理订阅，防止 Entity 池化后泄漏
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // ── ITickable: 每帧递减无敌帧 ──

        public void Tick(float dt)
        {
            if (_iFrameRemaining > 0)
                _iFrameRemaining--;
        }

        // ── IDamageModifier 管理 ──

        /// <summary>
        /// 注册伤害修正器。按 Priority 升序插入。
        /// 返回 true 成功，false 已满（最多 4 个）。
        /// </summary>
        public bool AddModifier(IDamageModifier modifier)
        {
            if (_modifierCount >= MAX_MODIFIERS) return false;

            // 插入排序保持 Priority 升序
            int insertAt = _modifierCount;
            for (int i = 0; i < _modifierCount; i++)
            {
                if (_modifiers[i].Priority > modifier.Priority)
                {
                    insertAt = i;
                    break;
                }
            }

            // 后移
            for (int i = _modifierCount; i > insertAt; i--)
            {
                _modifiers[i] = _modifiers[i - 1];
            }
            _modifiers[insertAt] = modifier;
            _modifierCount++;
            return true;
        }

        /// <summary>移除指定修正器</summary>
        public void RemoveModifier(IDamageModifier modifier)
        {
            for (int i = 0; i < _modifierCount; i++)
            {
                if (_modifiers[i] == modifier)
                {
                    // shift-left
                    _modifierCount--;
                    for (int j = i; j < _modifierCount; j++)
                    {
                        _modifiers[j] = _modifiers[j + 1];
                    }
                    _modifiers[_modifierCount] = null;
                    return;
                }
            }
        }

        /// <summary>清除所有修正器</summary>
        public void ClearModifiers()
        {
            for (int i = 0; i < _modifierCount; i++)
                _modifiers[i] = null;
            _modifierCount = 0;
        }

        // ── 伤害接口 ──

        /// <summary>
        /// 接收伤害（P2.4 完整流程）：
        ///   1. 无敌帧检查
        ///   2. 遍历 IDamageModifier 链
        ///   3. 计算 FinalDamage（含暴击倍率）
        ///   4. 扣血
        ///   5. 触发无敌帧 + HitStop
        ///   6. 发布 OnDamaged
        ///   7. HP ≤ 0 → OnDeath
        /// </summary>
        public void TakeDamage(ref DamageContext context)
        {
            if (!IsActive) return;
            if (IsDead) return;

            // 1. 无敌帧期间不可受伤
            if (_iFrameRemaining > 0) return;

            // 2. 暴击倍率修正（基础计算）
            int baseDmg = context.BaseDamage;
            if (context.IsCritical && context.CritMultiplier > 1f)
            {
                baseDmg = (int)(baseDmg * context.CritMultiplier);
            }
            context.FinalDamage = baseDmg;

            // 3. 遍历 IDamageModifier 链
            for (int i = 0; i < _modifierCount; i++)
            {
                bool continueChain = _modifiers[i].ProcessDamage(ref context, _owner);
                if (!continueChain)
                {
                    // 伤害被完全吸收（如护盾全挡了），不扣血
                    return;
                }
            }

            // 4. 读取最终伤害
            int finalDamage = context.FinalDamage;
            if (finalDamage <= 0) return;

            // 5. 扣血
            _currentHp -= finalDamage;
            if (_currentHp < 0) _currentHp = 0;
            NotifyHpChanged();

            // 6. 触发无敌帧
            if (_iFrameMax > 0)
                _iFrameRemaining = _iFrameMax;

            // 7. 触发 HitStop 顿帧
            if (_hitStopFrames > 0)
                _owner.PauseFor(_hitStopFrames);

            // 8. 发布 OnDamaged 事件
            _owner.EventBus.Publish(new OnDamaged
            {
                Damage = finalDamage,
                RemainingHp = _currentHp,
                Source = context.AttackerId,
                SourceId = context.SourceId
            });

            // 9. HP ≤ 0 → 触发死亡
            if (_currentHp <= 0)
            {
                HandleDeath(context.AttackerId);
            }
        }

        /// <summary>
        /// TakeDamage 便捷重载（无 ref）——供简单场景和测试使用。
        /// 不需要回读 FinalDamage 时使用此版本。
        /// </summary>
        public void TakeDamage(DamageContext context)
        {
            TakeDamage(ref context);
        }

        /// <summary>
        /// 治疗（恢复 HP，不超过 MaxHp）。V2 Sprint 2：道具修复用。
        /// </summary>
        /// <param name="amount">恢复量（正整数）</param>
        /// <returns>实际恢复量</returns>
        public int Heal(int amount)
        {
            if (!IsActive || amount <= 0) return 0;
            int before = _currentHp;
            _currentHp += amount;
            if (_currentHp > _maxHp) _currentHp = _maxHp;
            int healed = _currentHp - before;
            if (healed > 0)
                NotifyHpChanged();
            return healed;
        }

        /// <summary>
        /// 直接设置 HP（用于治疗/满血重置等非伤害场景）。
        /// </summary>
        public void SetHp(int hp)
        {
            if (!IsActive) return;
            int oldHp = _currentHp;
            _currentHp = hp > _maxHp ? _maxHp : (hp < 0 ? 0 : hp);
            if (_currentHp != oldHp)
                NotifyHpChanged();
        }

        // ── 内部方法 ──

        private void HandleDeath(EntityId killer)
        {
            // 1. 发布 OnDeath 事件
            _owner.EventBus.Publish(new OnDeath { Killer = killer });

            // 2. 通过 StateComponent 强制添加 Dead 状态
            var state = _owner.GetComponent(ComponentType.State) as StateComponent;
            state?.ForceAddState(EntityState.Dead);
        }
    }
}
