using UnityEngine;

namespace MiniGameTemplate.Danmaku
{
    /// <summary>
    /// 弹丸运动更新器——纯 static 工具类，每帧由 DanmakuSystem.Update 调用。
    /// 负责：位置更新、速度曲线、延迟变速、追踪、残影记录、Phase 推进、子弹幕触发、回收。
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
        public static void UpdateAll(
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
                    var bulletType = registry.BulletTypes[core.TypeIndex];
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

                // 延迟变速（Modifier 冷数据）
                float speedMultiplier = 1f;
                if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
                {
                    ref var mod = ref modifiers[i];
                    speedMultiplier = CalculateModifierSpeed(core.Elapsed, ref mod);
                }
                // 速度曲线（与延迟变速互斥）
                else if ((core.Flags & BulletCore.FLAG_SPEED_CURVE) != 0)
                {
                    var bulletType = registry.BulletTypes[core.TypeIndex];
                    float normalizedTime = core.Elapsed / core.Lifetime;
                    speedMultiplier = bulletType.SpeedOverLifetime.Evaluate(normalizedTime);
                }

                // 追踪（实时转向）
                if ((core.Flags & BulletCore.FLAG_HOMING) != 0)
                {
                    // 检查追踪延迟
                    bool shouldTrack = true;
                    if ((core.Flags & BulletCore.FLAG_HAS_MODIFIER) != 0)
                    {
                        ref var mod = ref modifiers[i];
                        if (core.Elapsed < mod.HomingStartTime)
                            shouldTrack = false;
                    }

                    if (shouldTrack)
                    {
                        Vector2 toPlayer = playerPos - core.Position;
                        if (toPlayer.sqrMagnitude > 0.001f)
                        {
                            float targetAngle = Mathf.Atan2(toPlayer.y, toPlayer.x);
                            float currentAngle = Mathf.Atan2(core.Velocity.y, core.Velocity.x);
                            // 从 Modifier 读取策划配置的转向速度（度/秒）
                            float homingDegPerSec = modifiers[i].HomingStrength;
                            float maxTurn = homingDegPerSec * Mathf.Deg2Rad * dt;
                            float angleDiff = Mathf.DeltaAngle(
                                currentAngle * Mathf.Rad2Deg,
                                targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                            float turn = Mathf.Clamp(angleDiff, -maxTurn, maxTurn);
                            float newAngle = currentAngle + turn;
                            // 使用 sqrMagnitude 避免冗余 sqrt
                            float speedSq = core.Velocity.sqrMagnitude;
                            float speed = speedSq > 0.001f ? Mathf.Sqrt(speedSq) : 0f;
                            core.Velocity = new Vector2(
                                Mathf.Cos(newAngle), Mathf.Sin(newAngle)) * speed;
                        }
                    }
                }

                // 残影记录（渲染前更新历史位置）
                ref var trail = ref trails[i];
                if (trail.TrailLength > 0)
                {
                    trail.PrevPos3 = trail.PrevPos2;
                    trail.PrevPos2 = trail.PrevPos1;
                    trail.PrevPos1 = core.Position;
                }

                // 位置更新
                core.Position += core.Velocity * speedMultiplier * dt;

                // 重量拖尾记录
                if ((core.Flags & BulletCore.FLAG_HEAVY_TRAIL) != 0 && trailPool != null)
                {
                    ref var trailRef = ref trails[i];
                    if (trailRef.HeavyTrailHandle >= 0)
                        trailPool.AddPoint(trailRef.HeavyTrailHandle, core.Position);
                }
            }
        }

        /// <summary>计算延迟变速倍率</summary>
        private static float CalculateModifierSpeed(float elapsed, ref BulletModifier mod)
        {
            if (elapsed < mod.DelayEndTime)
            {
                // 延迟期间——使用配置的速度倍率
                return mod.DelaySpeedScale;
            }
            else if (elapsed < mod.AccelEndTime)
            {
                // 加速过渡期——线性插值
                float t = (elapsed - mod.DelayEndTime) / (mod.AccelEndTime - mod.DelayEndTime);
                return Mathf.Lerp(mod.DelaySpeedScale, 1f, t);
            }
            else
            {
                return 1f;
            }
        }

        /// <summary>处理弹丸死亡——子弹幕触发 + 爆炸</summary>
        private static void HandleBulletDeath(
            ref BulletCore core, int index,
            BulletWorld world,
            DanmakuTypeRegistry registry,
            DanmakuSystem system)
        {
            var bulletType = registry.BulletTypes[core.TypeIndex];

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
                    core.Elapsed = 0;  // 复用 Elapsed 做爆炸计时
                    return;  // 不回收，等爆炸帧播完

                case ExplosionMode.PooledPrefab:
                    if (bulletType.HeavyExplosionPrefab != null)
                    {
                        // 从对象池取特效预制件（调用框架 PoolManager）
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
