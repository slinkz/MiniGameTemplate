using UnityEngine;
using MiniGameTemplate.Danmaku;

namespace MiniGameTemplate.Entity
{
    /// <summary>
    /// Entity vs Entity 碰撞求解器。
    /// 独立于弹幕 CollisionSolver（那是弹丸/激光/喷雾 vs TargetRegistry 目标）。
    /// 
    /// 功能：
    /// 1. 圆 vs 圆碰撞检测（O(n²)，Phase 2 Entity≤64 足够用）
    /// 2. 阵营碰撞矩阵过滤（复用 EnumCamp，同阵营不碰撞）
    /// 3. 推力分离（防重叠，等量反向推开）
    /// 4. 接触伤害（可配置 ContactDamage + ContactDamageInterval）
    /// 
    /// 由 EntitySystemBootstrap 驱动：
    ///   时序位于 EntityManager.Tick() 之后、EntityViewBridge.SyncAll() 之前。
    ///   这保证碰撞分离后视觉位置同步到修正后的逻辑位置。
    /// 
    /// 零 GC 设计：预分配数组，无临时 List/Array。
    /// </summary>
    public class EntityCollisionSolver
    {
        // ──────────── 碰撞对缓冲区（预分配，避免每帧 alloc）────────────
        private const int MAX_PAIRS = 256; // 64 Entity 最多 64*63/2 = 2016 对，取 256 限制实际处理数量

        private readonly EntityCollisionPair[] _pairs = new EntityCollisionPair[MAX_PAIRS];
        private int _pairCount;

        // ──────────── 接触伤害冷却追踪 ────────────
        // 使用 pair hash → cooldown timer 的简单预分配数组
        private const int MAX_COOLDOWNS = 128;
        private readonly ContactCooldown[] _cooldowns = new ContactCooldown[MAX_COOLDOWNS];
        private int _cooldownCount;

        // ──────────── 配置 ────────────
        private float _separationStrength = 1.0f;

        /// <summary>推力分离强度乘数（默认 1.0）</summary>
        public float SeparationStrength
        {
            get => _separationStrength;
            set => _separationStrength = Mathf.Max(0f, value);
        }

        // ──────────── 公共 API ────────────

        /// <summary>本帧碰撞对数量（调试用）</summary>
        public int PairCount => _pairCount;

        /// <summary>
        /// 每帧调用一次：检测所有 Entity 对之间的碰撞并应用分离/伤害。
        /// </summary>
        /// <param name="manager">EntityManager（获取活跃 Entity 列表）</param>
        /// <param name="dt">帧间隔时间</param>
        public void Solve(EntityManager manager, float dt)
        {
            _pairCount = 0;

            var entities = manager.ActiveEntities;
            int count = entities.Count;
            if (count < 2) return;

            // ──── 宽阶段 + 窄阶段（O(n²) 暴力扫描，Phase 2 规模足够）────
            for (int i = 0; i < count - 1; i++)
            {
                var a = entities[i];
                if (!IsCollidable(a)) continue;

                for (int j = i + 1; j < count; j++)
                {
                    var b = entities[j];
                    if (!IsCollidable(b)) continue;

                    // 阵营过滤
                    if (!ShouldCollide(a.Camp, b.Camp)) continue;

                    // 碰撞层过滤（同层才碰撞，0 = 默认层与所有碰撞）
                    if (!LayerCanCollide(a.ConfigSO.CollisionLayer, b.ConfigSO.CollisionLayer))
                        continue;

                    // 构造 Hitbox 做形状碰撞检测（支持圆+矩形）
                    var hitboxA = GetEntityHitbox(a);
                    var hitboxB = GetEntityHitbox(b);

                    if (!HitboxMath.HitboxVsHitbox(in hitboxA, in hitboxB)) continue;

                    // 碰撞！计算距离用于分离
                    float dx = a.Position.x - b.Position.x;
                    float dy = a.Position.y - b.Position.y;
                    float distSq = dx * dx + dy * dy;
                    float dist = Mathf.Sqrt(distSq);
                    // 等效半径和（用于分离计算）
                    float radiusSum = GetEffectiveRadius(a) + GetEffectiveRadius(b);

                    // 记录碰撞对
                    if (_pairCount < MAX_PAIRS)
                    {
                        _pairs[_pairCount++] = new EntityCollisionPair
                        {
                            EntityA = a,
                            EntityB = b,
                            Distance = dist,
                            RadiusSum = radiusSum,
                            DeltaX = dx,
                            DeltaY = dy
                        };
                    }
                }
            }

            // ──── 碰撞响应：推力分离 + 接触伤害 ────
            for (int i = 0; i < _pairCount; i++)
            {
                ref var pair = ref _pairs[i];
                ApplySeparation(ref pair, dt);
                ApplyContactDamage(ref pair, dt, manager);
            }

            // ──── 冷却计时器递减 ────
            TickCooldowns(dt);
        }

