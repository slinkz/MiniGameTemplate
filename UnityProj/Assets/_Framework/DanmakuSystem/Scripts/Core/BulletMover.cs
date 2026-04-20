using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸运动更新器——纯 static 工具类，每帧由 DanmakuSystem.Update 调用。
    /// 负责：通过 MotionRegistry 策略委托驱动位置更新、残影记录、Phase 推进、子弹幕触发、回收。
    /// 运动分支逻辑已委托给 MotionRegistry 策略，BulletMover 只保留生命周期管理。
    /// </summary>
    public static class BulletMover
    {
        /// <summary>
        /// 更新所有活跃弹丸的运动和生命周期。
        /// </summary>
        /// <param name="world">弹丸世界</param>
        /// <param name="registry">类型注册表（子弹幕触发用）</param>
        /// <param name="playerPos">玩家位置（追踪弹用）</param>
        /// <param name="dt">弹幕 deltaTime（已乘 TimeScale）</param>
        /// <param name="system">弹幕系统引用（子弹幕发射用）</param>
        /// <param name="trailPool">重量拖尾池（FLAG_HEAVY_TRAIL 弹丸用）</param>
        internal static void UpdateAll(
            BulletWorld world,
            DanmakuTypeRegistry registry,
            Vector2 playerPos,
            float dt,
            DanmakuSystem system,
            TrailPool trailPool)
        {
            var cores = world.Cores;
            var trails = world.Trails;
            var modifiers = world.Modifiers;
            int capacity = world.Capacity;

            for (int i = 0; i < capacity; i++)
            {
                ref var core = ref cores[i];

                // 跳过非活跃槽位
                if ((core.Flags & BulletCore.FLAG_ACTIVE) == 0) continue;

                // ── Exploding / Dead 阶段处理 ──
                if (core.Phase == (byte)BulletPhase.Exploding)
                {
                    // 爆炸帧倒计时（复用 Elapsed 字段）
                    core.Elapsed += dt;
                    var bulletType = registry.GetBulletType(core.TypeIndex);
                    float explosionDuration = bulletType.ExplosionFrameCount / 60f;
                    if (core.Elapsed >= explosionDuration)
                    {
                        core.Phase = (byte)BulletPhase.Dead;
                    }
                    continue;  // Exploding 不参与运动
                }

                if (core.Phase == (byte)BulletPhase.Dead)
                {
                    world.Free(i);
                    continue;
                }

                // ── Active 阶段：运动更新 ──

                core.Elapsed += dt;

                // 生命周期检查
                if (core.Elapsed >= core.Lifetime || core.HitPoints == 0)
                {
                    // 释放重量拖尾
                    if ((core.Flags & BulletCore.FLAG_HEAVY_TRAIL) != 0 && trailPool != null)
                    {
                        ref var trailData = ref trails[i];
                        if (trailData.HeavyTrailHandle >= 0)
                        {
                            trailPool.Free(trailData.HeavyTrailHandle);
                            trailData.HeavyTrailHandle = -1;
                        }
                    }
                    HandleBulletDeath(ref core, i, world, registry, system);
                    continue;
                }

                // 残影记录（间隔 N 帧采样，确保残影有足够间距）
                ref var trail = ref trails[i];
                if (trail.TrailLength > 0)
                {
                    var ghostInterval = registry.GetBulletType(core.TypeIndex).GhostInterval;
                    trail.GhostFrameCounter++;
                    if (trail.GhostFrameCounter >= ghostInterval)
                    {
                        trail.GhostFrameCounter = 0;
                        trail.PrevPos3 = trail.PrevPos2;
                        trail.PrevPos2 = trail.PrevPos1;
                        trail.PrevPos1 = core.Position;
                        if (trail.GhostFilledCount < 3)
                            trail.GhostFilledCount++;
                    }
                }

                // 运动策略委托——通过 MotionRegistry 查表执行
                var type = registry.GetBulletType(core.TypeIndex);
                var strategy = MotionRegistry.Get(type.MotionType);
                strategy(ref core, ref modifiers[i], type, playerPos, dt);

                // ── 视觉动画采样（DEC-005=C：Mover 写入，Renderer 读取） ──
                if (type.UseVisualAnimation)
                {
                    float t = core.Lifetime > 0f ? Mathf.Clamp01(core.Elapsed / core.Lifetime) : 0f;
                    core.AnimScale = type.ScaleOverLifetime.Evaluate(t);
                    core.AnimAlpha = type.AlphaOverLifetime.Evaluate(t);
                    if (type.ColorOverLifetime != null)
                    {
                        Color gc = type.ColorOverLifetime.Evaluate(t);
                        core.AnimColor = new Color32(
                            (byte)(gc.r * 255),
                            (byte)(gc.g * 255),
                            (byte)(gc.b * 255),
                            (byte)(gc.a * 255));
                    }
                    else
                    {
                        core.AnimColor = new Color32(255, 255, 255, 255);
                    }
                }
                else
                {
                    // 无动画——写入默认值（确保 Renderer 读到有效值）
                    core.AnimScale = 1f;
                    core.AnimAlpha = 1f;
                    core.AnimColor = new Color32(255, 255, 255, 255);
                }

                // 重量拖尾记录
                if ((core.Flags & BulletCore.FLAG_HEAVY_TRAIL) != 0 && trailPool != null)
                {
                    ref var trailRef = ref trails[i];
                    if (trailRef.HeavyTrailHandle >= 0)
                        trailPool.AddPoint(trailRef.HeavyTrailHandle, core.Position);
                }
            }
        }

        /// <summary>处理弹丸死亡——子弹幕触发 + 爆炸</summary>
        private static void HandleBulletDeath(
            ref BulletCore core, int index,
            BulletWorld world,
            DanmakuTypeRegistry registry,
            DanmakuSystem system)
        {
            var bulletType = registry.GetBulletType(core.TypeIndex);

            // 子弹幕触发
            if ((core.Flags & BulletCore.FLAG_HAS_CHILD) != 0 && bulletType.ChildPattern != null)
            {
                float angle = Mathf.Atan2(core.Velocity.y, core.Velocity.x) * Mathf.Rad2Deg;
                system.FireBullets(bulletType.ChildPattern, core.Position, angle);
            }

            // 爆炸处理
            switch (bulletType.Explosion)
            {
                case ExplosionMode.MeshFrame:
                    core.Phase = (byte)BulletPhase.Exploding;
                    core.Elapsed = 0;
                    return;

                case ExplosionMode.PooledPrefab:
                    if (bulletType.HeavyExplosionPrefab != null)
                    {
                        var pool = MiniGameTemplate.Pool.PoolManager.Instance;
                        if (pool != null)
                        {
                            var go = pool.Get(bulletType.HeavyExplosionPrefab);
                            if (go != null)
                                go.transform.position = new Vector3(core.Position.x, core.Position.y, 0);
                        }
                    }
                    break;

                case ExplosionMode.None:
                default:
                    break;
            }

            // 直接回收
            world.Free(index);
        }
    }
}
