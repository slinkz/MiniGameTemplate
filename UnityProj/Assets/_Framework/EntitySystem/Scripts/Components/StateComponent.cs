namespace MiniGameTemplate.Entity
{
    // ──────────────── StateMask 值类型 ────────────────

    /// <summary>
    /// 状态标签位掩码。内部 uint64，最多支持 64 种状态标签。
    /// 对外不暴露原始位操作，通过方法封装。
    /// v2.1（EC-014）：如未来需 > 64 种状态，改内部为 uint64[] 不影响外部接口。
    /// </summary>
    public struct StateMask
    {
        private ulong _bits;

        /// <summary>当前掩码原始值（仅内部/测试使用）</summary>
        internal ulong RawBits => _bits;

        /// <summary>是否包含指定状态</summary>
        public bool Has(int stateIndex) => (_bits & (1UL << stateIndex)) != 0;

        /// <summary>添加状态</summary>
        public void Add(int stateIndex) => _bits |= (1UL << stateIndex);

        /// <summary>移除状态</summary>
        public void Remove(int stateIndex) => _bits &= ~(1UL << stateIndex);

        /// <summary>清空所有状态</summary>
        public void Clear() => _bits = 0;

        /// <summary>是否与另一个掩码有重叠（用于互斥检查）</summary>
        public bool Overlaps(StateMask other) => (_bits & other._bits) != 0;

        /// <summary>是否为空</summary>
        public bool IsEmpty => _bits == 0;

        /// <summary>从 ulong 创建（内部/测试用）</summary>
        internal static StateMask FromRaw(ulong raw) => new StateMask { _bits = raw };
    }

    // ──────────────── 状态索引常量 ────────────────

    /// <summary>
    /// Phase 1 硬编码状态索引。Phase 2 迁移到 Luban 配置或 SO。
    /// 互斥规则（Phase 1 硬编码 < 5 条）：
    /// - Dead 与所有其他状态互斥
    /// - Stunned 与 Moving/Attacking 互斥
    /// </summary>
    public static class EntityState
    {
        public const int Idle = 0;
        public const int Moving = 1;
        public const int Attacking = 2;
        public const int Stunned = 3;
        public const int Dead = 4;
        public const int Invincible = 5;
        // 预留 6~63

        public const int MAX_STATES = 64;
    }

    // ──────────────── StateComponent ────────────────

    /// <summary>
    /// 状态组件——管理 Entity 的状态标签集合 + 互斥规则。
    /// 
    /// 设计要点（TDD §4.1 / v2.1 EC-014）：
    /// - StateMask 值类型封装位操作
    /// - 互斥规则矩阵 uint64[64]（启动时预计算，O(1) 检查）
    /// - Phase 1 硬编码 < 5 条互斥规则
    /// - 状态变化通过 EntityEventBus 发布 OnStateChanged
    /// </summary>
    public class StateComponent : IEntityComponent
    {
        // ── IEntityComponent 实现 ──
        public bool IsActive { get; private set; }
        public ComponentType Type => ComponentType.State;

        private Entity _owner;
        private StateMask _currentStates;

        // 互斥掩码矩阵：_exclusionMatrix[stateIndex] = 与该状态互斥的所有状态掩码
        private readonly StateMask[] _exclusionMatrix = new StateMask[EntityState.MAX_STATES];

        /// <summary>当前状态集合（只读访问）</summary>
        public StateMask CurrentStates => _currentStates;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            IsActive = true;
            _currentStates.Clear();

            // Phase 1 硬编码互斥规则（< 5 条）
            BuildExclusionMatrix();
        }

        public void Reset()
        {
            _currentStates.Clear();
            _owner = null;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
        }

        // ── 状态操作 ──

        /// <summary>
        /// 尝试添加状态。如果与当前状态互斥，返回 false 且不改变状态。
        /// 成功添加后发布 OnStateChanged 事件。
        /// </summary>
        public bool TryAddState(int stateIndex)
        {
            if (!IsActive) return false;
            if (_currentStates.Has(stateIndex)) return true; // 已有该状态，幂等

            // 互斥检查：新状态的互斥掩码与当前状态是否有重叠
            if (_currentStates.Overlaps(_exclusionMatrix[stateIndex]))
            {
                return false; // 互斥冲突，阻止添加
            }

            int oldRaw = GetPrimaryStateIndex();
            _currentStates.Add(stateIndex);
            int newRaw = GetPrimaryStateIndex();

            // 发布状态变化事件
            _owner.EventBus.Publish(new OnStateChanged
            {
                OldState = oldRaw,
                NewState = newRaw
            });

            return true;
        }

        /// <summary>
        /// 移除指定状态。移除后发布 OnStateChanged 事件。
        /// </summary>
        public void RemoveState(int stateIndex)
        {
            if (!IsActive) return;
            if (!_currentStates.Has(stateIndex)) return; // 没有该状态，无操作

            int oldRaw = GetPrimaryStateIndex();
            _currentStates.Remove(stateIndex);
            int newRaw = GetPrimaryStateIndex();

            _owner.EventBus.Publish(new OnStateChanged
            {
                OldState = oldRaw,
                NewState = newRaw
            });
        }

        /// <summary>
        /// 强制设置状态（跳过互斥检查）。用于死亡等不可阻止的状态。
        /// </summary>
        public void ForceAddState(int stateIndex)
        {
            if (!IsActive) return;
            if (_currentStates.Has(stateIndex)) return; // 已有该状态，幂等

            int oldRaw = GetPrimaryStateIndex();
            _currentStates.Add(stateIndex);
            int newRaw = GetPrimaryStateIndex();

            _owner.EventBus.Publish(new OnStateChanged
            {
                OldState = oldRaw,
                NewState = newRaw
            });
        }

        /// <summary>检查是否持有指定状态</summary>
        public bool HasState(int stateIndex) => _currentStates.Has(stateIndex);

        // ── 内部工具 ──

        /// <summary>获取"主要状态"索引（最高优先级的活跃状态，用于事件通知）</summary>
        private int GetPrimaryStateIndex()
        {
            // 优先级：Dead > Stunned > Attacking > Moving > Idle
            // 返回最高位的活跃状态索引
            if (_currentStates.Has(EntityState.Dead)) return EntityState.Dead;
            if (_currentStates.Has(EntityState.Stunned)) return EntityState.Stunned;
            if (_currentStates.Has(EntityState.Attacking)) return EntityState.Attacking;
            if (_currentStates.Has(EntityState.Moving)) return EntityState.Moving;
            return EntityState.Idle;
        }

        /// <summary>
        /// 构建互斥掩码矩阵（Phase 1 硬编码）。
        /// 规则：
        /// 1. Dead 与所有其他状态互斥（Dead 状态下不能添加任何其他状态）
        /// 2. Stunned 与 Moving/Attacking 互斥
        /// </summary>
        private void BuildExclusionMatrix()
        {
            // 清空矩阵
            for (int i = 0; i < EntityState.MAX_STATES; i++)
            {
                _exclusionMatrix[i] = default;
            }

            // 规则 1：Dead 与所有非 Dead 状态互斥
            // 表示：如果已有 Moving/Attacking/Stunned 等，不能通过 TryAdd 添加 Dead（需 ForceAdd）
            // 反向：如果已有 Dead，不能添加其他状态
            var deadExclusion = StateMask.FromRaw(
                (1UL << EntityState.Idle) |
                (1UL << EntityState.Moving) |
                (1UL << EntityState.Attacking) |
                (1UL << EntityState.Stunned) |
                (1UL << EntityState.Invincible));
            _exclusionMatrix[EntityState.Dead] = deadExclusion;

            // 反向：如果当前有 Dead，其他状态不能添加
            var blockedByDead = StateMask.FromRaw(1UL << EntityState.Dead);
            _exclusionMatrix[EntityState.Idle] = blockedByDead;
            _exclusionMatrix[EntityState.Moving] = blockedByDead;
            _exclusionMatrix[EntityState.Attacking] = blockedByDead;
            _exclusionMatrix[EntityState.Invincible] = blockedByDead;

            // 规则 2：Stunned 与 Moving/Attacking 互斥
            // Stunned 的互斥掩码中加入 Moving 和 Attacking
            var stunnedExclusion = StateMask.FromRaw(
                (1UL << EntityState.Moving) |
                (1UL << EntityState.Attacking) |
                (1UL << EntityState.Dead));
            _exclusionMatrix[EntityState.Stunned] = stunnedExclusion;

            // 反向：Moving/Attacking 的互斥掩码中加入 Stunned
            // Moving 互斥：Dead + Stunned
            _exclusionMatrix[EntityState.Moving] = StateMask.FromRaw(
                (1UL << EntityState.Dead) |
                (1UL << EntityState.Stunned));

            // Attacking 互斥：Dead + Stunned
            _exclusionMatrix[EntityState.Attacking] = StateMask.FromRaw(
                (1UL << EntityState.Dead) |
                (1UL << EntityState.Stunned));
        }
    }
}
