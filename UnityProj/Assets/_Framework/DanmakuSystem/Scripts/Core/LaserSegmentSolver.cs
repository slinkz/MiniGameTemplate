using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光折射段解算器——每帧为每条激光计算所有线段（直线 / 折射路径）。
    /// 纯 static 工具类，零分配。
    /// </summary>
    public static class LaserSegmentSolver
    {
        /// <summary>
        /// 解算激光的所有线段。结果写入 laser.Segments / laser.SegmentCount / laser.VisualLength。
        /// </summary>
        public static void Solve(
            ref LaserData laser,
            LaserTypeSO type,
            ObstaclePool obstaclePool,
            in Rect worldBounds)
        {
            // 直线激光（无折射能力或非 Firing 阶段）——只用一段
            if (type.MaxReflections == 0 && type.OnHitScreenEdge != LaserScreenEdgeResponse.Reflect)
            {
                SolveStraight(ref laser, type, obstaclePool, in worldBounds);
                return;
            }

            SolveWithReflection(ref laser, type, obstaclePool, in worldBounds);
        }

        // ──── 直线模式（快速路径） ────

        private static void SolveStraight(
            ref LaserData laser,
            LaserTypeSO type,
            ObstaclePool obstaclePool,
            in Rect worldBounds)
        {
            Vector2 origin = laser.Origin;
            Vector2 dir = new Vector2(Mathf.Cos(laser.Angle), Mathf.Sin(laser.Angle));
            float remainLen = laser.Length;

            // 检查障碍物截断
            if (type.OnHitObstacle == LaserObstacleResponse.Block ||
                type.OnHitObstacle == LaserObstacleResponse.BlockAndDamage)
            {
                float hitDist = RaycastObstacles(origin, dir, remainLen, laser.Width * 0.5f,
                    obstaclePool, out int hitObsIndex);

                if (hitDist < remainLen)
                {
                    remainLen = hitDist;

                    // 对障碍物造成伤害
                    if (type.OnHitObstacle == LaserObstacleResponse.BlockAndDamage && hitObsIndex >= 0)
                        DamageObstacle(ref obstaclePool.Data[hitObsIndex], (int)laser.DamagePerTick);
                }
            }
            else if (type.OnHitObstacle == LaserObstacleResponse.PierceAndDamage)
            {
                // 穿透但造成伤害——遍历所有障碍物检查是否在射线上
                DamageAllObstaclesOnRay(origin, dir, remainLen, laser.Width * 0.5f,
                    obstaclePool, (int)laser.DamagePerTick);
            }

            // 屏幕边缘裁剪
            if (type.OnHitScreenEdge == LaserScreenEdgeResponse.Clip)
            {
                float clipDist = RaycastBounds(origin, dir, remainLen, in worldBounds);
                if (clipDist < remainLen)
                    remainLen = clipDist;
            }

            // 写入单段结果
            Vector2 end = origin + dir * remainLen;
            ref var seg = ref laser.Segments[0];
            seg.Start = origin;
            seg.End = end;
            seg.Direction = dir;
            seg.Length = remainLen;
            laser.SegmentCount = 1;
            laser.VisualLength = remainLen;
        }

        // ──── 折射模式（射线行进） ────

        private static void SolveWithReflection(
            ref LaserData laser,
            LaserTypeSO type,
            ObstaclePool obstaclePool,
            in Rect worldBounds)
        {
            Vector2 pos = laser.Origin;
            Vector2 dir = new Vector2(Mathf.Cos(laser.Angle), Mathf.Sin(laser.Angle));
            float remainLen = laser.Length;
            int maxSegs = Mathf.Min(type.MaxReflections + 1, laser.Segments.Length);
            int segIndex = 0;
            float totalLen = 0f;
            const int MAX_ITERATIONS = 32; // 安全上限：防止密集穿透障碍物导致无限循环
            int iterations = 0;

            for (int bounce = 0; bounce <= type.MaxReflections && segIndex < maxSegs; bounce++)
            {
                if (remainLen <= 0.001f) break;
                if (++iterations > MAX_ITERATIONS) break; // 安全网

                // 找最近的碰撞：障碍物 or 屏幕边缘
                float obsDist = float.MaxValue;
                int hitObsIndex = -1;
                Vector2 obsNormal = Vector2.zero;

                bool obstacleBlocks = type.OnHitObstacle == LaserObstacleResponse.Block ||
                                      type.OnHitObstacle == LaserObstacleResponse.BlockAndDamage;

                if (obstacleBlocks || type.OnHitObstacle == LaserObstacleResponse.PierceAndDamage)
                {
                    obsDist = RaycastObstaclesWithNormal(pos, dir, remainLen, laser.Width * 0.5f,
                        obstaclePool, out hitObsIndex, out obsNormal);
                }

                float boundDist = float.MaxValue;
                Vector2 boundNormal = Vector2.zero;
                bool boundsReflect = type.OnHitScreenEdge == LaserScreenEdgeResponse.Reflect;

                boundDist = RaycastBoundsWithNormal(pos, dir, remainLen, in worldBounds, out boundNormal);

                // 取最近碰撞
                float hitDist = Mathf.Min(obsDist, boundDist);
                bool hitObstacle = obsDist <= boundDist && obsDist < float.MaxValue;
                bool hitBound = boundDist < obsDist && boundDist < float.MaxValue;

                if (hitDist >= remainLen)
                {
                    // 没碰到任何东西——记录最后一段到射线末端
                    ref var seg = ref laser.Segments[segIndex];
                    seg.Start = pos;
                    seg.End = pos + dir * remainLen;
                    seg.Direction = dir;
                    seg.Length = remainLen;
                    totalLen += remainLen;
                    segIndex++;
                    break;
                }

                // 记录到碰撞点的线段
                {
                    ref var seg = ref laser.Segments[segIndex];
                    seg.Start = pos;
                    seg.End = pos + dir * hitDist;
                    seg.Direction = dir;
                    seg.Length = hitDist;
                    totalLen += hitDist;
                    segIndex++;
                }

                remainLen -= hitDist;

                if (hitObstacle)
                {
                    // 对障碍物造成伤害
                    if ((type.OnHitObstacle == LaserObstacleResponse.BlockAndDamage ||
                         type.OnHitObstacle == LaserObstacleResponse.PierceAndDamage) && hitObsIndex >= 0)
                        DamageObstacle(ref obstaclePool.Data[hitObsIndex], (int)laser.DamagePerTick);

                    if (obstacleBlocks)
                    {
                        // 被障碍物截断——反射方向
                        dir = Reflect(dir, obsNormal);
                        pos = laser.Segments[segIndex - 1].End + dir * 0.01f;
                    }
                    else
                    {
                        // 穿透——不改方向，不算反弹次数
                        pos = laser.Segments[segIndex - 1].End + dir * 0.01f;
                        bounce--;
                    }
                }
                else if (hitBound)
                {
                    if (boundsReflect)
                    {
                        dir = Reflect(dir, boundNormal);
                        pos = laser.Segments[segIndex - 1].End + dir * 0.01f;
                    }
                    else
                    {
                        // Clip——停在边缘
                        break;
                    }
                }
            }

            laser.SegmentCount = (byte)segIndex;
            laser.VisualLength = totalLen;
        }

        // ──── 射线 vs 障碍物（返回最近碰撞距离）—— 使用 OBB ────

        private static float RaycastObstacles(
            Vector2 origin, Vector2 dir, float maxDist, float halfWidth,
            ObstaclePool pool, out int hitIndex)
        {
            float closest = float.MaxValue;
            hitIndex = -1;
            var obstacles = pool.Data;

            for (int j = 0; j < ObstaclePool.MAX_OBSTACLES; j++)
            {
                ref var obs = ref obstacles[j];
                if (obs.Phase != (byte)ObstaclePhase.Active) continue;

                float dist = ObstacleCollisionMath.RayVsOBB(origin, dir, maxDist, in obs, halfWidth);

                if (dist < closest)
                {
                    closest = dist;
                    hitIndex = j;
                }
            }

            return closest;
        }

        private static float RaycastObstaclesWithNormal(
            Vector2 origin, Vector2 dir, float maxDist, float halfWidth,
            ObstaclePool pool, out int hitIndex, out Vector2 normal)
        {
            float closest = float.MaxValue;
            hitIndex = -1;
            normal = Vector2.zero;
            var obstacles = pool.Data;

            for (int j = 0; j < ObstaclePool.MAX_OBSTACLES; j++)
            {
                ref var obs = ref obstacles[j];
                if (obs.Phase != (byte)ObstaclePhase.Active) continue;

                float dist = ObstacleCollisionMath.RayVsOBB(origin, dir, maxDist, in obs, halfWidth);

                if (dist < closest)
                {
                    closest = dist;
                    hitIndex = j;
                    // 碰撞点处的 OBB 法线
                    Vector2 hitPoint = origin + dir * dist;
                    normal = ObstacleCollisionMath.GetOBBNormal(hitPoint, in obs);
                }
            }

            return closest;
        }

        // ──── 射线 vs 屏幕边缘 ────

        private static float RaycastBounds(
            Vector2 origin, Vector2 dir, float maxDist,
            in Rect bounds)
        {
            return RaycastBoundsWithNormal(origin, dir, maxDist, in bounds, out _);
        }

        private static float RaycastBoundsWithNormal(
            Vector2 origin, Vector2 dir, float maxDist,
            in Rect bounds, out Vector2 normal)
        {
            normal = Vector2.zero;
            float closest = maxDist;

            // 检查四条边缘
            // 左边缘 x = xMin
            if (dir.x < -0.0001f)
            {
                float t = (bounds.xMin - origin.x) / dir.x;
                if (t > 0 && t < closest)
                {
                    float y = origin.y + dir.y * t;
                    if (y >= bounds.yMin && y <= bounds.yMax)
                    {
                        closest = t;
                        normal = Vector2.right; // 法线朝内
                    }
                }
            }
            // 右边缘 x = xMax
            if (dir.x > 0.0001f)
            {
                float t = (bounds.xMax - origin.x) / dir.x;
                if (t > 0 && t < closest)
                {
                    float y = origin.y + dir.y * t;
                    if (y >= bounds.yMin && y <= bounds.yMax)
                    {
                        closest = t;
                        normal = Vector2.left;
                    }
                }
            }
            // 下边缘 y = yMin
            if (dir.y < -0.0001f)
            {
                float t = (bounds.yMin - origin.y) / dir.y;
                if (t > 0 && t < closest)
                {
                    float x = origin.x + dir.x * t;
                    if (x >= bounds.xMin && x <= bounds.xMax)
                    {
                        closest = t;
                        normal = Vector2.up;
                    }
                }
            }
            // 上边缘 y = yMax
            if (dir.y > 0.0001f)
            {
                float t = (bounds.yMax - origin.y) / dir.y;
                if (t > 0 && t < closest)
                {
                    float x = origin.x + dir.x * t;
                    if (x >= bounds.xMin && x <= bounds.xMax)
                    {
                        closest = t;
                        normal = Vector2.down;
                    }
                }
            }

            return closest >= maxDist ? float.MaxValue : closest;
        }

        // ──── 穿透伤害（对射线上所有障碍物造成伤害）—— 使用 OBB ────

        private static void DamageAllObstaclesOnRay(
            Vector2 origin, Vector2 dir, float maxDist, float halfWidth,
            ObstaclePool pool, int damage)
        {
            var obstacles = pool.Data;
            for (int j = 0; j < ObstaclePool.MAX_OBSTACLES; j++)
            {
                ref var obs = ref obstacles[j];
                if (obs.Phase != (byte)ObstaclePhase.Active) continue;

                float dist = ObstacleCollisionMath.RayVsOBB(origin, dir, maxDist, in obs, halfWidth);

                if (dist < float.MaxValue)
                    DamageObstacle(ref obs, damage);
            }
        }

        // ──── 障碍物伤害 ────

        private static void DamageObstacle(ref ObstacleData obs, int damage)
        {
            if (obs.HitPoints <= 0) return; // 不可摧毁
            obs.HitPoints = Mathf.Max(0, obs.HitPoints - damage);
            if (obs.HitPoints == 0)
                obs.Phase = (byte)ObstaclePhase.Destroyed;
        }

        // ──── 几何工具 ────
        // RayVsAABB / GetAABBNormal 已移入 ObstacleCollisionMath 共享工具类

        private static Vector2 Reflect(Vector2 direction, Vector2 normal)
        {
            return direction - 2f * Vector2.Dot(direction, normal) * normal;
        }
    }
}
