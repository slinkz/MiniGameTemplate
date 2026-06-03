using UnityEngine;
using MiniGameTemplate.Pool;
using MiniGameTemplate.Rendering;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 受击表现管线——订阅 EntityEventBus 事件，驱动表现层反馈。
    /// P1.11 集成验收的核心交付物。
    /// 
    /// 职责（TDD §3.15 表现层规则）：
    /// - OnCollisionHit → 受击闪白 + 击退 + 伤害飘字 + 受击特效
    /// - OnDeath → 死亡延迟 + 死亡特效 + 延迟回收
    /// 
    /// FLOATING_TEXT_TDD：伤害飘字迁移到 FloatingTextSystem（RBM 渲染），
    /// 删除旧 TextMesh 对象池飘字管线。
    /// 
    /// 由 EntitySystemBootstrap 在 Entity 生成时注册到 EntityEventBus。
    /// 框架级组件——不是游戏层脚本（框架确保事件携带足够信息）。
    /// </summary>
    public class EntityHitReactionHandler
    {
        // ──────────── 闪白管理 ────────────

        private const int MAX_FLASH = 64;
        private readonly FlashState[] _flashStates = new FlashState[MAX_FLASH];
        private int _flashCount;

        // ──────────── 死亡延迟管理 ────────────

        private const int MAX_DEATH_DELAY = 32;
        private readonly DeathDelayState[] _deathDelays = new DeathDelayState[MAX_DEATH_DELAY];
        private int _deathDelayCount;

        // ──────────── 依赖 ────────────

        private readonly PoolManager _poolManager;
        private readonly FloatingTextSystem _floatingText; // FLOATING_TEXT_TDD：RBM 飘字系统（可 null）

        public EntityHitReactionHandler(PoolManager poolManager, FloatingTextSystem floatingText)
        {
            _poolManager = poolManager;
            _floatingText = floatingText;
        }

        // ──────────── Entity 生命周期钩子 ────────────

        /// <summary>
        /// Entity 生成时注册事件监听。由 Bootstrap 在 OnSpawned 中调用。
        /// </summary>
        public void RegisterEntity(Entity entity, EntityConfigSO config)
        {
            entity.EventBus.Subscribe<OnCollisionHit>(e => OnHit(entity, config, e));
            entity.EventBus.Subscribe<OnDeath>(e => OnDeath(entity, config, e));
        }

        /// <summary>
        /// 每帧更新表现（闪白淡出 + 死亡延迟倒计时）。
        /// 由 Bootstrap 在 ViewBridge.SyncAll() 之后调用。
        /// 
        /// FLOATING_TEXT_TDD：飘字更新已迁移到 DanmakuSystem.LateUpdate → Rebuild，
        /// 此处不再管理飘字生命周期。
        /// </summary>
        public void Tick(float dt, EntityManager entityManager)
        {
            TickFlash(dt);
            TickDeathDelays(dt, entityManager);
        }

        /// <summary>清除所有状态（DespawnAll 时调用）</summary>
        public void ClearAll()
        {
            // FLOATING_TEXT_TDD：RBM 飘字由 DanmakuSystem.ClearAll() 统一清除，无需此处管理
            _flashCount = 0;
            _deathDelayCount = 0;
        }

        // ──────────── 事件响应 ────────────

        private void OnHit(Entity entity, EntityConfigSO config, OnCollisionHit evt)
        {
            if (entity == null || !entity.IsAlive) return;

            // 提取 context 副本（struct 值类型），后续用 ref 传入 TakeDamage
            var context = evt.Context;

            // 1. 通过 HealthComponent 扣血（P2.4：ref 传递，FinalDamage 在内部计算后回写）
            var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
            health?.TakeDamage(ref context);

            // 2. 受击闪白
            if (config.HitFlashDuration > 0f)
            {
                RequestFlash(entity.Id, config.HitFlashDuration);
            }

            // 3. 击退（P2.4：支持 KnockbackCurve + SourcePosition 精确方向）
            if (config.KnockbackDistance > 0f && config.KnockbackDuration > 0f)
            {
                var movement = entity.GetComponent(ComponentType.Movement) as MovementComponent;
                if (movement != null)
                {
                    // 击退方向优先级：SourcePosition（弹丸位置）> AttackerId（攻击者Entity位置）> fallback 右
                    Vector2 knockDir = Vector2.right; // fallback

                    if (context.HasSourcePosition)
                    {
                        // 从弹丸/激光/喷雾位置 → 被击者位置
                        Vector2 dir = entity.Position - context.SourcePosition;
                        if (dir.sqrMagnitude > 0.001f)
                            knockDir = dir.normalized;
                    }
                    else if (context.AttackerId.Value != 0)
                    {
                        var mgr = EntityManagerAccessor.Instance;
                        if (mgr != null)
                        {
                            var attacker = FindEntityById(mgr, context.AttackerId.Value);
                            if (attacker != null)
                            {
                                Vector2 dir = entity.Position - attacker.Position;
                                if (dir.sqrMagnitude > 0.01f)
                                    knockDir = dir.normalized;
                            }
                        }
                    }
                    movement.ApplyKnockback(knockDir, config.KnockbackDistance, config.KnockbackDuration, config.KnockbackCurve);
                }
            }

            // 4. 受击特效
            if (config.HitEffect != null && _poolManager != null)
            {
                var fx = _poolManager.Get(config.HitEffect);
                if (fx != null)
                {
                    fx.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);
                }
            }

            // 5. 伤害飘字（FLOATING_TEXT_TDD：走 FloatingTextSystem RBM 渲染）
            if (config.ShowDamageNumber && _floatingText != null)
            {
                int displayDmg = context.FinalDamage > 0 ? context.FinalDamage : context.BaseDamage;
                var color = context.IsCritical
                    ? FloatingTextColors.Critical
                    : FloatingTextColors.Normal;
                // PK-R2 UA-009：保持与旧 TextMesh 飘字一致的 +0.5f Y 偏移
                _floatingText.Spawn(entity.Position + new Vector2(0, 0.5f), displayDmg, color, context.IsCritical);
            }
        }

        private void OnDeath(Entity entity, EntityConfigSO config, OnDeath evt)
        {
            if (entity == null) return;

            // 1. 死亡特效
            if (config.DeathEffect != null && _poolManager != null)
            {
                var fx = _poolManager.Get(config.DeathEffect);
                if (fx != null)
                {
                    fx.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);
                }
            }

            // 2. 死亡延迟回收
            if (config.DeathDelay > 0f)
            {
                RequestDeathDelay(entity, config.DeathDelay);
            }
            else
            {
                // 立即回收
                EntityManagerAccessor.Instance?.Despawn(entity);
            }
        }

        // ──────────── 闪白 ────────────

        private void RequestFlash(EntityId entityId, float duration)
        {
            // 检查是否已有此 Entity 的闪白（刷新时间）
            for (int i = 0; i < _flashCount; i++)
            {
                if (_flashStates[i].EntityId.Value == entityId.Value)
                {
                    _flashStates[i].Remaining = duration;
                    _flashStates[i].Duration = duration;
                    return;
                }
            }

            if (_flashCount >= MAX_FLASH) return;
            _flashStates[_flashCount++] = new FlashState
            {
                EntityId = entityId,
                Duration = duration,
                Remaining = duration,
            };
        }

        private void TickFlash(float dt)
        {
            for (int i = _flashCount - 1; i >= 0; i--)
            {
                _flashStates[i].Remaining -= dt;
                if (_flashStates[i].Remaining <= 0f)
                {
                    // swap-remove
                    _flashStates[i] = _flashStates[--_flashCount];
                }
            }
        }

        /// <summary>查询 Entity 是否正在闪白（ViewBridge 调用）</summary>
        public bool IsFlashing(EntityId entityId)
        {
            for (int i = 0; i < _flashCount; i++)
            {
                if (_flashStates[i].EntityId.Value == entityId.Value)
                    return true;
            }
            return false;
        }

        /// <summary>获取闪白进度（0=刚开始闪，1=闪完）（ViewBridge 调用）</summary>
        public float GetFlashProgress(EntityId entityId)
        {
            for (int i = 0; i < _flashCount; i++)
            {
                if (_flashStates[i].EntityId.Value == entityId.Value)
                {
                    float d = _flashStates[i].Duration;
                    return d > 0f ? 1f - (_flashStates[i].Remaining / d) : 1f;
                }
            }
            return 1f;
        }

        // ──────────── 死亡延迟 ────────────

        private void RequestDeathDelay(Entity entity, float delay)
        {
            if (_deathDelayCount >= MAX_DEATH_DELAY)
            {
                // 满了就立即回收
                EntityManagerAccessor.Instance?.Despawn(entity);
                return;
            }

            _deathDelays[_deathDelayCount++] = new DeathDelayState
            {
                Entity = entity,
                Remaining = delay,
            };
        }

        private void TickDeathDelays(float dt, EntityManager entityManager)
        {
            for (int i = _deathDelayCount - 1; i >= 0; i--)
            {
                ref var state = ref _deathDelays[i];
                state.Remaining -= dt;

                if (state.Remaining <= 0f)
                {
                    // 延迟到期，回收 Entity
                    if (state.Entity != null && state.Entity.IsAlive)
                    {
                        entityManager.Despawn(state.Entity);
                    }

                    // swap-remove
                    _deathDelays[i] = _deathDelays[--_deathDelayCount];
                }
            }
        }

        // ──────────── 工具方法 ────────────

        private static Entity FindEntityById(EntityManager mgr, uint id)
        {
            return mgr.FindEntityById(id);
        }

        // ──────────── 内部状态结构 ────────────

        private struct FlashState
        {
            public EntityId EntityId;
            public float Duration;
            public float Remaining;
        }

        private struct DeathDelayState
        {
            public Entity Entity;
            public float Remaining;
        }
    }
}
