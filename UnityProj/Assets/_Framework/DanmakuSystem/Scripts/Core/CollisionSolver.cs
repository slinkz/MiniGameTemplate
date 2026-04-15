using MiniGameTemplate.Audio;
using MiniGameTemplate.Pool;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 碰撞检测结果。
    /// </summary>
    public struct CollisionResult
    {
        /// <summary>本帧是否有任何目标被命中</summary>
        public bool HasAnyHit;

        /// <summary>本帧总伤害（所有目标累计）</summary>
        public int TotalDamage;

        /// <summary>本帧被命中的目标数量</summary>
        public int HitTargetCount;

        /// <summary>本帧是否发生了可用于播放命中特效的非玩家命中</summary>
        public bool NonPlayerHit;

        /// <summary>最后一个非玩家目标被命中的位置（用于播放命中特效）</summary>
        public Vector2 NonPlayerHitPosition;

        /// <summary>本帧 Player 阵营目标是否被命中</summary>
        public bool PlayerHit;

        /// <summary>本帧 Player 阵营目标累计受到的伤害</summary>
        public int PlayerDamage;

        /// <summary>最后一个被命中的 Player 目标位置（用于飘字定位）</summary>
        public Vector2 PlayerHitPosition;
    }

    /// <summary>
    /// 统一碰撞调度器。
    /// 7 阶段碰撞：弹丸vs目标 → 弹丸vs障碍物 → 弹丸vs屏幕边缘
    ///            → 激光vs目标（多段） → 喷雾vs目标 → 喷雾vs障碍物 → 喷雾vs屏幕边缘。
    /// 激光 vs 障碍物/屏幕边缘的碰撞与折射由 LaserSegmentSolver 在 LaserUpdater 中处理。
    /// </summary>
    public class CollisionSolver
    {
        private Rect _worldBounds;
        private CollisionEventBuffer _eventBuffer;

        // Phase 5/6 共享：标记本帧哪些 spray 触发了 tick（避免 Phase 6 重复推进 TickTimer）
        private static readonly bool[] _sprayTickedThisFrame = new bool[SprayPool.MAX_SPRAYS];

        /// <summary>
        /// 初始化碰撞求解器——设置世界边界和碰撞事件旁路 Buffer。
        /// </summary>
        public void Initialize(DanmakuWorldConfig config, CollisionEventBuffer eventBuffer)
        {
            _worldBounds = config.WorldBounds;
            _eventBuffer = eventBuffer;
        }

        /// <summary>
        /// 统一碰撞检测入口。
        /// </summary>
        public CollisionResult SolveAll(
            BulletWorld bulletWorld,
            LaserPool laserPool,
            SprayPool sprayPool,
            ObstaclePool obstaclePool,
            DanmakuTypeRegistry registry,
            AttachSourceRegistry attachRegistry,
            TargetRegistry targetRegistry,
            IDanmakuVFXRuntime sprayVfxRuntime,
            float dt)

        {
            var result = default(CollisionResult);

            var cores = bulletWorld.Cores;
            int capacity = bulletWorld.Capacity;

            // Phase 1: 弹丸 vs 目标对象
            SolveBulletVsTarget(cores, bulletWorld.Trails, capacity, registry, targetRegistry, _eventBuffer, ref result);

            // Phase 2: 弹丸 vs 障碍物（圆 vs AABB）
            SolveBulletVsObstacle(cores, bulletWorld.Trails, capacity, obstaclePool, registry, ref result);

            // Phase 3: 弹丸 vs 屏幕边缘
            SolveBulletVsScreenEdge(cores, bulletWorld.Trails, capacity, registry, ref result);

            // Phase 4: 激光 vs 目标（多段折射线段 vs 圆）
            SolveLasers(laserPool, targetRegistry, dt, _eventBuffer, ref result);

            // Phase 5: 喷雾 vs 目标
            SolveSprays(sprayPool, targetRegistry, dt, _eventBuffer, ref result);

            // Phase 6: 喷雾 vs 障碍物
            SolveSprayVsObstacle(sprayPool, obstaclePool, registry, dt, ref result);

            // Phase 7: 喷雾 vs 屏幕边缘
            SolveSprayVsScreenEdge(sprayPool, attachRegistry, registry, sprayVfxRuntime);


            return result;
        }

        // ──── Phase 1: 弹丸 vs 目标对象（圆 vs 圆） ────

        private static void SolveBulletVsTarget(
            BulletCore[] cores, BulletTrail[] trails, int capacity,
            DanmakuTypeRegistry registry,
            TargetRegistry targetRegistry,
            CollisionEventBuffer eventBuffer,
            ref CollisionResult result)
        {
            var targets = targetRegistry.Targets;

            for (int i = 0; i < capacity; i++)
            {
                ref var c = ref cores[i];
                if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;
                if (c.Phase != (byte)BulletPhase.Active) continue;

                var bulletFaction = (BulletFaction)c.Faction;

                for (int t = 0; t < TargetRegistry.MAX_TARGETS; t++)
                {
                    var target = targets[t];
                    if (target == null) continue;

                    // 阵营过滤
                    if (!ShouldCollide(bulletFaction, target.Faction)) continue;

                    // Pierce 冷却检查（位掩码：每 bit 对应一个 target 槽位）
                    ushort targetBit = (ushort)(1 << t);
                    if ((c.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0
                        && (c.PierceHitMask & targetBit) != 0)
                        continue;

                    // 圆 vs 圆
                    var hitbox = target.Hitbox;
                    float dx = c.Position.x - hitbox.Center.x;
                    float dy = c.Position.y - hitbox.Center.y;
                    float distSq = dx * dx + dy * dy;
                    float radiusSum = c.Radius + hitbox.Radius;

                    if (distSq >= radiusSum * radiusSum) continue;

                    // 命中
                    result.HasAnyHit = true;
                    var bulletType = registry.BulletTypes[c.TypeIndex];
                    int damage = bulletType.Damage;
                    result.TotalDamage += damage;
                    result.HitTargetCount++;

                    // 记录命中位置
                    if (target.Faction == BulletFaction.Player)
                    {
                        result.PlayerHit = true;
                        result.PlayerDamage += damage;
                        result.PlayerHitPosition = hitbox.Center;
                    }
                    else
                    {
                        result.NonPlayerHit = true;
                        result.NonPlayerHitPosition = hitbox.Center;
                    }

                    // 通知目标
                    target.OnBulletHit(damage, i);

                    // 写入旁路事件 Buffer
                    if (eventBuffer != null)
                    {
                        var evt = new CollisionEvent
                        {
                            BulletIndex = i,
                            TargetSlot = t,
                            Position = hitbox.Center,
                            Damage = damage,
                            SourceFaction = bulletFaction,
                            TargetFaction = target.Faction,
                            EventType = CollisionEventType.BulletHit,
                        };
                        eventBuffer.TryWrite(ref evt);
                    }

                    ApplyCollisionResponse(ref c, ref trails[i], bulletType, CollisionTarget.Target, targetBit, default);

                    // 如果弹丸已死（Die 响应），不再检测其他目标
                    if (c.HitPoints == 0) break;
                }

                // Pierce 冷却清除：逐 bit 检查，如果弹丸不再与该目标重叠则清除对应 bit
                if ((c.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0 && c.HitPoints > 0)
                {
                    ushort mask = c.PierceHitMask;
                    for (int t = 0; t < TargetRegistry.MAX_TARGETS && mask != 0; t++)
                    {
                        ushort bit = (ushort)(1 << t);
                        if ((mask & bit) == 0) continue;

                        var target = targets[t];
                        bool stillOverlapping = false;
                        if (target != null)
                        {
                            var hitbox = target.Hitbox;
                            float dx = c.Position.x - hitbox.Center.x;
                            float dy = c.Position.y - hitbox.Center.y;
                            float distSq = dx * dx + dy * dy;
                            float radiusSum = c.Radius + hitbox.Radius;
                            stillOverlapping = distSq < radiusSum * radiusSum;
                        }

                        if (!stillOverlapping)
                        {
                            c.PierceHitMask &= (ushort)~bit;
                        }
                    }

                    // 如果所有冷却都清除了，移除 FLAG
                    if (c.PierceHitMask == 0)
                        c.Flags &= unchecked((byte)~BulletCore.FLAG_PIERCE_COOLDOWN);
                }
            }
        }

        /// <summary>阵营碰撞判定</summary>
        private static bool ShouldCollide(BulletFaction bulletFaction, BulletFaction targetFaction)
        {
            // 同阵营不碰撞
            if (bulletFaction == targetFaction) return false;
            // Neutral 弹丸 / Neutral 目标 → 与所有碰撞
            if (bulletFaction == BulletFaction.Neutral || targetFaction == BulletFaction.Neutral) return true;
            // 不同非 Neutral 阵营 → 碰撞
            return true;
        }

        // ──── Phase 2: 弹丸 vs 障碍物（圆 vs AABB） ────

        private static void SolveBulletVsObstacle(
            BulletCore[] cores, BulletTrail[] trails, int capacity,
            ObstaclePool obstaclePool,
            DanmakuTypeRegistry registry,
            ref CollisionResult result)
        {
            var obstacles = obstaclePool.Data;

            for (int i = 0; i < capacity; i++)
            {
                ref var c = ref cores[i];
                if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;
                if (c.Phase != (byte)BulletPhase.Active) continue;

                for (int j = 0; j < ObstaclePool.MAX_OBSTACLES; j++)
                {
                    ref var obs = ref obstacles[j];
                    if (obs.Phase != (byte)ObstaclePhase.Active) continue;

                    // 阵营穿透检查（使用 Faction 字段简化）
                    if (c.Faction == (byte)BulletFaction.Player && obs.Faction == (byte)BulletFaction.Player) continue;
                    if (c.Faction == (byte)BulletFaction.Enemy && obs.Faction == (byte)BulletFaction.Enemy) continue;

                    // 圆 vs AABB
                    Vector2 center = (obs.Min + obs.Max) * 0.5f;
                    Vector2 halfSize = (obs.Max - obs.Min) * 0.5f;
                    Vector2 closest = ClampToAABB(c.Position, center, halfSize);
                    float dx = c.Position.x - closest.x;
                    float dy = c.Position.y - closest.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq >= c.Radius * c.Radius) continue;

                    // 命中——计算法线
                    Vector2 normal = GetAABBNormal(c.Position, center, halfSize);

                    // 对障碍物造成伤害（如果可摧毁）
                    if (obs.HitPoints > 0)
                    {
                        var bulletType = registry.BulletTypes[c.TypeIndex];
                        obs.HitPoints = Mathf.Max(0, obs.HitPoints - bulletType.Damage);
                        if (obs.HitPoints == 0)
                            obs.Phase = (byte)ObstaclePhase.Destroyed;
                    }

                    // 碰撞响应
                    ApplyCollisionResponse(ref c, ref trails[i], registry.BulletTypes[c.TypeIndex],
                        CollisionTarget.Obstacle, (byte)j, normal);
                }
            }
        }

        // ──── Phase 3: 弹丸 vs 屏幕边缘 ────

        private void SolveBulletVsScreenEdge(
            BulletCore[] cores, BulletTrail[] trails, int capacity,
            DanmakuTypeRegistry registry,
            ref CollisionResult result)
        {
            for (int i = 0; i < capacity; i++)
            {
                ref var c = ref cores[i];
                if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;
                if (c.Phase != (byte)BulletPhase.Active) continue;

                var type = registry.BulletTypes[c.TypeIndex];

                // 屏幕边缘检测
                bool outsideBounds = c.Position.x < _worldBounds.xMin - type.ScreenEdgeRecycleDistance
                    || c.Position.x > _worldBounds.xMax + type.ScreenEdgeRecycleDistance
                    || c.Position.y < _worldBounds.yMin - type.ScreenEdgeRecycleDistance
                    || c.Position.y > _worldBounds.yMax + type.ScreenEdgeRecycleDistance;

                if (!outsideBounds) continue;

                Vector2 normal = GetScreenEdgeNormal(c.Position);
                ApplyCollisionResponse(ref c, ref trails[i], type, CollisionTarget.ScreenEdge, 0, normal);
            }
        }

        // ──── Phase 4: 激光 vs 目标（多段折射线段 vs 圆） ────

        private static void SolveLasers(
            LaserPool pool,
            TargetRegistry targetRegistry,
            float dt,
            CollisionEventBuffer eventBuffer,
            ref CollisionResult result)
        {
            var targets = targetRegistry.Targets;

            for (int i = 0; i < LaserPool.MAX_LASERS; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase != 2) continue;  // 2 = Firing

                // 伤害 tick 推进 + 判断（推进和判断在同一处，避免时序 bug）
                laser.TickTimer += dt;
                if (laser.TickTimer < laser.TickInterval) continue;
                laser.TickTimer -= laser.TickInterval;

                for (int t = 0; t < TargetRegistry.MAX_TARGETS; t++)
                {
                    var target = targets[t];
                    if (target == null) continue;

                    // 阵营过滤：读取激光实际阵营
                    if (!ShouldCollide((BulletFaction)laser.Faction, target.Faction)) continue;

                    var hitbox = target.Hitbox;
                    float totalRadius = laser.Width * 0.5f + hitbox.Radius;
                    bool hit = false;

                    // 遍历所有折射线段
                    for (int s = 0; s < laser.SegmentCount; s++)
                    {
                        ref var seg = ref laser.Segments[s];
                        float dist = PointToSegmentDistance(hitbox.Center, seg.Start, seg.End);
                        if (dist < totalRadius)
                        {
                            hit = true;
                            break; // 一条激光每个目标只命中一次
                        }
                    }

                    if (!hit) continue;

                    result.HasAnyHit = true;
                    int damage = (int)laser.DamagePerTick;
                    result.TotalDamage += damage;
                    result.HitTargetCount++;

                    // 记录命中位置
                    if (target.Faction == BulletFaction.Player)
                    {
                        result.PlayerHit = true;
                        result.PlayerDamage += damage;
                        result.PlayerHitPosition = hitbox.Center;
                    }
                    else
                    {
                        result.NonPlayerHit = true;
                        result.NonPlayerHitPosition = hitbox.Center;
                    }

                    target.OnLaserHit(damage, i);

                    // 写入旁路事件 Buffer
                    if (eventBuffer != null)
                    {
                        var evt = new CollisionEvent
                        {
                            BulletIndex = i,
                            TargetSlot = t,
                            Position = hitbox.Center,
                            Damage = damage,
                            SourceFaction = (BulletFaction)laser.Faction,
                            TargetFaction = target.Faction,
                            EventType = CollisionEventType.LaserHit,
                        };
                        eventBuffer.TryWrite(ref evt);
                    }
                }
            }
        }

        // ──── Phase 5: 喷雾 vs 目标（扇形 vs 圆） ────

        private static void SolveSprays(
            SprayPool pool,
            TargetRegistry targetRegistry,
            float dt,
            CollisionEventBuffer eventBuffer,
            ref CollisionResult result)
        {
            var targets = targetRegistry.Targets;

            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref pool.Data[i];
                _sprayTickedThisFrame[i] = false;
                if (spray.Phase == 0) continue;

                // 伤害 tick 推进 + 判断
                spray.TickTimer += dt;
                if (spray.TickTimer < spray.TickInterval) continue;
                spray.TickTimer -= spray.TickInterval;
                _sprayTickedThisFrame[i] = true;

                for (int t = 0; t < TargetRegistry.MAX_TARGETS; t++)
                {
                    var target = targets[t];
                    if (target == null) continue;

                    // 阵营过滤：读取喷雾实际阵营
                    if (!ShouldCollide((BulletFaction)spray.Faction, target.Faction)) continue;

                    var hitbox = target.Hitbox;

                    // 距离检查
                    Vector2 diff = hitbox.Center - spray.Origin;
                    float dist = diff.magnitude;
                    if (dist > spray.Range + hitbox.Radius) continue;

                    // 扇形角度检查
                    float angle = Mathf.Atan2(diff.y, diff.x);
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
                        angle * Mathf.Rad2Deg,
                        spray.Direction * Mathf.Rad2Deg));
                    if (angleDiff > spray.ConeAngle * Mathf.Rad2Deg) continue;

                    result.HasAnyHit = true;
                    int damage = (int)spray.DamagePerTick;
                    result.TotalDamage += damage;
                    result.HitTargetCount++;

                    // 记录命中位置
                    if (target.Faction == BulletFaction.Player)
                    {
                        result.PlayerHit = true;
                        result.PlayerDamage += damage;
                        result.PlayerHitPosition = hitbox.Center;
                    }
                    else
                    {
                        result.NonPlayerHit = true;
                        result.NonPlayerHitPosition = hitbox.Center;
                    }

                    target.OnSprayHit(damage, i);

                    // 写入旁路事件 Buffer
                    if (eventBuffer != null)
                    {
                        var evt = new CollisionEvent
                        {
                            BulletIndex = i,
                            TargetSlot = t,
                            Position = hitbox.Center,
                            Damage = damage,
                            SourceFaction = (BulletFaction)spray.Faction,
                            TargetFaction = target.Faction,
                            EventType = CollisionEventType.SprayHit,
                        };
                        eventBuffer.TryWrite(ref evt);
                    }
                }
            }
        }

        // ──── Phase 6: 喷雾 vs 障碍物 ────

        private void SolveSprayVsObstacle(
            SprayPool sprayPool,
            ObstaclePool obstaclePool,
            DanmakuTypeRegistry registry,
            float dt,
            ref CollisionResult result)
        {
            var obstacles = obstaclePool.Data;

            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref sprayPool.Data[i];
                if (spray.Phase == 0) continue;

                var type = registry.SprayTypes[spray.SprayTypeIndex];
                if (type.OnHitObstacle == SprayObstacleResponse.Ignore) continue;

                // 伤害 tick 检查（tick 推进已在 Phase 5 SolveSprays 完成，此处读取标记）
                bool canDamage = type.OnHitObstacle == SprayObstacleResponse.PierceAndDamage
                    && _sprayTickedThisFrame[i];

                for (int j = 0; j < ObstaclePool.MAX_OBSTACLES; j++)
                {
                    ref var obs = ref obstacles[j];
                    if (obs.Phase != (byte)ObstaclePhase.Active) continue;

                    // 扇形 vs AABB 近似检测：先圆 vs AABB（射程圆），再角度检查
                    Vector2 center = (obs.Min + obs.Max) * 0.5f;
                    Vector2 halfSize = (obs.Max - obs.Min) * 0.5f;
                    Vector2 closest = ClampToAABB(spray.Origin, center, halfSize);

                    float dx = spray.Origin.x - closest.x;
                    float dy = spray.Origin.y - closest.y;
                    float distSq = dx * dx + dy * dy;
                    if (distSq > spray.Range * spray.Range) continue;

                    // 角度检查——AABB 中心相对于喷雾方向
                    Vector2 toObs = center - spray.Origin;
                    float angle = Mathf.Atan2(toObs.y, toObs.x);
                    float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
                        angle * Mathf.Rad2Deg,
                        spray.Direction * Mathf.Rad2Deg));
                    if (angleDiff > spray.ConeAngle * Mathf.Rad2Deg) continue;

                    // 命中——对障碍物造成伤害
                    if (canDamage && obs.HitPoints > 0)
                    {
                        obs.HitPoints = Mathf.Max(0, obs.HitPoints - (int)type.DamagePerTick);
                        if (obs.HitPoints == 0)
                            obs.Phase = (byte)ObstaclePhase.Destroyed;
                    }
                }
            }
        }

        // ──── Phase 7: 喷雾 vs 屏幕边缘（Origin 越界回收） ────

        private void SolveSprayVsScreenEdge(
            SprayPool sprayPool,
            AttachSourceRegistry attachRegistry,
            DanmakuTypeRegistry registry,
            IDanmakuVFXRuntime sprayVfxRuntime)

        {
            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref sprayPool.Data[i];
                if (spray.Phase == 0) continue;

                var type = registry.SprayTypes[spray.SprayTypeIndex];
                if (!type.RecycleOnOriginOutOfBounds) continue;

                float margin = type.ScreenEdgeRecycleMargin;
                if (spray.Origin.x < _worldBounds.xMin - margin ||
                    spray.Origin.x > _worldBounds.xMax + margin ||
                    spray.Origin.y < _worldBounds.yMin - margin ||
                    spray.Origin.y > _worldBounds.yMax + margin)
                {
                    SprayUpdater.FreeSpray(sprayPool, attachRegistry, sprayVfxRuntime, i);

                }
            }
        }

        // ──── 碰撞响应 ────

        private static void ApplyCollisionResponse(
            ref BulletCore core,
            ref BulletTrail trail,
            BulletTypeSO type,
            CollisionTarget target,
            ushort targetBit,
            Vector2 normal)
        {
            CollisionResponse response;
            byte hpCost;

            switch (target)
            {
                case CollisionTarget.Target:
                    response = type.OnHitTarget;
                    hpCost = type.HitTargetHPCost;
                    break;
                case CollisionTarget.Obstacle:
                    response = type.OnHitObstacle;
                    hpCost = type.HitObstacleHPCost;
                    break;
                case CollisionTarget.ScreenEdge:
                    response = type.OnHitScreenEdge;
                    hpCost = type.HitScreenEdgeHPCost;
                    break;
                default:
                    return;
            }

            switch (response)
            {
                case CollisionResponse.Die:
                    core.HitPoints = 0;
                    break;

                case CollisionResponse.ReduceHP:
                    core.HitPoints = (byte)Mathf.Max(0, core.HitPoints - hpCost);
                    // 受伤闪烁（未死亡时）
                    if (core.HitPoints > 0 && type.DamageFlashFrames > 0)
                        trail.FlashTimer = type.DamageFlashFrames;
                    break;

                case CollisionResponse.Pierce:
                    // Pierce：不消耗 HP，位掩码记录已命中的目标防多帧重复
                    core.Flags |= BulletCore.FLAG_PIERCE_COOLDOWN;
                    core.PierceHitMask |= targetBit;
                    // 穿透音效
                    if (type.PierceSFX != null)
                    {
                        var audioMgr = AudioManager.Instance;
                        if (audioMgr != null)
                            audioMgr.PlaySFX(type.PierceSFX);
                    }
                    break;

                case CollisionResponse.BounceBack:
                    core.Velocity = -core.Velocity;
                    PlayBounceEffects(type, core.Position);
                    break;

                case CollisionResponse.Reflect:
                    if (normal.sqrMagnitude > 0.001f)
                    {
                        // V' = V - 2(V·N)N
                        float dot = Vector2.Dot(core.Velocity, normal);
                        core.Velocity -= 2f * dot * normal;
                    }
                    else
                    {
                        core.Velocity = -core.Velocity;  // fallback to bounce
                    }
                    PlayBounceEffects(type, core.Position);
                    break;

                case CollisionResponse.RecycleOnDistance:
                    // 超距回收——在屏幕边缘检测中已判断距离
                    core.HitPoints = 0;
                    break;
            }
        }

        /// <summary>播放反弹/反射的视觉特效和音效</summary>
        private static void PlayBounceEffects(BulletTypeSO type, Vector2 position)
        {
            // 反弹音效
            if (type.BounceSFX != null)
            {
                var audioMgr = AudioManager.Instance;
                if (audioMgr != null)
                    audioMgr.PlaySFX(type.BounceSFX);
            }

            // 反弹特效
            if (type.BounceEffect != null)
            {
                var pool = PoolManager.Instance;
                if (pool != null)
                {
                    var go = pool.Get(type.BounceEffect);
                    if (go != null)
                        go.transform.position = new Vector3(position.x, position.y, 0);
                }
            }
        }

        // ──── 几何工具函数 ────

        private static Vector2 ClampToAABB(Vector2 point, Vector2 center, Vector2 halfSize)
        {
            return new Vector2(
                Mathf.Clamp(point.x, center.x - halfSize.x, center.x + halfSize.x),
                Mathf.Clamp(point.y, center.y - halfSize.y, center.y + halfSize.y));
        }

        private static Vector2 GetAABBNormal(Vector2 point, Vector2 center, Vector2 halfSize)
        {
            Vector2 d = point - center;
            float overlapX = halfSize.x - Mathf.Abs(d.x);
            float overlapY = halfSize.y - Mathf.Abs(d.y);

            if (overlapX < overlapY)
                return new Vector2(d.x > 0 ? 1 : -1, 0);
            else
                return new Vector2(0, d.y > 0 ? 1 : -1);
        }

        private Vector2 GetScreenEdgeNormal(Vector2 position)
        {
            float dLeft = position.x - _worldBounds.xMin;
            float dRight = _worldBounds.xMax - position.x;
            float dBottom = position.y - _worldBounds.yMin;
            float dTop = _worldBounds.yMax - position.y;

            float minDist = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

            if (Mathf.Approximately(minDist, dLeft)) return Vector2.right;
            if (Mathf.Approximately(minDist, dRight)) return Vector2.left;
            if (Mathf.Approximately(minDist, dBottom)) return Vector2.up;
            return Vector2.down;
        }

        /// <summary>点到线段的最短距离</summary>
        private static float PointToSegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab));
            Vector2 closest = a + ab * t;
            return (point - closest).magnitude;
        }
    }
}
