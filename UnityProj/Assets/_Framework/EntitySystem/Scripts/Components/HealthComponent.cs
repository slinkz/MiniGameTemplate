namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 生命组件——管理 HP + 受伤/死亡流程。
    /// 
    /// 设计要点（TDD §4.2）：
    /// - TakeDamage 接收 DamageContext（v2.4 新增），扣血并发布 OnDamaged
    /// - HP ≤ 0 时发布 OnDeath，并通过 StateComponent 强制添加 Dead 状态
    /// - 通过 EntityEventBus 发布事件，不直接操作其他组件
    /// - 从 EntityConfigSO.MaxHp 读取初始生命值
    /// </summary>
    public class HealthComponent : IEntityComponent
    {
        // ── IEntityComponent 实现 ──
        public bool IsActive { get; private set; }
        public ComponentType Type => ComponentType.Health;

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

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;

            // 从配置读取 MaxHp
            _maxHp = owner.ConfigSO != null ? owner.ConfigSO.MaxHp : 100;
            _currentHp = _maxHp;
        }

        public void Reset()
        {
            _currentHp = 0;
            _maxHp = 0;
            _owner = null;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // ── 伤害接口 ──

        /// <summary>
        /// 接收伤害（Phase 1 直接扣血）。
        /// Phase 2 扩展：游戏层可在此前拦截处理（护甲/暴击/减伤等 IDamageModifier）。
        /// </summary>
        /// <param name="context">伤害上下文，携带攻击者信息和命中类型</param>
        public void TakeDamage(DamageContext context)
        {
            if (!IsActive) return;
            if (IsDead) return; // 已死亡不重复处理

            int damage = context.BaseDamage;
            if (damage <= 0) return; // 无效伤害

            _currentHp -= damage;
            if (_currentHp < 0) _currentHp = 0;

            // 发布 OnDamaged 事件
            _owner.EventBus.Publish(new OnDamaged
            {
                Damage = damage,
                RemainingHp = _currentHp,
                Source = context.AttackerId
            });

            // HP ≤ 0 → 触发死亡
            if (_currentHp <= 0)
            {
                HandleDeath(context.AttackerId);
            }
        }

        /// <summary>
        /// 直接设置 HP（用于治疗/满血重置等非伤害场景）。
        /// </summary>
        public void SetHp(int hp)
        {
            if (!IsActive) return;
            _currentHp = hp > _maxHp ? _maxHp : (hp < 0 ? 0 : hp);
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
