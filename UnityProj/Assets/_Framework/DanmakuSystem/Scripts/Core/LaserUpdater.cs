using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 激光更新——纯 static 工具类，每帧由 DanmakuSystem.Update 调用。
    /// 负责：挂载源同步、Phase 推进（Charging → Firing → Fading）、宽度曲线驱动、
    /// TickTimer 推进、折射段解算。
    /// </summary>
    public static class LaserUpdater
    {
        public static void UpdateAll(
            LaserPool pool,
            DanmakuTypeRegistry registry,
            ObstaclePool obstaclePool,
            AttachSourceRegistry attachRegistry,
            in Rect worldBounds,
            float dt)
        {
            for (int i = 0; i < LaserPool.MAX_LASERS; i++)
            {
                ref var laser = ref pool.Data[i];
                if (laser.Phase == 0) continue;  // 未激活

                // ── 挂载源同步：每帧回写 Origin + Angle ──
                if (laser.AttachId != 0)
                {
                    if (attachRegistry.Transforms[laser.AttachId] == null)
                    {
                        // 挂载源已销毁 → 立即回收激光
                        FreeLaser(pool, attachRegistry, i);
                        continue;
                    }
                    laser.Origin = attachRegistry.GetWorldPosition(laser.AttachId, laser.Origin);
                    laser.Angle = attachRegistry.GetWorldAngle(laser.AttachId, laser.Angle);
                }

                laser.Elapsed += dt;
                var type = registry.LaserTypes[laser.LaserTypeIndex];

                // Phase 推进
                if (laser.Elapsed < type.ChargeDuration)
                {
                    // Charging: 细线闪烁，不造成伤害
                    laser.Phase = 1;
                    laser.Width = type.MaxWidth * 0.05f;

                    // Charging 阶段也计算直线段（用于细线预览渲染）
                    BuildStraightSegment(ref laser);
                }
                else if (laser.Elapsed < type.ChargeDuration + type.FiringDuration)
                {
                    // Firing: 宽度曲线驱动 + 伤害 tick + 折射段解算
                    laser.Phase = 2;
                    float normalizedTime = laser.Elapsed / laser.Lifetime;
                    laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;

                    // 注意：TickTimer 推进在 CollisionSolver.SolveLasers 中完成，
                    // 避免与 tick 判断分处两地导致时序 bug。

                    // 折射段解算（Firing 阶段才与障碍物/边缘交互）
                    LaserSegmentSolver.Solve(ref laser, type, obstaclePool, in worldBounds);

                    // Origin 越界检查
                    if (type.OnHitScreenEdge == LaserScreenEdgeResponse.RecycleOnOriginOut)
                    {
                        float margin = type.ScreenEdgeRecycleMargin;
                        if (laser.Origin.x < worldBounds.xMin - margin ||
                            laser.Origin.x > worldBounds.xMax + margin ||
                            laser.Origin.y < worldBounds.yMin - margin ||
                            laser.Origin.y > worldBounds.yMax + margin)
                        {
                            FreeLaser(pool, attachRegistry, i);
                            continue;
                        }
                    }
                }
                else if (laser.Elapsed < laser.Lifetime)
                {
                    // Fading: 宽度递减，不造成伤害
                    laser.Phase = 3;
                    float normalizedTime = laser.Elapsed / laser.Lifetime;
                    laser.Width = type.WidthOverLifetime.Evaluate(normalizedTime) * type.MaxWidth;

                    // Fading 阶段保持最后一帧的 Segments 结果（不重算）
                }
                else
                {
                    // 生命周期结束，回收
                    FreeLaser(pool, attachRegistry, i);
                }
            }
        }

        /// <summary>
        /// 回收激光——先释放挂载源引用，再归还池槽位。
        /// </summary>
        public static void FreeLaser(LaserPool pool, AttachSourceRegistry attachRegistry, int index)
        {
            byte attachId = pool.Data[index].AttachId;
            if (attachId != 0)
                attachRegistry.Release(attachId);
            pool.Free(index);
        }

        /// <summary>
        /// 构建直线段（无折射，用于 Charging/简单场景）。
        /// </summary>
        private static void BuildStraightSegment(ref LaserData laser)
        {
            Vector2 dir = new Vector2(Mathf.Cos(laser.Angle), Mathf.Sin(laser.Angle));
            ref var seg = ref laser.Segments[0];
            seg.Start = laser.Origin;
            seg.End = laser.Origin + dir * laser.Length;
            seg.Direction = dir;
            seg.Length = laser.Length;
            laser.SegmentCount = 1;
            laser.VisualLength = laser.Length;
        }
    }
}