        /// <summary>清除所有冷却记录（场景切换 / DespawnAll 时调用）</summary>
        public void ClearCooldowns()
        {
            _cooldownCount = 0;
        }

        // ──────────── 内部方法 ────────────

        /// <summary>Entity 是否可参与碰撞</summary>
        private static bool IsCollidable(Entity e)
        {
            return e.IsAlive
                && !e.IsPendingDespawn
                && e.ConfigSO != null
                && HasValidCollisionSize(e.ConfigSO)
                && e.ConfigSO.EnableEntityCollision;
        }

        /// <summary>是否有有效碰撞尺寸（兼容 Circle 与 Rect 两种形状）</summary>
        private static bool HasValidCollisionSize(EntityConfigSO cfg)
        {
            if (cfg.HitboxType == HitboxShape.Rect)
                return cfg.CollisionHalfWidth > 0f || cfg.CollisionHalfHeight > 0f;
            return cfg.CollisionRadius > 0f;
        }

        /// <summary>从 Entity 配置构造 Hitbox</summary>
        private static Hitbox GetEntityHitbox(Entity e)
        {
            if (e.ConfigSO.HitboxType == HitboxShape.Rect)
                return new Hitbox(e.Position, e.ConfigSO.CollisionHalfWidth, e.ConfigSO.CollisionHalfHeight);
            return new Hitbox(e.Position, e.ConfigSO.CollisionRadius);
        }

        /// <summary>
        /// 等效半径（用于分离计算——矩形取对角半长）。
        /// NOTE: 对角线半长作为等效半径会略微过度分离（最坏情况≈√2×实际），
        /// 但保证不会穿透。未来若有矩形 Entity 参与碰撞且分离手感需精调，
        /// 可改为按轴分离（SAT）替代此近似。
        /// </summary>
        private static float GetEffectiveRadius(Entity e)
        {
            if (e.ConfigSO.HitboxType == HitboxShape.Rect)
            {
                float hw = e.ConfigSO.CollisionHalfWidth;
                float hh = e.ConfigSO.CollisionHalfHeight;
                return Mathf.Sqrt(hw * hw + hh * hh);
            }
            return e.ConfigSO.CollisionRadius;
        }

        /// <summary>阵营碰撞规则（与弹幕系统一致）</summary>
        private static bool ShouldCollide(EnumCamp campA, EnumCamp campB)
        {
            // 同阵营不碰撞
            if (campA == campB) return false;
            // Neutral 与所有碰撞
            if (campA == EnumCamp.Neutral || campB == EnumCamp.Neutral) return true;
            // 不同非 Neutral 阵营碰撞
            return true;
        }

        /// <summary>碰撞层过滤（0 = 默认层，与所有层碰撞；相同非零层才碰撞）</summary>
        private static bool LayerCanCollide(int layerA, int layerB)
        {
            // 任一为 0（默认层）→ 与所有碰撞
            if (layerA == 0 || layerB == 0) return true;
            // 都非零 → 必须同层（简单实现，可扩展为位掩码矩阵）
            return layerA == layerB;
        }

