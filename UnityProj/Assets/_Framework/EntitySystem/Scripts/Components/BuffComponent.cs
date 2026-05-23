using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Buff 组件——管理 Entity 身上的 Buff 列表、DOT 列表和聚合属性修正。
    /// 
    /// ComponentType.Buff = 10
    /// TickOrder = 50（最先执行，属性修正在 Decision/Attack 之前生效）
    /// 
    /// V1 设计：固定槽位数组 + 乘法叠加 + 同 ID 刷新 + Movement push
    /// V2 Sprint 3 扩展：
    ///   - DotSlot[16] 独立 DOT 数组
    ///   - BuffTag / StackMode / MaxStacks 叠层
    ///   - BulletCountModifier（子弹数修正）
    ///   - RemoveByTag 批量清除
    ///   - 被动效果字段（Pierce/Crit/Magnet）
    ///   - VFX 实例 ID 追踪（预留，V2 不实装池化 VFX）
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
        private const int MAX_BUFFS = 16;
        private const int MAX_DOTS = 16;

        // v0.4（GD-004）：属性修正 Clamp 常量
        private const float MIN_MOVE_SPEED_RATIO = 0.4f;
        private const float MAX_MOVE_SPEED_RATIO = 2.5f;
        private const float MIN_ATTACK_INTERVAL_RATIO = 0.3f;
        private const float MAX_ATTACK_INTERVAL_RATIO = 3.0f;

        // ── 内部状态 ──
        private Entity _owner;
        private readonly BuffSlot[] _buffSlots = new BuffSlot[MAX_BUFFS];
        private int _activeBuffCount;
        private readonly DotSlot[] _dotSlots = new DotSlot[MAX_DOTS];
        private int _activeDotCount;

        // ── 聚合后的修正值 ──
        /// <summary>移速修正倍率（乘法叠加后 Clamp）</summary>
        public float MoveSpeedModifier { get; private set; } = 1f;
        /// <summary>攻击间隔修正倍率（乘法叠加后 Clamp）</summary>
        public float AttackIntervalModifier { get; private set; } = 1f;
        /// <summary>受伤修正倍率（不 Clamp，允许无敌/脆弱）</summary>
        public float DamageTakenModifier { get; private set; } = 1f;

        // ── V2 聚合查询 ──
        /// <summary>是否有穿透 Buff 生效（被动 PA-01）</summary>
        public bool HasActivePierce { get; private set; }
        /// <summary>当前暴击率加成（被动 PA-02）</summary>
        public float CritRateBonus { get; private set; }
        /// <summary>当前暴击倍率覆盖（0=使用配置默认值）</summary>
        public float CritMultiplierOverride { get; private set; }
        /// <summary>拾取半径倍率（被动 PA-03）</summary>
        public float PickupRadiusModifier { get; private set; } = 1f;

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            _activeBuffCount = 0;
            _activeDotCount = 0;
            RecalcModifiers();
            IsActive = true;
        }

        public void Reset()
        {
            _owner = null;
            _activeBuffCount = 0;
            _activeDotCount = 0;
            for (int i = 0; i < MAX_BUFFS; i++)
                _buffSlots[i] = default;
            for (int i = 0; i < MAX_DOTS; i++)
                _dotSlots[i] = default;
            RecalcModifiers();
            IsActive = false;
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            bool dirty = false;

            // 1. Buff 倒计时
            if (_activeBuffCount > 0)
            {
                for (int i = _activeBuffCount - 1; i >= 0; i--)
                {
                    if (_buffSlots[i].Duration <= 0f) continue; // 永久 Buff

                    _buffSlots[i].RemainingTime -= dt;
                    if (_buffSlots[i].RemainingTime <= 0f)
                    {
                        RemoveBuffAtIndex(i);
                        dirty = true;
                    }
                }
            }

            // 2. DOT tick
            if (_activeDotCount > 0)
            {
                for (int i = _activeDotCount - 1; i >= 0; i--)
                {
                    ref var dot = ref _dotSlots[i];
                    dot.Timer += dt;

                    // 每 Interval 造一次伤害
                    while (dot.Timer >= dot.Interval)
                    {
                        dot.Timer -= dot.Interval;
                        // 通过 DamageDealer 走完整伤害管线
                        var ctx = new DamageContext
                        {
                            BaseDamage = dot.DamagePerTick,
                            AttackerId = default, // DOT 无明确发射者
                            HitType = CollisionEventType.SprayHit, // 复用 SprayHit 类型表示持续伤害
                            Type = DamageType.Magical, // DOT 默认魔法伤害
                            SourceId = dot.DotId, // Sprint 4: DOT 伤害溯源
                        };
                        DamageDealer.DealDamageToEntity(_owner, ctx);
                    }

                    dot.RemainingTime -= dt;
                    if (dot.RemainingTime <= 0f)
                    {
                        RemoveDotAtIndex(i);
                    }
                }
            }

            if (dirty)
            {
                RecalcModifiers();
                SyncMoveSpeedToMovement();
            }
        }

        // ── Buff 公共 API ──

        /// <summary>
        /// 施加 Buff。行为由 StackMode 决定：
        /// - Refresh：同 ID 刷新（完整更新属性 + 持续时间）
        /// - Stack：同 ID 叠层（CurrentStacks++，≤ MaxStacks）
        /// 返回是否成功。
        /// </summary>
        public bool ApplyBuff(BuffConfigSO config)
        {
            if (config == null) return false;

            // 同 ID 检查
            for (int i = 0; i < _activeBuffCount; i++)
            {
                if (_buffSlots[i].BuffId == config.BuffId)
                {
                    if (config.StackMode == StackMode.Stack)
                    {
                        // 叠层模式：层数+1（不超 Max），刷新时间
                        if (_buffSlots[i].CurrentStacks < _buffSlots[i].MaxStacks)
                            _buffSlots[i].CurrentStacks++;
                        _buffSlots[i].RemainingTime = config.Duration;
                    }
                    else
                    {
                        // 刷新模式（V1 行为）：完整覆盖
                        _buffSlots[i].Duration = config.Duration;
                        _buffSlots[i].RemainingTime = config.Duration;
                        _buffSlots[i].MoveSpeedMod = config.MoveSpeedModifier;
                        _buffSlots[i].AttackIntervalMod = config.AttackIntervalModifier;
                        _buffSlots[i].DamageTakenMod = config.DamageTakenModifier;
                        _buffSlots[i].BulletCountMod = config.BulletCountModifier;
                        _buffSlots[i].GrantsPierce = config.GrantsPierce;
                        _buffSlots[i].CritRateBonus = config.CritRateBonus;
                        _buffSlots[i].CritMultiplierOverride = config.CritMultiplierOverride;
                        _buffSlots[i].PickupRadiusMod = config.PickupRadiusModifier;
                    }
                    RecalcModifiers();
                    SyncMoveSpeedToMovement();
                    return true;
                }
            }

            // 槽位满
            if (_activeBuffCount >= MAX_BUFFS)
            {
                Debug.LogWarning($"[BuffComponent] Buff 槽位已满({MAX_BUFFS})，无法施加: {config.DisplayName}");
                return false;
            }

            // 新增
            _buffSlots[_activeBuffCount] = new BuffSlot
            {
                BuffId = config.BuffId,
                Tag = config.Tag,
                StackMode = config.StackMode,
                MaxStacks = config.MaxStacks,
                CurrentStacks = 1,
                Duration = config.Duration,
                RemainingTime = config.Duration,
                MoveSpeedMod = config.MoveSpeedModifier,
                AttackIntervalMod = config.AttackIntervalModifier,
                DamageTakenMod = config.DamageTakenModifier,
                BulletCountMod = config.BulletCountModifier,
                GrantsPierce = config.GrantsPierce,
                CritRateBonus = config.CritRateBonus,
                CritMultiplierOverride = config.CritMultiplierOverride,
                PickupRadiusMod = config.PickupRadiusModifier,
                VfxInstanceId = -1,
            };
            _activeBuffCount++;
            RecalcModifiers();
            SyncMoveSpeedToMovement();
            return true;
        }

        /// <summary>按 BuffId 移除指定 Buff</summary>
        public bool RemoveBuff(int buffId)
        {
            for (int i = 0; i < _activeBuffCount; i++)
            {
                if (_buffSlots[i].BuffId == buffId)
                {
                    RemoveBuffAtIndex(i);
                    RecalcModifiers();
                    SyncMoveSpeedToMovement();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 按 Tag 移除所有匹配的 Buff 和 DOT。
        /// 用途：净化清除所有 Negative。
        /// </summary>
        public int RemoveByTag(BuffTag tag)
        {
            int removed = 0;

            // 清除 Buff
            for (int i = _activeBuffCount - 1; i >= 0; i--)
            {
                if (_buffSlots[i].Tag == tag)
                {
                    RemoveBuffAtIndex(i);
                    removed++;
                }
            }

            // 清除 DOT
            for (int i = _activeDotCount - 1; i >= 0; i--)
            {
                if (_dotSlots[i].Tag == tag)
                {
                    RemoveDotAtIndex(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                RecalcModifiers();
                SyncMoveSpeedToMovement();
            }
            return removed;
        }

        /// <summary>当前活跃 Buff 数量</summary>
        public int ActiveBuffCount => _activeBuffCount;

        /// <summary>当前活跃 DOT 数量</summary>
        public int ActiveDotCount => _activeDotCount;

        /// <summary>
        /// 获取子弹数修正倍率（AttackComponent 查询用）。
        /// 遍历活跃 BuffSlot，乘法累积 BulletCountModifier。
        /// </summary>
        public float GetBulletCountModifier()
        {
            float mod = 1f;
            for (int i = 0; i < _activeBuffCount; i++)
            {
                mod *= _buffSlots[i].BulletCountMod;
            }
            return mod;
        }

        /// <summary>查询指定 BuffId 是否存在</summary>
        public bool HasBuff(int buffId)
        {
            for (int i = 0; i < _activeBuffCount; i++)
            {
                if (_buffSlots[i].BuffId == buffId) return true;
            }
            return false;
        }

        // ── DOT 公共 API ──

        /// <summary>
        /// 施加 DOT。同 DotId 刷新 Duration（不叠加）。
        /// </summary>
        public bool ApplyDot(DotConfigSO config)
        {
            if (config == null) return false;

            // 同 ID 检查：刷新
            for (int i = 0; i < _activeDotCount; i++)
            {
                if (_dotSlots[i].DotId == config.DotId)
                {
                    _dotSlots[i].RemainingTime = config.Duration;
                    return true;
                }
            }

            // 槽位满
            if (_activeDotCount >= MAX_DOTS)
            {
                Debug.LogWarning($"[BuffComponent] DOT 槽位已满({MAX_DOTS})，无法施加: {config.DisplayName}");
                return false;
            }

            // 新增
            _dotSlots[_activeDotCount] = new DotSlot
            {
                DotId = config.DotId,
                Tag = config.Tag,
                DamagePerTick = config.DamagePerTick,
                Interval = config.Interval,
                RemainingTime = config.Duration,
                Timer = 0f,
                VfxInstanceId = -1,
            };
            _activeDotCount++;
            return true;
        }

        /// <summary>按 DotId 移除指定 DOT</summary>
        public bool RemoveDot(int dotId)
        {
            for (int i = 0; i < _activeDotCount; i++)
            {
                if (_dotSlots[i].DotId == dotId)
                {
                    RemoveDotAtIndex(i);
                    return true;
                }
            }
            return false;
        }

        // ── 内部方法 ──

        private void RemoveBuffAtIndex(int index)
        {
            _activeBuffCount--;
            if (index != _activeBuffCount)
                _buffSlots[index] = _buffSlots[_activeBuffCount];
            _buffSlots[_activeBuffCount] = default;
        }

        private void RemoveDotAtIndex(int index)
        {
            _activeDotCount--;
            if (index != _activeDotCount)
                _dotSlots[index] = _dotSlots[_activeDotCount];
            _dotSlots[_activeDotCount] = default;
        }

        private void RecalcModifiers()
        {
            float move = 1f, attack = 1f, damage = 1f;
            bool hasPierce = false;
            float critBonus = 0f;
            float critMultOverride = 0f;
            float pickupRadius = 1f;

            for (int i = 0; i < _activeBuffCount; i++)
            {
                ref var slot = ref _buffSlots[i];
                // Stack 模式：乘法因子按层数计算（线性递增）
                int stacks = slot.CurrentStacks;
                if (slot.StackMode == StackMode.Stack && stacks > 1)
                {
                    // 叠层公式：base^stacks（指数叠加）——与 V1 乘法累积一致
                    float moveMod = Mathf.Pow(slot.MoveSpeedMod, stacks);
                    float attackMod = Mathf.Pow(slot.AttackIntervalMod, stacks);
                    float damageMod = Mathf.Pow(slot.DamageTakenMod, stacks);
                    move *= moveMod;
                    attack *= attackMod;
                    damage *= damageMod;
                }
                else
                {
                    move *= slot.MoveSpeedMod;
                    attack *= slot.AttackIntervalMod;
                    damage *= slot.DamageTakenMod;
                }

                // 被动效果聚合
                if (slot.GrantsPierce) hasPierce = true;
                critBonus += slot.CritRateBonus * stacks;
                if (slot.CritMultiplierOverride > critMultOverride)
                    critMultOverride = slot.CritMultiplierOverride;
                pickupRadius *= slot.PickupRadiusMod;
            }

            MoveSpeedModifier = Mathf.Clamp(move, MIN_MOVE_SPEED_RATIO, MAX_MOVE_SPEED_RATIO);
            AttackIntervalModifier = Mathf.Clamp(attack, MIN_ATTACK_INTERVAL_RATIO, MAX_ATTACK_INTERVAL_RATIO);
            DamageTakenModifier = damage; // 不 Clamp——允许无敌(0)和脆弱(×5)

            HasActivePierce = hasPierce;
            CritRateBonus = critBonus;
            CritMultiplierOverride = critMultOverride;
            PickupRadiusModifier = pickupRadius;
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
            public BuffTag Tag;
            public StackMode StackMode;
            public int MaxStacks;
            public int CurrentStacks;
            public float Duration;
            public float RemainingTime;
            public float MoveSpeedMod;
            public float AttackIntervalMod;
            public float DamageTakenMod;
            public float BulletCountMod;
            public bool GrantsPierce;
            public float CritRateBonus;
            public float CritMultiplierOverride;
            public float PickupRadiusMod;
            public int VfxInstanceId; // -1 = 无 VFX
        }

        private struct DotSlot
        {
            public int DotId;
            public BuffTag Tag;
            public int DamagePerTick;
            public float Interval;
            public float RemainingTime;
            public float Timer;
            public int VfxInstanceId;
        }
    }
}
