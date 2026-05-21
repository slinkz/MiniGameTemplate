using UnityEngine;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// 被动技能组件——管理最多 3 个被动技能的独立 CD 与自动激活（V2 Sprint 3）。
    /// 
    /// ComponentType.Passive = 12
    /// TickOrder = 60（在 Buff=50 之后——被动需要查询/施加 Buff）
    /// 
    /// 被动分两种触发模式：
    /// - AutoOnReady：CD 就绪自动激活（PA-01 穿透 / PA-02 暴击 / PA-03 磁吸）
    /// - OnHit：被命中时触发（PA-04 尾翼反击），需订阅 OnCollisionHit 事件
    /// 
    /// 激活行为分两种：
    /// - Buff 桥接型：ApplyBuff(LinkedBuff) → Buff 到期自动清除
    /// - 即时型：执行 ActivateEffects[]（发射弹幕等）
    /// 
    /// 初始化模式：
    /// - 外部注入（V2）：BattleController 调用 InitWithPassives(passives[])
    /// - 无兜底（不同于 SkillComponent）——无被动时组件 inactive
    /// </summary>
    public sealed class PassiveComponent : IEntityComponent, ITickable
    {
        // ── IEntityComponent ──
        public ComponentType Type => ComponentType.Passive;
        public bool IsActive { get; private set; }
        public void SetActive(bool active) => IsActive = active;

        // ── ITickable ──
        public int TickOrder => TickOrders.Passive; // 60

        // ── 常量 ──
        public const int MAX_PASSIVES = 3;

        // ── 内部状态 ──
        private Entity _owner;
        private readonly PassiveSlot[] _slots = new PassiveSlot[MAX_PASSIVES];
        private int _activeSlotCount;

        /// <summary>活跃被动数量</summary>
        public int ActiveSlotCount => _activeSlotCount;

        /// <summary>获取指定槽位状态（UI 显示用）</summary>
        public ref readonly PassiveSlot GetSlot(int index) => ref _slots[index];

        // ── 生命周期 ──

        public void Init(Entity owner)
        {
            _owner = owner;
            _activeSlotCount = 0;
            IsActive = false; // 默认不启用，需 InitWithPassives 注入
        }

        public void Reset()
        {
            // 取消 OnHit 订阅
            if (_owner != null)
            {
                _owner.EventBus.Unsubscribe<OnCollisionHit>(OnOwnerHit);
            }

            _owner = null;
            _activeSlotCount = 0;
            for (int i = 0; i < MAX_PASSIVES; i++)
                _slots[i] = default;
            IsActive = false;
        }

        /// <summary>
        /// V2 外部注入装备被动（由 BattleController 调用）。
        /// </summary>
        public void InitWithPassives(PassiveAbilitySO[] equipped)
        {
            // 先清空
            for (int i = 0; i < MAX_PASSIVES; i++)
                _slots[i] = default;

            if (equipped == null || equipped.Length == 0)
            {
                _activeSlotCount = 0;
                IsActive = false;
                return;
            }

            int count = Mathf.Min(equipped.Length, MAX_PASSIVES);
            bool hasOnHit = false;

            for (int i = 0; i < count; i++)
            {
                if (equipped[i] == null) continue;
                _slots[i].Config = equipped[i];
                _slots[i].TotalCooldown = equipped[i].CooldownTime;
                _slots[i].CooldownTimer = 1f; // 初始短 CD，避免开场瞬发
                _slots[i].IsEffectActive = false;
                _slots[i].ActiveTimer = 0f;

                if (equipped[i].TriggerMode == PassiveTriggerMode.OnHit)
                    hasOnHit = true;
            }

            _activeSlotCount = count;
            IsActive = count > 0;

            // OnHit 型被动需要订阅碰撞事件
            if (hasOnHit && _owner != null)
            {
                _owner.EventBus.Subscribe<OnCollisionHit>(OnOwnerHit);
            }
        }

        // ── Tick ──

        public void Tick(float dt)
        {
            if (_activeSlotCount == 0) return;

            // 死亡时不 tick
            if (!_owner.IsAlive || _owner.IsPendingDespawn) return;

            for (int i = 0; i < MAX_PASSIVES; i++)
            {
                if (_slots[i].Config == null) continue;

                if (_slots[i].IsEffectActive)
                {
                    // 效果持续中——倒计时
                    _slots[i].ActiveTimer -= dt;
                    if (_slots[i].ActiveTimer <= 0f)
                    {
                        _slots[i].IsEffectActive = false;
                        // Buff 到期由 BuffComponent 自动处理，此处仅标记状态
                    }
                }
                else
                {
                    // CD 中——倒计时
                    _slots[i].CooldownTimer -= dt;
                    if (_slots[i].CooldownTimer <= 0f)
                    {
                        // AutoOnReady 型：CD 就绪自动激活
                        if (_slots[i].Config.TriggerMode == PassiveTriggerMode.AutoOnReady)
                        {
                            ActivateSlot(ref _slots[i]);
                        }
                        else
                        {
                            // OnHit 型：CD 归零等待触发，不自动消耗
                            _slots[i].CooldownTimer = 0f;
                        }
                    }
                }
            }
        }

        // ── OnHit 触发 ──

        /// <summary>
        /// 飞机被命中时的回调。检查所有 OnHit 型被动，CD 就绪则触发。
        /// 在碰撞事件级触发（先于 IDamageModifier 链），无敌帧期间仍触发。
        /// </summary>
        private void OnOwnerHit(OnCollisionHit evt)
        {
            if (!IsActive || !_owner.IsAlive) return;

            for (int i = 0; i < MAX_PASSIVES; i++)
            {
                if (_slots[i].Config == null) continue;
                if (_slots[i].Config.TriggerMode != PassiveTriggerMode.OnHit) continue;
                if (_slots[i].CooldownTimer > 0f) continue; // CD 中
                if (_slots[i].IsEffectActive) continue; // 已激活

                ActivateSlot(ref _slots[i]);
            }
        }

        // ── 激活逻辑 ──

        private void ActivateSlot(ref PassiveSlot slot)
        {
            var config = slot.Config;

            // 1. Buff 桥接型：施加 LinkedBuff
            if (config.LinkedBuff != null)
            {
                var buff = _owner.GetComponent(ComponentType.Buff) as BuffComponent;
                buff?.ApplyBuff(config.LinkedBuff);

                // 标记效果持续（以 Buff Duration 为准）
                slot.IsEffectActive = config.LinkedBuff.Duration > 0f;
                slot.ActiveTimer = config.LinkedBuff.Duration;
            }

            // 2. 即时型：执行 ActivateEffects
            if (config.ActivateEffects != null && config.ActivateEffects.Length > 0)
            {
                var ctx = new SkillContext
                {
                    Caster = _owner,
                    CastPosition = _owner.Position,
                    AimDirection = Vector2.up, // 被动效果默认向上
                    DeltaTime = 0f,
                    SkillConfig = null, // 被动无 SkillConfig
                };

                for (int i = 0; i < config.ActivateEffects.Length; i++)
                {
                    config.ActivateEffects[i]?.Execute(ctx);
                }

                // 即时型无持续效果
                if (config.LinkedBuff == null)
                {
                    slot.IsEffectActive = false;
                }
            }

            // 3. 重置 CD
            slot.CooldownTimer = slot.TotalCooldown;
        }
    }

    /// <summary>
    /// 被动技能槽结构体（值类型，固定数组，零 GC）。
    /// </summary>
    public struct PassiveSlot
    {
        /// <summary>被动配置（null = 空槽）</summary>
        public PassiveAbilitySO Config;

        /// <summary>总冷却时间（秒）</summary>
        public float TotalCooldown;

        /// <summary>冷却剩余时间（0 = 就绪）</summary>
        public float CooldownTimer;

        /// <summary>效果是否激活中（Buff 持续期间 = true）</summary>
        public bool IsEffectActive;

        /// <summary>效果剩余持续时间</summary>
        public float ActiveTimer;

        /// <summary>是否为空槽位</summary>
        public bool IsEmpty => Config == null;

        /// <summary>CD 进度（0~1，0=就绪，1=刚开始 CD）</summary>
        public float CooldownProgress => TotalCooldown <= 0
            ? 0f
            : Mathf.Clamp01(CooldownTimer / TotalCooldown);
    }
}
