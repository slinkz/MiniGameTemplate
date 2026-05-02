using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 组件——管理 Entity 身上的 Buff 列表和聚合属性修正。
    /// 
    /// ComponentType.Buff = 10
    /// TickOrder = 50（最先执行，属性修正在 Decision/Attack 之前生效）
    /// 
    /// 设计：
    /// - 固定槽位数组（MAX_BUFFS=8），零 GC
    /// - 乘法叠加 + Clamp 极端值（GD-004）
    /// - 同 ID 完整刷新（SA-013）
    /// - Buff→Movement push by-ID（SA-003）
    /// - Attack→Buff pull AttackIntervalModifier（SA-003）
    /// </summary>
    public sealed class BuffComponent : IEntityComponent, ITickable
    {
        // ── IEntityComponent ──
        public ComponentType Type => ComponentType.Buff;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ── ITickable ──
        public int TickOrder => TickOrders.Buff; // 50

        // ── 常量 ──
        private const int MAX_BUFFS = 8;

        // v0.4（GD-004）：属性修正 Clamp 常量 [占位符]
        private const float MIN_MOVE_SPEED_RATIO = 0.4f;
        private const float MAX_MOVE_SPEED_RATIO = 2.5f;
        private const float MIN_ATTACK_INTERVAL_RATIO = 0.3f;
        private const float MAX_ATTACK_INTERVAL_RATIO = 3.0f;

        // ── 内部状态 ──
        private Entity _owner;
        private readonly BuffSlot[] _slots = new BuffSlot[MAX_BUFFS];
        private int _activeCount;

        // ── 聚合后的修正值 ──
        /// <summary>移速修正倍率（乘法叠加后 Clamp）</summary>
        public float MoveSpeedModifier { get; private set; } = 1f;
        /// <summary>攻击间隔修正倍率（乘法叠加后 Clamp）</summary>
        public float AttackIntervalModifier { get; private set; } = 1f;
        /// <summary>受伤修正倍率（不 Clamp，允许无敌/脆弱）</summary>
        public float DamageTakenModifier { get; private set; } = 1f;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            _activeCount = 0;
            RecalcModifiers();
            IsActive = true;
        }

        public void Reset()
        {
            _owner = null;
            _activeCount = 0;
            for (int i = 0; i < MAX_BUFFS; i++)
                _slots[i] = default;
            RecalcModifiers();
            IsActive = false;
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            if (_activeCount == 0) return;

            bool dirty = false;
            for (int i = _activeCount - 1; i >= 0; i--)
            {
                if (_slots[i].Duration <= 0f) continue; // 永久 Buff

                _slots[i].RemainingTime -= dt;
                if (_slots[i].RemainingTime <= 0f)
                {
                    RemoveAtIndex(i);
                    dirty = true;
                }
            }
            if (dirty)
            {
                RecalcModifiers();
                SyncMoveSpeedToMovement();
            }
        }

        // ── 公共 API ──

        /// <summary>施加 Buff。同 ID 刷新（完整更新属性 + 持续时间）。返回是否成功。</summary>
        public bool ApplyBuff(BuffConfigSO config)
        {
            if (config == null) return false;

            // 同 ID 检查：完整刷新（v0.4 SA-013）
            for (int i = 0; i < _activeCount; i++)
            {
                if (_slots[i].BuffId == config.BuffId)
                {
                    _slots[i].Duration = config.Duration;
                    _slots[i].RemainingTime = config.Duration;
                    _slots[i].MoveSpeedMod = config.MoveSpeedModifier;
                    _slots[i].AttackIntervalMod = config.AttackIntervalModifier;
                    _slots[i].DamageTakenMod = config.DamageTakenModifier;
                    RecalcModifiers();
                    SyncMoveSpeedToMovement();
                    return true;
                }
            }

            // 槽位满
            if (_activeCount >= MAX_BUFFS)
            {
                Debug.LogWarning($"[BuffComponent] Buff 槽位已满({MAX_BUFFS})，无法施加: {config.DisplayName}");
                return false;
            }

            // 新增
            _slots[_activeCount] = new BuffSlot
            {
                BuffId = config.BuffId,
                Duration = config.Duration,
                RemainingTime = config.Duration,
                MoveSpeedMod = config.MoveSpeedModifier,
                AttackIntervalMod = config.AttackIntervalModifier,
                DamageTakenMod = config.DamageTakenModifier,
            };
            _activeCount++;
            RecalcModifiers();
            SyncMoveSpeedToMovement();
            return true;
        }

        /// <summary>按 BuffId 移除指定 Buff</summary>
        public bool RemoveBuff(int buffId)
        {
            for (int i = 0; i < _activeCount; i++)
            {
                if (_slots[i].BuffId == buffId)
                {
                    RemoveAtIndex(i);
                    RecalcModifiers();
                    SyncMoveSpeedToMovement();
                    return true;
                }
            }
            return false;
        }

        /// <summary>当前活跃 Buff 数量</summary>
        public int ActiveBuffCount => _activeCount;

        // ── 内部 ──

        private void RemoveAtIndex(int index)
        {
            _activeCount--;
            if (index != _activeCount)
                _slots[index] = _slots[_activeCount];
            _slots[_activeCount] = default;
        }

        private void RecalcModifiers()
        {
            float move = 1f, attack = 1f, damage = 1f;
            for (int i = 0; i < _activeCount; i++)
            {
                move *= _slots[i].MoveSpeedMod;
                attack *= _slots[i].AttackIntervalMod;
                damage *= _slots[i].DamageTakenMod;
            }
            MoveSpeedModifier = Mathf.Clamp(move, MIN_MOVE_SPEED_RATIO, MAX_MOVE_SPEED_RATIO);
            AttackIntervalModifier = Mathf.Clamp(attack, MIN_ATTACK_INTERVAL_RATIO, MAX_ATTACK_INTERVAL_RATIO);
            DamageTakenModifier = damage; // 不 Clamp——允许无敌(0)和脆弱(×5)
        }

        private void SyncMoveSpeedToMovement()
        {
            var movement = _owner?.GetComponent(ComponentType.Movement) as MovementComponent;
            if (movement == null) return;

            if (Mathf.Approximately(MoveSpeedModifier, 1f))
                movement.RemoveSpeedModifierById(SpeedModifierIds.Buff);
            else
                movement.AddOrUpdateSpeedModifier(SpeedModifierIds.Buff, MoveSpeedModifier);
        }

        // ── 内部结构 ──

        private struct BuffSlot
        {
            public int BuffId;
            public float Duration;
            public float RemainingTime;
            public float MoveSpeedMod;
            public float AttackIntervalMod;
            public float DamageTakenMod;
        }
    }
}
