using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 碰撞检测结果。
    /// </summary>
    public struct CollisionResult
    {
        /// <summary>本帧是否有伤害源命中玩家</summary>
        public bool HasPlayerHit;

        /// <summary>本帧总伤害（用于飘字/事件）</summary>
        public int TotalDamage;
    }

    /// <summary>
    /// 统一碰撞调度器。
    /// 5 阶段碰撞：弹丸vs目标 → 弹丸vs障碍物 → 弹丸vs屏幕边缘 → 激光vs玩家 → 喷雾vs玩家。
    /// </summary>
    public class CollisionSolver
    {
        private Rect _worldBounds;

        public void Initialize(DanmakuWorldConfig config)
        {
            _worldBounds = config.WorldBounds;
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
            in CircleHitbox player,
            BulletFaction playerFaction,
            float dt)
        {
            var result = default(CollisionResult);

            var cores = bulletWorld.Cores;
            int capacity = bulletWorld.Capacity;

            // Phase 1: 弹丸 vs 目标对象
            SolveBulletVsTarget(cores, capacity, registry, in player, playerFaction, ref result);

            // Phase 2: 弹丸 vs 障碍物（圆 vs AABB）
            SolveBulletVsObstacle(cores, capacity, obstaclePool, registry, ref result);

            // Phase 3: 弹丸 vs 屏幕边缘
            SolveBulletVsScreenEdge(cores, capacity, registry, ref result);

            // Phase 4: 激光 vs 玩家
            SolveLasers(laserPool, in player, dt, ref result);

            // Phase 5: 喷雾 vs 玩家
            SolveSprays(sprayPool, in player, dt, ref result);

            return result;
        }

        // ──── Phase 1: 弹丸 vs 目标对象（圆 vs 圆） ────

        private static void SolveBulletVsTarget(
            BulletCore[] cores, int capacity,
            DanmakuTypeRegistry registry,
            in CircleHitbox player,
            BulletFaction playerFaction,
            ref CollisionResult result)
        {
            for (int i = 0; i < capacity; i++)
            {
                ref var c = ref cores[i];
                if ((c.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;
                if (c.Phase != (byte)BulletPhase.Active) continue;

                // 阵营过滤：Enemy 弹丸 vs Player 目标
                var bulletFaction = (BulletFaction)c.Faction;
                if (bulletFaction == BulletFaction.Enemy && playerFaction != BulletFaction.Player) continue;
                if (bulletFaction == BulletFaction.Player) continue; // 玩家弹丸不打自己
                // Neutral 与所有碰撞

                // Pierce 冷却检查
                if ((c.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0 && c.LastHitId == 0)
                    c.Flags &= unchecked((byte)~BulletCore.FLAG_PIERCE_COOLDOWN);

                // 圆 vs 圆
                float dx = c.Position.x - player.Center.x;
                float dy = c.Position.y - player.Center.y;
                float distSq = dx * dx + dy * dy;
                float radiusSum = c.Radius + player.Radius;

                if (distSq >= radiusSum * radiusSum) continue;

                // 命中
                result.HasPlayerHit = true;
                var bulletType = registry.BulletTypes[c.TypeIndex];
                result.TotalDamage += bulletType.Damage;

                ApplyCollisionResponse(ref c, bulletType, CollisionTarget.Target, 0, default);
            }
        }

        // ──── Phase 2: 弹丸 vs 障碍物（圆 vs AABB） ────

        private static void SolveBulletVsObstacle(
            BulletCore[] cores, int capacity,
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
                    ApplyCollisionResponse(ref c, registry.BulletTypes[c.TypeIndex],
                        CollisionTarget.Obstacle, (byte)j, normal);
                }
            }
        }

        // ──── Phase 3: 弹丸 vs 屏幕边缘 ────

        private void SolveBulletVsScreenEdge(
            BulletCore[] cores, int capacity,
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
                ApplyCollisionResponse(ref c, type, CollisionTarget.ScreenEdge, 0, normal);
            }
        }

        // ──── Phase 4: 激光 vs 玩家（线段 vs 圆） ────

        private static void SolveLasers(
            LaserPool pool,
            in CircleHitbox player,
            float dt,
            ref CollisionResult result)
        {
            for (int i = 0; i < LaserPool.MAX_LASERS; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase != 2) continue;  // 2 = Firing

                // 线段 vs 圆
                Vector2 dir = new Vector2(
                    Mathf.Cos(laser.Angle),
                    Mathf.Sin(laser.Angle));
                Vector2 end = laser.Origin + dir * laser.Length;

                float dist = PointToSegmentDistance(player.Center, laser.Origin, end);
                float totalRadius = laser.Width * 0.5f + player.Radius;

                if (dist >= totalRadius) continue;

                // 伤害 tick 检查
                if (laser.TickTimer < laser.TickInterval) continue;

                result.HasPlayerHit = true;
                result.TotalDamage += (int)laser.DamagePerTick;
            }
        }

        // ──── Phase 5: 喷雾 vs 玩家（扇形 vs 圆） ────

        private static void SolveSprays(
            SprayPool pool,
            in CircleHitbox player,
            float dt,
            ref CollisionResult result)
        {
            for (int i = 0; i < SprayPool.MAX_SPRAYS; i++)
            {
                ref var spray = ref pool.Data[i];
                if (spray.Phase == 0) continue;

                // 距离检查
                Vector2 diff = player.Center - spray.Origin;
                float dist = diff.magnitude;
                if (dist > spray.Range + player.Radius) continue;

                // 扇形角度检查
                float angle = Mathf.Atan2(diff.y, diff.x);
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
                    angle * Mathf.Rad2Deg,
                    spray.Direction * Mathf.Rad2Deg));
                if (angleDiff > spray.ConeAngle * Mathf.Rad2Deg) continue;

                // 伤害 tick 检查
                if (spray.TickTimer < spray.TickInterval) continue;

                result.HasPlayerHit = true;
                result.TotalDamage += (int)spray.DamagePerTick;
            }
        }

        // ──── 碰撞响应 ────

        private static void ApplyCollisionResponse(
            ref BulletCore core,
            BulletTypeSO type,
            CollisionTarget target,
            byte targetId,
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
                    break;

                case CollisionResponse.Pierce:
                    // Pierce：不消耗 HP，记录 targetId 防多帧重复
                    if ((core.Flags & BulletCore.FLAG_PIERCE_COOLDOWN) != 0 && core.LastHitId == targetId)
                        return;  // 冷却中，跳过
                    core.Flags |= BulletCore.FLAG_PIERCE_COOLDOWN;
                    core.LastHitId = targetId;
                    break;

                case CollisionResponse.BounceBack:
                    core.Velocity = -core.Velocity;
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
                    break;

                case CollisionResponse.RecycleOnDistance:
                    // 超距回收——在屏幕边缘检测中已判断距离
                    core.HitPoints = 0;
                    break;
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
