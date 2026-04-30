using UnityEngine;
using MiniGameTemplate.Pool;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity 受击表现管线——订阅 EntityEventBus 事件，驱动表现层反馈。
    /// P1.11 集成验收的核心交付物。
    /// 
    /// 职责（TDD §3.15 表现层规则）：
    /// - OnCollisionHit → 受击闪白 + 击退 + 伤害数字 + 受击特效
    /// - OnDeath → 死亡延迟 + 死亡特效 + 延迟回收
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

        // ──────────── 伤害数字管理 ────────────

        private const int MAX_DAMAGE_NUMBERS = 32;
        private readonly DamageNumberState[] _damageNumbers = new DamageNumberState[MAX_DAMAGE_NUMBERS];
        private int _damageNumberCount;

        // ──────────── 死亡延迟管理 ────────────

        private const int MAX_DEATH_DELAY = 32;
        private readonly DeathDelayState[] _deathDelays = new DeathDelayState[MAX_DEATH_DELAY];
        private int _deathDelayCount;

        // ──────────── 依赖 ────────────

        private readonly PoolManager _poolManager;
        private readonly PoolDefinition _damageNumberPool; // 伤害数字 Prefab 池（可选）

        public EntityHitReactionHandler(PoolManager poolManager, PoolDefinition damageNumberPool)
        {
            _poolManager = poolManager;
            _damageNumberPool = damageNumberPool;
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
        /// 每帧更新表现（闪白淡出 + 伤害数字漂浮 + 死亡延迟倒计时）。
        /// 由 Bootstrap 在 ViewBridge.SyncAll() 之后调用。
        /// </summary>
        public void Tick(float dt, EntityManager entityManager)
        {
            TickFlash(dt);
            TickDamageNumbers(dt);
            TickDeathDelays(dt, entityManager);
        }

        /// <summary>清除所有状态（DespawnAll 时调用）</summary>
        public void ClearAll()
        {
            // 回收伤害数字 GO
            for (int i = 0; i < _damageNumberCount; i++)
            {
                if (_damageNumbers[i].Go != null && _damageNumberPool != null)
                    _poolManager.Return(_damageNumberPool, _damageNumbers[i].Go);
            }
            _damageNumberCount = 0;
            _flashCount = 0;
            _deathDelayCount = 0;
        }

        // ──────────── 事件响应 ────────────

        private void OnHit(Entity entity, EntityConfigSO config, OnCollisionHit evt)
        {
            if (entity == null || !entity.IsAlive) return;

            // 1. 受击闪白
            if (config.HitFlashDuration > 0f)
            {
                RequestFlash(entity.Id, config.HitFlashDuration);
            }

            // 2. 击退
            if (config.KnockbackDistance > 0f && config.KnockbackDuration > 0f)
            {
                var movement = entity.GetComponent(ComponentType.Movement) as MovementComponent;
                if (movement != null)
                {
                    // 击退方向：从攻击者朝向被击者（Phase 1 简化：默认向右）
                    // 正式实现需从 DamageContext.AttackerId 查找攻击者位置
                    Vector2 knockDir = Vector2.right; // Phase 1 fallback
                    if (evt.Context.AttackerId.Value != 0)
                    {
                        // 尝试从 EntityManager 获取攻击者位置
                        var mgr = EntityManagerAccessor.Instance;
                        if (mgr != null)
                        {
                            var attacker = FindEntityById(mgr, evt.Context.AttackerId.Value);
                            if (attacker != null)
                            {
                                Vector2 dir = entity.Position - attacker.Position;
                                if (dir.sqrMagnitude > 0.01f)
                                    knockDir = dir.normalized;
                            }
                        }
                    }
                    movement.ApplyKnockback(knockDir, config.KnockbackDistance, config.KnockbackDuration);
                }
            }

            // 3. 受击特效
            if (config.HitEffect != null && _poolManager != null)
            {
                var fx = _poolManager.Get(config.HitEffect);
                if (fx != null)
                {
                    fx.transform.position = new Vector3(entity.Position.x, entity.Position.y, 0f);
                    // 自动回收由 ParticleAutoReturn 处理（或 Timer）
                }
            }

            // 4. 伤害数字
            if (config.ShowDamageNumber && _damageNumberPool != null && _poolManager != null)
            {
                SpawnDamageNumber(entity.Position, evt.Context.BaseDamage);
            }

            // 5. 通过 HealthComponent 扣血
            var health = entity.GetComponent(ComponentType.Health) as HealthComponent;
            health?.TakeDamage(evt.Context);
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

        // ──────────── 伤害数字 ────────────

        private void SpawnDamageNumber(Vector2 position, int damage)
        {
            if (_damageNumberCount >= MAX_DAMAGE_NUMBERS) return;

            var go = _poolManager.Get(_damageNumberPool);
            if (go == null) return;

            go.transform.position = new Vector3(position.x, position.y + 0.5f, 0f);

            var tm = go.GetComponentInChildren<TextMesh>();
            if (tm != null) tm.text = damage.ToString();

            _damageNumbers[_damageNumberCount++] = new DamageNumberState
            {
                Go = go,
                Timer = 0f,
                StartY = position.y + 0.5f,
            };
        }

        private const float DAMAGE_NUMBER_DURATION = 0.8f;
        private const float DAMAGE_NUMBER_RISE = 1.0f;

        private void TickDamageNumbers(float dt)
        {
            for (int i = _damageNumberCount - 1; i >= 0; i--)
            {
                ref var state = ref _damageNumbers[i];
                state.Timer += dt;

                if (state.Go != null)
                {
                    float t = state.Timer / DAMAGE_NUMBER_DURATION;
                    float y = state.StartY + DAMAGE_NUMBER_RISE * t;
                    var pos = state.Go.transform.position;
                    state.Go.transform.position = new Vector3(pos.x, y, pos.z);

                    // 淡出（通过缩放模拟，Phase 2 用 CanvasGroup.alpha）
                    float scale = Mathf.Lerp(1f, 0.3f, t);
                    state.Go.transform.localScale = new Vector3(scale, scale, 1f);
                }

                if (state.Timer >= DAMAGE_NUMBER_DURATION)
                {
                    // 回收
                    if (state.Go != null && _damageNumberPool != null)
                        _poolManager.Return(_damageNumberPool, state.Go);

                    // swap-remove
                    _damageNumbers[i] = _damageNumbers[--_damageNumberCount];
                }
            }
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
            var entities = mgr.ActiveEntities;
            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Id.Value == id)
                    return entities[i];
            }
            return null;
        }

        // ──────────── 内部状态结构 ────────────

        private struct FlashState
        {
            public EntityId EntityId;
            public float Duration;
            public float Remaining;
        }

        private struct DamageNumberState
        {
            public GameObject Go;
            public float Timer;
            public float StartY;
        }

        private struct DeathDelayState
        {
            public Entity Entity;
            public float Remaining;
        }
    }
}
