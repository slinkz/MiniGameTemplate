using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// Hitbox 碰撞数学工具——纯静态方法，零 GC。
    /// 所有方法内联友好（无 virtual、无 alloc）。
    /// </summary>
    public static class HitboxMath
    {
        // ──── 通用分派：任意形状 vs 任意形状 ────

        /// <summary>
        /// 检测圆（弹丸 position + radius）vs 任意 Hitbox 是否碰撞。
        /// Phase 1 弹丸 vs 目标核心路径。
        /// </summary>
        public static bool CircleVsHitbox(Vector2 circleCenter, float circleRadius, in Hitbox hitbox)
        {
            if (hitbox.Shape == HitboxShape.Rect)
                return CircleVsAABB(circleCenter, circleRadius, hitbox.Center, hitbox.HalfWidth, hitbox.HalfHeight);
            else
                return CircleVsCircle(circleCenter, circleRadius, hitbox.Center, hitbox.Radius);
        }

        /// <summary>
        /// 检测线段 vs 任意 Hitbox 是否碰撞。
        /// Phase 4 激光 vs 目标核心路径。
        /// </summary>
        public static bool SegmentVsHitbox(Vector2 segA, Vector2 segB, float halfWidth, in Hitbox hitbox)
        {
            if (hitbox.Shape == HitboxShape.Rect)
                return SegmentVsAABB(segA, segB, halfWidth, hitbox.Center, hitbox.HalfWidth, hitbox.HalfHeight);
            else
                return SegmentVsCircle(segA, segB, halfWidth, hitbox.Center, hitbox.Radius);
        }

        /// <summary>
        /// 检测扇形 vs 任意 Hitbox 是否碰撞。
        /// Phase 5 喷雾 vs 目标核心路径。
        /// </summary>
        public static bool SectorVsHitbox(
            Vector2 origin, float range, float direction, float coneAngle,
            in Hitbox hitbox)
        {
            if (hitbox.Shape == HitboxShape.Rect)
                return SectorVsAABB(origin, range, direction, coneAngle,
                    hitbox.Center, hitbox.HalfWidth, hitbox.HalfHeight);
            else
                return SectorVsCircle(origin, range, direction, coneAngle,
                    hitbox.Center, hitbox.Radius);
        }

        /// <summary>
        /// 检测两个任意 Hitbox 是否碰撞（Entity vs Entity）。
        /// </summary>
        public static bool HitboxVsHitbox(in Hitbox a, in Hitbox b)
        {
            if (a.Shape == HitboxShape.Circle && b.Shape == HitboxShape.Circle)
                return CircleVsCircle(a.Center, a.Radius, b.Center, b.Radius);
            if (a.Shape == HitboxShape.Circle && b.Shape == HitboxShape.Rect)
                return CircleVsAABB(a.Center, a.Radius, b.Center, b.HalfWidth, b.HalfHeight);
            if (a.Shape == HitboxShape.Rect && b.Shape == HitboxShape.Circle)
                return CircleVsAABB(b.Center, b.Radius, a.Center, a.HalfWidth, a.HalfHeight);
            // Rect vs Rect → AABB overlap
            return AABBvsAABB(a.Center, a.HalfWidth, a.HalfHeight,
                              b.Center, b.HalfWidth, b.HalfHeight);
        }

        // ──── 基础几何 ────

        /// <summary>圆 vs 圆碰撞（距离平方比较）</summary>
        public static bool CircleVsCircle(Vector2 cA, float rA, Vector2 cB, float rB)
        {
            float dx = cA.x - cB.x;
            float dy = cA.y - cB.y;
            float radiusSum = rA + rB;
            return dx * dx + dy * dy < radiusSum * radiusSum;
        }

        /// <summary>圆 vs 轴对齐矩形碰撞（Clamp + 距离检测）</summary>
        public static bool CircleVsAABB(
            Vector2 circleCenter, float circleRadius,
            Vector2 rectCenter, float halfW, float halfH)
        {
            // 将圆心 Clamp 到矩形最近点
            float closestX = Mathf.Clamp(circleCenter.x, rectCenter.x - halfW, rectCenter.x + halfW);
            float closestY = Mathf.Clamp(circleCenter.y, rectCenter.y - halfH, rectCenter.y + halfH);

            float dx = circleCenter.x - closestX;
            float dy = circleCenter.y - closestY;
            return dx * dx + dy * dy < circleRadius * circleRadius;
        }

        /// <summary>AABB vs AABB 碰撞</summary>
        public static bool AABBvsAABB(
            Vector2 cA, float hwA, float hhA,
            Vector2 cB, float hwB, float hhB)
        {
            return Mathf.Abs(cA.x - cB.x) < hwA + hwB
                && Mathf.Abs(cA.y - cB.y) < hhA + hhB;
        }

        /// <summary>
        /// 线段 vs 圆碰撞（胶囊体 vs 圆：totalRadius = 线段半宽 + 圆半径）。
        /// 复用 CollisionSolver 原有的 PointToSegmentDistanceSq 逻辑。
        /// </summary>
        public static bool SegmentVsCircle(
            Vector2 segA, Vector2 segB, float segHalfWidth,
            Vector2 circleCenter, float circleRadius)
        {
            float totalRadius = segHalfWidth + circleRadius;
            float distSq = PointToSegmentDistanceSq(circleCenter, segA, segB);
            return distSq < totalRadius * totalRadius;
        }

        /// <summary>
        /// 线段 vs 轴对齐矩形碰撞。
        /// 膨胀矩形 halfWidth 方向各加 segHalfWidth，然后做线段 vs 膨胀 AABB。
        /// </summary>
        public static bool SegmentVsAABB(
            Vector2 segA, Vector2 segB, float segHalfWidth,
            Vector2 rectCenter, float halfW, float halfH)
        {
            // 膨胀矩形（Minkowski sum：线段半宽膨胀到 AABB 各边）
            float ew = halfW + segHalfWidth;
            float eh = halfH + segHalfWidth;

            // 1) 任一端点在膨胀矩形内 → 碰撞
            if (PointInAABB(segA, rectCenter, ew, eh)) return true;
            if (PointInAABB(segB, rectCenter, ew, eh)) return true;

            // 2) 线段与膨胀矩形的 4 条边做线段-线段相交测试
            Vector2 min = rectCenter - new Vector2(ew, eh);
            Vector2 max = rectCenter + new Vector2(ew, eh);

            // 四条边
            if (SegmentsIntersect(segA, segB, new Vector2(min.x, min.y), new Vector2(max.x, min.y))) return true; // bottom
            if (SegmentsIntersect(segA, segB, new Vector2(max.x, min.y), new Vector2(max.x, max.y))) return true; // right
            if (SegmentsIntersect(segA, segB, new Vector2(max.x, max.y), new Vector2(min.x, max.y))) return true; // top
            if (SegmentsIntersect(segA, segB, new Vector2(min.x, max.y), new Vector2(min.x, min.y))) return true; // left

            return false;
        }

        /// <summary>
        /// 扇形 vs 圆碰撞（距离 + 角度判定）。
        /// 与 CollisionSolver.SolveSprays 原逻辑一致。
        /// </summary>
        public static bool SectorVsCircle(
            Vector2 origin, float range, float direction, float coneAngle,
            Vector2 circleCenter, float circleRadius)
        {
            Vector2 diff = circleCenter - origin;
            float dist = diff.magnitude;
            if (dist > range + circleRadius) return false;

            float angle = Mathf.Atan2(diff.y, diff.x);
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
                angle * Mathf.Rad2Deg,
                direction * Mathf.Rad2Deg));
            return angleDiff <= coneAngle * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 扇形 vs 轴对齐矩形碰撞。
        /// 近似算法：检查"中心 + 4 角 + 最近点"共 6 采样点是否在扇形内。
        /// ⚠️ 局限性：若矩形的一条边穿过扇形而所有采样点均在扇形外（如细长矩形
        /// 斜穿扇形边缘），会漏判。当前 ShooterGame 场景中矩形目标仅基地（不会被
        /// 喷雾命中），故影响极低。未来若需精确判定，应升级为扇形两边线段与 AABB
        /// 四边的线段相交测试。
        /// </summary>
        public static bool SectorVsAABB(
            Vector2 origin, float range, float direction, float coneAngle,
            Vector2 rectCenter, float halfW, float halfH)
        {
            // 1) 距离检查：origin 到 AABB 最近点的距离
            float closestX = Mathf.Clamp(origin.x, rectCenter.x - halfW, rectCenter.x + halfW);
            float closestY = Mathf.Clamp(origin.y, rectCenter.y - halfH, rectCenter.y + halfH);
            float dx = origin.x - closestX;
            float dy = origin.y - closestY;
            if (dx * dx + dy * dy > range * range) return false;

            // 2) 角度检查：检查矩形的 4 个角是否有任一在扇形内
            //    或者矩形中心在扇形内
            float halfAngleDeg = coneAngle * Mathf.Rad2Deg;
            float dirDeg = direction * Mathf.Rad2Deg;

            // 检查中心点
            if (IsPointInSector(rectCenter, origin, range, dirDeg, halfAngleDeg)) return true;

            // 检查 4 个角
            Vector2 c0 = new Vector2(rectCenter.x - halfW, rectCenter.y - halfH);
            Vector2 c1 = new Vector2(rectCenter.x + halfW, rectCenter.y - halfH);
            Vector2 c2 = new Vector2(rectCenter.x + halfW, rectCenter.y + halfH);
            Vector2 c3 = new Vector2(rectCenter.x - halfW, rectCenter.y + halfH);

            if (IsPointInSector(c0, origin, range, dirDeg, halfAngleDeg)) return true;
            if (IsPointInSector(c1, origin, range, dirDeg, halfAngleDeg)) return true;
            if (IsPointInSector(c2, origin, range, dirDeg, halfAngleDeg)) return true;
            if (IsPointInSector(c3, origin, range, dirDeg, halfAngleDeg)) return true;

            // 3) 补充：AABB 最近点在扇形内（覆盖角都在外但边与扇形相交的情况）
            Vector2 closest = new Vector2(closestX, closestY);
            if (IsPointInSector(closest, origin, range, dirDeg, halfAngleDeg)) return true;

            return false;
        }

        // ──── 工具函数 ────

        /// <summary>点到线段最短距离平方</summary>
        public static float PointToSegmentDistanceSq(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float abLenSq = Vector2.Dot(ab, ab);
            if (abLenSq <= 1e-6f)
                return (point - a).sqrMagnitude;

            Vector2 ap = point - a;
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
            Vector2 closest = a + ab * t;
            return (point - closest).sqrMagnitude;
        }

        /// <summary>点是否在 AABB 内</summary>
        private static bool PointInAABB(Vector2 p, Vector2 center, float hw, float hh)
        {
            return Mathf.Abs(p.x - center.x) <= hw && Mathf.Abs(p.y - center.y) <= hh;
        }

        /// <summary>点是否在扇形内</summary>
        private static bool IsPointInSector(
            Vector2 point, Vector2 origin, float range,
            float dirDeg, float halfAngleDeg)
        {
            Vector2 diff = point - origin;
            if (diff.sqrMagnitude > range * range) return false;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle, dirDeg));
            return angleDiff <= halfAngleDeg;
        }

        /// <summary>两条线段是否相交（叉积法）</summary>
        private static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            float d1 = Cross(b2 - b1, a1 - b1);
            float d2 = Cross(b2 - b1, a2 - b1);
            float d3 = Cross(a2 - a1, b1 - a1);
            float d4 = Cross(a2 - a1, b2 - a1);

            if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
                return true;

            // 端点在另一条线段上
            if (Mathf.Abs(d1) < 1e-6f && OnSegment(b1, b2, a1)) return true;
            if (Mathf.Abs(d2) < 1e-6f && OnSegment(b1, b2, a2)) return true;
            if (Mathf.Abs(d3) < 1e-6f && OnSegment(a1, a2, b1)) return true;
            if (Mathf.Abs(d4) < 1e-6f && OnSegment(a1, a2, b2)) return true;

            return false;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool OnSegment(Vector2 segA, Vector2 segB, Vector2 p)
        {
            return p.x >= Mathf.Min(segA.x, segB.x) - 1e-6f
                && p.x <= Mathf.Max(segA.x, segB.x) + 1e-6f
                && p.y >= Mathf.Min(segA.y, segB.y) - 1e-6f
                && p.y <= Mathf.Max(segA.y, segB.y) + 1e-6f;
        }
    }
}
