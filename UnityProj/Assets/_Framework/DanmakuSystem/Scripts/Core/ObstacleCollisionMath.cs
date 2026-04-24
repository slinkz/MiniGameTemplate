using System.Runtime.CompilerServices;
using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// OBB 碰撞数学工具——零分配、静态方法、AggressiveInlining。
    /// 位于 _Framework/DanmakuSystem/Scripts/Core/ObstacleCollisionMath.cs
    /// 注意：internal 是刻意设计——只对 Framework 内部暴露，Example 层通过 ObstaclePool API 间接使用。
    /// </summary>
    internal static class ObstacleCollisionMath
    {
        // ── 坐标变换 ──

        /// <summary>世界坐标点 → OBB 局部空间。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 WorldToLocal(Vector2 worldPoint, in ObstacleData obs)
        {
            Vector2 d = worldPoint - obs.Center;
            return new Vector2(
                d.x * obs.Cos + d.y * obs.Sin,    // 逆旋转
               -d.x * obs.Sin + d.y * obs.Cos);
        }

        /// <summary>局部空间方向向量 → 世界空间方向。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 LocalDirToWorld(Vector2 localDir, in ObstacleData obs)
        {
            return new Vector2(
                localDir.x * obs.Cos - localDir.y * obs.Sin,   // 正旋转
                localDir.x * obs.Sin + localDir.y * obs.Cos);
        }

        // ── 碰撞原语 ──

        /// <summary>圆 vs 障碍物。矩形走 OBB，圆形走真圆碰撞。返回 true=碰撞，outNormal=世界空间法线。</summary>
        /// <remarks>矩形分支的法线计算内联，消除重复 WorldToLocal 调用。</remarks>
        internal static bool CircleVsOBB(
            Vector2 circleCenter, float radius,
            in ObstacleData obs, out Vector2 normal)
        {
            if (obs.Shape == (byte)ObstacleShape.Circle)
                return CircleVsCircle(circleCenter, radius, in obs, out normal);

            Vector2 local = WorldToLocal(circleCenter, in obs);
            Vector2 closest = ClampLocal(local, obs.HalfExtents);
            float dx = local.x - closest.x;
            float dy = local.y - closest.y;
            if (dx * dx + dy * dy >= radius * radius)
            {
                normal = default;
                return false;
            }
            // 法线直接在局部空间计算，复用已有的 local（避免重复 WorldToLocal）
            float ox = obs.HalfExtents.x - Mathf.Abs(local.x);
            float oy = obs.HalfExtents.y - Mathf.Abs(local.y);
            Vector2 ln = ox < oy
                ? new Vector2(local.x > 0 ? 1 : -1, 0)
                : new Vector2(0, local.y > 0 ? 1 : -1);
            normal = LocalDirToWorld(ln, in obs);
            return true;
        }

        /// <summary>射线 vs OBB（膨胀 halfWidth）。Slab 算法，返回 t 值。</summary>
        internal static float RayVsOBB(
            Vector2 origin, Vector2 dir, float maxDist,
            in ObstacleData obs, float halfWidth)
        {
            // 射线变换到局部空间
            Vector2 lo = WorldToLocal(origin, in obs);
            Vector2 ld = new Vector2(
                dir.x * obs.Cos + dir.y * obs.Sin,
               -dir.x * obs.Sin + dir.y * obs.Cos);
            // 膨胀
            float hex = obs.HalfExtents.x + halfWidth;
            float hey = obs.HalfExtents.y + halfWidth;
            // Slab
            float tMin = 0f, tMax = maxDist;
            // X slab
            if (Mathf.Abs(ld.x) < 1e-4f)
            { if (lo.x < -hex || lo.x > hex) return float.MaxValue; }
            else
            {
                float inv = 1f / ld.x;
                float t1 = (-hex - lo.x) * inv, t2 = (hex - lo.x) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) return float.MaxValue;
            }
            // Y slab
            if (Mathf.Abs(ld.y) < 1e-4f)
            { if (lo.y < -hey || lo.y > hey) return float.MaxValue; }
            else
            {
                float inv = 1f / ld.y;
                float t1 = (-hey - lo.y) * inv, t2 = (hey - lo.y) * inv;
                if (t1 > t2) (t1, t2) = (t2, t1);
                tMin = Mathf.Max(tMin, t1); tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax) return float.MaxValue;
            }
            return tMin >= 0 ? tMin : 0f;
        }

        /// <summary>OBB 法线——局部空间分离轴法，旋转回世界空间。供 LaserSegmentSolver 等外部调用。</summary>
        internal static Vector2 GetOBBNormal(Vector2 worldPoint, in ObstacleData obs)
        {
            Vector2 local = WorldToLocal(worldPoint, in obs);
            float ox = obs.HalfExtents.x - Mathf.Abs(local.x);
            float oy = obs.HalfExtents.y - Mathf.Abs(local.y);
            Vector2 ln = ox < oy
                ? new Vector2(local.x > 0 ? 1 : -1, 0)
                : new Vector2(0, local.y > 0 ? 1 : -1);
            return LocalDirToWorld(ln, in obs);
        }

        // ── 封装原语（供外部调用） ──

        /// <summary>世界点到障碍物的最近距离平方。供 Phase 6 使用。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float DistanceSqToOBB(Vector2 worldPoint, in ObstacleData obs)
        {
            if (obs.Shape == (byte)ObstacleShape.Circle)
            {
                float radius = obs.HalfExtents.x;
                Vector2 delta = worldPoint - obs.Center;
                float centerDist = delta.magnitude;
                if (centerDist <= radius) return 0f;
                float edgeDist = centerDist - radius;
                return edgeDist * edgeDist;
            }

            Vector2 local = WorldToLocal(worldPoint, in obs);
            Vector2 closest = ClampLocal(local, obs.HalfExtents);
            float dx = local.x - closest.x;
            float dy = local.y - closest.y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CircleVsCircle(Vector2 circleCenter, float radius, in ObstacleData obs, out Vector2 normal)
        {
            float obstacleRadius = obs.HalfExtents.x;
            Vector2 delta = circleCenter - obs.Center;
            float distSq = delta.sqrMagnitude;
            float radiusSum = radius + obstacleRadius;
            if (distSq >= radiusSum * radiusSum)
            {
                normal = default;
                return false;
            }

            normal = distSq > 1e-6f ? delta / Mathf.Sqrt(distSq) : Vector2.up;
            return true;
        }

        // ── 内部辅助 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector2 ClampLocal(Vector2 p, Vector2 he)
        {
            return new Vector2(
                Mathf.Clamp(p.x, -he.x, he.x),
                Mathf.Clamp(p.y, -he.y, he.y));
        }
    }
}