        /// <summary>推力分离——将重叠的两个 Entity 等量推开</summary>
        private void ApplySeparation(ref EntityCollisionPair pair, float dt)
        {
            float overlap = pair.RadiusSum - pair.Distance;
            if (overlap <= 0f) return;

            float dist = pair.Distance;
            float nx, ny;

            if (dist > 0.001f)
            {
                // 法线方向：A→B（归一化）
                float invDist = 1f / dist;
                nx = pair.DeltaX * invDist;
                ny = pair.DeltaY * invDist;
            }
            else
            {
                // 完全重叠——用随机偏移避免卡死
                nx = 1f;
                ny = 0f;
            }

            // 等量分离（各推一半 overlap）
            float push = overlap * 0.5f * _separationStrength;

            var moveA = pair.EntityA.GetComponent(ComponentType.Movement) as MovementComponent;
            var moveB = pair.EntityB.GetComponent(ComponentType.Movement) as MovementComponent;

            if (moveA != null)
            {
                var posA = pair.EntityA.Position;
                posA.x += nx * push;
                posA.y += ny * push;
                moveA.SetPosition(posA);
            }

            if (moveB != null)
            {
                var posB = pair.EntityB.Position;
                posB.x -= nx * push;
                posB.y -= ny * push;
                moveB.SetPosition(posB);
            }
        }

        /// <summary>接触伤害——冷却间隔到达后对双方施加伤害</summary>
        private void ApplyContactDamage(ref EntityCollisionPair pair, float dt, EntityManager manager)
        {
            // A 对 B 造成接触伤害
            TryApplyDamage(pair.EntityA, pair.EntityB, manager);
            // B 对 A 造成接触伤害
            TryApplyDamage(pair.EntityB, pair.EntityA, manager);
        }

        private void TryApplyDamage(Entity attacker, Entity victim, EntityManager manager)
        {
            if (attacker.ConfigSO.ContactDamage <= 0) return;

            // 冷却检查
            uint pairKey = GetPairKey(attacker.Id, victim.Id);
            int cooldownIdx = FindCooldown(pairKey);

            if (cooldownIdx >= 0 && _cooldowns[cooldownIdx].RemainingTime > 0f)
                return; // 冷却中

            // 通过 OnCollisionHit 事件统一走伤害管线（闪白/击退/被动触发）
            var dmgCtx = new DamageContext
            {
                BaseDamage = attacker.ConfigSO.ContactDamage,
                AttackerId = attacker.Id,
                HitType = CollisionEventType.ContactHit,
                SourcePosition = attacker.Position,
                HasSourcePosition = true
            };
            victim.EventBus.Publish(new OnCollisionHit { Context = dmgCtx });

            // 设置冷却
            float interval = attacker.ConfigSO.ContactDamageInterval;
            if (interval <= 0f) interval = 0.5f; // 默认 0.5s 冷却

            if (cooldownIdx >= 0)
            {
                _cooldowns[cooldownIdx].RemainingTime = interval;
            }
            else if (_cooldownCount < MAX_COOLDOWNS)
            {
                _cooldowns[_cooldownCount++] = new ContactCooldown
                {
                    PairKey = pairKey,
                    RemainingTime = interval
                };
            }
        }

        /// <summary>递减所有冷却计时器，移除已过期的</summary>
        private void TickCooldowns(float dt)
        {
            for (int i = _cooldownCount - 1; i >= 0; i--)
            {
                _cooldowns[i].RemainingTime -= dt;
                if (_cooldowns[i].RemainingTime <= -1f) // 过期 1 秒后移除（防止频繁重建）
                {
                    // swap-remove
                    _cooldowns[i] = _cooldowns[--_cooldownCount];
                }
            }
        }

        private int FindCooldown(uint pairKey)
        {
            for (int i = 0; i < _cooldownCount; i++)
            {
                if (_cooldowns[i].PairKey == pairKey)
                    return i;
            }
            return -1;
        }

        /// <summary>生成有序 pair key（小 ID 在高位）</summary>
        private static uint GetPairKey(EntityId a, EntityId b)
        {
            uint va = a.Value;
            uint vb = b.Value;
            // 确保有序性：attacker→victim 方向
            return (va << 16) | (vb & 0xFFFF);
        }

        // ──────────── 内部数据结构 ────────────

        private struct EntityCollisionPair
        {
            public Entity EntityA;
            public Entity EntityB;
            public float Distance;
            public float RadiusSum;
            public float DeltaX;
            public float DeltaY;
        }

        private struct ContactCooldown
        {
            public uint PairKey;
            public float RemainingTime;
        }
    }
}
